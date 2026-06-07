using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MotelSimulator.Data;
using MotelSimulator.Save;

namespace MotelSimulator.Blueprint
{
    /// <summary>
    /// Core controller for the blueprint designer panel.
    /// Attach to the BlueprintPanel root GameObject.
    /// </summary>
    public class BlueprintManager : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────────────

        [Header("References")]
        public GridDrawTool gridDrawTool;
        public RectTransform roomContainer;      // parent for spawned room prefabs
        public GameObject roomPrefab;            // prefab with RoomInstance component
        public RoomColorConfigSO roomColorConfig;

        [Header("UI – Header")]
        public TextMeshProUGUI apartmentNameLabel;
        public Button closeButton;

        [Header("UI – Toolbar")]
        public Button addRoomButton;             // + Draw new room
        public Button deleteRoomButton;          // 🗑 Delete selected
        public Button saveButton;

        [Header("UI – Room Type Panel")]
        public GameObject roomTypePanel;         // shown after drawing a rect
        public Transform roomTypeButtonContainer;
        public GameObject roomTypeButtonPrefab;  // has Button + TextMeshProUGUI

        [Header("UI – Grid Lines")]
        public GameObject gridLinesPrefab;       // optional cosmetic grid overlay

        // ── Private state ────────────────────────────────────────────────────────

        private ApartmentSaveData _apartmentData;
        private List<RoomInstance> _rooms = new List<RoomInstance>();
        private RoomInstance _selectedRoom;
        private RectInt _pendingGridRect;        // rect waiting for type assignment
        private bool _awaitingTypeAssignment;
        private int _roomCounter;

        // ── Lifecycle ────────────────────────────────────────────────────────────

