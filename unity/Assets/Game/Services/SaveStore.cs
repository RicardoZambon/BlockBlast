#nullable enable
using System;
using System.IO;
using System.Text;
using BlockPuzzle.Core;
using UnityEngine;

namespace BlockPuzzle.Game
{
    /// <summary>
    /// The save file, as JSON. A write goes to a temporary file that then replaces the save, so an app killed
    /// mid-write leaves the previous save, never a truncated one.
    /// </summary>
    public sealed class SaveStore
    {
        readonly string _path;

        public SaveStore(string path)
        {
            _path = path;
        }

        public static string DefaultPath => Path.Combine(Application.persistentDataPath, "save.json");

        public void Write(SaveData data)
        {
            string? folder = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);

            string temp = _path + ".tmp";
            byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(data));
            using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(flushToDisk: true);
            }

            if (File.Exists(_path)) File.Replace(temp, _path, null);
            else File.Move(temp, _path);
        }

        /// <summary>False for a missing, unreadable or other-version file. Never throws because of what the file contains.</summary>
        public bool TryRead(out SaveData data)
        {
            data = new SaveData();
            try
            {
                if (!File.Exists(_path)) return false;
                var read = JsonUtility.FromJson<SaveData>(File.ReadAllText(_path));
                if (read == null || read.Version != SaveData.CurrentVersion) return false;
                if (read.Game == null) read.Game = new GameSnapshot();
                data = read;
                return true;
            }
            catch (Exception e) when (e is ArgumentException || e is IOException || e is UnauthorizedAccessException)
            {
                return false;
            }
        }
    }
}
