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

        /// <summary>
        /// The save, or the one before it when the save is missing, unreadable or from another version. False when
        /// neither will do. Never throws because of what the files contain.
        /// </summary>
        public bool TryRead(out SaveData data)
        {
            if (!JsonFile.TryReadOrBackup(_path, read => read.Version == SaveData.CurrentVersion, out data)) return false;
            if (data.Game == null) data.Game = new GameSnapshot();
            return true;
        }
    }
}
