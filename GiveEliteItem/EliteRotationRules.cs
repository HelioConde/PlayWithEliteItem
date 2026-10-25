namespace GiveEliteItem
{
    /// <summary>Pure decision rules that can be tested without a running game.</summary>
    internal static class EliteRotationRules
    {
        internal static bool ShouldRotate(
            bool activationSucceeded, bool hasReserve, bool alreadyPending)
            => activationSucceeded && hasReserve && !alreadyPending;

        internal static bool ShouldProtectElitePickup(
            bool hasReserve, bool pending, bool activeIsElite, bool supportedInventory)
            => hasReserve && !pending && activeIsElite && supportedInventory;

        internal static bool CanCommitSwap(
            bool sameRun, bool samePlayer, bool activeUnchanged, bool hasReserve)
            => sameRun && samePlayer && activeUnchanged && hasReserve;
    }
}
