using BepInEx;
using RoR2;

namespace GiveEliteItem
{
    /// <summary>Server-side entrypoint. No custom UI or client input is needed.</summary>
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class GiveEliteItem : BaseUnityPlugin
    {
        public const string PluginGuid = "AlchemyFlame.PlayWithEliteItem";
        public const string PluginName = "PlayWithEliteItem";
        public const string PluginVersion = "1.1.3";

        private EliteRunDistributor distributor;

        private void Awake()
        {
            distributor = new EliteRunDistributor(this, new EliteSettings(Config), Logger);
            On.RoR2.EquipmentSlot.PerformEquipmentAction += OnPerformEquipmentAction;
            On.RoR2.GenericPickupController.AttemptGrant += OnAttemptGrant;
            Logger.LogInfo(PluginName + " " + PluginVersion + " loaded (server-side elite reserve).");
        }

        private void Update()
        {
            distributor?.Tick();
        }

        private bool OnPerformEquipmentAction(
            On.RoR2.EquipmentSlot.orig_PerformEquipmentAction orig,
            EquipmentSlot equipmentSlot, EquipmentDef equipmentDef)
        {
            bool success = orig(equipmentSlot, equipmentDef);
            if (success && equipmentSlot != null && equipmentSlot.characterBody != null)
                distributor?.OnEquipmentActivated(equipmentSlot.characterBody.master, equipmentDef);

            return success;
        }

        private void OnAttemptGrant(
            On.RoR2.GenericPickupController.orig_AttemptGrant orig,
            GenericPickupController pickupController, CharacterBody characterBody)
        {
            if (distributor != null)
                distributor.HandlePickup(orig, pickupController, characterBody);
            else
                orig(pickupController, characterBody);
        }

        private void OnDestroy()
        {
            On.RoR2.EquipmentSlot.PerformEquipmentAction -= OnPerformEquipmentAction;
            On.RoR2.GenericPickupController.AttemptGrant -= OnAttemptGrant;
            StopAllCoroutines();
            distributor?.Reset();
        }
    }
}
