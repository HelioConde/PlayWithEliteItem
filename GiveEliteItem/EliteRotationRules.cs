namespace GiveEliteItem
{
    /// <summary>Pure, testable rules for initialization, rotation and pickups.</summary>
    internal static class EliteRotationRules
    {
        internal static bool ShouldGrantAtSpawn(
            bool playerBodyReady, bool supportedInventory, bool alreadyAssigned)
            => playerBodyReady && supportedInventory && !alreadyAssigned;

        internal static bool ShouldRotate(
            bool activationSucceeded, bool hasReserve, bool alreadyPending)
            => activationSucceeded && hasReserve && !alreadyPending;

        // The first normal pickup must be protected even when the reserve is
        // still empty: this is exactly when the starting elite could be dropped.
        internal static bool ShouldProtectElitePickup(
            bool pending, bool activeIsElite, bool supportedInventory)
            => !pending && activeIsElite && supportedInventory;

        internal static bool CanCommitSwap(
            bool sameRun, bool samePlayer, bool activeUnchanged, bool hasReserve)
            => sameRun && samePlayer && activeUnchanged && hasReserve;
    }
}
