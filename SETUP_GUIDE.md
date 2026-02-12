# JELLY GUNNER - HƯỚNG DẪN SETUP CHI TIẾT

## TỔNG QUAN

Hệ thống gồm 37 file, bạn giải nén zip rồi kéo folder `JellyGunner` vào `Assets/`.
Sau đó làm theo từng bước dưới đây.

---

## BƯỚC 0: PREREQUISITES (Kiểm tra trước)

### Bắt buộc có trong Project:
- **Unity 2022.3+** (URP)
- **Universal Render Pipeline** package đã cài
- **Odin Inspector** (Sirenix) - dùng cho `[Required]`, `[Title]`, `[Button]`, `[ReadOnly]`...

### Nếu KHÔNG có Odin Inspector:
Tìm và xóa tất cả dòng sau trong code:
```
using Sirenix.OdinInspector;
```
Và xóa các attribute: `[Title(...)]`, `[Required]`, `[ReadOnly]`, `[Button(...)]`, `[GUIColor(...)]`, `[ShowInInspector]`, `[ListDrawerSettings(...)]`, `[TableList]`, `[HorizontalGroup(...)]`, `[ShowIf(...)]`

Code vẫn chạy bình thường, chỉ mất giao diện Inspector đẹp.

---

## BƯỚC 1: GIẢI NÉN VÀ IMPORT

```
1. Giải nén JellyGunner_FullProject.zip
2. Copy folder "JellyGunner" vào Assets/
   → Kết quả: Assets/JellyGunner/Core/, Assets/JellyGunner/Gameplay/, ...
3. Đợi Unity compile xong (có thể mất 10-20 giây)
```

### Fix lỗi Assembly nếu có:
- Mở `Assets/JellyGunner/JellyGunner.asmdef` trong Inspector
- Nếu reference `Sirenix.OdinInspector.Attributes` báo đỏ → bấm dấu `-` xóa nó
- Nếu reference `Unity.RenderPipelines.Universal.Runtime` báo đỏ → kiểm tra URP đã cài chưa
- Tương tự cho `Editor/JellyGunner.Editor.asmdef`

---

## BƯỚC 2: TẠO ASSETS (ScriptableObjects)

Cần tạo 5 ScriptableObject. Vào `Assets/JellyGunner/Data/` để tạo.

### 2A. Game Config
```
Right-click trong Project > Create > JellyGunner > Game Config
Đặt tên: "GameConfig"
```
**Giữ nguyên default values** hoặc chỉnh theo ý:

| Field              | Default | Mô tả                                |
|--------------------|---------|---------------------------------------|
| Cell Size          | 1.2     | Khoảng cách giữa các ô grid          |
| Grid Advance Speed | 0.05    | Tốc độ grid tiến lại gần             |
| Blaster Fly Duration | 0.35  | Thời gian bay từ Supply vào Tray     |
| Merge Fly Duration | 0.25    | Thời gian 2 blaster bay vào merge    |
| Projectile Speed   | 25      | Tốc độ viên đạn                      |
| Deform Decay Rate  | 0.85    | Tốc độ hết hiệu ứng jelly            |
| Death Shrink Duration | 0.35 | Thời gian thu nhỏ khi chết           |
| Cull Distance      | 200     | Khoảng cách GPU culling              |
| Supply Buffer Cap  | 32      | Số lượng tối đa trong buffer supply  |

### 2B. Color Palette
```
Right-click > Create > JellyGunner > Color Palette
Đặt tên: "ColorPalette"
```
Chỉnh màu nếu muốn, hoặc giữ default:
- Red: `(0.95, 0.25, 0.3)`
- Blue: `(0.2, 0.5, 0.95)`
- Green: `(0.25, 0.9, 0.4)`
- Yellow: `(1.0, 0.85, 0.2)`

### 2C. Blaster Definitions (TẠO 3 CÁI)

```
Right-click > Create > JellyGunner > Blaster Definition
```

**Blaster_Pistol:**
| Field      | Value         |
|------------|---------------|
| Type       | Pistol        |
| Mesh       | (Cube mesh)   |
| Model Scale| 0.5           |
| Recoil Angle| 5            |

