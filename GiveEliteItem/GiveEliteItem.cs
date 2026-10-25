using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace GiveEliteItem
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class GiveEliteItem : BaseUnityPlugin
    {
        public const string PluginGuid = "AlchemyFlame.PlayWithEliteItem";
        public const string PluginName = "PlayWithEliteItem";
        public const string PluginVersion = "1.1.0";

        private const float ScanIntervalSeconds = 0.5f;
        private const string DefaultEliteEquipment =
            "EliteEarthEquipment,EliteFireEquipment,EliteHauntedEquipment," +
            "EliteIceEquipment,EliteLightningEquipment,EliteLunarEquipment,ElitePoisonEquipment";

        private readonly HashSet<CharacterMaster> handledPlayers = new HashSet<CharacterMaster>();
        private readonly HashSet<EquipmentIndex> assignedAspects = new HashSet<EquipmentIndex>();
        private readonly List<EquipmentIndex> availableAspects = new List<EquipmentIndex>();

        private ConfigEntry<bool> enabledConfig;
        private ConfigEntry<bool> uniqueBeforeRepeatsConfig;
        private ConfigEntry<bool> includeAllEliteAspectsConfig;
        private ConfigEntry<bool> replaceExistingEquipmentConfig;
        private ConfigEntry<bool> verboseLoggingConfig;
        private ConfigEntry<string> allowedEliteEquipmentConfig;

        private Run activeRun;
        private System.Random random;
        private float nextScanTime;
        private bool emptyPoolLogged;

        private void Awake()
        {
            enabledConfig = Config.Bind(
                "General", "Enabled", true, "Grant one elite aspect at the start of each run.");
            uniqueBeforeRepeatsConfig = Config.Bind(
                "Distribution", "UniqueBeforeRepeats", true,
                "Prefer different aspects for each player; allow repeats when the pool is exhausted.");
            includeAllEliteAspectsConfig = Config.Bind(
                "Distribution", "IncludeAllRegisteredEliteAspects", true,
                "Discover equipment with an elite passive buff, including compatible DLC and modded aspects.");
            allowedEliteEquipmentConfig = Config.Bind(
                "Distribution", "AllowedEliteEquipment", DefaultEliteEquipment,
                "Comma-separated internal equipment names. Used when IncludeAllRegisteredEliteAspects is false.");
            replaceExistingEquipmentConfig = Config.Bind(
                "Equipment", "ReplaceExistingEquipment", false,
                "If false, skip players whose active equipment slot already contains something.");
            verboseLoggingConfig = Config.Bind(
                "Diagnostics", "VerboseLogging", false, "Log each granted or skipped equipment.");

            Logger.LogInfo("PlayWithEliteItem " + PluginVersion + " loaded.");
        }

        // The server owns inventory changes. Polling also catches players joining late or
        // spawning after Run.Start, without hooking equipment activation or swapping slots.
        private void Update()
        {
            if (!NetworkServer.active || !enabledConfig.Value)
            {
                return;
            }

            Run run = Run.instance;
            if (run == null)
            {
                if (activeRun != null)
                {
                    ResetRunState();
                }
                return;
            }

            if (activeRun != run)
            {
                ResetRunState();
                activeRun = run;
                random = new System.Random();
                Logger.LogInfo("Elite distribution initialized for a new run.");
            }

            if (Time.unscaledTime < nextScanTime)
            {
                return;
            }

            nextScanTime = Time.unscaledTime + ScanIntervalSeconds;

            if (availableAspects.Count == 0)
            {
                DiscoverEliteAspects();
                if (availableAspects.Count == 0)
                {
                    if (!emptyPoolLogged)
                    {
                        Logger.LogWarning("No eligible elite equipment found. Retrying after catalog initialization.");
                        emptyPoolLogged = true;
                    }
                    return;
                }
            }

            foreach (PlayerCharacterMasterController controller in PlayerCharacterMasterController.instances)
            {
                if (controller == null || controller.networkUser == null || controller.master == null)
                {
                    continue;
                }

                CharacterMaster master = controller.master;
                if (handledPlayers.Contains(master) || master.inventory == null)
                {
                    continue;
                }

                // A newly joined player's inventory may not be ready in the same frame.
                TryGrantEliteAspect(master, controller.networkUser.userName);
            }
        }

        private void DiscoverEliteAspects()
        {
            availableAspects.Clear();

            HashSet<string> allowedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string name in allowedEliteEquipmentConfig.Value.Split(','))
            {
                if (!string.IsNullOrWhiteSpace(name))
                {
                    allowedNames.Add(name.Trim());
                }
            }

            for (int i = 0; i < EquipmentCatalog.equipmentCount; i++)
            {
                EquipmentIndex equipmentIndex = (EquipmentIndex)i;
                EquipmentDef definition = EquipmentCatalog.GetEquipmentDef(equipmentIndex);

                // The passive elite buff is what makes an aspect an elite aspect.
                if (definition == null || definition.passiveBuffDef == null ||
                    !definition.passiveBuffDef.isElite)
                {
                    continue;
                }

                if (!includeAllEliteAspectsConfig.Value && !allowedNames.Contains(definition.name))
                {
                    continue;
                }

                availableAspects.Add(equipmentIndex);
            }

            if (availableAspects.Count > 0)
            {
                Logger.LogInfo("Found " + availableAspects.Count + " eligible elite aspects.");
            }
        }

        private void TryGrantEliteAspect(CharacterMaster master, string playerName)
        {
            Inventory inventory = master.inventory;
            if (inventory == null)
            {
                return;
            }

            EquipmentIndex existing = inventory.GetEquipment(0u).equipmentIndex;
            if (existing != EquipmentIndex.None && !replaceExistingEquipmentConfig.Value)
            {
                handledPlayers.Add(master);
                if (verboseLoggingConfig.Value)
                {
                    Logger.LogInfo("Skipping " + playerName + ": the active equipment slot is occupied.");
                }
                return;
            }

            List<EquipmentIndex> candidates = availableAspects;
            if (uniqueBeforeRepeatsConfig.Value && assignedAspects.Count < availableAspects.Count)
            {
                candidates = availableAspects.FindAll(e => !assignedAspects.Contains(e));
            }

            if (candidates.Count == 0)
            {
                candidates = availableAspects; // Exhaustion must never cause an infinite loop.
            }

            EquipmentIndex selection = candidates[random.Next(candidates.Count)];
            inventory.SetEquipmentIndexForSlot(selection, 0u);

            if (inventory.GetEquipment(0u).equipmentIndex != selection)
            {
                Logger.LogWarning("Could not give elite aspect to " + playerName + "; will retry.");
                return;
            }

            assignedAspects.Add(selection);
            handledPlayers.Add(master);

            if (verboseLoggingConfig.Value)
            {
                EquipmentDef definition = EquipmentCatalog.GetEquipmentDef(selection);
                Logger.LogInfo("Granted " + definition.name + " to " + playerName + ".");
            }
        }

        private void ResetRunState()
        {
            activeRun = null;
            random = null;
            nextScanTime = 0f;
            emptyPoolLogged = false;
            availableAspects.Clear();
            assignedAspects.Clear();
            handledPlayers.Clear();
        }
    }
}
