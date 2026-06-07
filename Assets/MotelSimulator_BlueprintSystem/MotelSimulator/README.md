# Motel Idle Simulator — Apartment Blueprint System
## Unity C# | UI Toolkit-free | Mobile-first

---

## Overview

This is a **self-contained, isolated system** you can drop into any Unity project.
It provides:

| Feature | Details |
|---|---|
| **Motel Lobby** | Grid of apartment cards organised by floor |
| **Blueprint Designer** | Drag-to-draw room rectangles on a snapping grid |
| **Room Type Assignment** | Color-coded room types via a type-picker panel |
| **Persistent Save** | JSON save file at `Application.persistentDataPath` |

---

## File Structure

```
MotelSimulator/
├── Scripts/
│   ├── Data/
│   │   ├── RoomData.cs          ← RoomType enum + RoomColorConfigSO
│   │   └── SaveData.cs          ← Serializable save models
│   ├── Save/
│   │   └── SaveSystem.cs        ← Singleton JSON save/load
│   ├── Blueprint/
│   │   ├── GridDrawTool.cs      ← Drag-to-draw interaction
│   │   ├── GridLineRenderer.cs  ← Cosmetic grid overlay
│   │   ├── RoomInstance.cs      ← Runtime placed room rectangle
│   │   └── BlueprintManager.cs  ← Core blueprint controller
│   ├── UI/
│   │   ├── ApartmentCard.cs     ← Lobby card button
│   │   └── MotelLobbyManager.cs ← Lobby screen controller
│   └── GameBootstrapper.cs      ← Scene wiring / startup
```

---

## Quick Setup

### 1 — Import & Dependencies
- Requires **TextMeshPro** (install via Package Manager → Unity Registry)
- No other third-party dependencies

### 2 — Create the ScriptableObject
1. Right-click in Project → **Create → MotelSimulator → Room Color Config**
2. Name it `RoomColorConfig` and place it in `Resources/` or assign directly in Inspector
3. Tweak colors per room type if desired

### 3 — Scene Hierarchy

```
[Scene]
├── GameManager (GameBootstrapper + SaveSystem)
│
├── LobbyPanel (MotelLobbyManager)
│   └── ScrollView
│       └── Content  ← set as "floorContainer"
│
└── BlueprintPanel (BlueprintManager)
    ├── Header
    │   ├── TitleLabel
    │   └── CloseButton
    ├── GridArea
    │   ├── GridLines (GridLineRenderer)   ← cosmetic
    │   ├── DrawOverlay (GridDrawTool)     ← transparent, raycast on
    │   │   └── PreviewImage               ← child Image, starts hidden
    │   └── RoomContainer                  ← empty RectTransform
    ├── Toolbar
    │   ├── AddRoomButton (+)
    │   ├── DeleteButton  (🗑)
    │   └── SaveButton
    └── RoomTypePanel (starts hidden)
        └── ButtonContainer
```

### 4 — Prefabs You Need to Create

#### RoomPrefab
```
RoomPrefab (RoomInstance)
├── Image (background — set as backgroundImage)
├── Outline Image (optional border — outlineImage)
├── TextMeshProUGUI (labelText)
└── Button (full-rect — selectButton)
```
- Pivot: center (0.5, 0.5)
- Anchor: center

#### ApartmentCardPrefab
```
ApartmentCardPrefab (ApartmentCard)
├── TextMeshProUGUI "Unit Label"
├── TextMeshProUGUI "Room Count"
├── Image "StatusDot"
└── Button "Open"
```

#### RoomTypeButtonPrefab
```
RoomTypeButtonPrefab
├── Image (background — will be tinted to room color)
├── TextMeshProUGUI
└── Button
```

#### FloorRowPrefab
```
FloorRowPrefab
├── TextMeshProUGUI "Floor X"
└── HorizontalLayoutGroup "CardRow"
```

### 5 — Inspector Wiring (BlueprintManager)
| Field | Assign |
|---|---|
| gridDrawTool | DrawOverlay object |
| roomContainer | RoomContainer RectTransform |
| roomPrefab | RoomPrefab |
| roomColorConfig | Your RoomColorConfig SO |
| apartmentNameLabel | Header TitleLabel |
| closeButton | Header CloseButton |
| addRoomButton | Toolbar AddRoomButton |
| deleteRoomButton | Toolbar DeleteButton |
| saveButton | Toolbar SaveButton |
| roomTypePanel | RoomTypePanel root |
| roomTypeButtonContainer | ButtonContainer inside RoomTypePanel |
| roomTypeButtonPrefab | RoomTypeButtonPrefab |

### 6 — GridDrawTool Setup
- Attach to the **DrawOverlay** panel
- Set **Image → Color = (0,0,0,0)** (invisible but catches raycasts)
- Enable **Raycast Target = true**
- Set `columns` and `rows` to match GridLineRenderer values (default: 20×14)
- Assign the `previewImage` child Image

---

## How It Works

### Drawing Flow
```
Player taps [+] button
  → GridDrawTool activates (intercepts pointer events)
  → Player drags → preview rect shown in real time
  → Player lifts finger → OnRectConfirmed fires
  → RoomTypePanel appears
  → Player picks a room type
  → Room is spawned, colored, and added to save data
```

### Saving
- Rooms are added to `ApartmentSaveData` immediately on placement
- `SaveButton` / `CloseButton` calls `SaveSystem.SaveApartment()`
- Data is written to `{persistentDataPath}/motel_save.json`
- On next load, `LoadRoomsFromData()` re-spawns all rooms

---

## Extending

### Add a new Room Type
1. Add an entry to the `RoomType` enum in `RoomData.cs`
2. Add a matching `RoomColorConfig` entry in your `RoomColorConfigSO`
3. Done — the button panel auto-generates from the SO

### Add Rooms per Apartment Limit
In `BlueprintManager.StartDrawingMode()`:
```csharp
if (_apartmentData.rooms.Count >= MAX_ROOMS) return;
```

### Add Undo
Keep a `Stack<RoomSaveData>` and pop on undo, removing the last spawned `RoomInstance`.

---

## Save File Location
```
Android : /data/data/<package>/files/motel_save.json
iOS     : <AppSandbox>/Documents/motel_save.json
Editor  : <ProjectRoot>/motel_save.json  (printed in Console on save)
```