**Blaster_Sniper:**
| Field      | Value                |
|------------|----------------------|
| Type       | Sniper               |
| Mesh       | (Prism/Triangle mesh)|
| Model Scale| 0.6                  |
| Recoil Angle| 3                   |

**Blaster_Gatling:**
| Field      | Value            |
|------------|------------------|
| Type       | Gatling          |
| Mesh       | (Cylinder mesh)  |
| Model Scale| 0.7              |
| Recoil Angle| 8               |

> **TIP Mesh:** Không có mesh riêng? Dùng Unity built-in:
> - Pistol → `Cube` (tìm trong Project: "Cube")
> - Sniper → `Cube` xoay 45 độ, hoặc bất kỳ mesh
> - Gatling → `Cylinder`
>
> Tạo mesh nhanh: tạo GameObject > Add Cube > lấy mesh từ MeshFilter

### 2D. Level Data (Tạo level test đầu tiên)

```
Right-click > Create > JellyGunner > Level Data
Đặt tên: "Level_01"
```

Hoặc dùng **Level Editor** (xem Bước 5). Nhưng nếu muốn tạo tay:

| Field        | Value  |
|--------------|--------|
| Level Index  | 0      |
| Level Name   | "Test" |
| Columns      | 5      |
| Rows         | 4      |
| Tray Slots   | 5      |
| Supply Columns| 4     |
| Hammer Charges| 1     |

Trong **Waves**, bấm `+` thêm 1 wave:

**Wave 0 > Enemies** (bấm `+` nhiều lần thêm enemy):
```
gridX=0, gridY=0, color=Red,    tier=Standard  (20 HP)
gridX=1, gridY=0, color=Blue,   tier=Standard  (20 HP)
gridX=2, gridY=0, color=Red,    tier=Standard  (20 HP)
gridX=3, gridY=0, color=Green,  tier=Standard  (20 HP)
gridX=4, gridY=0, color=Blue,   tier=Standard  (20 HP)
gridX=0, gridY=1, color=Green,  tier=Tiny      (1 HP)
gridX=1, gridY=1, color=Red,    tier=Tiny      (1 HP)
gridX=2, gridY=1, color=Blue,   tier=Tiny      (1 HP)
gridX=3, gridY=1, color=Red,    tier=Tiny      (1 HP)
gridX=4, gridY=1, color=Green,  tier=Tiny      (1 HP)
```
**Tổng HP: 20+20+20+20+20+1+1+1+1+1 = 105**

**Wave 0 > Supply** (bấm `+` thêm supply entries):
```
color=Red,   type=Pistol   → 20 ammo
color=Red,   type=Pistol   → 20 ammo
color=Red,   type=Pistol   → 20 ammo ← Tổng Red: 60, cần 42 → thừa 18 OK
color=Blue,  type=Pistol   → 20 ammo
color=Blue,  type=Pistol   → 20 ammo ← Tổng Blue: 40, cần 41 → thiếu 1
color=Green, type=Pistol   → 20 ammo ← Tổng Green: 20, cần 22 → thiếu 2
```
> Chỉnh lại số cho khớp, hoặc dùng **Auto-Generate Supply** ở Level Editor (Bước 5).

**Wave 0 > Advance Speed**: `0.03`

**Kiểm tra**: Inspector sẽ hiện "BALANCED" (xanh) hoặc "UNBALANCED" (đỏ).

---

## BƯỚC 3: TẠO MATERIALS VÀ MESHES

### 3A. Enemy Material
```
1. Right-click > Create > Material
2. Đặt tên: "Mat_JellyEnemy"
3. Đổi Shader thành: JellyGunner/JellyDeform_Instanced
4. Chỉnh tùy ý:
   - Shadow Color: (0.3, 0.3, 0.4) → màu bóng cel-shading
   - Threshold: 0.5
   - Smoothness: 0.05
   - Rim Color: trắng
   - Rim Power: 3
   - Breath Amplitude: 0.08 → enemy thở nhẹ
   - Impact Strength: 0.4 → mức biến dạng khi trúng đạn
```

