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
    /// Equip the starting elite in the normal active slot so the player can
    /// actually see/use its passive. When another equipment is collected, move
    /// the elite into a virtual server-side reserve without ejecting it.
    /// Only the active state is synchronized through vanilla inventory replication.
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
        private readonly HashSet<CharacterMaster> reportedUnsupportedPlayers =
            new HashSet<CharacterMaster>();
        private bool waitingForPlayersLogged;

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
                logger.LogInfo("Eligible elite aspects: " + availableAspects.Count +
                    " (auto-discovery=" + settings.IncludeAllRegisteredEliteAspects.Value + ").");
            }

            int candidateControllers = 0;
            foreach (PlayerCharacterMasterController controller in
                PlayerCharacterMasterController.instances)
            {
                candidateControllers++;
                if (controller == null || controller.master == null ||
                    controller.networkUser == null || controller.master.inventory == null)
                    continue;

                CharacterMaster master = controller.master;
                if (players.ContainsKey(master))
                    continue;

                Inventory inventory = master.inventory;
                if (!IsSingleEquipmentLocation(inventory))
                {
                    if (reportedUnsupportedPlayers.Add(master))
                    {
                        logger.LogWarning("Skipping elite assignment for " +
                            controller.networkUser.userName +
                            ": equipmentSlotCount=" + inventory.equipmentSlotCount +
                            ", equipmentSetCount=" + inventory.equipmentSetCount +
                            ". The extra equipment location must be checked for this survivor.");
                    }
                    continue;
                }

                // Wait until the playable body is actually present. Giving equipment
                // before character initialization can cause the grant to be overwritten.
                CharacterBody body = master.GetBody();
                if (!EliteRotationRules.ShouldGrantAtSpawn(
                    body != null && body.inventory != null,
                    IsSingleEquipmentLocation(inventory),
                    players.ContainsKey(master)))
                    continue;

                int chosen = AspectSelection.Choose(
                    availableAspects, assignedAspects, random,
                    settings.UniqueBeforeRepeats.Value);
                EquipmentIndex elite = (EquipmentIndex)chosen;

                EquipmentLocation location = inventory.activeEquipmentLocation;
                EquipmentState previous = inventory.GetEquipment(location);
                var state = new PlayerEquipment
                {
                    EliteIndex = elite,
                    // Preserve equipment supplied by another mod, rather than
                    // silently deleting it when the elite is equipped.
                    Reserve = previous,
                    HasReserve = previous.equipmentIndex != EquipmentIndex.None
                };

                // The elite must be ACTIVE on spawn to show in Q and grant its buff.
                inventory.SetEquipment(
                    new EquipmentState(elite, Run.FixedTimeStamp.positiveInfinity, 1, false),
                    location.slot, location.set);

                if (inventory.GetEquipment(location).equipmentIndex != elite)
                {
                    logger.LogWarning("Failed to equip starting elite for " +
                        controller.networkUser.userName + "; will retry.");
                    continue;
                }

                players.Add(master, state);
                assignedAspects.Add(chosen);
                EquipmentDef definition = EquipmentCatalog.GetEquipmentDef(elite);
                logger.LogInfo("Equipped STARTING elite " + definition.name + " for " +
                    controller.networkUser.userName + " in the active equipment slot.");
            }

            if (candidateControllers == 0 && !waitingForPlayersLogged)
            {
                waitingForPlayersLogged = true;
                logger.LogWarning("Run is active, but no PlayerCharacterMasterController is registered yet.");
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
                state.SwapPending,
                active.equipmentIndex == state.EliteIndex,
                true))
            {
                orig(controller, body);
                return;
            }

            // The elite is ACTIVE at spawn. Before vanilla processes a pickup,
            // temporarily show a saved ordinary equipment (if any) or an empty
            // slot. This prevents vanilla from ejecting the elite onto the ground.
            EquipmentState oldReserve = state.Reserve;
            bool hadReserve = state.HasReserve;
            EquipmentState replacement = hadReserve
                ? oldReserve
                : new EquipmentState(
                    EquipmentIndex.None, Run.FixedTimeStamp.positiveInfinity, 0, false);
            inventory.SetEquipment(replacement, location.slot, location.set);

            if (inventory.GetEquipment(location).equipmentIndex != replacement.equipmentIndex)
            {
                logger.LogWarning("Could not prepare pickup without dropping the elite.");
                inventory.SetEquipment(active, location.slot, location.set);
                return;
            }

            try
            {
                orig(controller, body);
            }
            finally
            {
                EquipmentState result = inventory.GetEquipment(location);
                if (result.equipmentIndex == pickup.equipmentIndex)
                {
                    // A new ordinary item was acquired. The elite becomes the
                    // second logical equipment, regardless of prior reserve.
                    state.Reserve = active;
                    state.HasReserve = true;

                    if (settings.VerboseLogging.Value)
                        logger.LogInfo("Elite kept in logical slot 2 after pickup.");
                }
                else
                {
                    // Pickup was denied: keep both equipment states as they were.
                    state.Reserve = oldReserve;
                    state.HasReserve = hadReserve;
                    if (result.equipmentIndex == replacement.equipmentIndex)
                        inventory.SetEquipment(active, location.slot, location.set);
                    else
                        logger.LogWarning(
                            "Unexpected pickup result; inventory modified externally.");
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
            waitingForPlayersLogged = false;
            reportedUnsupportedPlayers.Clear();
            players.Clear();
            assignedAspects.Clear();
            availableAspects.Clear();
        }
    }
}
