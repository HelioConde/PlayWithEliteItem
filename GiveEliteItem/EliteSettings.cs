using BepInEx.Configuration;

namespace GiveEliteItem
{
    /// <summary>Configuration kept in one place for future compatibility.</summary>
    internal sealed class EliteSettings
    {
        private const string ClassicAspects =
            "EliteEarthEquipment,EliteFireEquipment,EliteHauntedEquipment," +
            "EliteIceEquipment,EliteLightningEquipment,EliteLunarEquipment,ElitePoisonEquipment";

        internal readonly ConfigEntry<bool> Enabled;
        internal readonly ConfigEntry<bool> UniqueBeforeRepeats;
        internal readonly ConfigEntry<bool> IncludeAllRegisteredEliteAspects;
        internal readonly ConfigEntry<string> AllowedEliteEquipment;
        internal readonly ConfigEntry<bool> AutomaticSwapAfterUse;
        internal readonly ConfigEntry<float> SwapDelaySeconds;
        internal readonly ConfigEntry<bool> VerboseLogging;

        internal EliteSettings(ConfigFile config)
        {
            Enabled = config.Bind(
                "General", "Enabled", true,
                "Equip a random elite aspect at spawn, preserving it in a virtual second slot on pickup.");

            UniqueBeforeRepeats = config.Bind(
                "Distribution", "UniqueBeforeRepeats", true,
                "Prefer different aspects until all eligible aspects have been assigned.");

            IncludeAllRegisteredEliteAspects = config.Bind(
                "Distribution", "IncludeAllRegisteredEliteAspects", false,
                "Advanced: include modded or hidden elite equipment from the equipment catalog.");

            AllowedEliteEquipment = config.Bind(
                "Distribution", "AllowedEliteEquipment", ClassicAspects,
                "Comma-separated internal names used when catalog discovery is disabled.");

            AutomaticSwapAfterUse = config.Bind(
                "Switching", "AutomaticSwapAfterUse", true,
                "Exchange active and reserved equipment after a successful equipment activation.");

            SwapDelaySeconds = config.Bind(
                "Switching", "SwapDelaySeconds", 0.3f,
                new ConfigDescription("Delay after a successful activation before swapping.",
                    new AcceptableValueRange<float>(0f, 2f)));

            VerboseLogging = config.Bind(
                "Diagnostics", "VerboseLogging", false,
                "Log equipment assignments, rotations and protected pickups.");
        }
    }
}