### 3B. Projectile Material
```
1. Right-click > Create > Material
2. Đặt tên: "Mat_Projectile"
3. Shader: JellyGunner/JellyDeform_Instanced (hoặc URP/Lit nếu muốn đơn giản)
4. Base Color: trắng (sẽ bị override bởi instance color)
```

### 3C. Blaster Material
```
1. Right-click > Create > Material
2. Đặt tên: "Mat_Blaster"
3. Shader: Universal Render Pipeline/Lit (hoặc JellyDeform nếu muốn)
4. Base Color: trắng (sẽ bị override bởi MaterialPropertyBlock)
```

### 3D. Meshes
**Enemy Mesh**: dùng Sphere hoặc bất kỳ mesh tròn tròn jelly-like
**Projectile Mesh**: dùng Sphere nhỏ

> **Lấy built-in mesh:**
> ```
> 1. Hierarchy > Create > 3D Object > Sphere
> 2. Chọn Sphere > MeshFilter > bấm vào tên mesh "Sphere"
> 3. Project window sẽ highlight mesh asset
> 4. Ghi nhớ vị trí, dùng nó kéo vào slot sau
> 5. Xóa Sphere khỏi Hierarchy
> ```

---

## BƯỚC 4: BUILD SCENE (CÁCH NHANH - WIZARD)

Đây là cách nhanh nhất:

```
1. Tạo scene mới: File > New Scene > Basic (Built-in)
2. Menu bar: JellyGunner > Scene Setup Wizard
3. Kéo assets vào từng slot:
```

| Slot              | Kéo cái gì vào                    |
|-------------------|------------------------------------|
| Game Config       | GameConfig (SO tạo ở Bước 2A)     |
| Color Palette     | ColorPalette (SO tạo ở Bước 2B)   |
| Level Data        | Level_01 (SO tạo ở Bước 2D)       |
| Culling Shader    | JellyCulling.compute (trong Shaders/) |
| Enemy Mesh        | Sphere mesh                        |
| Enemy Material    | Mat_JellyEnemy                     |
| Projectile Mesh   | Sphere mesh (nhỏ)                  |
| Projectile Mat    | Mat_Projectile                     |
| Death VFX Prefab  | (Tùy chọn - xem Bước 4B)          |

```
4. Bấm "BUILD SCENE"
5. Wizard tự tạo toàn bộ hierarchy
```

### Sau khi Build, Hierarchy sẽ có:
```
=== JELLY GUNNER ===
├── GPU Renderer          (JellyInstanceRenderer)
├── Enemy Grid            (EnemyGridManager)
├── Tray Anchor           (TraySystem)           ← Position: (0, 0.5, 0)
├── Blaster Factory       (BlasterFactory)
├── Supply Anchor         (SupplyLineManager)     ← Position: (0, -3, 0)
├── Projectile Manager    (ProjectileManager)
├── Hammer PowerUp        (HammerPowerUp)
├── Input Handler         (InputHandler)
├── Audio Handler         (AudioHandler)
├── VFX Handler           (nếu có VFX prefab)
└── Game Manager          (GameManager)
```

### 4A. CẦN LÀM THÊM SAU KHI BUILD:

**A. Blaster Factory → gán Blaster Definitions:**
```
1. Chọn "Blaster Factory" trong Hierarchy
2. Inspector > Definitions array
3. Size = 3
4. Element 0 = Blaster_Pistol (SO)
5. Element 1 = Blaster_Sniper (SO)
6. Element 2 = Blaster_Gatling (SO)
```

**B. Input Handler → set Supply Layer:**
```
1. Chọn "Input Handler"
2. Supply Layer = layer mà Supply blocks sẽ dùng
3. Tạo layer mới: Edit > Project Settings > Tags and Layers
4. Thêm layer "Supply" (ví dụ layer 8)
5. Set Supply Layer mask = "Supply"
6. Đảm bảo Supply Anchor object cũng ở layer "Supply"
```

**C. Camera Setup:**
```
1. Chọn Main Camera
2. Wizard đã tự gắn GameCameraController
3. Chỉnh:
   - Position: (0, 8, -5)
   - Rotation: (45, 0, 0)
   → Nhìn từ trên xuống xiên, thấy cả Grid + Tray + Supply
4. Camera > Clear Flags = Solid Color
5. Background = màu tối (0.1, 0.1, 0.15)
```

