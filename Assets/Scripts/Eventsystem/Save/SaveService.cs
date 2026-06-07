using System.IO;
using UnityEngine;

namespace StoryEvents
{
    /// <summary>
    /// Reads and writes EventSaveData to a JSON file in Application.persistentDataPath.
    /// Static utility — no MonoBehaviour needed.
    /// </summary>
    public static class SaveService
    {
        private const string FileName = "Motel_events.json";

        private static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        /// <summary>
        /// Write save data to disk. Call on every choice and on application pause/quit.
        /// </summary>
        public static void Save(EventSaveData data)
        {
            try
            {
                string json = JsonUtility.ToJson(data, prettyPrint: true);
                File.WriteAllText(FilePath, json);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[CK2Events] Save failed: {e.Message}");
            }
        }

        /// <summary>
        /// Load save data from disk. Returns a fresh EventSaveData if no file exists.
        /// </summary>
        public static EventSaveData Load()
        {
            if (!File.Exists(FilePath))
                return new EventSaveData();

            try
            {
                string json = File.ReadAllText(FilePath);
                return JsonUtility.FromJson<EventSaveData>(json) ?? new EventSaveData();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[CK2Events] Load failed: {e.Message}");
                return new EventSaveData();
            }
        }

        /// <summary>
        /// Delete save data. Call on new game.
        /// </summary>
        public static void DeleteSave()
        {
            if (File.Exists(FilePath))
                File.Delete(FilePath);
        }

        /// <summary>
        /// Returns true if a save file exists.
        /// </summary>
        public static bool SaveExists() => File.Exists(FilePath);
    }
}
