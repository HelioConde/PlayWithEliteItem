using System.Collections.Generic;
using RoR2;

namespace GiveEliteItem
{
    /// <summary>Find equipment with elite passives that this run can actually use.</summary>
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

            var allowlist = EliteEquipmentNames.Parse(allowedEquipment);

            for (int i = 0; i < EquipmentCatalog.equipmentCount; i++)
            {
                EquipmentDef equipment = EquipmentCatalog.GetEquipmentDef((EquipmentIndex)i);

                if (equipment == null || equipment.passiveBuffDef == null ||
                    !equipment.passiveBuffDef.isElite)
                    continue;

                // Registration in EquipmentCatalog does not guarantee the DLC is enabled.
                if (equipment.requiredExpansion != null &&
                    !run.IsExpansionEnabled(equipment.requiredExpansion))
                    continue;

                if (!includeAllRegistered && !allowlist.Contains(equipment.name))
                    continue;

                destination.Add(i);
            }
        }
    }
}
