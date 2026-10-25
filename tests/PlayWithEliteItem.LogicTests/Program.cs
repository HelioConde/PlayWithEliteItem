using System;
using System.Collections.Generic;
using GiveEliteItem;

internal static class Program
{
    private static int assertions;

    private static void Check(bool condition, string message)
    {
        assertions++;
        if (!condition)
            throw new Exception("TEST FAILED: " + message);
    }

    private static void ExpectException<T>(Action action, string message) where T : Exception
    {
        bool thrown = false;
        try { action(); }
        catch (T) { thrown = true; }
        Check(thrown, message);
    }

    private static int Main()
    {
        try
        {
            var random = new Random(1234);
            var pool = new List<int> { 11, 21, 31 };
            var assigned = new HashSet<int>();

            // Different aspects are chosen until the initial pool is exhausted.
            for (int i = 0; i < pool.Count; i++)
            {
                int chosen = AspectSelection.Choose(pool, assigned, random, true);
                Check(pool.Contains(chosen), "selection belongs to the pool");
                Check(assigned.Add(chosen), "unique until exhaustion");
            }

            Check(assigned.Count == pool.Count, "all available aspects distributed");

            // A full room (more players than aspects) must allow repetition, not hang.
            for (int i = 0; i < 100; i++)
                Check(pool.Contains(AspectSelection.Choose(pool, assigned, random, true)),
                    "repeat is allowed after exhaustion");

            // A disabled unique preference never restricts the candidates.
            var singleAlreadyUsed = new HashSet<int> { 11 };
            for (int i = 0; i < 100; i++)
                Check(pool.Contains(AspectSelection.Choose(pool, singleAlreadyUsed, random, false)),
                    "all entries remain eligible when uniqueness is disabled");

            // A new run can reset the tracking state.
            assigned.Clear();
            Check(pool.Contains(AspectSelection.Choose(pool, assigned, random, true)),
                "new run can draw from a cleared pool");

            // Supports a pool of exactly one elite aspect.
            for (int i = 0; i < 100; i++)
                Check(AspectSelection.Choose(new[] { 9 }, new HashSet<int> { 9 }, random, true) == 9,
                    "single-entry pool never hangs");

            // No empty-pool IndexOutOfRange / endless loops; invalid arguments fail fast.
            ExpectException<ArgumentException>(
                () => AspectSelection.Choose(Array.Empty<int>(), assigned, random, true),
                "empty list must fail with a readable error");
            ExpectException<ArgumentNullException>(
                () => AspectSelection.Choose(pool, null, random, true),
                "null already-assigned set must fail");
            ExpectException<ArgumentNullException>(
                () => AspectSelection.Choose(pool, assigned, null, true),
                "null random source must fail");

            Console.WriteLine("PASS: " + assertions + " assertions.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }
}
