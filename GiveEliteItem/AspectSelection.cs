using System;
using System.Collections.Generic;

namespace GiveEliteItem
{
    /// <summary>
    /// Pure selection logic; intentionally independent of Unity and Risk of Rain 2.
    /// </summary>
    internal static class AspectSelection
    {
        internal static int Choose(
            IReadOnlyList<int> available,
            ISet<int> alreadyAssigned,
            Random random,
            bool preferUnique)
        {
            if (available == null || available.Count == 0)
                throw new ArgumentException("At least one aspect is required.", nameof(available));
            if (alreadyAssigned == null)
                throw new ArgumentNullException(nameof(alreadyAssigned));
            if (random == null)
                throw new ArgumentNullException(nameof(random));

            int candidateCount = 0;
            if (preferUnique)
            {
                for (int i = 0; i < available.Count; i++)
                {
                    if (!alreadyAssigned.Contains(available[i]))
                        candidateCount++;
                }
            }

            // Once every aspect has been assigned, all entries become valid again.
            bool onlyUnused = preferUnique && candidateCount > 0;
            if (!onlyUnused)
                candidateCount = available.Count;

            int chosenOrdinal = random.Next(candidateCount);
            for (int i = 0; i < available.Count; i++)
            {
                if (onlyUnused && alreadyAssigned.Contains(available[i]))
                    continue;

                if (chosenOrdinal-- == 0)
                    return available[i];
            }

            throw new InvalidOperationException("Aspect selection failed unexpectedly.");
        }
    }
}
