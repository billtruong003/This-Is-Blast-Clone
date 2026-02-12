# Universal Drag & Drop for Unity

A flexible, modular drag-and-drop system supporting **all cross-space combinations**:

| Source → Target | Use Cases |
|---|---|
| **UI → UI** | Inventory, equipment, card games, deck builders |
| **UI → 3D** | Strategy deploy, unit placement, building |
| **UI → 2D** | Tower defense, puzzle placement |
| **3D → 3D** | Object rearrangement, chess, RTS unit movement |
| **2D → 2D** | 2D puzzle, tile-based games |
| **3D → UI** | Loot pickup to inventory |

## Quick Start

### Installation
Copy the `UniversalDragDrop` folder into your project's `Packages` directory, or import via Unity Package Manager using the local path.

### 1. UI → UI (Inventory)

```csharp
// On each inventory item (child of a slot):
// Add component: Draggable
//   - Source Space: UI
//   - Channel: "inventory"
//   - Visual Mode: MoveOriginal

// On each slot:    
// Add component: DropZone
//   - Target Space: UI
//   - Channel: "inventory"
//   - Snap Mode: Center
//   - Capacity: 1
//   - Allow Swap: true

// Or use the pre-built InventorySlot component:
// Add component: InventorySlot (includes DropZone automatically)
```

### 2. UI → 3D (Strategy Unit Deploy)

```csharp
// UI Panel — unit cards:
// Add component: Draggable
//   - Source Space: UI
//   - Channel: "deploy"
//   - Visual Mode: Ghost (keeps card in panel)
// Add component: WorldPreviewVisual
//   - World Preview Prefab: [your 3D unit prefab]
//   - Ground Layer: [your ground layer mask]

// 3D Battlefield — ground plane:
// Add component: DropZone  (needs a Collider!)
//   - Target Space: World3D
//   - Channel: "deploy"
//   - Snap Mode: Grid
// Or use: GridDropZone for full grid management

// Handle the drop:
dropZone.OnItemDropped.AddListener((draggable, context) => {
    // Spawn the actual unit at the snap position
    var unitPrefab = draggable.DragData as GameObject;
    Instantiate(unitPrefab, context.FinalDropZone.GetSnapPosition(draggable, context), Quaternion.identity);
});
```

### 3. 3D → 3D (Move Units)

```csharp
// On the 3D object (needs a Collider):
// Add component: Draggable
//   - Source Space: World3D
//   - Channel: "units"
//   - Visual Mode: MoveOriginal

// On target positions/zones (need Colliders):
// Add component: DropZone
//   - Target Space: World3D
//   - Channel: "units"
```

### 4. Listen to Events Globally

```csharp
void OnEnable()
{
    DragDropEvents.OnDragStarted += HandleDragStart;
    DragDropEvents.OnDropAccepted += HandleDrop;
    DragDropEvents.OnSpaceTransition += HandleTransition;
}

void HandleDragStart(IDraggable draggable, DragContext context)
{
    Debug.Log($"Started dragging {draggable.GameObject.name}");
}

void HandleDrop(IDraggable draggable, IDropZone zone, DragContext context)
{
    Debug.Log($"Dropped {draggable.GameObject.name} onto {zone.GameObject.name}");
}

void HandleTransition(IDraggable draggable, DragSpace from, DragSpace to, DragContext context)
{
    Debug.Log($"Crossed from {from} to {to}!");
    // Example: swap visual from UI ghost to 3D preview
}
```

---

## Architecture

```
DragDropManager (Singleton)
├── Handles input routing
├── Manages active drag session (DragContext)
├── Detects drop zones via multi-space raycast
└── Fires global events (DragDropEvents)

SpaceConverter (Static Utility)
├── ScreenToWorld3D / ScreenToWorld2D / ScreenToUILocal
├── WorldToScreen
├── Cross-space Convert()
└── FindDropZoneAtScreenPosition (UI → 3D → 2D priority)

Draggable (MonoBehaviour, IDraggable)
├── Handles pointer input (UI via IPointerHandler, 3D/2D via OnMouse*)
├── Manages visual mode (MoveOriginal / Ghost / CustomPreview)
├── Carries DragData payload
└── Return-on-cancel animation

DropZone (MonoBehaviour, IDropZone)
├── Channel-based filtering
├── Capacity management
├── Snap modes (Center / Grid / Custom)
├── Visual hover feedback
└── Swap support

GridDropZone (extends DropZone)
├── Cell-based occupancy tracking
├── WorldToCell / CellToWorld conversion
├── Editor gizmos for grid visualization
└── Strategy/tower defense ready

InventorySlot (MonoBehaviour)
├── Pre-built UI inventory slot
├── Icon + quantity display
├── Auto-swap between slots
└── Event callbacks for inventory logic

WorldPreviewVisual (extends DragVisualBase)
├── Spawns 3D preview when dragging over world
├── Valid/invalid placement colors
├── Bobbing animation
└── Perfect for UI → 3D deploy pattern
```

