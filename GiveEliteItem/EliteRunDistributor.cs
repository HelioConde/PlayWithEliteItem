using System;
using System.Collections;
using System.Collections.Generic;
using BepInEx.Logging;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace GiveEliteItem
{
    /// <summary>
    /// The elite aspect is a second LOGICAL equipment slot stored exclusively on
    /// the server. An ordinary pickup stays in the real active slot.
    /// All visible inventory updates use the game's built-in replication.
    /// </summary>
    internal sealed class EliteRunDistributor
    {
        private const float ScanIntervalSeconds = 0.5f;

        private sealed class PlayerEquipment
        {
            internal EquipmentIndex EliteIndex;
            internal EquipmentState Reserve;
            internal bool HasReserve;
            internal bool SwapPending;
        }

        private readonly GiveEliteItem plugin;
        private readonly EliteSettings settings;
        private readonly ManualLogSource logger;
        private readonly Dictionary<CharacterMaster, PlayerEquipment> players =
            new Dictionary<CharacterMaster, PlayerEquipment>();
        private readonly HashSet<int> assignedAspects = new HashSet<int>();
        private readonly List<int> availableAspects = new List<int>();

        private Run activeRun;
        private System.Random random;
        private float nextScanTime;
        private bool emptyPoolLogged;
        private int runGeneration;

        internal EliteRunDistributor(
            GiveEliteItem plugin, EliteSettings settings, ManualLogSource logger)
        {
            this.plugin = plugin;
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
            {
                Reset();
                activeRun = run;
                random = new System.Random();
                logger.LogInfo("Elite equipment reserve initialized for a new run.");
            }

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
                        logger.LogWarning("No eligible elite aspects. Check expansions and config.");
                        emptyPoolLogged = true;
                    }
                    return;
                }

                emptyPoolLogged = false;
                logger.LogInfo("Eligible elite aspects: " + availableAspects.Count + ".");
            }

            foreach (PlayerCharacterMasterController controller in
                PlayerCharacterMasterController.instances)
            {
                if (controller == null || controller.master == null ||
                    controller.networkUser == null || controller.master.inventory == null)
                    continue;

                CharacterMaster master = controller.master;
                if (players.ContainsKey(master))
                    continue;

                Inventory inventory = master.inventory;

                // MUL-T / Functional Coupler have their own real slots/sets;
                // leave them unmodified until those mechanics have been tested.
                if (!IsSingleEquipmentLocation(inventory))
                    continue;

                int chosen = AspectSelection.Choose(
                    availableAspects, assignedAspects, random,
                    settings.UniqueBeforeRepeats.Value);
                EquipmentIndex elite = (EquipmentIndex)chosen;
                var state = new PlayerEquipment
                {
                    EliteIndex = elite,
                    Reserve = new EquipmentState(
                        elite, Run.FixedTimeStamp.positiveInfinity, 1, false),
                    HasReserve = true
                };

                players.Add(master, state);
                assignedAspects.Add(chosen);
                EquipmentDef definition = EquipmentCatalog.GetEquipmentDef(elite);
                logger.LogInfo("Reserved elite " + definition.name + " for " +
                    controller.networkUser.userName + " in logical slot 2.");

                // Deliberately DO NOT write elite into slot 0. A new equipment pickup
                // can now enter slot 0 normally, without dropping the elite aspect.
            }
        }

        internal void OnEquipmentActivated(CharacterMaster master, EquipmentDef activated)
        {
            if (!NetworkServer.active || !settings.Enabled.Value ||
                !settings.AutomaticSwapAfterUse.Value || activeRun == null ||
                master == null || master.inventory == null || activated == null ||
                !players.TryGetValue(master, out PlayerEquipment state))
                return;

            if (!EliteRotationRules.ShouldRotate(
                true, state.HasReserve, state.SwapPending))
                return;

            state.SwapPending = true;
            plugin.StartCoroutine(SwapAfterDelay(
                master, state, activated.equipmentIndex, runGeneration));
        }

        private IEnumerator SwapAfterDelay(
            CharacterMaster master, PlayerEquipment state,
            EquipmentIndex activatedIndex, int expectedGeneration)
        {
            yield return new WaitForSeconds(settings.SwapDelaySeconds.Value);
            try
            {
                if (!NetworkServer.active || !settings.Enabled.Value ||
                    !settings.AutomaticSwapAfterUse.Value ||
                    activeRun == null || Run.instance != activeRun ||
                    runGeneration != expectedGeneration ||
                    master == null || master.inventory == null ||
                    !players.TryGetValue(master, out PlayerEquipment currentState) ||
                    !ReferenceEquals(currentState, state) ||
                    !IsSingleEquipmentLocation(master.inventory))
                    yield break;

                Inventory inventory = master.inventory;
                EquipmentLocation location = inventory.activeEquipmentLocation;
                EquipmentState equipped = inventory.GetEquipment(location);

                if (!EliteRotationRules.CanCommitSwap(
                    true, ReferenceEquals(currentState, state),
                    equipped.equipmentIndex == activatedIndex, state.HasReserve))
                    yield break;

                EquipmentState next = state.Reserve;
                if (next.equipmentIndex == EquipmentIndex.None)
                    yield break;

                inventory.SetEquipment(next, location.slot, location.set);
                if (inventory.GetEquipment(location).equipmentIndex != next.equipmentIndex)
                {
                    logger.LogWarning("Swap failed, keeping the second equipment in reserve.");
                    yield break;
                }

                state.Reserve = equipped;
                state.HasReserve = equipped.equipmentIndex != EquipmentIndex.None;

                if (settings.VerboseLogging.Value)
                    logger.LogInfo("Equipment switched: " + equipped.equipmentIndex +
                        " -> " + next.equipmentIndex);
            }
            finally
            {
                state.SwapPending = false;
            }
        }

        internal void HandlePickup(
            On.RoR2.GenericPickupController.orig_AttemptGrant orig,
            GenericPickupController controller, CharacterBody body)
        {
            if (!NetworkServer.active || !settings.Enabled.Value ||
                activeRun == null || controller == null || body == null ||
                body.master == null || body.inventory == null ||
                !players.TryGetValue(body.master, out PlayerEquipment state))
            {
                orig(controller, body);
                return;
            }

            Inventory inventory = body.inventory;
            PickupDef pickup = PickupCatalog.GetPickupDef(controller.pickup.pickupIndex);

            if (pickup == null || pickup.equipmentIndex == EquipmentIndex.None ||
                !IsSingleEquipmentLocation(inventory))
            {
                orig(controller, body);
                return;
            }

            EquipmentLocation location = inventory.activeEquipmentLocation;
            EquipmentState active = inventory.GetEquipment(location);

            if (!EliteRotationRules.ShouldProtectElitePickup(
                state.HasReserve, state.SwapPending,
                active.equipmentIndex == state.EliteIndex,
                true))
            {
                orig(controller, body);
                return;
            }

            // The elite is active and the previous normal equipment is stored
            // in our virtual reserve. Place the normal equipment into the real
            // slot so vanilla can replace/drop it instead of dropping the elite.
            EquipmentState oldReserve = state.Reserve;
            inventory.SetEquipment(oldReserve, location.slot, location.set);

            try
            {
                orig(controller, body);
            }
            finally
            {
                EquipmentState result = inventory.GetEquipment(location);
                if (result.equipmentIndex == pickup.equipmentIndex)
                {
                    // Pickup succeeded; the prior normal equipment was replaced
                    // by vanilla. Keep the elite safely in the virtual second slot.
                    state.Reserve = active;
                    state.HasReserve = true;

                    if (settings.VerboseLogging.Value)
                        logger.LogInfo("Protected elite aspect from world pickup.");
                }
                else
                {
                    // Pickup denied: restore both pieces and do not duplicate items.
                    state.Reserve = oldReserve;
                    state.HasReserve = oldReserve.equipmentIndex != EquipmentIndex.None;
                    if (result.equipmentIndex == oldReserve.equipmentIndex)
                        inventory.SetEquipment(active, location.slot, location.set);
                    else
                        logger.LogWarning(
                            "Unexpected pickup result; cannot restore the prior equipment safely.");
                }
            }
        }

        private static bool IsSingleEquipmentLocation(Inventory inventory)
        {
            return inventory != null &&
                inventory.equipmentSlotCount == 1 &&
                inventory.equipmentSetCount == 1;
        }

        internal void Reset()
        {
            runGeneration++;
            activeRun = null;
            random = null;
            nextScanTime = 0f;
            emptyPoolLogged = false;
            players.Clear();
            assignedAspects.Clear();
            availableAspects.Clear();
        }
    }
}
