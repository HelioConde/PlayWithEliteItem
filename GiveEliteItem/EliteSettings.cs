using BepInEx.Configuration;

namespace GiveEliteItem
{
    /// <summary>All user-facing settings and their default values.</summary>
    internal sealed class EliteSettings
    {
        private const string ClassicAspects =
            "EliteEarthEquipment,EliteFireEquipment,EliteHauntedEquipment," +
            "EliteIceEquipment,EliteLightningEquipment,EliteLunarEquipment,ElitePoisonEquipment";

        internal readonly ConfigEntry<bool> Enabled;
        internal readonly ConfigEntry<bool> UniqueBeforeRepeats;
        internal readonly ConfigEntry<bool> IncludeAllRegisteredEliteAspects;
        internal readonly ConfigEntry<string> AllowedEliteEquipment;
        internal readonly ConfigEntry<bool> ReplaceExistingEquipment;
        internal readonly ConfigEntry<bool> VerboseLogging;

        internal EliteSettings(ConfigFile config)
        {
            Enabled = config.Bind(
                "General", "Enabled", true,
                "Give players a random elite aspect at the beginning of each run.");

            UniqueBeforeRepeats = config.Bind(
                "Distribution", "UniqueBeforeRepeats", true,
                "Prefer different aspects until every eligible aspect has been assigned.");

            IncludeAllRegisteredEliteAspects = config.Bind(
                "Distribution", "IncludeAllRegisteredEliteAspects", false,
                "Advanced: include all registered elite equipment, including modded or hidden aspects.");

            AllowedEliteEquipment = config.Bind(
                "Distribution", "AllowedEliteEquipment", ClassicAspects,
                "Comma-separated equipment internal names. Used when automatic discovery is disabled.");

            ReplaceExistingEquipment = config.Bind(
                "Equipment", "ReplaceExistingEquipment", false,
                "Replace the active equipment if one is already equipped; otherwise skip that player.");

            VerboseLogging = config.Bind(
                "Diagnostics", "VerboseLogging", false,
                "Log assignments and skipped players.");
        }
    }
}
