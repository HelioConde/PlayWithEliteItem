using System.Collections.Generic;
using RoR2;

namespace GiveEliteItem
{
    /// <summary>Resolve eligible elite equipment for the active run.</summary>
    internal static class EliteAspectCatalog
    {
        internal static void CollectEligible(
            Run run,
            bool includeAllRegistered,
            string allowedEquipment,
            List<int> destination)
        {
            destination.Clear();
            if (run == null)
                return;

            if (!includeAllRegistered)
            {
                // These names are an explicit allowlist. Resolving them directly
                // avoids silently dropping classic aspects if their elite-buff
                // metadata is populated later than the equipment catalogue.
                foreach (string name in EliteEquipmentNames.Parse(allowedEquipment))
                {
                    EquipmentIndex index = EquipmentCatalog.FindEquipmentIndex(name);
                    if (index == EquipmentIndex.None)
                        continue;

                    EquipmentDef equipment = EquipmentCatalog.GetEquipmentDef(index);
                    if (equipment == null || !ExpansionIsEnabled(run, equipment))
                        continue;

                    destination.Add((int)index);
                }
                return;
            }

            // Opt-in discovery keeps the strict elite marker to avoid accidentally
            // including unrelated equipment from other mods or the game catalogue.
            for (int i = 0; i < EquipmentCatalog.equipmentCount; i++)
            {
                EquipmentDef equipment = EquipmentCatalog.GetEquipmentDef((EquipmentIndex)i);
                if (equipment == null || equipment.passiveBuffDef == null ||
                    !equipment.passiveBuffDef.isElite ||
                    !ExpansionIsEnabled(run, equipment))
                    continue;

                destination.Add(i);
            }
        }

        private static bool ExpansionIsEnabled(Run run, EquipmentDef equipment)
        {
            return equipment.requiredExpansion == null ||
                run.IsExpansionEnabled(equipment.requiredExpansion);
        }
    }
}
