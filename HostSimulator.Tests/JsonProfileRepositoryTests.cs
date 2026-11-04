using System;
using System.Collections.Generic;
using System.IO;
using HostSimulator.Domain;
using HostSimulator.Infrastructure.Profile;
using Xunit;

namespace HostSimulator.Tests
{
    public class JsonProfileRepositoryTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly string _filePath;

        public JsonProfileRepositoryTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "HostProfileTest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
            _filePath = Path.Combine(_tempDir, "profiles.json");
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }

        private EquipmentProfile CreateProfile(string name = "EQ01", string ip = "192.168.1.100", int port = 5000)
        {
            return new EquipmentProfile(name, ip, port, true, 0, 45, 10, 5, 10, 5, true, 10);
        }

        [Fact]
        public void LoadAll_returns_empty_when_file_missing()
        {
            var repo = new JsonProfileRepository(_filePath);
            var result = repo.LoadAll();
            Assert.Empty(result);
        }

        [Fact]
        public void Save_and_LoadAll_round_trips()
        {
            var repo = new JsonProfileRepository(_filePath);
            var profile = CreateProfile();

            repo.Save(profile);
            var loaded = repo.LoadAll();

            Assert.Single(loaded);
            Assert.Equal("EQ01", loaded[0].ProfileName);
            Assert.Equal("192.168.1.100", loaded[0].IpAddress);
            Assert.Equal(5000, loaded[0].Port);
            Assert.True(loaded[0].IsActive);
            Assert.Equal(45, loaded[0].T3);
            Assert.Equal(10, loaded[0].T5);
            Assert.Equal(5, loaded[0].T6);
            Assert.Equal(10, loaded[0].T7);
            Assert.Equal(5, loaded[0].T8);
            Assert.True(loaded[0].AutoReconnect);
            Assert.Equal(10, loaded[0].ReconnectIntervalSeconds);
        }

        [Fact]
        public void Save_updates_existing_profile_by_name()
        {
            var repo = new JsonProfileRepository(_filePath);
            repo.Save(CreateProfile("EQ01", "192.168.1.100", 5000));
            repo.Save(CreateProfile("EQ01", "10.0.0.1", 6000));

            var loaded = repo.LoadAll();
            Assert.Single(loaded);
            Assert.Equal("10.0.0.1", loaded[0].IpAddress);
            Assert.Equal(6000, loaded[0].Port);
        }

        [Fact]
        public void Multiple_profiles_are_isolated()
        {
            var repo = new JsonProfileRepository(_filePath);
            repo.Save(CreateProfile("EQ01", "192.168.1.100", 5000));
            repo.Save(CreateProfile("EQ02", "192.168.1.200", 5001));

            var loaded = repo.LoadAll();
            Assert.Equal(2, loaded.Count);
        }

        [Fact]
        public void LoadByName_returns_matching_profile()
        {
            var repo = new JsonProfileRepository(_filePath);
            repo.Save(CreateProfile("EQ01"));
            repo.Save(CreateProfile("EQ02", "10.0.0.1"));

            var found = repo.LoadByName("EQ02");
            Assert.Equal("10.0.0.1", found.IpAddress);
        }

        [Fact]
        public void LoadByName_throws_when_not_found()
        {
            var repo = new JsonProfileRepository(_filePath);
            Assert.Throws<KeyNotFoundException>(() => repo.LoadByName("MISSING"));
        }

        [Fact]
        public void LoadByName_is_case_insensitive()
        {
            var repo = new JsonProfileRepository(_filePath);
            repo.Save(CreateProfile("EQ01"));

            var found = repo.LoadByName("eq01");
            Assert.Equal("EQ01", found.ProfileName);
        }

        [Fact]
        public void SaveAll_replaces_all_profiles()
        {
            var repo = new JsonProfileRepository(_filePath);
            repo.Save(CreateProfile("EQ01"));
            repo.Save(CreateProfile("EQ02", "10.0.0.1"));

            repo.SaveAll(new[] { CreateProfile("EQ03", "172.16.0.1") });
            var loaded = repo.LoadAll();
            Assert.Single(loaded);
            Assert.Equal("EQ03", loaded[0].ProfileName);
        }

        [Fact]
        public void Invalid_json_profile_throws_validation_exception()
        {
            File.WriteAllText(_filePath, @"[{""profileName"":"""",""ipAddress"":""invalid"",""port"":0,""isActive"":true,""deviceId"":-1,""t3"":0,""t5"":0,""t6"":0,""t7"":0,""t8"":0,""reconnectIntervalSeconds"":0}]");
            var repo = new JsonProfileRepository(_filePath);

            Assert.Throws<ProfileValidationException>(() => repo.LoadAll());
        }

        [Fact]
        public void Missing_required_field_isActive_throws()
        {
            File.WriteAllText(_filePath, @"[{""profileName"":""EQ01"",""ipAddress"":""192.168.1.100"",""port"":5000,""deviceId"":0}]");
            var repo = new JsonProfileRepository(_filePath);

            Assert.Throws<ProfileValidationException>(() => repo.LoadAll());
        }

        [Fact]
        public void Missing_required_field_deviceId_throws()
        {
            File.WriteAllText(_filePath, @"[{""profileName"":""EQ01"",""ipAddress"":""192.168.1.100"",""port"":5000,""isActive"":true}]");
            var repo = new JsonProfileRepository(_filePath);

            Assert.Throws<ProfileValidationException>(() => repo.LoadAll());
        }

        [Fact]
        public void Atomic_save_does_not_leave_temp_file()
        {
            var repo = new JsonProfileRepository(_filePath);
            repo.Save(CreateProfile());

            Assert.False(File.Exists(_filePath + ".tmp"));
            Assert.True(File.Exists(_filePath));
        }

        [Fact]
        public void Atomic_save_preserves_existing_file_on_replace()
        {
            var repo = new JsonProfileRepository(_filePath);
            repo.Save(CreateProfile("EQ01", "192.168.1.100"));
            repo.Save(CreateProfile("EQ01", "10.0.0.1"));

            var loaded = repo.LoadAll();
            Assert.Single(loaded);
            Assert.Equal("10.0.0.1", loaded[0].IpAddress);
            Assert.False(File.Exists(_filePath + ".tmp"));
        }

        [Fact]
        public void Duplicate_profile_names_in_json_are_rejected()
        {
            File.WriteAllText(_filePath, @"[
                {""profileName"":""EQ01"",""ipAddress"":""192.168.1.100"",""port"":5000,""isActive"":true,""deviceId"":0},
                {""profileName"":""eq01"",""ipAddress"":""10.0.0.1"",""port"":5001,""isActive"":false,""deviceId"":1}
            ]");
            var repo = new JsonProfileRepository(_filePath);

            var ex = Assert.Throws<ProfileValidationException>(() => repo.LoadAll());
            Assert.Contains("Duplicate profile name", ex.Message);
        }

        [Fact]
        public void Null_json_array_item_throws_validation_exception()
        {
            File.WriteAllText(_filePath, "[null]");
            var repo = new JsonProfileRepository(_filePath);

            var ex = Assert.Throws<ProfileValidationException>(() => repo.LoadAll());
            Assert.Contains("null", ex.Message);
        }

        [Fact]
        public void Malformed_json_throws_validation_exception()
        {
            File.WriteAllText(_filePath, "[{]");
            var repo = new JsonProfileRepository(_filePath);

            Assert.Throws<ProfileValidationException>(() => repo.LoadAll());
        }

        [Fact]
        public void SaveAll_rejects_duplicate_names()
        {
            var repo = new JsonProfileRepository(_filePath);
            var profiles = new[]
            {
                CreateProfile("EQ01", "192.168.1.100"),
                CreateProfile("eq01", "10.0.0.1")
            };

            Assert.Throws<ProfileValidationException>(() => repo.SaveAll(profiles));
        }
    }
}
