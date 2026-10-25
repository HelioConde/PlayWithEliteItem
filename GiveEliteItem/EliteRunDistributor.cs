using System;
using System.Collections.Generic;
using BepInEx.Logging;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace GiveEliteItem
{
    /// <summary>
    /// Per-run server-authoritative equipment distribution.
    /// No hooks or custom networking: the game replicates normal inventory changes.
    /// </summary>
    internal sealed class EliteRunDistributor
    {
        private const float ScanIntervalSeconds = 0.5f;
        private const uint EquipmentSlot = 0u;

        private readonly EliteSettings settings;
        private readonly ManualLogSource logger;
        private readonly HashSet<CharacterMaster> handledPlayers = new HashSet<CharacterMaster>();
        private readonly HashSet<int> assignedAspects = new HashSet<int>();
        private readonly List<int> availableAspects = new List<int>();

        private Run activeRun;
        private Random random;
        private float nextScanTime;
        private bool emptyPoolLogged;

        internal EliteRunDistributor(EliteSettings settings, ManualLogSource logger)
        {
            this.settings = settings;
            this.logger = logger;
        }

        internal void Tick()
        {
            if (!NetworkServer.active)
                return;

            Run run = Run.instance;
            if (run == null)
            {
                if (activeRun != null)
                    Reset();
                return;
            }

            if (activeRun != run)
                StartRun(run);

            if (!settings.Enabled.Value || Time.unscaledTime < nextScanTime)
                return;

            nextScanTime = Time.unscaledTime + ScanIntervalSeconds;

            if (availableAspects.Count == 0)
            {
                EliteAspectCatalog.CollectEligible(
                    run,
                    settings.IncludeAllRegisteredEliteAspects.Value,
                    settings.AllowedEliteEquipment.Value,
                    availableAspects);

                if (availableAspects.Count == 0)
                {
                    if (!emptyPoolLogged)
                    {
                        logger.LogWarning(
                            "No eligible elite aspects. Check enabled expansions and config.");
                        emptyPoolLogged = true;
                    }
                    return;
                }

                emptyPoolLogged = false;
                logger.LogInfo("Eligible elite aspects: " + availableAspects.Count + ".");
            }

            GiveToNewPlayers();
        }

        private void StartRun(Run run)
        {
            Reset();
            activeRun = run;
            random = new Random();
            logger.LogInfo("Elite distribution ready for the new run.");
        }

        private void GiveToNewPlayers()
        {
            foreach (PlayerCharacterMasterController player in
                PlayerCharacterMasterController.instances)
            {
                if (player == null || player.networkUser == null || player.master == null)
                    continue;

                CharacterMaster master = player.master;
                if (master.inventory == null || handledPlayers.Contains(master))
                    continue;

                TryGiveEquipment(master, player.networkUser.userName);
            }
        }

        private void TryGiveEquipment(CharacterMaster master, string playerName)
        {
            Inventory inventory = master.inventory;
            if (inventory == null)
                return;

            EquipmentIndex equipped = inventory.GetEquipment(EquipmentSlot).equipmentIndex;
            if (equipped != EquipmentIndex.None && !settings.ReplaceExistingEquipment.Value)
            {
                handledPlayers.Add(master);
                if (settings.VerboseLogging.Value)
                    logger.LogInfo("Skipped " + playerName + ": active equipment occupied.");
                return;
            }

            int chosen = AspectSelection.Choose(
                availableAspects, assignedAspects, random,
                settings.UniqueBeforeRepeats.Value);

            EquipmentIndex index = (EquipmentIndex)chosen;
            inventory.SetEquipmentIndexForSlot(index, EquipmentSlot);

            if (inventory.GetEquipment(EquipmentSlot).equipmentIndex != index)
            {
                logger.LogWarning("Could not grant an elite aspect to " + playerName +
                    "; retrying on the next scan.");
                return;
            }

            assignedAspects.Add(chosen);
            handledPlayers.Add(master);

            if (settings.VerboseLogging.Value)
            {
                EquipmentDef equipment = EquipmentCatalog.GetEquipmentDef(index);
                logger.LogInfo("Granted " + equipment.name + " to " + playerName + ".");
            }
        }

        private void Reset()
        {
            activeRun = null;
            random = null;
            nextScanTime = 0f;
            emptyPoolLogged = false;
            handledPlayers.Clear();
            assignedAspects.Clear();
            availableAspects.Clear();
        }
    }
}