        private void Awake()
        {
            addRoomButton.onClick.AddListener(StartDrawingMode);
            deleteRoomButton.onClick.AddListener(DeleteSelectedRoom);
            saveButton.onClick.AddListener(SaveApartment);
            closeButton.onClick.AddListener(Close);

            gridDrawTool.OnRectConfirmed = OnDrawingComplete;
            gridDrawTool.SetActive(false);

            roomTypePanel.SetActive(false);
            BuildRoomTypeButtons();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Call this to open the blueprint for a specific apartment.</summary>
        public void OpenApartment(ApartmentSaveData apartment)
        {
            _apartmentData = apartment;
            apartmentNameLabel.text = apartment.apartmentName;
            gameObject.SetActive(true);

            ClearAllRooms();
            LoadRoomsFromData();
        }

        public void OnRoomClicked(RoomInstance room)
        {
            if (_awaitingTypeAssignment) return;
            SelectRoom(room);
        }

        // ── Drawing flow ─────────────────────────────────────────────────────────

        private void StartDrawingMode()
        {
            DeselectRoom();
            roomTypePanel.SetActive(false);
            _awaitingTypeAssignment = false;
            gridDrawTool.SetActive(true);
            addRoomButton.interactable = false;
        }

        private void OnDrawingComplete(RectInt gridRect)
        {
            gridDrawTool.SetActive(false);
            addRoomButton.interactable = true;
            _pendingGridRect = gridRect;
            _awaitingTypeAssignment = true;
            roomTypePanel.SetActive(true);
        }

        private void AssignTypeToNewRoom(RoomType type)
        {
            if (!_awaitingTypeAssignment) return;
            _awaitingTypeAssignment = false;
            roomTypePanel.SetActive(false);

            var id = $"room_{_apartmentData.apartmentId}_{_roomCounter++}";
            SpawnRoom(id, _pendingGridRect, type);
        }

        private void AssignTypeToSelectedRoom(RoomType type)
        {
            if (_selectedRoom == null) return;
            _selectedRoom.SetType(type);
            roomTypePanel.SetActive(false);

            // Sync data
            var data = _apartmentData.rooms.Find(r => r.roomId == _selectedRoom.RoomId);
            if (data != null) data.roomType = type;
        }

        // ── Room type button panel ────────────────────────────────────────────────

        private void BuildRoomTypeButtons()
        {
            foreach (Transform child in roomTypeButtonContainer) Destroy(child.gameObject);

            foreach (var cfg in roomColorConfig.roomColors)
            {
                if (cfg.roomType == RoomType.Unassigned) continue;

                var go = Instantiate(roomTypeButtonPrefab, roomTypeButtonContainer);
                var label = go.GetComponentInChildren<TextMeshProUGUI>();
                if (label) label.text = cfg.displayName;

                var img = go.GetComponent<Image>();
                if (img) img.color = cfg.color;

                var btn = go.GetComponent<Button>();
                var capturedType = cfg.roomType;
                btn.onClick.AddListener(() =>
                {
                    if (_awaitingTypeAssignment) AssignTypeToNewRoom(capturedType);
                    else if (_selectedRoom != null) AssignTypeToSelectedRoom(capturedType);
                    else roomTypePanel.SetActive(false);
                });
            }
        }

        // ── Room management ───────────────────────────────────────────────────────

        private void SpawnRoom(string id, RectInt gridRect, RoomType type)
        {
            var go = Instantiate(roomPrefab, roomContainer);
            var rt = go.GetComponent<RectTransform>();

            // Position and size in pixel space
            var pixelRect = gridDrawTool.GridRectToPixel(gridRect);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(
                pixelRect.x + pixelRect.width  * 0.5f,
                pixelRect.y + pixelRect.height * 0.5f);
            rt.sizeDelta = new Vector2(pixelRect.width, pixelRect.height);

            var room = go.GetComponent<RoomInstance>();
            room.Init(id, gridRect, type, roomColorConfig, this);
            _rooms.Add(room);

            // Persist
            _apartmentData.rooms.Add(new RoomSaveData
            {
                roomId = id,
                roomType = type,
                gridX = gridRect.x,
                gridY = gridRect.y,
                gridWidth = gridRect.width,
                gridHeight = gridRect.height
            });

            SelectRoom(room);
        }

        private void SelectRoom(RoomInstance room)
        {
            DeselectRoom();
            _selectedRoom = room;
            room.SetSelected(true);
            deleteRoomButton.interactable = true;
        }

        private void DeselectRoom()
        {
            if (_selectedRoom != null) _selectedRoom.SetSelected(false);
            _selectedRoom = null;
            deleteRoomButton.interactable = false;
        }

        private void DeleteSelectedRoom()
        {
            if (_selectedRoom == null) return;
            _apartmentData.rooms.RemoveAll(r => r.roomId == _selectedRoom.RoomId);
            _rooms.Remove(_selectedRoom);
            Destroy(_selectedRoom.gameObject);
            _selectedRoom = null;
            deleteRoomButton.interactable = false;
        }

        private void ClearAllRooms()
        {
            foreach (var r in _rooms) if (r) Destroy(r.gameObject);
            _rooms.Clear();
            _selectedRoom = null;
        }

        private void LoadRoomsFromData()
        {
            _roomCounter = _apartmentData.rooms.Count;
            foreach (var data in _apartmentData.rooms)
            {
                var gridRect = new RectInt(data.gridX, data.gridY, data.gridWidth, data.gridHeight);
                SpawnRoomFromData(data.roomId, gridRect, data.roomType);
            }
        }

        /// <summary>Spawn without adding to _apartmentData (already loaded).</summary>
        private void SpawnRoomFromData(string id, RectInt gridRect, RoomType type)
        {
            var go = Instantiate(roomPrefab, roomContainer);
            var rt = go.GetComponent<RectTransform>();

            var pixelRect = gridDrawTool.GridRectToPixel(gridRect);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(
                pixelRect.x + pixelRect.width  * 0.5f,
                pixelRect.y + pixelRect.height * 0.5f);
            rt.sizeDelta = new Vector2(pixelRect.width, pixelRect.height);

            var room = go.GetComponent<RoomInstance>();
            room.Init(id, gridRect, type, roomColorConfig, this);
            _rooms.Add(room);
        }

        // ── Save / Close ─────────────────────────────────────────────────────────

        private void SaveApartment()
        {
            SaveSystem.Instance.SaveApartment(_apartmentData);
            Debug.Log($"[BlueprintManager] Saved apartment {_apartmentData.apartmentId}");
        }

        private void Close()
        {
            SaveApartment();
            gameObject.SetActive(false);
        }
    }
}
