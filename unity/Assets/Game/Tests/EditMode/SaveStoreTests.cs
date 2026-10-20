#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using BlockPuzzle.Core;
using NUnit.Framework;

namespace BlockPuzzle.Game.Tests
{
    public class SaveStoreTests
    {
        string _folder = "";

        string SavePath => Path.Combine(_folder, "save.json");

        [SetUp]
        public void CreateFolder()
        {
            _folder = Path.Combine(Path.GetTempPath(), "blockpuzzle-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
        }

        [TearDown]
        public void DeleteFolder()
        {
            if (Directory.Exists(_folder)) Directory.Delete(_folder, recursive: true);
        }

        static SaveData Sample() => new SaveData
        {
            HasGame = true,
            Game = new GameSnapshot
            {
                Board = 0x8000_0000_0000_0001UL,
                Tray = new[] { "o3", "", "i5h" },
                Score = 140,
                Placements = 12,
                LinesCleared = 2,
                RngState = 0xFEDC_BA98_7654_3210UL,
                RngIncrement = 0xFFFF_FFFF_FFFF_FFFFUL,
                GameIndex = 3,
                RecentQuickLosses = 1,
            },
            NextGameIndex = 4,
            NextRecentQuickLosses = 2,
            BestScore = 900,
        };

        [Test]
        public void A_save_reads_back_the_same_data()
        {
            var store = new SaveStore(SavePath);

            store.Write(Sample());

            Assert.IsTrue(store.TryRead(out var data));
            Assert.AreEqual(SaveData.CurrentVersion, data.Version);
            Assert.IsTrue(data.HasGame);
            Assert.AreEqual(0x8000_0000_0000_0001UL, data.Game.Board);
            CollectionAssert.AreEqual(new[] { "o3", "", "i5h" }, data.Game.Tray);
            Assert.AreEqual(140, data.Game.Score);
            Assert.AreEqual(12, data.Game.Placements);
            Assert.AreEqual(0xFEDC_BA98_7654_3210UL, data.Game.RngState);
            Assert.AreEqual(0xFFFF_FFFF_FFFF_FFFFUL, data.Game.RngIncrement);
            Assert.AreEqual(3, data.Game.GameIndex);
            Assert.AreEqual(4, data.NextGameIndex);
            Assert.AreEqual(2, data.NextRecentQuickLosses);
            Assert.AreEqual(900, data.BestScore);
        }

        [Test]
        public void A_missing_file_reads_as_no_save()
        {
            Assert.IsFalse(new SaveStore(SavePath).TryRead(out _));
        }

        [Test]
        public void A_corrupt_or_truncated_file_reads_as_no_save()
        {
            var store = new SaveStore(SavePath);

            File.WriteAllText(SavePath, "{");
            Assert.IsFalse(store.TryRead(out _));

            store.Write(Sample());
            string json = File.ReadAllText(SavePath);
            File.WriteAllText(SavePath, json.Substring(0, json.Length / 2));
            Assert.IsFalse(store.TryRead(out _));
        }

        [Test]
        public void A_save_from_another_version_reads_as_no_save()
        {
            var store = new SaveStore(SavePath);
            var data = Sample();
            data.Version = SaveData.CurrentVersion + 1;

            store.Write(data);

            Assert.IsFalse(store.TryRead(out _));
        }

        [Test]
        public void A_leftover_temp_file_is_ignored_and_overwritten()
        {
            File.WriteAllText(SavePath + ".tmp", "half-written junk from a killed app");
            var store = new SaveStore(SavePath);

            store.Write(Sample());

            Assert.IsTrue(store.TryRead(out var data));
            Assert.AreEqual(900, data.BestScore);
            Assert.IsFalse(File.Exists(SavePath + ".tmp"));
        }

        [Test]
        public void Write_replaces_the_previous_save()
        {
            var store = new SaveStore(SavePath);
            store.Write(Sample());
            var newer = Sample();
            newer.BestScore = 1200;

            store.Write(newer);

            Assert.IsTrue(store.TryRead(out var data));
            Assert.AreEqual(1200, data.BestScore);
        }

        static SaveData WithBest(int best)
        {
            var data = Sample();
            data.BestScore = best;
            return data;
        }

        [Test]
        public void A_damaged_save_falls_back_to_the_one_before()
        {
            var store = new SaveStore(SavePath);
            store.Write(WithBest(100));
            store.Write(WithBest(200));

            File.WriteAllText(SavePath, "{ damaged");

            Assert.IsTrue(store.TryRead(out var data));
            Assert.AreEqual(100, data.BestScore, "the backup holds the save before the last one");
        }

        [Test]
        public void A_save_killed_between_renames_reads_the_backup()
        {
            var store = new SaveStore(SavePath);
            store.Write(WithBest(100));
            store.Write(WithBest(200));

            File.Delete(SavePath);   // a replace moves the old file to the backup before it moves the new one in

            Assert.IsTrue(store.TryRead(out var data));
            Assert.AreEqual(100, data.BestScore);
        }

        [Test]
        public void A_backup_from_another_version_is_ignored()
        {
            var old = WithBest(100);
            old.Version = SaveData.CurrentVersion + 1;
            JsonFile.Write(SavePath + ".bak", old);
            File.WriteAllText(SavePath, "{ damaged");

            Assert.IsFalse(new SaveStore(SavePath).TryRead(out _));
        }

        [Test]
        public void An_old_save_without_daily_data_loads_with_an_empty_daily()
        {
            File.WriteAllText(SavePath, "{\"Version\":1,\"HasGame\":false,\"NextGameIndex\":3,\"BestScore\":250}");

            Assert.IsTrue(new SaveStore(SavePath).TryRead(out var data));

            Assert.AreEqual(250, data.BestScore);
            Assert.IsFalse(data.Daily.HasGame);
            Assert.AreEqual(-1, data.Daily.Record.LastDay);
            Assert.AreEqual(0, data.Daily.Record.Streak);
        }

        [Test]
        public void Write_creates_the_folder_if_needed()
        {
            var store = new SaveStore(Path.Combine(_folder, "nested", "save.json"));

            store.Write(Sample());

            Assert.IsTrue(store.TryRead(out _));
        }

        [Test]
        public void New_seeds_differ()
        {
            var seeds = new HashSet<ulong>();
            for (int i = 0; i < 100; i++) seeds.Add(SeedSource.NewSeed());

            Assert.AreEqual(100, seeds.Count);
        }
    }
}
