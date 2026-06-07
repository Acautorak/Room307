using System;
using System.IO;
using UnityEngine;
using MotelSimulator.Data;

namespace MotelSimulator.Save
{
    public class SaveSystem : MonoBehaviour
    {
        public static SaveSystem Instance { get; private set; }

        private const string SAVE_FILE_NAME = "motel_save.json";
        private string SavePath => Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);

        public MotelSaveData CurrentData { get; private set; }

        public event Action OnDataLoaded;
        public event Action OnDataSaved;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadOrCreate();
        }

        // ── Public API ──────────────────────────────────────────────────────────

        public void Save()
        {
            CurrentData.lastSaveTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string json = JsonUtility.ToJson(CurrentData, prettyPrint: true);
            File.WriteAllText(SavePath, json);
            Debug.Log($"[SaveSystem] Saved to {SavePath}");
            OnDataSaved?.Invoke();
        }

        public void Load()
        {
            if (!File.Exists(SavePath)) { CreateNew(); return; }
            string json = File.ReadAllText(SavePath);
            CurrentData = JsonUtility.FromJson<MotelSaveData>(json);
            Debug.Log("[SaveSystem] Loaded save data.");
            OnDataLoaded?.Invoke();
        }

        public void DeleteSave()
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);
            CreateNew();
        }

        /// <summary>Upserts an apartment in the current data then auto-saves.</summary>
        public void SaveApartment(ApartmentSaveData apartment)
        {
            int idx = CurrentData.apartments.FindIndex(a => a.apartmentId == apartment.apartmentId);
            if (idx >= 0) CurrentData.apartments[idx] = apartment;
            else CurrentData.apartments.Add(apartment);
            Save();
        }

        public ApartmentSaveData GetApartment(string id)
            => CurrentData.apartments.Find(a => a.apartmentId == id);

        // ── Internal ────────────────────────────────────────────────────────────

        private void LoadOrCreate()
        {
            if (File.Exists(SavePath)) Load();
            else CreateNew();
        }

        private void CreateNew()
        {
            CurrentData = new MotelSaveData();
            // Seed with some default apartments so the motel isn't empty
            for (int floor = 1; floor <= 3; floor++)
            {
                for (int unit = 1; unit <= 4; unit++)
                {
                    CurrentData.apartments.Add(new ApartmentSaveData
                    {
                        apartmentId = $"{floor}-{unit}",
                        apartmentName = $"Apt {floor}{unit:00}",
                        floorNumber = floor,
                        unitNumber = unit
                    });
                }
            }
            Save();
            OnDataLoaded?.Invoke();
        }
    }
}
