using System;
using System.Collections.Generic;
using BepInEx.Logging;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace GiveEliteItem
{
    /// <summary>
    /// Original 2024 technique, simplified after the October 2026 gameplay test:
    /// put the elite in physical slot 1 and NEVER rotate it into active slot 0.
    /// RoR2 keeps the elite passive active from the real secondary slot.
    /// No custom HUD, extra buff injection, inventory pickup hook or RPC.
    /// </summary>
    internal sealed class EliteRunDistributor
    {
        private const uint EliteEquipmentSlot = 1u;
        private const float ScanInterval = 0.5f;
        private const float StatusInterval = 10f;

        private sealed class PlayerAssignment
        {
            internal EquipmentIndex Elite;
            internal float GrantedAt;
            internal bool NeedsVerification = true;
            internal bool LostEliteReported;
        }

        private readonly EliteSettings settings;
        private readonly ManualLogSource logger;
        private readonly Dictionary<CharacterMaster, PlayerAssignment> assignments =
            new Dictionary<CharacterMaster, PlayerAssignment>();
        private readonly HashSet<CharacterMaster> reportedOccupied =
            new HashSet<CharacterMaster>();
        private readonly HashSet<CharacterMaster> reportedFailures =
            new HashSet<CharacterMaster>();
        private readonly HashSet<int> usedAspects = new HashSet<int>();
        private readonly List<int> eligibleAspects = new List<int>();

        private Run currentRun;
        private System.Random random;
        private float nextScan;
        private float nextStatus;
        private bool missingCatalogReported;
        private bool catalogListed;

        internal EliteRunDistributor(EliteSettings settings, ManualLogSource logger)
        {
            this.settings = settings;
            this.logger = logger;
        }

        private void Trace(string type, string message)
        {
            if (settings.TraceGameplay.Value)
                logger.LogInfo("[PWEI][" + type + "] " + message);
        }

        internal void Tick()
        {
            if (!NetworkServer.active)
                return;

            Run run = Run.instance;
            if (run == null)
            {
                if (currentRun != null)
                    Reset();
                return;
            }

            if (currentRun != run)
            {
                Reset();
                currentRun = run;
                random = new System.Random();
                Trace("RUN", "New run detected; physical elite slot=" + EliteEquipmentSlot);
            }

            if (!settings.Enabled.Value || Time.unscaledTime < nextScan)
                return;

            nextScan = Time.unscaledTime + ScanInterval;

            if (eligibleAspects.Count == 0)
            {
                EliteAspectCatalog.CollectEligible(
                    run,
                    settings.IncludeAllRegisteredEliteAspects.Value,
                    settings.AllowedEliteEquipment.Value,
                    eligibleAspects);

                if (eligibleAspects.Count == 0)
                {
                    if (!missingCatalogReported)
                    {
                        missingCatalogReported = true;
                        logger.LogWarning("[PWEI][CATALOG] No eligible elite equipment found.");
                    }
                    return;
                }

                if (!catalogListed)
                {
                    catalogListed = true;
                    Trace("CATALOG", "Eligible elite equipment count=" + eligibleAspects.Count);
                }
            }

            int controllers = 0;
            foreach (PlayerCharacterMasterController controller in
                PlayerCharacterMasterController.instances)
            {
                controllers++;

                if (controller == null || controller.master == null ||
                    controller.networkUser == null || controller.master.inventory == null)
                    continue;

                CharacterMaster master = controller.master;
                Inventory inventory = master.inventory;

                if (assignments.TryGetValue(master, out PlayerAssignment alreadyAssigned))
                {
                    VerifyAssignment(master, alreadyAssigned);
                    continue;
                }

                // Avoid assigning before the character has spawned; RoR2 can
                // initialize or replace equipment during body creation.
                CharacterBody body = master.GetBody();
                bool ready = body != null && body.inventory != null;
                if (!ready)
                    continue;

                EquipmentLocation activeLocation = inventory.activeEquipmentLocation;
                EquipmentState second = inventory.GetEquipment(EliteEquipmentSlot);
                bool slotIsFree = second.equipmentIndex == EquipmentIndex.None;

                if (!EliteSlotPolicy.CanAssign(
                    ready, slotIsFree, false, activeLocation.slot == EliteEquipmentSlot))
                {
                    if (reportedOccupied.Add(master))
                    {
                        logger.LogWarning("[PWEI][SLOT] Skipping " +
                            controller.networkUser.userName +
                            ": physical slot 1 is occupied or active. Current=" +
                            second.equipmentIndex + ", activeSlot=" + activeLocation.slot +
                            ". Existing equipment will not be overwritten.");
                    }
                    continue;
                }

                int selectedNumber = AspectSelection.Choose(
                    eligibleAspects, usedAspects, random,
                    settings.UniqueBeforeRepeats.Value);
                EquipmentIndex elite = (EquipmentIndex)selectedNumber;
                EquipmentIndex previousActive =
                    inventory.GetEquipment(activeLocation).equipmentIndex;

                Trace("GRANT", "Attempt player=" + controller.networkUser.userName +
                    " elite=" + elite + " targetSlot=1 activeBefore=" + previousActive +
                    " reportedSlotCount=" + inventory.equipmentSlotCount);

                try
                {
                    // The exact API from the author's original working mod.
                    inventory.SetEquipmentIndexForSlot(elite, EliteEquipmentSlot);
                }
                catch (Exception ex)
                {
                    if (reportedFailures.Add(master))
                        logger.LogWarning("[PWEI][GRANT] Cannot write physical slot 1: " + ex);
                    continue;
                }

                EquipmentIndex stored = inventory.GetEquipment(EliteEquipmentSlot).equipmentIndex;
                EquipmentIndex activeAfter =
                    inventory.GetEquipment(activeLocation).equipmentIndex;

                if (!EliteSlotPolicy.IsEliteStillInPlace((int)elite, (int)stored) ||
                    activeAfter != previousActive)
                {
                    if (reportedFailures.Add(master))
                        logger.LogWarning("[PWEI][GRANT] Verification failed: requested=" +
                            elite + " slot1=" + stored + " activeBefore=" +
                            previousActive + " activeAfter=" + activeAfter);
                    continue;
                }

                usedAspects.Add(selectedNumber);
                assignments.Add(master, new PlayerAssignment
                {
                    Elite = elite,
                    GrantedAt = Time.unscaledTime
                });
                EquipmentDef definition = EquipmentCatalog.GetEquipmentDef(elite);
                logger.LogInfo("[PWEI][GRANT] SUCCESS player=" +
                    controller.networkUser.userName + " elite=" +
                    (definition != null ? definition.name : elite.ToString()) +
                    " slot1=" + stored + " Q=" + activeAfter + " noSwap=true");
            }

            // A throttled heartbeat also detects unexpected changes after pickups.
            if (Time.unscaledTime >= nextStatus)
            {
                nextStatus = Time.unscaledTime + StatusInterval;
                Trace("SCAN", "controllers=" + controllers +
                    " assigned=" + assignments.Count +
                    " eligible=" + eligibleAspects.Count +
                    " runTime=" + run.time);
                foreach (var entry in assignments)
                    TraceAssignmentStatus(entry.Key, entry.Value);
            }
        }

        private void VerifyAssignment(CharacterMaster master, PlayerAssignment assignment)
        {
            if (!assignment.NeedsVerification ||
                Time.unscaledTime < assignment.GrantedAt + 1.5f)
                return;

            assignment.NeedsVerification = false;
            TraceAssignmentStatus(master, assignment);
        }

        private void TraceAssignmentStatus(CharacterMaster master, PlayerAssignment assignment)
        {
            if (master == null || master.inventory == null)
                return;

            Inventory inventory = master.inventory;
            EquipmentIndex stored = inventory.GetEquipment(EliteEquipmentSlot).equipmentIndex;
            EquipmentIndex active =
                inventory.GetEquipment(inventory.activeEquipmentLocation).equipmentIndex;
            CharacterBody body = master.GetBody();
            EquipmentDef elite = EquipmentCatalog.GetEquipmentDef(assignment.Elite);
            BuffDef buff = elite != null ? elite.passiveBuffDef : null;
            bool hasBuff = body != null && buff != null && body.HasBuff(buff);

            Trace("VERIFY", "elite=" + assignment.Elite +
                " slot1=" + stored + " Q=" + active +
                " vanillaEliteBuffPresent=" + hasBuff);

            if (!assignment.LostEliteReported && stored != assignment.Elite)
            {
                assignment.LostEliteReported = true;
                logger.LogWarning("[PWEI][VERIFY] Elite left physical slot 1 unexpectedly. " +
                    "Mod will not overwrite replacement equipment or create duplicates.");
            }
        }

        internal void Reset()
        {
            currentRun = null;
            random = null;
            nextScan = 0f;
            nextStatus = 0f;
            missingCatalogReported = false;
            catalogListed = false;
            assignments.Clear();
            reportedOccupied.Clear();
            reportedFailures.Clear();
            usedAspects.Clear();
            eligibleAspects.Clear();
        }
    }
}
