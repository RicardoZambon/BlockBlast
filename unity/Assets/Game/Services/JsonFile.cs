#nullable enable
using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace BlockPuzzle.Game
{
    /// <summary>
    /// JSON files that survive the app being killed: a write goes to a temporary file, is flushed to disk and then
    /// replaces the old file, so a reader sees the previous version or the new one, never half of one. The previous
    /// version is kept as a backup, so a damaged file costs one write at most.
    /// </summary>
    public static class JsonFile
    {
        /// <summary>Where Write keeps the previous version of a file.</summary>
        public static string BackupPath(string path) => path + ".bak";

        public static void Write<T>(string path, T data)
        {
            string? folder = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);

            string temp = path + ".tmp";
            byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(data));
            using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(flushToDisk: true);
            }

            // On Android this renames the old file to the backup, then the new one into place. A kill between the two
            // leaves only the backup, which TryReadOrBackup reads.
            if (File.Exists(path)) File.Replace(temp, path, BackupPath(path));
            else File.Move(temp, path);
        }

        /// <summary>
        /// The file if it reads and passes accept, otherwise its backup on the same terms. False when neither does.
        /// Never throws because of what the files contain.
        /// </summary>
        public static bool TryReadOrBackup<T>(string path, Predicate<T> accept, out T data) where T : class, new()
        {
            if (TryRead(path, out data) && accept(data)) return true;
            if (TryRead(BackupPath(path), out data) && accept(data)) return true;
            data = new T();
            return false;
        }

        /// <summary>False for a missing file, bad JSON or a file that can't be read. Never throws because of what the file contains.</summary>
        public static bool TryRead<T>(string path, out T data) where T : class, new()
        {
            data = new T();
            try
            {
                if (!File.Exists(path)) return false;
                var read = JsonUtility.FromJson<T>(File.ReadAllText(path));
                if (read == null) return false;
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
