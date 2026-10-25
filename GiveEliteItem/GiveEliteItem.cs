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
        public const string PluginVersion = "1.1.1";

        private const float ScanIntervalSeconds = 0.5f;

        // The classic distribution excludes internal/debug aspect equipment by default.
        private const string ClassicEliteEquipment =
            "EliteEarthEquipment,EliteFireEquipment,EliteHauntedEquipment," +
            "EliteIceEquipment,EliteLightningEquipment,EliteLunarEquipment,ElitePoisonEquipment";

        private readonly HashSet<CharacterMaster> handledPlayers = new HashSet<CharacterMaster>();
        private readonly HashSet<int> assignedAspects = new HashSet<int>();
        private readonly List<int> availableAspects = new List<int>();

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
                "General", "Enabled", true, "Give players a random elite aspect when a run starts.");
            uniqueBeforeRepeatsConfig = Config.Bind(
                "Distribution", "UniqueBeforeRepeats", true,
                "Give distinct aspects to players until every available aspect has been assigned.");
            includeAllEliteAspectsConfig = Config.Bind(
                "Distribution", "IncludeAllRegisteredEliteAspects", false,
                "Advanced option: include all catalogued elite equipment with a passive elite buff (including modded aspects). May include hidden or nonstandard equipment.");
            allowedEliteEquipmentConfig = Config.Bind(
                "Distribution", "AllowedEliteEquipment", ClassicEliteEquipment,
                "Comma-separated internal equipment names used when IncludeAllRegisteredEliteAspects is false. Edit to add DLC aspects.");
            replaceExistingEquipmentConfig = Config.Bind(
                "Equipment", "ReplaceExistingEquipment", false,
                "If disabled, players whose active slot is occupied keep their existing equipment and get no elite aspect.");
            verboseLoggingConfig = Config.Bind(
                "Diagnostics", "VerboseLogging", false,
                "Log aspects granted to players and skipped assignments.");

            Logger.LogInfo("PlayWithEliteItem " + PluginVersion + " loaded.");
        }

        // Only the server may modify player inventories. Periodic checks cover
        // late joins without patching every equipment use or stage transition.
        private void Update()
        {
            if (!NetworkServer.active)
                return;

            Run run = Run.instance;
            if (run == null)
            {
                if (activeRun != null)
                    ResetRunState();
                return;
            }

            if (activeRun != run)
            {
                ResetRunState();
                activeRun = run;
                random = new System.Random();
                Logger.LogInfo("Elite distribution ready for the new run.");
            }

            if (!enabledConfig.Value || Time.unscaledTime < nextScanTime)
                return;

            nextScanTime = Time.unscaledTime + ScanIntervalSeconds;

            if (availableAspects.Count == 0)
            {
                DiscoverEliteAspects(run);
                if (availableAspects.Count == 0)
                {
                    if (!emptyPoolLogged)
                    {
                        Logger.LogWarning("No eligible elite aspects detected. Check the active DLCs and configuration.");
                        emptyPoolLogged = true;
                    }
                    return;
                }
            }

            foreach (PlayerCharacterMasterController controller in PlayerCharacterMasterController.instances)
            {
                if (controller == null || controller.networkUser == null || controller.master == null)
                    continue;

                CharacterMaster master = controller.master;
                if (handledPlayers.Contains(master) || master.inventory == null)
                    continue;

                TryGrantEliteAspect(master, controller.networkUser.userName);
            }
        }

        private void DiscoverEliteAspects(Run run)
        {
            availableAspects.Clear();

            var allowedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string name in allowedEliteEquipmentConfig.Value.Split(','))
            {
                if (!string.IsNullOrWhiteSpace(name))
                    allowedNames.Add(name.Trim());
            }

            for (int i = 0; i < EquipmentCatalog.equipmentCount; i++)
            {
                EquipmentIndex index = (EquipmentIndex)i;
                EquipmentDef definition = EquipmentCatalog.GetEquipmentDef(index);
                if (definition == null || definition.passiveBuffDef == null ||
                    !definition.passiveBuffDef.isElite)
                    continue;

                // Being registered in the catalogue doesn't mean the DLC is active for this run.
                if (definition.requiredExpansion != null && !run.IsExpansionEnabled(definition.requiredExpansion))
                    continue;

                if (!includeAllEliteAspectsConfig.Value && !allowedNames.Contains(definition.name))
                    continue;

                availableAspects.Add(i);
            }

            if (availableAspects.Count > 0)
            {
                emptyPoolLogged = false;
                Logger.LogInfo("Eligible elite aspects: " + availableAspects.Count +
                    (includeAllEliteAspectsConfig.Value ? " (catalog discovery)." : " (allowlist)."));
            }
        }

        private void TryGrantEliteAspect(CharacterMaster master, string playerName)
        {
            Inventory inventory = master.inventory;
            if (inventory == null)
                return;

            EquipmentIndex existing = inventory.GetEquipment(0u).equipmentIndex;
            if (existing != EquipmentIndex.None && !replaceExistingEquipmentConfig.Value)
            {
                handledPlayers.Add(master);
                if (verboseLoggingConfig.Value)
                    Logger.LogInfo("Skipped " + playerName + ": active equipment slot already occupied.");
                return;
            }

            int chosenIndex = AspectSelection.Choose(
                availableAspects, assignedAspects, random, uniqueBeforeRepeatsConfig.Value);
            EquipmentIndex selection = (EquipmentIndex)chosenIndex;
            inventory.SetEquipmentIndexForSlot(selection, 0u);

            if (inventory.GetEquipment(0u).equipmentIndex != selection)
            {
                Logger.LogWarning("Failed to give an elite aspect to " + playerName + "; retrying.");
                return;
            }

            assignedAspects.Add(chosenIndex);
            handledPlayers.Add(master);

            if (verboseLoggingConfig.Value)
            {
                EquipmentDef equipment = EquipmentCatalog.GetEquipmentDef(selection);
                Logger.LogInfo("Granted " + equipment.name + " to " + playerName + ".");
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
