using System;
using System.Collections.Generic;
using System.IO;

namespace Oddyseus.Core
{
    // Progres meta lintas run (Pack ditemukan, boss dikalahkan, achievement). Disimpan sebagai teks "key=value".
    // Tanpa UnityEngine: SavePath diisi dari luar (Application.persistentDataPath).
    public static class MetaProgress
    {
        public static string SavePath;
        static readonly Dictionary<string, int> data = new Dictionary<string, int>();
        static bool loaded;

        public static void Load()
        {
            data.Clear();
            loaded = true;
            if (string.IsNullOrEmpty(SavePath) || !File.Exists(SavePath)) return;
            try
            {
                var lines = File.ReadAllLines(SavePath);
                for (int i = 0; i < lines.Length; i++)
                {
                    int eq = lines[i].IndexOf('=');
                    if (eq <= 0) continue;
                    int v;
                    if (int.TryParse(lines[i].Substring(eq + 1), out v)) data[lines[i].Substring(0, eq)] = v;
                }
            }
            catch (Exception) { }
        }

        static void EnsureLoaded() { if (!loaded) Load(); }

        public static void Save()
        {
            if (string.IsNullOrEmpty(SavePath)) return;
            try
            {
                var lines = new List<string>();
                foreach (var kv in data) lines.Add(kv.Key + "=" + kv.Value);
                File.WriteAllLines(SavePath, lines.ToArray());
            }
            catch (Exception) { }
        }

        public static int Get(string key)
        {
            EnsureLoaded();
            int v;
            return data.TryGetValue(key, out v) ? v : 0;
        }

        public static void Set(string key, int value)
        {
            EnsureLoaded();
            if (Get(key) == value) return;
            data[key] = value;
            Save();
        }

        public static void Add(string key, int n = 1) { Set(key, Get(key) + n); }

        public static void ResetAll()
        {
            EnsureLoaded();
            data.Clear();
            Save();
        }

        // ---------- Pack ----------

        public static bool IsPackCleared(int id) { return Get("pack.cleared." + id) > 0; }
        public static bool IsPackSeen(int id) { return Get("pack.seen." + id) > 0 || IsPackCleared(id); }
        public static void MarkPackSeen(int id) { Set("pack.seen." + id, 1); }
        public static void MarkPackCleared(int id) { Set("pack.seen." + id, 1); Set("pack.cleared." + id, 1); }

        public static int PackSeenCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < PackCatalog.All.Length; i++) if (IsPackSeen(PackCatalog.All[i].Id)) n++;
                return n;
            }
        }

        public static int PackClearedCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < PackCatalog.All.Length; i++) if (IsPackCleared(PackCatalog.All[i].Id)) n++;
                return n;
            }
        }

        // ---------- Achievement ----------

        public static bool IsUnlocked(string achId) { return Get("ach." + achId) > 0; }
        public static void MarkUnlocked(string achId) { Set("ach." + achId, 1); }
    }
}