using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace OddyseusJourneysEnd.Save
{
    /// <summary>
    /// Implementasikan di sistem apa pun yang perlu ikut tersimpan (Party, Gold, progres map, dll).
    /// Daftarkan di OnEnable: SaveSystem.Register(this); lepas di OnDisable: SaveSystem.Unregister(this);
    /// </summary>
    public interface ISaveable
    {
        string SaveKey { get; }          // unik per sistem, mis. "party", "gold"
        string CaptureState();           // kembalikan JSON (JsonUtility.ToJson)
        void RestoreState(string json);  // terima JSON yang sama saat load
        void ResetState();               // kembali ke kondisi awal (dipanggil saat Restart)
    }

    [Serializable]
    public class SaveEntry
    {
        public string key;
        public string json;
    }

    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public string savedAt;
        public string sceneName;
        public List<SaveEntry> entries = new List<SaveEntry>();
    }

    /// <summary>Menulis / membaca satu file save JSON di persistentDataPath.</summary>
    public static class SaveSystem
    {
        private static readonly List<ISaveable> Saveables = new List<ISaveable>();

        public static string FilePath => Path.Combine(Application.persistentDataPath, "savegame.json");

        // ---------- Registrasi ----------
        public static void Register(ISaveable saveable)
        {
            if (saveable != null && !Saveables.Contains(saveable)) Saveables.Add(saveable);
        }

        public static void Unregister(ISaveable saveable) => Saveables.Remove(saveable);

        // ---------- Save / Load ----------
        public static bool HasSave() => File.Exists(FilePath);

        public static bool Save(string sceneName)
        {
            try
            {
                var data = new SaveData
                {
                    savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    sceneName = sceneName,
                };

                foreach (var s in Saveables.ToArray())
                    data.entries.Add(new SaveEntry { key = s.SaveKey, json = s.CaptureState() });

                // Tulis ke file sementara dulu supaya save lama tidak rusak kalau gagal di tengah jalan
                string tmp = FilePath + ".tmp";
                File.WriteAllText(tmp, JsonUtility.ToJson(data, true));
                if (File.Exists(FilePath)) File.Delete(FilePath);
                File.Move(tmp, FilePath);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Save] Gagal menyimpan: {e.Message}");
                return false;
            }
        }

        public static bool TryLoad(out SaveData data)
        {
            data = null;
            if (!HasSave()) return false;

            try
            {
                data = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
                return data != null;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Save] Gagal membaca save: {e.Message}");
                return false;
            }
        }

        /// <summary>Kirim data save ke setiap ISaveable yang terdaftar.</summary>
        public static void Apply(SaveData data)
        {
            foreach (var s in Saveables.ToArray())
            {
                var entry = data.entries.Find(e => e.key == s.SaveKey);
                if (entry != null) s.RestoreState(entry.json);
            }
        }

        public static void Delete()
        {
            if (File.Exists(FilePath)) File.Delete(FilePath);
        }

        public static void ResetAll()
        {
            foreach (var s in Saveables.ToArray()) s.ResetState();
        }
    }

    /// <summary>
    /// Mencari ScriptableObject berdasarkan nama asset (dipakai saat load save).
    /// Asset (CharacterData, MoveData, ...) HARUS berada di dalam folder bernama "Resources",
    /// dan nama asset harus unik per tipe.
    /// </summary>
    public static class AssetLookup
    {
        private static readonly Dictionary<Type, Dictionary<string, UnityEngine.Object>> Cache =
            new Dictionary<Type, Dictionary<string, UnityEngine.Object>>();

        public static T Find<T>(string id) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(id)) return null;

            if (!Cache.TryGetValue(typeof(T), out var map))
            {
                map = new Dictionary<string, UnityEngine.Object>();
                foreach (var asset in Resources.LoadAll<T>(""))
                    map[asset.name] = asset;
                Cache[typeof(T)] = map;
            }

            return map.TryGetValue(id, out var found) ? (T)found : null;
        }
    }
}
