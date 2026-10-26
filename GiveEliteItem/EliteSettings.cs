using BepInEx.Configuration;

namespace GiveEliteItem
{
    internal sealed class EliteSettings
    {
        private const string ClassicAspects =
            "EliteEarthEquipment,EliteFireEquipment,EliteHauntedEquipment," +
            "EliteIceEquipment,EliteLightningEquipment,EliteLunarEquipment,ElitePoisonEquipment";

        internal readonly ConfigEntry<bool> Enabled;
        internal readonly ConfigEntry<bool> UniqueBeforeRepeats;
        internal readonly ConfigEntry<bool> IncludeAllRegisteredEliteAspects;
        internal readonly ConfigEntry<string> AllowedEliteEquipment;
        internal readonly ConfigEntry<bool> TraceGameplay;

        internal EliteSettings(ConfigFile config)
        {
            Enabled = config.Bind(
                "General", "Enabled", true,
                "Assign one elite aspect to physical equipment slot 1 for each player.");

            UniqueBeforeRepeats = config.Bind(
                "Distribution", "UniqueBeforeRepeats", true,
                "Prefer unique elite aspects until the eligible pool is exhausted.");

            IncludeAllRegisteredEliteAspects = config.Bind(
                "Distribution", "IncludeAllRegisteredEliteAspects", false,
                "Advanced: include all registered equipment with elite passive buffs.");

            AllowedEliteEquipment = config.Bind(
                "Distribution", "AllowedEliteEquipment", ClassicAspects,
                "Comma-separated internal equipment names used when auto-discovery is disabled.");

            TraceGameplay = config.Bind(
                "Diagnostics", "TraceGameplay", true,
                "Log initial elite assignments, inventory verification and run status.");
        }
    }
}