---

## Key Concepts

### Channels
Channels are string-based filters. A `Draggable` with channel `"weapon"` can only be dropped on a `DropZone` with channel `"weapon"`. Use empty string `""` to accept anything.

### DragContext
Every drag session creates a `DragContext` that carries all information:
- Positions (screen, world, original)
- Raycast hits (3D, 2D)
- Timing (duration, distance)
- Custom data dictionary (`context.Set<T>()` / `context.Get<T>()`)

### DragData
Each `Draggable` has a `DragData` property (type `object`) for carrying arbitrary payloads — item definitions, unit stats, card data, etc.

```csharp
// When setting up:
draggable.SetData(new ItemData { Id = "sword_01", Damage = 25 });

// When receiving:
var item = draggable.DragData as ItemData;
```

### Cross-Space Transitions
When dragging from UI over a 3D drop zone, the system:
1. Detects the space change via `SpaceConverter.FindDropZoneAtScreenPosition`
2. Fires `DragDropEvents.OnSpaceTransition`
3. Updates `context.CurrentWorldPosition` using the target space's conversion

This lets you create effects like showing a 3D preview when a UI card enters the battlefield.

### Custom Validators

```csharp
dropZone.CustomValidator = (draggable, context) =>
{
    var item = draggable.DragData as ItemData;
    return item != null && item.Level >= requiredLevel;
};
```

### Custom Snap Positions

```csharp
dropZone.CustomSnapPosition = (draggable, context) =>
{
    // Snap to nearest available tile
    return FindNearestAvailableTile(context.CurrentWorldPosition);
};
```

---

## API Reference

### DragDropManager
| Method | Description |
|---|---|
| `StartDrag(IDraggable, Vector2)` | Begin a drag programmatically |
| `EndDrag(Vector2)` | End current drag at position |
| `CancelDrag()` | Cancel current drag, return to origin |
| `SetDragPlane(Plane)` | Set custom 3D drag plane |
| `RegisterDropZone(IDropZone)` | Register a drop zone |

### DragDropEvents
| Event | Args |
|---|---|
| `OnDragStarted` | `(IDraggable, DragContext)` |
| `OnDragUpdated` | `(IDraggable, DragContext)` |
| `OnDragEnded` | `(IDraggable, DragContext, DropResult)` |
| `OnDragCancelled` | `(IDraggable, DragContext)` |
| `OnDropZoneEnter` | `(IDraggable, IDropZone, DragContext)` |
| `OnDropZoneExit` | `(IDraggable, IDropZone, DragContext)` |
| `OnDropAccepted` | `(IDraggable, IDropZone, DragContext)` |
| `OnDropRejected` | `(IDraggable, IDropZone, DragContext)` |
| `OnSwap` | `(IDraggable, IDraggable, IDropZone, DragContext)` |
| `OnSpaceTransition` | `(IDraggable, DragSpace, DragSpace, DragContext)` |

### GridDropZone
| Method | Description |
|---|---|
| `WorldToCell(Vector3)` | Convert world position to grid cell |
| `CellToWorld(Vector2Int)` | Convert grid cell to world position |
| `IsCellAvailable(Vector2Int)` | Check if cell is free |
| `OccupyCell(Vector2Int, IDraggable)` | Mark cell as occupied |
| `FreeCell(Vector2Int)` | Free a cell |
| `ClearGrid()` | Clear all cells |

---

## Requirements
- Unity 2021.3+
- TextMeshPro (for InventorySlot quantity text)
- EventSystem in scene (for UI raycasting)

## License
MIT
