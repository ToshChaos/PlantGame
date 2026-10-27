using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PlantGame.Core
{
    /// <summary>
    /// Contract 5.5: JSON in Application.persistentDataPath/save.json. Best times per key, last seed code.
    /// Tolerant to a missing or corrupt file (starts empty); writes atomically (temp file, then move). T25 extends it (audio settings).
    /// </summary>
    public static class SaveService
    {
        [Serializable] class Entry { public string key; public int mode; /* legacy: 0 = the plain run, 1 = the removed countdown mode (ignored) */ public float seconds; }
        [Serializable] class Data { public int version = 1; public List<Entry> times = new List<Entry>(); public string lastSeedCode; }

        public static string PathOverride;   // tests
        static string FilePath => PathOverride ?? Path.Combine(Application.persistentDataPath, "save.json");

        static Data Load()
        {
            try { if (File.Exists(FilePath)) { var d = JsonUtility.FromJson<Data>(File.ReadAllText(FilePath)); if (d != null && d.times != null) return d; } }
            catch (Exception e) { Debug.LogWarning("[PlantGame] save.json unreadable, starting fresh: " + e.Message); }
            return new Data();
        }

        static void Save(Data d)
        {
            try
            {
                var tmp = FilePath + ".tmp";
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                File.WriteAllText(tmp, JsonUtility.ToJson(d, true));
                if (File.Exists(FilePath)) File.Delete(FilePath);
                File.Move(tmp, FilePath);
            }
            catch (Exception e) { Debug.LogWarning("[PlantGame] could not write save.json: " + e.Message); }
        }

        /// <summary>Stores `seconds` if it beats the saved time (or none exists). Returns true for a new best.</summary>
        public static bool TrySubmitTime(string key, float seconds)
        {
            var d = Load();
            var e = d.times.Find(x => x.key == key && x.mode == 0);
            if (e != null && e.seconds <= seconds) return false;
            if (e == null) d.times.Add(new Entry { key = key, mode = 0, seconds = seconds }); else e.seconds = seconds;
            Save(d);
            return true;
        }

        public static float? BestTime(string key)
        {
            var e = Load().times.Find(x => x.key == key && x.mode == 0);
            return e != null ? e.seconds : (float?)null;
        }

        /// <summary>Forgets every saved best time (the seed code is kept).</summary>
        public static void ResetTimes() { var d = Load(); d.times.Clear(); Save(d); }

        public static void SetLastSeedCode(string c) { var d = Load(); d.lastSeedCode = c; Save(d); }
    }
}
