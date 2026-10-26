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
    /// Risk of Rain 2 does NOT automatically maintain all elite passive buffs
    /// for an inactive second slot. Maintain its existing BuffDef on the
    /// server, without changing the equipment slots or adding a custom HUD/RPC.
    /// </summary>
    internal sealed class EliteRunDistributor
    {
        private const uint EliteEquipmentSlot = 1u;
        private const float ScanInterval = 0.5f;
        private const float StatusInterval = 10f;
        private const float PassiveCheckInterval = 0.1f;

        private sealed class PlayerAssignment
        {
            internal EquipmentIndex Elite;
            internal float GrantedAt;
            internal bool NeedsVerification = true;
            internal bool LostEliteReported;
            internal CharacterBody InjectedBody;
            internal BuffDef InjectedBuff;
            internal bool MissingBuffReported;
            internal int Reapplications;
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
        private float nextPassiveCheck;
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

        /// <summary>
        /// Tracks ONLY the buff this mod injected, so cleanup does not remove
        /// buffs from unrelated equipment or other mods.
        /// </summary>
        private void ReleaseOwnedPassive(PlayerAssignment assignment)
        {
            if (assignment.InjectedBody != null && assignment.InjectedBuff != null &&
                assignment.InjectedBody.HasBuff(assignment.InjectedBuff))
            {
                assignment.InjectedBody.RemoveBuff(assignment.InjectedBuff);
                Trace("PASSIVE", "RELEASE buff=" + assignment.InjectedBuff.name);
            }

            assignment.InjectedBody = null;
            assignment.InjectedBuff = null;
        }

        private void MaintainPassives()
        {
            foreach (var entry in assignments)
            {
                CharacterMaster master = entry.Key;
                PlayerAssignment state = entry.Value;
                CharacterBody body = master != null ? master.GetBody() : null;
                Inventory inventory = master != null ? master.inventory : null;

                // A respawn produces a new body. Never leave our buff on an old body.
                if (state.InjectedBody != null && state.InjectedBody != body)
                    ReleaseOwnedPassive(state);

                if (inventory == null || body == null)
                {
                    if (state.InjectedBody != null)
                        ReleaseOwnedPassive(state);
                    continue;
                }

                EquipmentIndex slotOne = inventory.GetEquipment(EliteEquipmentSlot).equipmentIndex;
                EquipmentIndex active = inventory.GetEquipment(
                    inventory.activeEquipmentLocation).equipmentIndex;

                bool correctSlot = slotOne == state.Elite;
                bool shouldMaintain = EliteSlotPolicy.ShouldMaintainPassive(
                    settings.MaintainPassiveWhileStored.Value, correctSlot,
                    active == state.Elite, body.healthComponent != null &&
                    body.healthComponent.alive);

                EquipmentDef elite = EquipmentCatalog.GetEquipmentDef(state.Elite);
                BuffDef passiveBuff = elite != null ? elite.passiveBuffDef : null;

                if (!shouldMaintain || passiveBuff == null || !passiveBuff.isElite)
                {
                    ReleaseOwnedPassive(state);
                    if (correctSlot && passiveBuff == null && !state.MissingBuffReported)
                    {
                        state.MissingBuffReported = true;
                        logger.LogWarning("[PWEI][PASSIVE] No BuffDef for elite " +
                            state.Elite + ". Some aspect abilities may be unavailable.");
                    }
                    continue;
                }

                // If the game (or another mod) already granted the buff, do not
                // stack or claim ownership of it.
                if (body.HasBuff(passiveBuff))
                    continue;

                body.AddBuff(passiveBuff);
                if (body.HasBuff(passiveBuff))
                {
                    state.InjectedBody = body;
                    state.InjectedBuff = passiveBuff;
                    state.Reapplications++;

                    if (state.Reapplications == 1 || state.Reapplications % 50 == 0)
                        Trace("PASSIVE", "APPLIED elite=" + state.Elite +
                            " buff=" + passiveBuff.name +
                            " activeQ=" + active +
                            " reapplications=" + state.Reapplications);
                }
                else if (!state.MissingBuffReported)
                {
                    state.MissingBuffReported = true;
                    logger.LogWarning("[PWEI][PASSIVE] Buff insertion failed for " +
                        state.Elite + "; inspect the game's logs.");
                }
            }
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

            if (!settings.Enabled.Value)
            {
                // If disabled mid-run, relinquish previously injected buffs.
                foreach (PlayerAssignment assignment in assignments.Values)
                    ReleaseOwnedPassive(assignment);
                return;
            }

            if (Time.unscaledTime >= nextPassiveCheck)
            {
                nextPassiveCheck = Time.unscaledTime + PassiveCheckInterval;
                MaintainPassives();
            }

            if (Time.unscaledTime < nextScan)
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

                // Immediately reconcile the passive; later checks keep it
                // active even after Q equipment changes.
                MaintainPassives();
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
                " eliteBuffPresent=" + hasBuff +
                " injectedByMod=" + (assignment.InjectedBody == body) +
                " reapplications=" + assignment.Reapplications);

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
            nextPassiveCheck = 0f;
            nextStatus = 0f;
            missingCatalogReported = false;
            catalogListed = false;
            foreach (PlayerAssignment assignment in assignments.Values)
                ReleaseOwnedPassive(assignment);
            assignments.Clear();
            reportedOccupied.Clear();
            reportedFailures.Clear();
            usedAspects.Clear();
            eligibleAspects.Clear();
        }
    }
}
