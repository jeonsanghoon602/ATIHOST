using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using HostSimulator.Domain;
using Newtonsoft.Json;

namespace HostSimulator.Infrastructure.Profile
{
    public sealed class JsonProfileRepository : IProfileRepository
    {
        private readonly string _filePath;

        public JsonProfileRepository(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path must not be null or empty.", nameof(filePath));
            _filePath = filePath;
        }

        public IReadOnlyList<EquipmentProfile> LoadAll()
        {
            if (!File.Exists(_filePath))
                return Array.Empty<EquipmentProfile>();

            var json = File.ReadAllText(_filePath, Encoding.UTF8);

            List<ProfileDto> dtos;
            try
            {
                dtos = JsonConvert.DeserializeObject<List<ProfileDto>>(json);
            }
            catch (JsonSerializationException ex)
            {
                throw new ProfileValidationException(new[] { "Required JSON property missing: " + ex.Message });
            }

            if (dtos == null)
                return Array.Empty<EquipmentProfile>();

            return dtos.Select(ToProfile).ToList().AsReadOnly();
        }

        public EquipmentProfile LoadByName(string profileName)
        {
            var all = LoadAll();
            var match = all.FirstOrDefault(p =>
                string.Equals(p.ProfileName, profileName, StringComparison.OrdinalIgnoreCase));

            if (match == null)
                throw new KeyNotFoundException($"Profile not found: {profileName}");

            return match;
        }

        public void Save(EquipmentProfile profile)
        {
            var all = LoadAll().ToList();
            var index = all.FindIndex(p =>
                string.Equals(p.ProfileName, profile.ProfileName, StringComparison.OrdinalIgnoreCase));

            if (index >= 0)
                all[index] = profile;
            else
                all.Add(profile);

            SaveAll(all);
        }

        public void SaveAll(IEnumerable<EquipmentProfile> profiles)
        {
            var dtos = profiles.Select(ToDto).ToList();
            var json = JsonConvert.SerializeObject(dtos, Formatting.Indented);

            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var tempPath = _filePath + ".tmp";
            File.WriteAllText(tempPath, json, Encoding.UTF8);
            if (File.Exists(_filePath))
                File.Delete(_filePath);
            File.Move(tempPath, _filePath);
        }

        private static EquipmentProfile ToProfile(ProfileDto dto)
        {
            return new EquipmentProfile(
                dto.ProfileName,
                dto.IpAddress,
                dto.Port,
                dto.IsActive,
                dto.DeviceId,
                dto.T3,
                dto.T5,
                dto.T6,
                dto.T7,
                dto.T8,
                dto.AutoReconnect,
                dto.ReconnectIntervalSeconds);
        }

        private static ProfileDto ToDto(EquipmentProfile profile)
        {
            return new ProfileDto
            {
                ProfileName = profile.ProfileName,
                IpAddress = profile.IpAddress,
                Port = profile.Port,
                IsActive = profile.IsActive,
                DeviceId = profile.DeviceId,
                T3 = profile.T3,
                T5 = profile.T5,
                T6 = profile.T6,
                T7 = profile.T7,
                T8 = profile.T8,
                AutoReconnect = profile.AutoReconnect,
                ReconnectIntervalSeconds = profile.ReconnectIntervalSeconds
            };
        }
    }
}
