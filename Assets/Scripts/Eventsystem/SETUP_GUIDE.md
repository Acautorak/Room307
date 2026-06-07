# CK2 Event System — Unity Setup Guide

## Folder Structure

```
CK2EventSystem/
├── Data/
│   ├── GameContextSO.cs       — central game state (SO singleton)
│   ├── EventOutcomeSO.cs      — abstract base for end-node effects
│   ├── EventChainSO.cs        — one event story
│   ├── EventNodeSO.cs         — one panel/page in a chain
│   └── ChoiceSO.cs            — one choice button
├── Runtime/
│   └── EventManager.cs        — timer, queue, chain walker
├── UI/
│   ├── EventPanel.cs          — the single reusable panel
│   └── NotificationButton.cs  — HUD button with badge
├── Save/
│   ├── SaveService.cs         — JSON read/write
│   └── EventSaveData.cs       — serializable save struct
├── Outcomes/
│   └── ExampleOutcomes.cs     — StatChange, SetFlag, Compound
└── Editor/
    └── EventSOEditors.cs      — custom inspectors + chain preview
```

---

## 1. Create ScriptableObject Assets

### GameContext (one per project)
`Right-click in Project → CK2Events → Game Context`
Name it `GameContext`. Add fields for your game's stats.

### Event Chains
For each event story:
1. `Right-click → CK2Events → Event Chain` — create `Chain_PeasantRevolt`
2. `Right-click → CK2Events → Event Node` — create nodes for each page
3. `Right-click → CK2Events → Choice` — create choices
4. Wire them: drag nodes into the chain's `Entry Node`, drag choices into node's `Choices[]`, set each choice's `Next Node`.

---

## 2. Scene Setup

### EventManager GameObject
- Create an empty GameObject, name it `EventManager`
- Add `EventManager` component
- Assign:
  - `All Chains` — drag every EventChainSO here
  - `Game Context` — drag the GameContext asset
  - `Event Panel` — (see below)
  - `Notification Button` — (see below)
  - `Interval Seconds` — e.g. 30

### EventPanel Prefab
Build this UI hierarchy in your Canvas:
```
EventPanel (Panel / Image background)
├── ArtworkImage          Image component
├── TitleText             TextMeshPro
├── BodyScrollView        ScrollRect
│   └── Viewport
│       └── Content
│           └── BodyText  TextMeshPro
└── ChoiceContainer       VerticalLayoutGroup + ContentSizeFitter
                          (Child Control Size: Height checked)
```
- Add `EventPanel` MonoBehaviour to the root
- Assign all references in the Inspector
- Create a `ChoiceButton` prefab: Button → child TMP label. Drag into `Choice Button Prefab`.
- Set `EventPanel` root `SetActive(false)` by default.

### NotificationButton Prefab
```
NotificationButton
├── Button                the clickable area
├── IconImage             Image (your scroll/envelope icon)
└── BadgeRoot             (small circle, default SetActive false)
    └── BadgeText         TextMeshPro ("1")
```
- Add `NotificationButton` MonoBehaviour
- Assign references + drag the `EventManager` into the field

---

## 3. Creating Event Chains (Designer Workflow)

### Linear chain with a branch:
```
EntryNode → Node2 → ChoiceNode
                        ├── Choice A → BranchNodeA → EndNodeA [outcome: +50 gold]
                        └── Choice B → BranchNodeB → EndNodeB [outcome: +30 prestige]
```

1. Create `EventChainSO` named `Plague_Arrives`
   - `chainId`: `plague_arrives` (auto-fills from asset name)
   - `defaultTitle`: `A Plague Spreads`
   - `weight`: 1

2. Create nodes:
   - `Node_PlagueArrives` — artwork, body text, no choices → `continueNode: Node_PlagueWorsens`
   - `Node_PlagueWorsens` — new artwork, new text, no choices → `continueNode: Node_PlagueChoice`
   - `Node_PlagueChoice` — choices: `[Choice_Quarantine, Choice_Pray]`
   - `Node_Quarantine` — isEndNode, `outcome: StatChange(-200 gold)`
   - `Node_Pray` — isEndNode, `outcome: StatChange(+20 piety)`

3. Create choices:
   - `Choice_Quarantine` — label: `"Enforce quarantine"`, nextNode: `Node_Quarantine`
   - `Choice_Pray` — label: `"Call for prayers"`, nextNode: `Node_Pray`

4. Drag all chains into `EventManager.allChains[]`

---

## 4. Creating Custom Outcomes

Subclass `EventOutcomeSO`:
```csharp
[CreateAssetMenu(menuName = "CK2Events/Outcomes/My Outcome")]
public class MyOutcome : EventOutcomeSO
{
    public int someValue;

    public override void Apply(GameContextSO context)
    {
        // Do anything with context here
        context.gold += someValue;
        // Or fire a C# event, call a service, load a scene, etc.
    }

    public override string GetPreviewText() => $"+{someValue} gold";
}
```
Then `right-click → CK2Events/Outcomes/My Outcome` to create the asset.

---

## 5. Save / Load

- Saves automatically on every choice and on `ApplicationPause`/`ApplicationQuit`
- Save file: `Application.persistentDataPath/ck2_events.json`
- To start a new game: call `EventManager.ResetAllProgress()`

---

## 6. Extending GameContextSO

Add fields for your game:
```csharp
public int health = 100;
public string currentRegion = "Westmarch";
public bool hasHeir = false;
```
Then update `ToSaveData()` and `FromSaveData()` to include the new fields.
Outcomes read and write these directly — no extra wiring needed.