### 4B. Death VFX Prefab (Tùy chọn):
```
1. Hierarchy > Create > Effects > Particle System
2. Chỉnh settings:
   - Duration: 0.5
   - Start Lifetime: 0.3-0.5
   - Start Speed: 3-8
   - Start Size: 0.1-0.3
   - Emission > Bursts: 1 burst, count = 15
   - Shape: Sphere
   - Color over Lifetime: Fade out
   - Renderer > Material: Default-Particle
3. Kéo từ Hierarchy vào Project (tạo prefab)
4. Đặt tên: "VFX_Death"
5. Xóa khỏi Hierarchy
6. Kéo prefab vào VFX Handler > Death VFX Prefab
```

---

## BƯỚC 5: TẠO UI CANVAS

UI không được tự tạo bởi Wizard, cần làm tay:

```
1. Hierarchy > UI > Canvas
2. Canvas:
   - Render Mode: Screen Space - Overlay
   - Canvas Scaler: Scale With Screen Size
   - Reference Resolution: 1080 x 1920
   - Match: 0.5
```

### 5A. Deadlock Warning UI
```
Canvas/
├── WarningOverlay (Image)
│   - Stretch full screen (Anchor: stretch-stretch)
│   - Color: Red (1, 0.1, 0.1, 0)
│   - Raycast Target: OFF
│
├── GameOverPanel (Panel - mặc định tắt)
│   - Anchor: Center
│   - Size: 600 x 400
│   - Chứa Text "GAME OVER" + Button "Retry"
│
├── VictoryPanel (Panel - mặc định tắt)
│   - Anchor: Center
│   - Size: 600 x 400
│   - Chứa Text "VICTORY!" + Button "Next"
│
└── TrayCountText (Text)
    - Anchor: Top-Right
    - Font size: 36
    - Text: "0/5"
```

**Gán component:**
```
1. Tạo empty GameObject "UI Manager" dưới Canvas
2. Add Component > DeadlockWarningUI
3. Kéo:
   - Config = GameConfig (SO)
   - Warning Overlay = WarningOverlay image
   - Game Over Panel = GameOverPanel
   - Victory Panel = VictoryPanel
   - Tray Count Text = TrayCountText
```

### 5B. Hammer Button UI
```
Canvas/
└── HammerButton (Button)
    - Anchor: Bottom-Right
    - Size: 120 x 120
    - Image: icon búa (hoặc text "🔨")
    ├── ChargeCount (Text)
    │   - Font size: 24
    │   - Text: "1"
    └── EmptyOverlay (Image - bán trong suốt đen)
```

**Gán component:**
```
1. Add Component > HammerButtonUI trên HammerButton
2. Kéo:
   - Input = InputHandler (trong Hierarchy)
   - Hammer = HammerPowerUp (trong Hierarchy)
   - Hammer Button = chính nó (Button component)
   - Charge Count Text = ChargeCount text
   - Empty Overlay = EmptyOverlay
```

### 5C. Progress Bar UI
```
Canvas/
└── ProgressBar (empty)
    ├── BG (Image)
    │   - Anchor: Top-Center
    │   - Size: 800 x 30
    │   - Color: Dark gray
    ├── Fill (Image)
    │   - Anchor: same as BG
    │   - Image Type: Filled
    │   - Fill Method: Horizontal
    │   - Color: Green
    ├── EnemyCount (Text)
    │   - Text: "0"
    └── SupplyCount (Text)
        - Text: "Supply: 0"
```

**Gán component:**
```
1. Add Component > ProgressBarUI trên ProgressBar
2. Kéo:
   - Enemy Grid = EnemyGridManager (Hierarchy)
   - Supply = SupplyLineManager (Hierarchy)
   - Progress Fill = Fill image
   - Enemy Count Text = EnemyCount text
   - Supply Count Text = SupplyCount text
```

### 5D. Merge Effect UI
```
Canvas/
└── MergeFlash (Image)
    - Stretch full screen
    - Color: White (1, 1, 1, 0)
    - Raycast Target: OFF
```

