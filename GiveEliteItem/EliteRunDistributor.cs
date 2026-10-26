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
            internal bool PhysicalSecondSlot;
            internal bool GrantCheckPending;
            internal float GrantTime;
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
        private readonly HashSet<CharacterMaster> reportedWaitingForBody =
            new HashSet<CharacterMaster>();
        private bool waitingForPlayersLogged;
        private float nextAuthorityWarning;
        private float nextStatusLog;
        private bool catalogDiagnosticsLogged;
        private float nextCatalogDiagnosticTime;
        private readonly HashSet<CharacterMaster> reportedGrantFailures =
            new HashSet<CharacterMaster>();

        internal EliteRunDistributor(
            GiveEliteItem plugin, EliteSettings settings, ManualLogSource logger)
        {
            this.plugin = plugin;
            this.settings = settings;
            this.logger = logger;
        }

        private void Trace(string category, string detail)
        {
            if (settings.TraceGameplay.Value)
                logger.LogInfo("[PWEI][" + category + "] " + detail);
        }

        private void DumpCatalogDiagnostics(Run run)
        {
            Trace("CATALOG", "EquipmentCatalog count=" + EquipmentCatalog.equipmentCount +
                " autoDiscovery=" + settings.IncludeAllRegisteredEliteAspects.Value +
                " eligible=" + availableAspects.Count);

            if (settings.IncludeAllRegisteredEliteAspects.Value)
                return;

            foreach (string name in EliteEquipmentNames.Parse(settings.AllowedEliteEquipment.Value))
            {
                EquipmentIndex index = EquipmentCatalog.FindEquipmentIndex(name);
                EquipmentDef def = index == EquipmentIndex.None
                    ? null
                    : EquipmentCatalog.GetEquipmentDef(index);
                if (index == EquipmentIndex.None || def == null)
                {
                    Trace("CATALOG", name + " => NOT FOUND");
                    continue;
                }

                bool expansionEnabled = def.requiredExpansion == null ||
                    run.IsExpansionEnabled(def.requiredExpansion);
                bool eliteBuff = def.passiveBuffDef != null && def.passiveBuffDef.isElite;
                Trace("CATALOG", name + " => index=" + index +
                    " actualName=" + def.name +
                    " expansionEnabled=" + expansionEnabled +
                    " passiveEliteBuff=" + eliteBuff);
            }
        }

        private const uint OriginalSecondSlot = 1u;

        // 2024 original: SetEquipmentIndexForSlot(elite, 1), then read
        // GetEquipment(0) and GetEquipment(1) during swapping.
        // In 1.5 the inventory exposes explicit slot/set counts; never assume
        // the legacy extra location exists or silently claim success.
        private bool TryInitializePhysicalSecondSlot(
            Inventory inventory, EquipmentIndex elite, PlayerEquipment state, string playerName)
        {
            if (!settings.PreferOriginalPhysicalSlot.Value)
                return false;

            EquipmentLocation activeLocation = inventory.activeEquipmentLocation;
            if (activeLocation.slot == OriginalSecondSlot)
                return false;

            EquipmentIndex before = EquipmentIndex.None;
            try
            {
                if (inventory.equipmentSlotCount > OriginalSecondSlot)
                    before = inventory.GetEquipment(OriginalSecondSlot).equipmentIndex;

                // If a character already owns its own second equipment slot,
                // never replace an equipment belonging to the base game.
                if (before != EquipmentIndex.None)
                {
                    Trace("LEGACY", "Slot 1 already occupied by " + before +
                        "; preserving it, using virtual fallback.");
                    return false;
                }

                Trace("LEGACY", "Trying original slot=1 for " + playerName +
                    "; reportedSlots=" + inventory.equipmentSlotCount +
                    " sets=" + inventory.equipmentSetCount);
                inventory.SetEquipmentIndexForSlot(elite, OriginalSecondSlot);

                EquipmentIndex result = inventory.GetEquipment(OriginalSecondSlot).equipmentIndex;
                if (result != elite)
                {
                    logger.LogWarning("[PWEI][LEGACY] Physical slot 1 rejected " + elite +
                        "; readback=" + result + ". Using virtual fallback.");
                    return false;
                }

                state.PhysicalSecondSlot = true;
                state.Reserve = inventory.GetEquipment(OriginalSecondSlot);
                state.HasReserve = true;
                Trace("LEGACY", "SUCCESS physical slot=1 elite=" + elite +
                    " slotsAfter=" + inventory.equipmentSlotCount);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogWarning("[PWEI][LEGACY] Cannot use physical slot 1: " +
                    ex.GetType().Name + " " + ex.Message +
                    ". Falling back without claiming the extra slot worked.");
                return false;
            }
        }

        private EquipmentState ReadSecondEquipment(Inventory inventory, PlayerEquipment state)
        {
            return state.PhysicalSecondSlot ?
                inventory.GetEquipment(OriginalSecondSlot) : state.Reserve;
        }

        private bool PhysicalSwap(
            CharacterMaster master, PlayerEquipment state,
            EquipmentState active, EquipmentLocation location)
        {
            Inventory inventory = master.inventory;
            EquipmentState second = inventory.GetEquipment(OriginalSecondSlot);
            if (second.equipmentIndex == EquipmentIndex.None)
                return false;

            if (second.equipmentIndex == state.EliteIndex)
                ReleaseReservedBuff(master);

            // Use the same slot 0 <-> 1 algorithm as 2024, preserving
            // entire equipment state including cooldown and charge count.
            inventory.SetEquipment(second, location.slot, location.set);
            inventory.SetEquipment(active, OriginalSecondSlot, 0u);

            bool committed =
                inventory.GetEquipment(location).equipmentIndex == second.equipmentIndex &&
                inventory.GetEquipment(OriginalSecondSlot).equipmentIndex == active.equipmentIndex;
            if (!committed)
            {
                logger.LogWarning("[PWEI][LEGACY] Physical swap readback failed. " +
                    "Attempting to restore the two previous equipment states.");
                inventory.SetEquipment(active, location.slot, location.set);
                inventory.SetEquipment(second, OriginalSecondSlot, 0u);
                return false;
            }

            state.Reserve = inventory.GetEquipment(OriginalSecondSlot);
            state.HasReserve = state.Reserve.equipmentIndex != EquipmentIndex.None;
            SynchronizeReservedBuff(master, state);
            Trace("LEGACY", "SWAP physical 0<->1 active=" +
                inventory.GetEquipment(location).equipmentIndex +
                " second=" + state.Reserve.equipmentIndex);
            return true;
        }

        private void HandlePhysicalPickup(
            On.RoR2.GenericPickupController.orig_AttemptGrant orig,
            GenericPickupController controller, CharacterBody body,
            PlayerEquipment state, PickupDef pickup, EquipmentLocation location)
        {
            Inventory inventory = body.inventory;
            EquipmentState active = inventory.GetEquipment(location);
            EquipmentState second = inventory.GetEquipment(OriginalSecondSlot);

            // If elite is already in second slot, the ordinary pickup
            // can use vanilla's normal active-slot replacement behavior.
            if (active.equipmentIndex != state.EliteIndex || state.SwapPending)
            {
                orig(controller, body);
                return;
            }

            // If elite was rotated to Q, move it back to the REAL second
            // slot before allowing vanilla to replace/drop ordinary equipment.
            inventory.SetEquipment(second, location.slot, location.set);
            inventory.SetEquipment(active, OriginalSecondSlot, 0u);

            if (inventory.GetEquipment(OriginalSecondSlot).equipmentIndex != state.EliteIndex)
            {
                logger.LogWarning("[PWEI][LEGACY] Failed to protect elite on pickup.");
                inventory.SetEquipment(active, location.slot, location.set);
                inventory.SetEquipment(second, OriginalSecondSlot, 0u);
                return;
            }

            try
            {
                orig(controller, body);
            }
            finally
            {
                EquipmentIndex after = inventory.GetEquipment(location).equipmentIndex;
                if (after != pickup.equipmentIndex &&
                    after == second.equipmentIndex)
                {
                    // Pickup denied: restore the prior physical orientation.
                    inventory.SetEquipment(active, location.slot, location.set);
                    inventory.SetEquipment(second, OriginalSecondSlot, 0u);
                }
                state.Reserve = inventory.GetEquipment(OriginalSecondSlot);
                state.HasReserve = state.Reserve.equipmentIndex != EquipmentIndex.None;
                SynchronizeReservedBuff(body.master, state);
                Trace("LEGACY", "PICKUP active=" +
                    inventory.GetEquipment(location).equipmentIndex +
                    " second=" + state.Reserve.equipmentIndex);
            }
        }

        // The virtual reserve retains EquipmentState but vanilla only derives
        // passive elite buffs from equipped gear. This controller grants the
        // corresponding VANILLA BuffDef while that elite is in reserve, and
        // relinquishes ownership before it is equipped again.
        private sealed class EliteBuffOwner
        {
            internal CharacterBody Body;
            internal BuffDef Buff;
        }

        private readonly Dictionary<CharacterMaster, EliteBuffOwner> buffOwners =
            new Dictionary<CharacterMaster, EliteBuffOwner>();

        private void ReleaseReservedBuff(CharacterMaster master)
        {
            if (!buffOwners.TryGetValue(master, out EliteBuffOwner owner))
                return;

            buffOwners.Remove(master);
            if (owner.Body != null && owner.Buff != null && owner.Body.HasBuff(owner.Buff))
            {
                owner.Body.RemoveBuff(owner.Buff);
                Trace("BUFF", "RELEASE buff=" + owner.Buff.name + " master=" + master.name);
            }
        }

        private void SynchronizeReservedBuff(CharacterMaster master, PlayerEquipment state)
        {
            CharacterBody body = master != null ? master.GetBody() : null;
            if (buffOwners.TryGetValue(master, out EliteBuffOwner oldOwner) && oldOwner.Body != body)
                ReleaseReservedBuff(master);

            if (body == null || master.inventory == null)
                return;

            EquipmentLocation location = master.inventory.activeEquipmentLocation;
            EquipmentIndex equipped = master.inventory.GetEquipment(location).equipmentIndex;
            if (state.PhysicalSecondSlot)
            {
                state.Reserve = ReadSecondEquipment(master.inventory, state);
                state.HasReserve = state.Reserve.equipmentIndex != EquipmentIndex.None;
            }
            bool reserveHasElite = state.HasReserve &&
                state.Reserve.equipmentIndex == state.EliteIndex;
            bool shouldMaintain = EliteRotationRules.ShouldMaintainEliteBuff(
                settings.KeepEliteBuffWhileReserved.Value,
                reserveHasElite, equipped == state.EliteIndex, body != null);

            EquipmentDef equipment = EquipmentCatalog.GetEquipmentDef(state.EliteIndex);
            BuffDef buff = equipment != null ? equipment.passiveBuffDef : null;
            if (!shouldMaintain || buff == null || !buff.isElite)
            {
                ReleaseReservedBuff(master);
                return;
            }

            if (!body.HasBuff(buff))
            {
                body.AddBuff(buff);
                buffOwners[master] = new EliteBuffOwner { Body = body, Buff = buff };
                Trace("BUFF", "APPLIED elite=" + equipment.name +
                    " active=" + equipped + " reserve=" + state.Reserve.equipmentIndex);
            }
        }

        private void VerifyGrantedEquipment()
        {
            foreach (var player in players)
            {
                PlayerEquipment state = player.Value;
                if (!state.GrantCheckPending || Time.unscaledTime < state.GrantTime + 1.5f)
                    continue;

                state.GrantCheckPending = false;
                CharacterMaster master = player.Key;
                if (master == null || master.inventory == null)
                {
                    logger.LogWarning("[PWEI][VERIFY] Player inventory vanished after grant.");
                    continue;
                }

                EquipmentLocation location = master.inventory.activeEquipmentLocation;
                EquipmentIndex current = master.inventory.GetEquipment(location).equipmentIndex;
                EquipmentIndex physicalSecond = state.PhysicalSecondSlot
                    ? master.inventory.GetEquipment(OriginalSecondSlot).equipmentIndex
                    : EquipmentIndex.None;
                bool present = state.PhysicalSecondSlot
                    ? physicalSecond == state.EliteIndex
                    : current == state.EliteIndex;
                Trace("VERIFY", "Master=" + master.name + " expectedElite=" +
                    state.EliteIndex + " active=" + current +
                    " physicalSlot1=" + physicalSecond +
                    " location=" + location.slot + "/" + location.set +
                    " mode=" + (state.PhysicalSecondSlot ? "physical" : "virtual"));
                if (!present)
                    logger.LogWarning("[PWEI][VERIFY] Elite equipment no longer resides " +
                        "in the expected inventory location 1.5s after grant.");
            }
        }

        internal void Tick()
        {
            if (!NetworkServer.active)
            {
                if (Run.instance != null && Time.unscaledTime >= nextAuthorityWarning)
                {
                    nextAuthorityWarning = Time.unscaledTime + 8f;
                    logger.LogWarning("A run exists but this process is not the network server; " +
                        "PlayWithEliteItem must be installed on the host.");
                }
                return;
            }

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
                logger.LogInfo("[PWEI][RUN] New run detected, NetworkServer.active=" +
                    NetworkServer.active + " runTime=" + run.time);
            }

            if (!settings.Enabled.Value)
            {
                if (Time.unscaledTime >= nextStatusLog)
                {
                    nextStatusLog = Time.unscaledTime + 10f;
                    Trace("RUN", "Mod disabled by General.Enabled=false.");
                }
                return;
            }

            if (Time.unscaledTime < nextScanTime)
                return;

            nextScanTime = Time.unscaledTime + ScanIntervalSeconds;

            if (availableAspects.Count == 0)
            {
                EliteAspectCatalog.CollectEligible(
                    run,
                    settings.IncludeAllRegisteredEliteAspects.Value,
                    settings.AllowedEliteEquipment.Value,
                    availableAspects);

                if (!catalogDiagnosticsLogged || Time.unscaledTime >= nextCatalogDiagnosticTime)
                {
                    DumpCatalogDiagnostics(run);
                    catalogDiagnosticsLogged = true;
                    nextCatalogDiagnosticTime = Time.unscaledTime + 15f;
                }

                if (availableAspects.Count == 0)
                {
                    if (!emptyPoolLogged)
                    {
                        logger.LogWarning("[PWEI][CATALOG] No eligible elite aspects. " +
                            "Check the internal equipment names and enabled expansions.");
                        emptyPoolLogged = true;
                    }
                    return;
                }

                emptyPoolLogged = false;
                logger.LogInfo("[PWEI][CATALOG] Eligible elite aspects: " +
                    availableAspects.Count + ".");
            }

            int candidateControllers = 0;
            int missingMaster = 0;
            int missingUser = 0;
            int missingInventory = 0;
            int missingBody = 0;
            foreach (PlayerCharacterMasterController controller in
                PlayerCharacterMasterController.instances)
            {
                candidateControllers++;
                if (controller == null || controller.master == null)
                {
                    missingMaster++;
                    continue;
                }
                if (controller.networkUser == null)
                {
                    missingUser++;
                    continue;
                }
                if (controller.master.inventory == null)
                {
                    missingInventory++;
                    continue;
                }

                CharacterMaster master = controller.master;
                if (players.ContainsKey(master))
                    continue;

                Inventory inventory = master.inventory;

                // Do not reject ordinary players based on an assumed slot/set
                // count. The current game has different inventory layouts.
                // Wait for the spawned body so the loadout cannot overwrite the grant.
                CharacterBody body = master.GetBody();
                if (body == null || body.inventory == null)
                {
                    missingBody++;
                    if (reportedWaitingForBody.Add(master))
                        Trace("PLAYER", "Waiting for character body: " +
                            controller.networkUser.userName + " master=" + master.name);
                    continue;
                }

                if (!EliteRotationRules.ShouldGrantAtSpawn(true, true, players.ContainsKey(master)))
                    continue;

                int chosen = AspectSelection.Choose(
                    availableAspects, assignedAspects, random,
                    settings.UniqueBeforeRepeats.Value);
                EquipmentIndex elite = (EquipmentIndex)chosen;

                EquipmentLocation location = inventory.activeEquipmentLocation;
                EquipmentState previous = inventory.GetEquipment(location);
                if (!reportedGrantFailures.Contains(master))
                    Trace("GRANT", "Attempt player=" + controller.networkUser.userName +
                        " selected=" + elite +
                        " location=" + location.slot + "/" + location.set +
                        " previousEquipment=" + previous.equipmentIndex +
                        " slots=" + inventory.equipmentSlotCount +
                        " sets=" + inventory.equipmentSetCount);
                var state = new PlayerEquipment
                {
                    EliteIndex = elite,
                    // Virtual fallback keeps old equipment safe.
                    Reserve = previous,
                    HasReserve = previous.equipmentIndex != EquipmentIndex.None
                };

                // Preserve the 2024 method FIRST. The physical slot is only
                // selected when it can be read back successfully.
                if (TryInitializePhysicalSecondSlot(
                    inventory, elite, state, controller.networkUser.userName))
                {
                    state.GrantTime = Time.unscaledTime;
                    state.GrantCheckPending = true;
                    players.Add(master, state);
                    assignedAspects.Add(chosen);
                    Trace("GRANT", "SUCCESS physical second slot index=" + elite +
                        " active=" + previous.equipmentIndex + " second=" +
                        inventory.GetEquipment(OriginalSecondSlot).equipmentIndex);
                    continue;
                }

                // Fallback: if the game cannot address a physical slot 1,
                // keep the current virtual-reserve behavior.

                // Match the original 1.0.1 grant: use SetEquipmentIndexForSlot.
                // For multi-set inventories, the active set may not be set 0;
                // use the location-aware overload so Q displays the correct item.
                if (location.set == 0u)
                    inventory.SetEquipmentIndexForSlot(elite, location.slot);
                else
                    inventory.SetEquipment(
                        new EquipmentState(elite, Run.FixedTimeStamp.positiveInfinity, 1, false),
                        location.slot, location.set);

                // Some equipment providers can replace the slot on spawn. Verify
                // the result instead of recording a successful grant incorrectly.
                if (inventory.GetEquipment(location).equipmentIndex != elite)
                {
                    inventory.SetEquipment(
                        new EquipmentState(elite, Run.FixedTimeStamp.positiveInfinity, 1, false),
                        location.slot, location.set);
                }

                EquipmentIndex actualAfterGrant =
                    inventory.GetEquipment(location).equipmentIndex;
                if (actualAfterGrant != elite)
                {
                    if (reportedGrantFailures.Add(master))
                        logger.LogWarning("[PWEI][GRANT] FAILED player=" +
                            controller.networkUser.userName + " selected=" + elite +
                            " actual=" + actualAfterGrant +
                            " location=" + location.slot + "/" + location.set +
                            " slots=" + inventory.equipmentSlotCount +
                            " sets=" + inventory.equipmentSetCount + ". Retrying.");
                    continue;
                }

                reportedGrantFailures.Remove(master);
                state.GrantTime = Time.unscaledTime;
                state.GrantCheckPending = true;
                players.Add(master, state);
                assignedAspects.Add(chosen);
                EquipmentDef definition = EquipmentCatalog.GetEquipmentDef(elite);
                logger.LogInfo("[PWEI][GRANT] SUCCESS " + definition.name +
                    " player=" + controller.networkUser.userName +
                    " slot=" + location.slot + " set=" + location.set +
                    " actual=" + actualAfterGrant);
            }

            if (candidateControllers == 0 && !waitingForPlayersLogged)
            {
                waitingForPlayersLogged = true;
                logger.LogWarning("[PWEI][PLAYER] No PlayerCharacterMasterController instances in run.");
            }

            foreach (var assigned in players)
                SynchronizeReservedBuff(assigned.Key, assigned.Value);

            VerifyGrantedEquipment();
            if (Time.unscaledTime >= nextStatusLog)
            {
                nextStatusLog = Time.unscaledTime + 10f;
                Trace("SCAN", "controllers=" + candidateControllers +
                    " registered=" + players.Count +
                    " missingMaster=" + missingMaster +
                    " missingUser=" + missingUser +
                    " missingInventory=" + missingInventory +
                    " waitingBody=" + missingBody +
                    " eligibleAspects=" + availableAspects.Count +
                    " runTime=" + run.time);
            }
        }

        internal void OnEquipmentActivated(
            CharacterMaster master, EquipmentDef activated, bool actionSucceeded)
        {
            if (!NetworkServer.active || !settings.Enabled.Value ||
                !settings.AutomaticSwapAfterUse.Value || activeRun == null ||
                master == null || master.inventory == null || activated == null ||
                !players.TryGetValue(master, out PlayerEquipment state))
                return;

            // The original 2024 hook queued the swap BEFORE checking the
            // return value of PerformEquipmentAction. Elite equipment is
            // passive and activation can return false; still allow the player
            // to rotate back to their normal equipment by pressing Q.
            EquipmentIndex current = master.inventory.GetEquipment(
                master.inventory.activeEquipmentLocation).equipmentIndex;
            bool canRotate = actionSucceeded ||
                (current == state.EliteIndex && activated.equipmentIndex == state.EliteIndex);

            if (!EliteRotationRules.ShouldRotate(
                canRotate, state.HasReserve, state.SwapPending))
            {
                Trace("USE", "Activation ignored: succeeded=" + actionSucceeded +
                    " reserve=" + state.HasReserve +
                    " pending=" + state.SwapPending + " equipment=" + activated.name);
                return;
            }

            Trace("USE", "Activation success; scheduling switch equipment=" +
                activated.name + " delay=" + settings.SwapDelaySeconds.Value);
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
                    !ReferenceEquals(currentState, state))
                    yield break;

                Inventory inventory = master.inventory;
                EquipmentLocation location = inventory.activeEquipmentLocation;
                EquipmentState equipped = inventory.GetEquipment(location);

                if (!EliteRotationRules.CanCommitSwap(
                    true, ReferenceEquals(currentState, state),
                    equipped.equipmentIndex == activatedIndex, state.HasReserve))
                    yield break;

                EquipmentState next = ReadSecondEquipment(inventory, state);
                if (next.equipmentIndex == EquipmentIndex.None)
                    yield break;

                if (state.PhysicalSecondSlot)
                {
                    bool swapped = PhysicalSwap(master, state, equipped, location);
                    if (!swapped)
                        logger.LogWarning("[PWEI][LEGACY] Physical swap aborted.");
                    yield break;
                }

                if (next.equipmentIndex == state.EliteIndex)
                    ReleaseReservedBuff(master);

                inventory.SetEquipment(next, location.slot, location.set);
                if (inventory.GetEquipment(location).equipmentIndex != next.equipmentIndex)
                {
                    logger.LogWarning("Swap failed, keeping the second equipment in reserve.");
                    yield break;
                }

                state.Reserve = equipped;
                state.HasReserve = equipped.equipmentIndex != EquipmentIndex.None;
                SynchronizeReservedBuff(master, state);

                Trace("SWAP", "SUCCESS activeBefore=" + equipped.equipmentIndex +
                    " activeAfter=" + next.equipmentIndex +
                    " reserveAfter=" + state.Reserve.equipmentIndex);
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

            if (pickup == null || pickup.equipmentIndex == EquipmentIndex.None)
            {
                orig(controller, body);
                return;
            }

            EquipmentLocation location = inventory.activeEquipmentLocation;
            EquipmentState active = inventory.GetEquipment(location);
            Trace("PICKUP", "Equipment pickup=" + pickup.equipmentIndex +
                " activeBefore=" + active.equipmentIndex +
                " reserve=" + state.Reserve.equipmentIndex +
                " reservePresent=" + state.HasReserve +
                " pendingSwap=" + state.SwapPending +
                " mode=" + (state.PhysicalSecondSlot ? "physical" : "virtual"));

            if (state.PhysicalSecondSlot)
            {
                HandlePhysicalPickup(orig, controller, body, state, pickup, location);
                return;
            }

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

                    state.GrantCheckPending = false;
                    Trace("PICKUP", "SUCCESS activeNow=" + result.equipmentIndex +
                        " eliteStored=" + state.Reserve.equipmentIndex);
                    SynchronizeReservedBuff(body.master, state);
                    Trace("BUFF", "PICKUP after sync: hasEliteBuff=" +
                        (EquipmentCatalog.GetEquipmentDef(state.EliteIndex).passiveBuffDef != null &&
                         body.HasBuff(EquipmentCatalog.GetEquipmentDef(state.EliteIndex).passiveBuffDef)));
                }
                else
                {
                    // Pickup was denied: keep both equipment states as they were.
                    state.Reserve = oldReserve;
                    state.HasReserve = hadReserve;
                    if (result.equipmentIndex == replacement.equipmentIndex)
                        inventory.SetEquipment(active, location.slot, location.set);
                    else
                        logger.LogWarning("[PWEI][PICKUP] Unexpected inventory result " +
                            result.equipmentIndex + " after pickup " + pickup.equipmentIndex);
                    Trace("PICKUP", "Pickup not confirmed. ActiveNow=" +
                        inventory.GetEquipment(location).equipmentIndex);
                }
            }
        }

         internal void Reset()
        {
            runGeneration++;
            activeRun = null;
            random = null;
            nextScanTime = 0f;
            emptyPoolLogged = false;
            waitingForPlayersLogged = false;
            reportedWaitingForBody.Clear();
            nextAuthorityWarning = 0f;
            nextStatusLog = 0f;
            catalogDiagnosticsLogged = false;
            nextCatalogDiagnosticTime = 0f;
            reportedGrantFailures.Clear();
            foreach (CharacterMaster master in new List<CharacterMaster>(buffOwners.Keys))
                ReleaseReservedBuff(master);
            players.Clear();
            assignedAspects.Clear();
            availableAspects.Clear();
        }
    }
}
