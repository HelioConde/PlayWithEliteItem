using BepInEx;

namespace GiveEliteItem
{
    /// <summary>
    /// Server-side mod: one permanently reserved physical elite equipment slot.
    /// Vanilla gameplay owns the active Q slot and all equipment pickups.
    /// </summary>
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class GiveEliteItem : BaseUnityPlugin
    {
        public const string PluginGuid = "AlchemyFlame.PlayWithEliteItem";
        public const string PluginName = "PlayWithEliteItem";
        public const string PluginVersion = "1.2.0";

        private EliteRunDistributor distributor;

        private void Awake()
        {
            var settings = new EliteSettings(Config);
            distributor = new EliteRunDistributor(settings, Logger);
            Logger.LogInfo("[PWEI][BOOT] version=" + PluginVersion +
                " enabled=" + settings.Enabled.Value +
                " secondSlot=1 autoSwap=DISABLED" +
                " trace=" + settings.TraceGameplay.Value);
        }

        private void Update()
        {
            distributor?.Tick();
        }

        private void OnDestroy()
        {
            distributor?.Reset();
        }
    }
}
