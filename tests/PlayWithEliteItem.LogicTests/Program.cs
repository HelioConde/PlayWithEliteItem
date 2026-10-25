using System;
using System.Collections.Generic;
using GiveEliteItem;

internal static class Program
{
    private static int assertions;

    private static void Check(bool condition, string description)
    {
        assertions++;
        if (!condition)
            throw new Exception("TEST FAILED: " + description);
    }

    private static void ExpectException<T>(Action action, string description)
        where T : Exception
    {
        bool thrown = false;
        try { action(); }
        catch (T) { thrown = true; }
        Check(thrown, description);
    }

    private static void TestDistribution()
    {
        var random = new Random(1234);
        var pool = new List<int> { 11, 21, 31 };
        var assigned = new HashSet<int>();

        for (int i = 0; i < pool.Count; i++)
        {
            int chosen = AspectSelection.Choose(pool, assigned, random, true);
            Check(pool.Contains(chosen), "selection is in pool");
            Check(assigned.Add(chosen), "no duplicates before exhaustion");
        }

        Check(assigned.Count == pool.Count, "every aspect was assigned");

        // Simulates a lobby much larger than the eligible aspect pool.
        for (int i = 0; i < 100; i++)
            Check(pool.Contains(AspectSelection.Choose(pool, assigned, random, true)),
                "duplicates after exhaustion do not hang");

        var used = new HashSet<int> { 11 };
        for (int i = 0; i < 100; i++)
            Check(pool.Contains(AspectSelection.Choose(pool, used, random, false)),
                "uniqueness can be disabled");

        assigned.Clear();
        Check(pool.Contains(AspectSelection.Choose(pool, assigned, random, true)),
            "resetting a run permits fresh assignments");

        for (int i = 0; i < 100; i++)
            Check(AspectSelection.Choose(new[] { 9 }, new HashSet<int> { 9 }, random, true) == 9,
                "a single available aspect never hangs");

        ExpectException<ArgumentException>(
            () => AspectSelection.Choose(Array.Empty<int>(), assigned, random, true),
            "empty pool rejected");
        ExpectException<ArgumentNullException>(
            () => AspectSelection.Choose(pool, null, random, true),
            "null assigned set rejected");
        ExpectException<ArgumentNullException>(
            () => AspectSelection.Choose(pool, assigned, null, true),
            "null RNG rejected");
    }

    private static void TestAllowlist()
    {
        var names = EliteEquipmentNames.Parse(
            " EliteFireEquipment , EliteIceEquipment,,elitefireequipment, ");
        Check(names.Count == 2, "trim, empty entries and duplicates handled");
        Check(names.Contains("eliteFIREequipment"), "case-insensitive matching");
        Check(names.Contains("EliteIceEquipment"), "second equipment included");
        Check(EliteEquipmentNames.Parse(null).Count == 0, "null config handled");
        Check(EliteEquipmentNames.Parse(" ,  , ").Count == 0, "blank config handled");
    }

    private static void TestRotation()
    {
        Check(EliteRotationRules.ShouldRotate(true, true, false),
            "successful equipment use should swap with reserve");
        Check(!EliteRotationRules.ShouldRotate(false, true, false),
            "failed activation must not swap");
        Check(!EliteRotationRules.ShouldRotate(true, false, false),
            "empty secondary slot must not swap");
        Check(!EliteRotationRules.ShouldRotate(true, true, true),
            "pending swap must not duplicate");

        Check(EliteRotationRules.ShouldGrantAtSpawn(true, true, false),
            "spawned player should receive an active elite");
        Check(!EliteRotationRules.ShouldGrantAtSpawn(false, true, false),
            "must wait for spawned player body");
        Check(!EliteRotationRules.ShouldGrantAtSpawn(true, true, true),
            "grant only once per run");
        Check(!EliteRotationRules.ShouldGrantAtSpawn(true, false, false),
            "extra-slot characters use vanilla equipment behavior");

        Check(EliteRotationRules.ShouldProtectElitePickup(false, true, true),
            "first normal pickup protects elite even with empty reserve");
        Check(!EliteRotationRules.ShouldProtectElitePickup(false, false, true),
            "normal active item uses vanilla pickup");
        Check(!EliteRotationRules.ShouldProtectElitePickup(true, true, true),
            "pending swap must not intercept pickup");
        Check(!EliteRotationRules.ShouldProtectElitePickup(false, true, false),
            "extra-slot inventory remains vanilla");

        Check(EliteRotationRules.CanCommitSwap(true, true, true, true),
            "same run, master and equipment allows swap");
        Check(!EliteRotationRules.CanCommitSwap(false, true, true, true),
            "old run cannot commit");
        Check(!EliteRotationRules.CanCommitSwap(true, false, true, true),
            "another player's reserve cannot commit");
        Check(!EliteRotationRules.CanCommitSwap(true, true, false, true),
            "equipment changed during delay cannot commit");
    }

    private static int Main()
    {
        try
        {
            TestDistribution();
            TestAllowlist();
            TestRotation();
            Console.WriteLine("PASS: " + assertions + " assertions.");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            return 1;
        }
    }
}
