#nullable enable
using System;
using System.IO;
using UnityEngine;

namespace BlockPuzzle.Game
{
    /// <summary>
    /// The settings file, kept apart from the game save so a damaged save never resets the settings or the other way
    /// round. A missing, bad or other-version file gives the defaults; a failed write is logged, not thrown.
    /// </summary>
    public sealed class SettingsStore
    {
        readonly string _path;

        public SettingsStore(string path)
        {
            _path = path;
        }

        public const string FileName = "settings.json";

        public static string DefaultPath => Path.Combine(Application.persistentDataPath, FileName);

        /// <summary>The settings, or the ones before the last change when the file is damaged; the defaults when neither reads.</summary>
        public SettingsData Load() =>
            JsonFile.TryReadOrBackup(_path, read => read.Version == SettingsData.CurrentVersion, out SettingsData data) ? data : new SettingsData();

        public void Save(SettingsData settings)
        {
            try
            {
                JsonFile.Write(_path, settings);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogWarning("Couldn't save the settings: " + e.Message);
            }
        }
    }
}
