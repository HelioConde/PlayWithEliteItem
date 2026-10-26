namespace GiveEliteItem
{
    /// <summary>Testable guardrails for the permanent second physical slot.</summary>
    internal static class EliteSlotPolicy
    {
        internal static bool CanAssign(
            bool playerReady, bool slotOneIsFree,
            bool alreadyAssigned, bool activeLocationIsSlotOne)
            => playerReady && slotOneIsFree && !alreadyAssigned && !activeLocationIsSlotOne;

        internal static bool ShouldMaintainPassive(
            bool enabled, bool correctSlot, bool eliteIsActive, bool characterAlive)
            => enabled && correctSlot && !eliteIsActive && characterAlive;

        internal static bool IsEliteStillInPlace(
            int expectedEliteIndex, int slotOneEquipmentIndex)
            => expectedEliteIndex == slotOneEquipmentIndex;
    }
}
