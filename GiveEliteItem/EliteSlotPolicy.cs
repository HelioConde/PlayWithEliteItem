namespace GiveEliteItem
{
    /// <summary>Testable guardrails for the permanent second physical slot.</summary>
    internal static class EliteSlotPolicy
    {
        internal static bool CanAssign(
            bool playerReady, bool slotOneIsFree,
            bool alreadyAssigned, bool activeLocationIsSlotOne)
            => playerReady && slotOneIsFree && !alreadyAssigned && !activeLocationIsSlotOne;

        internal static bool IsEliteStillInPlace(
            int expectedEliteIndex, int slotOneEquipmentIndex)
            => expectedEliteIndex == slotOneEquipmentIndex;
    }
}
