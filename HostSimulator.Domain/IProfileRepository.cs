using System.Collections.Generic;

namespace HostSimulator.Domain
{
    public interface IProfileRepository
    {
        IReadOnlyList<EquipmentProfile> LoadAll();
        EquipmentProfile LoadByName(string profileName);
        void Save(EquipmentProfile profile);
        void SaveAll(IEnumerable<EquipmentProfile> profiles);
    }
}
