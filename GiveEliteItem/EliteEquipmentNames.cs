using System;
using System.Collections.Generic;

namespace GiveEliteItem
{
    /// <summary>Parse configuration without depending on RoR2/Unity.</summary>
    internal static class EliteEquipmentNames
    {
        internal static HashSet<string> Parse(string commaSeparatedNames)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(commaSeparatedNames))
                return result;

            foreach (string entry in commaSeparatedNames.Split(','))
            {
                string name = entry.Trim();
                if (name.Length > 0)
                    result.Add(name);
            }

            return result;
        }
    }
}
