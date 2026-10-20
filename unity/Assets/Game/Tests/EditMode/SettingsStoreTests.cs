#nullable enable
using System;
using System.IO;
using NUnit.Framework;

namespace BlockPuzzle.Game.Tests
{
    public class SettingsStoreTests
    {
        string _folder = "";

        string SettingsPath => Path.Combine(_folder, "settings.json");

        [SetUp]
        public void CreateFolder()
        {
            _folder = Path.Combine(Path.GetTempPath(), "blockpuzzle-settings-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
        }

        [TearDown]
        public void DeleteFolder()
        {
            if (Directory.Exists(_folder)) Directory.Delete(_folder, recursive: true);
        }

        [Test]
        public void Settings_default_to_sound_music_and_haptics_on_and_reduced_motion_off()
        {
            var settings = new SettingsStore(SettingsPath).Load();

            Assert.IsTrue(settings.Sound);
            Assert.IsTrue(settings.Music);
            Assert.IsTrue(settings.Haptics);
            Assert.IsFalse(settings.ReducedMotion);
        }

        [Test]
        public void Saved_settings_load_back()
        {
            var store = new SettingsStore(SettingsPath);

            store.Save(new SettingsData { Sound = false, Music = true, Haptics = false, ReducedMotion = true });

            var loaded = store.Load();
            Assert.IsFalse(loaded.Sound);
            Assert.IsTrue(loaded.Music);
            Assert.IsFalse(loaded.Haptics);
            Assert.IsTrue(loaded.ReducedMotion);
        }

        [Test]
        public void A_corrupt_settings_file_gives_the_defaults()
        {
            File.WriteAllText(SettingsPath, "{ not json");

            var loaded = new SettingsStore(SettingsPath).Load();

            Assert.IsTrue(loaded.Sound);
            Assert.IsFalse(loaded.ReducedMotion);
        }

        [Test]
        public void A_settings_file_from_another_version_gives_the_defaults()
        {
            var store = new SettingsStore(SettingsPath);

            store.Save(new SettingsData { Version = SettingsData.CurrentVersion + 1, Sound = false });

            Assert.IsTrue(store.Load().Sound);
        }

        [Test]
        public void A_damaged_settings_file_falls_back_to_the_settings_before()
        {
            var store = new SettingsStore(SettingsPath);
            store.Save(new SettingsData { Music = false });
            store.Save(new SettingsData { Music = false, Haptics = false });

            File.WriteAllText(SettingsPath, "{ damaged");
            var loaded = store.Load();

            Assert.IsFalse(loaded.Music, "the backup holds the settings before the last change");
            Assert.IsTrue(loaded.Haptics);
        }

        [Test]
        public void A_failed_settings_save_does_not_throw()
        {
            Directory.CreateDirectory(SettingsPath);   // a folder where the file goes

            Assert.DoesNotThrow(() => new SettingsStore(SettingsPath).Save(new SettingsData { Sound = false }));
        }
    }
}
