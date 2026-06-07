using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MotelSimulator.Data;
using MotelSimulator.Save;
using MotelSimulator.Blueprint;

namespace MotelSimulator.UI
{
    /// <summary>
    /// The main motel overview / lobby screen.
    /// Shows a grid of apartments organised by floor.
    /// Attach to the LobbyPanel root GameObject.
    /// </summary>
    public class MotelLobbyManager : MonoBehaviour
    {
        [Header("References")]
        public Transform floorContainer;         // ScrollRect content root
        public GameObject floorRowPrefab;        // horizontal row with a label
        public GameObject apartmentCardPrefab;   // the card button
        public BlueprintManager blueprintManager;

        [Header("Motel Info")]
        public TextMeshProUGUI motelNameLabel;

        private List<ApartmentCard> _cards = new List<ApartmentCard>();

        // ── Lifecycle ────────────────────────────────────────────────────────────

        private void Awake()
        {
            // Wait for SaveSystem to finish loading
            if (SaveSystem.Instance != null)
                SaveSystem.Instance.OnDataLoaded += BuildLobby;
        }

        private void Start()
        {
            // In case data is already ready (SaveSystem loaded before us)
            if (SaveSystem.Instance?.CurrentData != null)
                BuildLobby();
        }

        private void OnDestroy()
        {
            if (SaveSystem.Instance != null)
                SaveSystem.Instance.OnDataLoaded -= BuildLobby;
        }

        // ── Build UI ─────────────────────────────────────────────────────────────

        private void BuildLobby()
        {
            var data = SaveSystem.Instance.CurrentData;
            if (data == null) return;

            motelNameLabel.text = data.motelName;

            // Clear old rows
            foreach (Transform child in floorContainer) Destroy(child.gameObject);
            _cards.Clear();

            // Group by floor
            var byFloor = new Dictionary<int, List<ApartmentSaveData>>();
            foreach (var apt in data.apartments)
            {
                if (!byFloor.ContainsKey(apt.floorNumber))
                    byFloor[apt.floorNumber] = new List<ApartmentSaveData>();
                byFloor[apt.floorNumber].Add(apt);
            }

            // Render floor rows (top floor first)
            var floors = new List<int>(byFloor.Keys);
            floors.Sort((a, b) => b.CompareTo(a));

            foreach (int floor in floors)
            {
                var row = Instantiate(floorRowPrefab, floorContainer);
                var rowLabel = row.GetComponentInChildren<TextMeshProUGUI>();
                if (rowLabel) rowLabel.text = $"Floor {floor}";

                // Find or create the card container inside the row
                var cardRow = row.transform.Find("CardRow") ?? row.transform;

                foreach (var apt in byFloor[floor])
                {
                    var cardGo = Instantiate(apartmentCardPrefab, cardRow);
                    var card = cardGo.GetComponent<ApartmentCard>();
                    card.Init(apt, OpenApartment);
                    _cards.Add(card);
                }
            }
        }

        private void OpenApartment(ApartmentSaveData apt)
        {
            blueprintManager.OpenApartment(apt);
            gameObject.SetActive(false);
        }

        // Called by BlueprintManager's close button to return to lobby
        public void ReturnToLobby()
        {
            // Refresh cards in case rooms changed
            var data = SaveSystem.Instance.CurrentData;
            foreach (var card in _cards)
                card.Refresh();
            gameObject.SetActive(true);
        }
    }
}