**Gán component:**
```
1. Add Component > MergeEffectUI trên Canvas (hoặc empty child)
2. Kéo:
   - Palette = ColorPalette (SO)
   - Flash Overlay = MergeFlash image
```

### 5E. Kéo ProgressBarUI vào GameManager:
```
1. Chọn Game Manager trong Hierarchy
2. Inspector > Progress Bar = ProgressBarUI component
```

---

## BƯỚC 6: LEVEL EDITOR (Vẽ map nhanh)

```
1. Menu: JellyGunner > Level Editor
2. Kéo Level_01 vào slot "Target Level"
3. Chỉnh Columns, Rows, Tray Slots
4. Chọn màu + tier từ Palette
5. Click ô để tô, click lại để đổi
6. Bấm "Randomize" nếu muốn random map test
7. Bấm "Auto-Generate Supply" → tự tạo supply match 1:1
8. Bấm "SAVE TO LEVEL"
```

**Kiểm tra balance:**
```
1. Chọn Level_01 trong Project
2. Inspector cuộn xuống cuối: "BALANCE REPORT"
3. Hiện "BALANCED" = OK
4. Hiện "UNBALANCED" = chỉnh lại supply
```

---

## BƯỚC 7: TEST CHẠY

```
1. Bấm Play
2. Click vào block trong Supply (vùng dưới) để đặt vào Tray
3. Blaster tự aim + bắn enemy cùng màu
4. Khi Blaster hết đạn → mọc chân chạy đi
5. Đặt 3 cùng màu vào Tray → Merge!
6. Bấm nút Hammer → kéo lên enemy → xóa hết 1 màu
```

### Nếu không thấy gì khi Play:
```
□ Camera có nhìn đúng hướng không? (0, 8, -5) rotation (45, 0, 0)
□ Level Data có wave với enemies không?
□ Material dùng đúng shader JellyGunner/JellyDeform_Instanced?
□ Culling Shader đã gán vào JellyInstanceRenderer?
□ Enemy Mesh + Material đã gán vào GameManager?
□ Console có error đỏ nào không?
```

### Nếu click Supply block mà không phản hồi:
```
□ Supply blocks có Collider không? (BlasterFactory tự thêm BoxCollider)
□ Supply blocks ở đúng Layer "Supply" chưa?
□ InputHandler > Supply Layer có set đúng layer mask?
□ Main Camera đã gán vào InputHandler?
```

---

## BƯỚC 8: CHỈNH LAYOUT CHO ĐẸP

Positions quan trọng (chỉnh trong Inspector hoặc Scene View):

```
Grid Origin (GameManager):     (0, 2, 15)    ← Enemy grid, xa camera
Tray Anchor:                   (0, 0.5, 0)   ← Giữa, nơi súng ngồi
Supply Anchor:                 (0, -3, 0)     ← Dưới cùng, supply blocks
Camera:                        (0, 8, -5)     ← Nhìn từ trên xuống
```

Điều chỉnh theo kích thước grid:
- Grid rộng (10+ columns) → camera lùi xa hơn
- Grid hẹp (3-5 columns) → camera lại gần

---

## TÓM TẮT CHECKLIST

```
[  ] 1. Giải nén zip, kéo JellyGunner/ vào Assets/
[  ] 2. Fix assembly references nếu cần
[  ] 3. Tạo GameConfig (SO)
[  ] 4. Tạo ColorPalette (SO)
[  ] 5. Tạo 3 BlasterDefinition (Pistol, Sniper, Gatling)
[  ] 6. Tạo Enemy Material (shader: JellyDeform_Instanced)
[  ] 7. Tạo Projectile Material
[  ] 8. Tạo Level_01 (SO) hoặc dùng Level Editor
[  ] 9. Scene Setup Wizard > BUILD SCENE
[  ] 10. Gán Blaster Definitions vào BlasterFactory
[  ] 11. Tạo layer "Supply" + set vào InputHandler
[  ] 12. Setup Camera position
[  ] 13. Tạo UI Canvas + 4 UI components
[  ] 14. Kéo ProgressBarUI vào GameManager
[  ] 15. Tạo Death VFX prefab (tùy chọn)
[  ] 16. Bấm Play → test!
```
