using BepInEx;

namespace GiveEliteItem
{
    // Entrypoint only: game lifecycle and equipment distribution live in separate classes.
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class GiveEliteItem : BaseUnityPlugin
    {
        public const string PluginGuid = "AlchemyFlame.PlayWithEliteItem";
        public const string PluginName = "PlayWithEliteItem";
        public const string PluginVersion = "1.1.1";

        private EliteRunDistributor distributor;

        private void Awake()
        {
            var settings = new EliteSettings(Config);
            distributor = new EliteRunDistributor(settings, Logger);
            Logger.LogInfo(PluginName + " " + PluginVersion + " loaded.");
        }

        private void Update()
        {
            distributor?.Tick();
        }
    }
}
