using System;
using System.IO;
using System.Text;

namespace Game.Save
{
    /// <summary>
    /// Keeps the record in one file. A write goes fully into "&lt;file&gt;.tmp" first and only then replaces the
    /// file, so a crash mid-write leaves either the old or the new file, never a half one (rules-write-whole).
    /// Reading only looks at the main file.
    /// </summary>
    internal sealed class FileRecordStore : IRecordStore
    {
        private readonly string path;

        public FileRecordStore(string path)
        {
            this.path = path ?? throw new ArgumentNullException(nameof(path));
        }

        public string Path => path;
        public string TempPath => path + ".tmp";

        public bool TryLoad(out string text)
        {
            text = null;
            try
            {
                if (!File.Exists(path)) return false;
                text = File.ReadAllText(path, Encoding.UTF8);
                return true;
            }
            catch (Exception)
            {
                text = null;
                return false;
            }
        }

        public bool TrySave(string text, out string reason)
        {
            reason = null;
            try
            {
                var dir = System.IO.Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                var bytes = Encoding.UTF8.GetBytes(text);
                using (var stream = new FileStream(TempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }
                if (File.Exists(path)) File.Replace(TempPath, path, null);
                else File.Move(TempPath, path);
                return true;
            }
            catch (Exception e)
            {
                reason = e.GetType().Name + ": " + e.Message;
                try { if (File.Exists(TempPath)) File.Delete(TempPath); } catch (Exception) { }
                return false;
            }
        }
    }
}
