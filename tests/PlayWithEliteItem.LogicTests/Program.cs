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

    private static void TestPermanentPhysicalSlot()
    {
        Check(EliteSlotPolicy.CanAssign(true, true, false, false),
            "spawned player receives elite in empty physical slot 1");
        Check(!EliteSlotPolicy.CanAssign(false, true, false, false),
            "must wait for player body");
        Check(!EliteSlotPolicy.CanAssign(true, false, false, false),
            "must not overwrite another secondary equipment");
        Check(!EliteSlotPolicy.CanAssign(true, true, true, false),
            "must not assign twice in the same run");
        Check(!EliteSlotPolicy.CanAssign(true, true, false, true),
            "must not replace the currently active equipment location");

        Check(EliteSlotPolicy.IsEliteStillInPlace(22, 22),
            "elite stays in slot 1 when Q changes");
        Check(!EliteSlotPolicy.IsEliteStillInPlace(22, 9),
            "unexpected replacement of elite is detectable");
    }

    private static int Main()
    {
        try
        {
            TestDistribution();
            TestAllowlist();
            TestPermanentPhysicalSlot();
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
