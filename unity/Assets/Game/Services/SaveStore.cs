#nullable enable
using System.IO;
using BlockPuzzle.Core;
using UnityEngine;

namespace BlockPuzzle.Game
{
    /// <summary>The game save, a JsonFile, so an app killed mid-write leaves the previous save, never a truncated one.</summary>
    public sealed class SaveStore
    {
        readonly string _path;

        public SaveStore(string path)
        {
            _path = path;
        }

        public const string FileName = "save.json";

        public static string DefaultPath => Path.Combine(Application.persistentDataPath, FileName);

        public void Write(SaveData data) => JsonFile.Write(_path, data);

        /// <summary>False for a missing, unreadable or other-version file. Never throws because of what the file contains.</summary>
        public bool TryRead(out SaveData data)
        {
            data = new SaveData();
            if (!JsonFile.TryRead(_path, out SaveData read) || read.Version != SaveData.CurrentVersion) return false;
            if (read.Game == null) read.Game = new GameSnapshot();
            data = read;
            return true;
        }
    }
}
