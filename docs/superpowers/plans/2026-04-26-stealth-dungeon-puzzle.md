# Stealth Puzzle Game Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a 2D top-down stealth puzzle prototype where a soldier escapes two dungeon rooms avoiding guards with visible vision cones.

**Architecture:** Escena-per-room structure with a persistent GameManager (DontDestroyOnLoad) tracking lives and scene transitions. Guards use a simple 3-state machine (Watching/Patrolling → Alerted → back). Vision cones are runtime-generated 2D meshes clipped by raycasts against the Walls layer.

**Tech Stack:** Unity 6000.4.0f1, C#, Unity 2D (Rigidbody2D, Physics2D), SceneManager, no external packages.

---

## File Structure

```
Assets/
├── Scripts/
│   ├── Core/
│   │   └── GameManager.cs          — singleton, lives, scene loading
│   ├── Player/
│   │   ├── PlayerMovement.cs       — WASD Rigidbody2D movement
│   │   ├── PlayerInventory.cs      — holds 1 item, pick up / throw
│   │   └── PlayerInteraction.cs    — E key: interact with doors/levers/items
│   ├── Guard/
│   │   ├── VisionCone.cs           — mesh generation + raycast clipping + detection
│   │   ├── GuardBase.cs            — state machine + alert response
│   │   ├── GuardStatic.cs          — oscillating cone rotation
│   │   └── GuardPatrol.cs          — waypoint following
│   ├── World/
│   │   ├── Door.cs                 — locked/open state, responds to key/lever
│   │   ├── Lever.cs                — toggles linked Door
│   │   ├── PickupItem.cs           — base for Key and Stone
│   │   ├── Stone.cs                — throwable, creates noise
│   │   ├── ThrownStone.cs          — projectile physics, triggers NoiseSource
│   │   ├── NoiseSource.cs          — alerts nearest guard on trigger
│   │   └── ExitTrigger.cs          — loads next scene on player enter
│   └── UI/
│       ├── HUDManager.cs           — updates heart icons + inventory icon
│       ├── MainMenuUI.cs           — Start / Quit buttons
│       └── GameOverUI.cs           — Restart / Quit buttons
├── Scenes/
│   ├── MainMenu.unity
│   ├── Room_01.unity
│   ├── Room_02.unity
│   └── GameOver.unity
├── Prefabs/
│   ├── Player.prefab
│   ├── Guard_Static.prefab
│   ├── Guard_Patrol.prefab
│   ├── Key.prefab
│   ├── Stone.prefab
│   ├── ThrownStone.prefab
│   ├── Door.prefab
│   └── Lever.prefab
└── Materials/
    ├── VisionCone_Normal.mat       — yellow, alpha ~0.35
    └── VisionCone_Alert.mat        — red, alpha ~0.45
```

---

## Task 1: Project Setup — 2D Config, Layers, Folders, Scenes

**Files:**
- Modify: `ProjectSettings/TagManager.asset` (via Unity Editor)
- Modify: `ProjectSettings/EditorBuildSettings.asset` (via Unity Editor)
- Create folders: `Assets/Scripts/Core`, `Assets/Scripts/Player`, `Assets/Scripts/Guard`, `Assets/Scripts/World`, `Assets/Scripts/UI`, `Assets/Prefabs`, `Assets/Materials`, `Assets/Scenes`

- [ ] **Step 1: Switch project to 2D mode**

  In Unity Editor: Edit → Project Settings → Editor → Default Behavior Mode → **2D**. Also set Graphics Settings to use the 2D renderer if prompted.

- [ ] **Step 2: Create Physics 2D layers**

  Edit → Project Settings → Tags and Layers. Add these layers (use slots 6–10):
  - `Player`
  - `Guard`
  - `Walls`
  - `Items`
  - `Interactable`

- [ ] **Step 3: Create folder structure**

  In the Project panel, create all folders listed in the File Structure above under `Assets/`.

- [ ] **Step 4: Create the four scenes**

  File → New Scene (Basic 2D) → save as `Assets/Scenes/MainMenu.unity`. Repeat for `Room_01.unity`, `Room_02.unity`, `GameOver.unity`.

- [ ] **Step 5: Add scenes to Build Settings**

  File → Build Settings → Add Open Scenes for each. Order: MainMenu (0), Room_01 (1), Room_02 (2), GameOver (3).

- [ ] **Step 6: Create vision cone materials**

  In `Assets/Materials/`: right-click → Create → Material. Name `VisionCone_Normal`. Shader: `Sprites/Default`. Color: `FFAA00`, Alpha: `90`. Duplicate → name `VisionCone_Alert`. Color: `FF2200`, Alpha: `115`.

- [ ] **Step 7: Commit**

  ```bash
  git add Assets/Scenes Assets/Materials ProjectSettings/TagManager.asset ProjectSettings/EditorBuildSettings.asset
  git commit -m "chore: project setup — 2D mode, layers, scenes, materials"
  ```

---

## Task 2: GameManager

**Files:**
- Create: `Assets/Scripts/Core/GameManager.cs`

- [ ] **Step 1: Create GameManager.cs**

  ```csharp
  using UnityEngine;
  using UnityEngine.SceneManagement;

  public class GameManager : MonoBehaviour
  {
      public static GameManager Instance { get; private set; }

      public int Lives { get; private set; } = 3;
      public bool IsWin { get; private set; }

      private string _currentRoomScene;

      void Awake()
      {
          if (Instance != null) { Destroy(gameObject); return; }
          Instance = this;
          DontDestroyOnLoad(gameObject);
      }

      public void StartGame()
      {
          Lives = 3;
          IsWin = false;
          LoadScene("Room_01");
      }

      public void LoadScene(string sceneName)
      {
          _currentRoomScene = sceneName;
          SceneManager.LoadScene(sceneName);
      }

      public void PlayerDetected()
      {
          Lives--;
          if (Lives <= 0)
              SceneManager.LoadScene("GameOver");
          else
              SceneManager.LoadScene(_currentRoomScene);
      }

      public void LoadNextRoom()
      {
          string next = _currentRoomScene == "Room_01" ? "Room_02" : "GameOver";
          LoadScene(next);
      }

      public void WinGame()
      {
          IsWin = true;
          SceneManager.LoadScene("GameOver");
      }
  }
  ```

- [ ] **Step 2: Create GameManager GameObject in MainMenu scene**

  Open `MainMenu.unity`. Create empty GameObject → name `GameManager`. Attach `GameManager.cs`.

- [ ] **Step 3: Verify in Play mode**

  Enter Play mode in MainMenu scene. Open Console. Verify no null reference errors. Check `GameManager.Instance` is not null via Quick Inspector. Exit Play mode.

- [ ] **Step 4: Commit**

  ```bash
  git add Assets/Scripts/Core/GameManager.cs Assets/Scenes/MainMenu.unity
  git commit -m "feat: GameManager singleton with lives, win flag, and scene loading"
  ```

---

## Task 3: Player Movement

**Files:**
- Create: `Assets/Scripts/Player/PlayerMovement.cs`

- [ ] **Step 1: Create PlayerMovement.cs**

  ```csharp
  using UnityEngine;

  [RequireComponent(typeof(Rigidbody2D))]
  public class PlayerMovement : MonoBehaviour
  {
      [SerializeField] float speed = 4f;

      Rigidbody2D _rb;

      void Awake() => _rb = GetComponent<Rigidbody2D>();

      void FixedUpdate()
      {
          float h = Input.GetAxisRaw("Horizontal");
          float v = Input.GetAxisRaw("Vertical");
          _rb.linearVelocity = new Vector2(h, v).normalized * speed;
      }
  }
  ```

- [ ] **Step 2: Create Player prefab in Room_01**

  Open `Room_01.unity`. Create → 2D Object → Sprites → Square. Name `Player`. Scale to `(0.5, 0.5, 1)`. Add `Rigidbody2D` (Gravity Scale = 0, Freeze Rotation Z = true). Add `CircleCollider2D`. Set Layer to `Player`. Tag to `Player` (Create tag first: Edit → Project Settings → Tags and Layers → Tags → add `Player`). Attach `PlayerMovement.cs`. Drag to `Assets/Prefabs/Player.prefab`.

- [ ] **Step 3: Add a wall to test collision**

  Create a Sprite Square → name `Wall`, scale `(8, 1, 1)`, position `(0, -3, 0)`. Add `BoxCollider2D`. Set Layer to `Walls`.

- [ ] **Step 4: Verify in Play mode**

  Enter Play in Room_01. Press WASD — player moves. Player stops at wall. Exit Play.

- [ ] **Step 5: Commit**

  ```bash
  git add Assets/Scripts/Player/PlayerMovement.cs Assets/Prefabs/Player.prefab Assets/Scenes/Room_01.unity
  git commit -m "feat: player top-down movement with Rigidbody2D"
  ```

---

## Task 4: Vision Cone

**Files:**
- Create: `Assets/Scripts/Guard/VisionCone.cs`

- [ ] **Step 1: Create VisionCone.cs**

  ```csharp
  using UnityEngine;

  [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
  public class VisionCone : MonoBehaviour
  {
      [SerializeField] public float angle = 60f;
      [SerializeField] public float distance = 5f;
      [SerializeField] int rayCount = 30;
      [SerializeField] LayerMask wallLayer;
      [SerializeField] LayerMask playerLayer;
      [SerializeField] Material normalMaterial;
      [SerializeField] Material alertMaterial;

      MeshFilter _mf;
      MeshRenderer _mr;
      Mesh _mesh;

      public event System.Action OnPlayerDetected;

      void Awake()
      {
          _mf = GetComponent<MeshFilter>();
          _mr = GetComponent<MeshRenderer>();
          _mesh = new Mesh();
          _mf.mesh = _mesh;
          _mr.material = normalMaterial;
      }

      void Update()
      {
          BuildMesh();
          CheckDetection();
      }

      void BuildMesh()
      {
          float halfAngle = angle / 2f;
          float angleStep = angle / rayCount;

          Vector3[] vertices = new Vector3[rayCount + 2];
          int[] triangles = new int[rayCount * 3];

          vertices[0] = Vector3.zero;

          for (int i = 0; i <= rayCount; i++)
          {
              float currentAngle = -halfAngle + angleStep * i;
              Vector2 dir = DirFromAngle(currentAngle);
              RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, distance, wallLayer);
              Vector3 point = hit ? (Vector3)hit.point - transform.position
                                  : (Vector3)(dir * distance);
              vertices[i + 1] = point;
          }

          for (int i = 0; i < rayCount; i++)
          {
              triangles[i * 3 + 0] = 0;
              triangles[i * 3 + 1] = i + 1;
              triangles[i * 3 + 2] = i + 2;
          }

          _mesh.Clear();
          _mesh.vertices = vertices;
          _mesh.triangles = triangles;
          _mesh.RecalculateNormals();
      }

      void CheckDetection()
      {
          Collider2D hit = Physics2D.OverlapCircle(transform.position, distance, playerLayer);
          if (hit == null) return;

          Vector2 toPlayer = hit.transform.position - transform.position;
          float angleTo = Vector2.Angle(transform.up, toPlayer);
          if (angleTo < angle / 2f)
          {
              RaycastHit2D los = Physics2D.Raycast(transform.position, toPlayer.normalized, distance, wallLayer);
              if (!los) OnPlayerDetected?.Invoke();
          }
      }

      public void SetAlerted(bool alerted) =>
          _mr.material = alerted ? alertMaterial : normalMaterial;

      Vector2 DirFromAngle(float angleDeg)
      {
          float rad = (transform.eulerAngles.z + angleDeg) * Mathf.Deg2Rad;
          return new Vector2(Mathf.Sin(rad), Mathf.Cos(rad));
      }
  }
  ```

- [ ] **Step 2: Create a test guard in Room_01 to verify cone renders**

  Create empty GameObject → name `Guard_Test`. Add child empty → name `VisionCone`. Attach `VisionCone.cs` to the child. Assign `normalMaterial` and `alertMaterial` in Inspector. Set `wallLayer` to `Walls`, `playerLayer` to `Player`. Set VisionCone child's `Sorting Layer` to Default, Order in Layer 1.

- [ ] **Step 3: Verify in Play mode**

  Enter Play. Visible yellow fan shape appears in front of guard. Move player behind wall — cone clips. Exit Play. (Detection event fires but nothing subscribes yet — no Console errors expected.)

- [ ] **Step 4: Commit**

  ```bash
  git add Assets/Scripts/Guard/VisionCone.cs Assets/Scenes/Room_01.unity
  git commit -m "feat: vision cone with runtime mesh and raycast wall clipping"
  ```

---

## Task 5: Guard Base + Static Guard

**Files:**
- Create: `Assets/Scripts/Guard/GuardBase.cs`
- Create: `Assets/Scripts/Guard/GuardStatic.cs`

- [ ] **Step 1: Create GuardBase.cs**

  ```csharp
  using System.Collections;
  using UnityEngine;

  public abstract class GuardBase : MonoBehaviour
  {
      protected enum GuardState { Normal, Alerted }
      protected GuardState State = GuardState.Normal;

      [SerializeField] protected float alertDuration = 3f;

      protected VisionCone VisionCone;

      protected virtual void Awake()
      {
          VisionCone = GetComponentInChildren<VisionCone>();
          VisionCone.OnPlayerDetected += HandlePlayerDetected;
      }

      void HandlePlayerDetected()
      {
          if (State == GuardState.Alerted) return;
          State = GuardState.Alerted;
          VisionCone.SetAlerted(true);
          OnAlerted();
          StartCoroutine(ReturnToNormalAfter(alertDuration));
      }

      public void AlertAt(Vector2 noisePosition)
      {
          if (State == GuardState.Alerted) return;
          State = GuardState.Alerted;
          VisionCone.SetAlerted(true);
          OnNoiseAlerted(noisePosition);
          StartCoroutine(ReturnToNormalAfter(alertDuration));
      }

      IEnumerator ReturnToNormalAfter(float seconds)
      {
          yield return new WaitForSeconds(seconds);
          State = GuardState.Normal;
          VisionCone.SetAlerted(false);
          OnReturnToNormal();
      }

      protected virtual void OnAlerted() => GameManager.Instance.PlayerDetected();
      protected virtual void OnNoiseAlerted(Vector2 position) { }
      protected virtual void OnReturnToNormal() { }
  }
  ```

- [ ] **Step 2: Create GuardStatic.cs**

  ```csharp
  using UnityEngine;

  public class GuardStatic : GuardBase
  {
      [SerializeField] float rotationSpeed = 30f;
      [SerializeField] float maxAngle = 45f;

      float _baseAngle;
      float _time;

      protected override void Awake()
      {
          base.Awake();
          _baseAngle = transform.eulerAngles.z;
      }

      void Update()
      {
          if (State == GuardState.Alerted) return;
          _time += Time.deltaTime * rotationSpeed * Mathf.Deg2Rad;
          float offset = Mathf.Sin(_time) * maxAngle;
          transform.rotation = Quaternion.Euler(0, 0, _baseAngle + offset);
      }
  }
  ```

- [ ] **Step 3: Wire up Guard_Test in Room_01**

  Remove any old test scripts from `Guard_Test`. Attach `GuardStatic.cs` to `Guard_Test`. The `VisionCone` child already has `VisionCone.cs`. Assign materials in Inspector if lost. Set Guard_Test Layer to `Guard`.

- [ ] **Step 4: Verify in Play mode**

  Enter Play. Guard cone oscillates left/right. Walk player into cone — GameManager.Instance.Lives drops by 1 (check in Inspector). Guard cone turns red then back to yellow after `alertDuration` seconds. Exit Play.

- [ ] **Step 5: Create Guard_Static prefab**

  Drag `Guard_Test` to `Assets/Prefabs/Guard_Static.prefab`. Delete from scene.

- [ ] **Step 6: Commit**

  ```bash
  git add Assets/Scripts/Guard/GuardBase.cs Assets/Scripts/Guard/GuardStatic.cs Assets/Prefabs/Guard_Static.prefab Assets/Scenes/Room_01.unity
  git commit -m "feat: GuardBase state machine + GuardStatic oscillating cone"
  ```

---

## Task 6: Guard Patrol

**Files:**
- Create: `Assets/Scripts/Guard/GuardPatrol.cs`

- [ ] **Step 1: Create GuardPatrol.cs**

  ```csharp
  using UnityEngine;

  public class GuardPatrol : GuardBase
  {
      [SerializeField] Transform[] waypoints;
      [SerializeField] float moveSpeed = 2f;

      int _index;
      Vector2 _alertTarget;

      void Update()
      {
          if (State == GuardState.Alerted)
          {
              MoveToward(_alertTarget);
              return;
          }
          Patrol();
      }

      void Patrol()
      {
          if (waypoints == null || waypoints.Length == 0) return;
          MoveToward(waypoints[_index].position);
          FaceDirection((Vector2)waypoints[_index].position - (Vector2)transform.position);

          if (Vector2.Distance(transform.position, waypoints[_index].position) < 0.1f)
              _index = (_index + 1) % waypoints.Length;
      }

      void MoveToward(Vector2 target)
      {
          transform.position = Vector2.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
      }

      void FaceDirection(Vector2 dir)
      {
          if (dir == Vector2.zero) return;
          float angle = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
          transform.rotation = Quaternion.Euler(0, 0, -angle);
      }

      protected override void OnNoiseAlerted(Vector2 position)
      {
          _alertTarget = position;
      }

      protected override void OnReturnToNormal()
      {
          _alertTarget = Vector2.zero;
      }
  }
  ```

- [ ] **Step 2: Create Guard_Patrol prefab in Room_01**

  In the Prefabs folder, duplicate `Guard_Static.prefab` → name copy `Guard_Patrol`. Open prefab in Prefab Mode, remove `GuardStatic`, attach `GuardPatrol`. Save and close prefab. Back in Room_01, drag `Guard_Patrol.prefab` into scene. Create 2 empty GameObjects as waypoints (`Waypoint_A` at `(-3, 0, 0)`, `Waypoint_B` at `(3, 0, 0)`). Assign to Guard_Patrol's `waypoints` array in Inspector.

- [ ] **Step 3: Verify in Play mode**

  Enter Play. Guard_Patrol moves between two waypoints, cone follows direction. Walk into cone — lives drop. Exit Play.

- [ ] **Step 4: Commit**

  ```bash
  git add Assets/Scripts/Guard/GuardPatrol.cs Assets/Prefabs/Guard_Patrol.prefab Assets/Scenes/Room_01.unity
  git commit -m "feat: GuardPatrol following waypoints with facing direction"
  ```

---

## Task 7: Player Inventory + Interaction

**Files:**
- Create: `Assets/Scripts/Player/PlayerInventory.cs`
- Create: `Assets/Scripts/Player/PlayerInteraction.cs`
- Create: `Assets/Scripts/World/PickupItem.cs`
- Create: `Assets/Scripts/World/IInteractable.cs`

- [ ] **Step 1: Create IInteractable.cs**

  ```csharp
  public interface IInteractable
  {
      void Interact(PlayerInventory inventory);
  }
  ```

- [ ] **Step 2: Create PickupItem.cs**

  ```csharp
  using UnityEngine;

  public class PickupItem : MonoBehaviour
  {
      public Sprite icon;
      public virtual void OnPickedUp(PlayerInventory inventory) { }
      public virtual void OnDropped() { }
  }
  ```

- [ ] **Step 3: Create PlayerInventory.cs**

  ```csharp
  using UnityEngine;

  public class PlayerInventory : MonoBehaviour
  {
      public PickupItem HeldItem { get; private set; }

      public bool TryPickUp(PickupItem item)
      {
          if (HeldItem != null) return false;
          HeldItem = item;
          item.OnPickedUp(this);
          item.gameObject.SetActive(false);
          return true;
      }

      public PickupItem TakeItem()
      {
          var item = HeldItem;
          HeldItem = null;
          return item;
      }

      public bool HasItem<T>() where T : PickupItem => HeldItem is T;
  }
  ```

- [ ] **Step 4: Create PlayerInteraction.cs**

  ```csharp
  using UnityEngine;

  [RequireComponent(typeof(PlayerInventory))]
  public class PlayerInteraction : MonoBehaviour
  {
      [SerializeField] float interactRadius = 1f;
      [SerializeField] LayerMask interactLayer;
      [SerializeField] LayerMask itemLayer;

      PlayerInventory _inventory;

      void Awake() => _inventory = GetComponent<PlayerInventory>();

      void Update()
      {
          if (Input.GetKeyDown(KeyCode.E)) TryInteract();
          if (Input.GetKeyDown(KeyCode.F)) TryThrow();
      }

      void TryInteract()
      {
          Collider2D item = Physics2D.OverlapCircle(transform.position, interactRadius, itemLayer);
          if (item != null)
          {
              var pickup = item.GetComponent<PickupItem>();
              if (pickup != null) { _inventory.TryPickUp(pickup); return; }
          }

          Collider2D interactable = Physics2D.OverlapCircle(transform.position, interactRadius, interactLayer);
          if (interactable != null)
              interactable.GetComponent<IInteractable>()?.Interact(_inventory);
      }

      void TryThrow()
      {
          if (!_inventory.HasItem<Stone>()) return;
          var stone = _inventory.TakeItem() as Stone;
          Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
          stone.Throw(transform.position, mouseWorld);
      }
  }
  ```

  > **Note:** `Stone` is defined in Task 9. `PlayerInteraction` won't compile until then. Keep this in mind — Tasks 7 and 9 must be committed together or in sequence without intervening Play mode tests that require compilation.

- [ ] **Step 5: Attach components to Player prefab**

  Open `Player.prefab`. Add `PlayerInventory` and `PlayerInteraction` components. Set `interactLayer` to `Interactable`, `itemLayer` to `Items`.

- [ ] **Step 6: Commit**

  ```bash
  git add Assets/Scripts/Player/PlayerInventory.cs Assets/Scripts/Player/PlayerInteraction.cs Assets/Scripts/World/PickupItem.cs Assets/Scripts/World/IInteractable.cs Assets/Prefabs/Player.prefab
  git commit -m "feat: player inventory + interaction system (E/F keys)"
  ```

---

## Task 8: Door + Lever + Key

**Files:**
- Create: `Assets/Scripts/World/Door.cs`
- Create: `Assets/Scripts/World/Lever.cs`
- Create: `Assets/Scripts/World/Key.cs`

- [ ] **Step 1: Create Door.cs**

  ```csharp
  using UnityEngine;

  public class Door : MonoBehaviour
  {
      [SerializeField] Sprite closedSprite;
      [SerializeField] Sprite openSprite;

      SpriteRenderer _sr;
      Collider2D _col;
      bool _open;

      void Awake()
      {
          _sr = GetComponent<SpriteRenderer>();
          _col = GetComponent<Collider2D>();
      }

      public void Open()
      {
          if (_open) return;
          _open = true;
          _sr.sprite = openSprite;
          _col.enabled = false;
      }

      public void Toggle() { if (_open) Close(); else Open(); }

      void Close()
      {
          _open = false;
          _sr.sprite = closedSprite;
          _col.enabled = true;
      }
  }
  ```

- [ ] **Step 2: Create Lever.cs**

  ```csharp
  using UnityEngine;

  public class Lever : MonoBehaviour, IInteractable
  {
      [SerializeField] Door linkedDoor;

      public void Interact(PlayerInventory inventory) => linkedDoor.Toggle();
  }
  ```

- [ ] **Step 3: Create Key.cs**

  ```csharp
  using UnityEngine;

  public class Key : PickupItem
  {
      [SerializeField] Door linkedDoor;

      public override void OnPickedUp(PlayerInventory inventory)
      {
          linkedDoor.Open();
      }
  }
  ```

- [ ] **Step 4: Create Door prefab**

  In Room_01, create Sprite Square → name `Door`. Add `BoxCollider2D`. Set Layer `Interactable`. Tint color dark grey (closed state). Attach `Door.cs`. For `closedSprite` and `openSprite`: duplicate the default Unity sprite and tint one grey, one green — or use the same sprite and rely on collider toggle. Drag to `Assets/Prefabs/Door.prefab`.

- [ ] **Step 5: Create Key prefab**

  Create Sprite → name `Key`. Scale `(0.4, 0.4, 1)`. Tint yellow. Add `CircleCollider2D` (Is Trigger = true). Set Layer `Items`. Attach `Key.cs`. Link to a Door in Inspector. Drag to `Assets/Prefabs/Key.prefab`.

- [ ] **Step 6: Create Lever prefab**

  Create Sprite → name `Lever`. Scale `(0.4, 0.6, 1)`. Tint blue. Add `BoxCollider2D` (Is Trigger = true). Set Layer `Interactable`. Attach `Lever.cs`. Link to a Door in Inspector. Drag to `Assets/Prefabs/Lever.prefab`.

- [ ] **Step 7: Verify in Play mode**

  Enter Play. Walk near Key, press E — key disappears, door opens (collider disabled). Walk near Lever, press E — door toggles closed/open. Exit Play.

- [ ] **Step 8: Commit**

  ```bash
  git add Assets/Scripts/World/Door.cs Assets/Scripts/World/Lever.cs Assets/Scripts/World/Key.cs Assets/Prefabs/Door.prefab Assets/Prefabs/Key.prefab Assets/Prefabs/Lever.prefab Assets/Scenes/Room_01.unity
  git commit -m "feat: door, lever, key with pickup and toggle mechanics"
  ```

---

## Task 9: Stone + Throw + NoiseSource

**Files:**
- Create: `Assets/Scripts/World/Stone.cs`
- Create: `Assets/Scripts/World/ThrownStone.cs`
- Create: `Assets/Scripts/World/NoiseSource.cs`

- [ ] **Step 1: Create NoiseSource.cs**

  ```csharp
  using UnityEngine;

  public class NoiseSource : MonoBehaviour
  {
      [SerializeField] float noiseRadius = 8f;
      [SerializeField] LayerMask guardLayer;

      public void TriggerNoise()
      {
          Collider2D[] guards = Physics2D.OverlapCircleAll(transform.position, noiseRadius, guardLayer);
          GuardBase nearest = null;
          float minDist = float.MaxValue;
          foreach (var g in guards)
          {
              float d = Vector2.Distance(transform.position, g.transform.position);
              if (d < minDist) { minDist = d; nearest = g.GetComponent<GuardBase>(); }
          }
          nearest?.AlertAt(transform.position);
          Destroy(gameObject);
      }
  }
  ```

- [ ] **Step 2: Create ThrownStone.cs**

  ```csharp
  using UnityEngine;

  [RequireComponent(typeof(Rigidbody2D))]
  public class ThrownStone : MonoBehaviour
  {
      [SerializeField] float speed = 8f;

      Rigidbody2D _rb;

      void Awake() => _rb = GetComponent<Rigidbody2D>();

      public void Launch(Vector2 direction) =>
          _rb.linearVelocity = direction.normalized * speed;

      void OnCollisionEnter2D(Collision2D col)
      {
          GetComponent<NoiseSource>().TriggerNoise();
      }
  }
  ```

- [ ] **Step 3: Create ThrownStone prefab**

  Create Sprite Circle → name `ThrownStone`. Scale `(0.2, 0.2, 1)`. Add `Rigidbody2D` (Gravity Scale = 0). Add `CircleCollider2D`. Attach `ThrownStone.cs` and `NoiseSource.cs`. Set `guardLayer` to `Guard`. Drag to `Assets/Prefabs/ThrownStone.prefab`.

- [ ] **Step 4: Create Stone.cs**

  ```csharp
  using UnityEngine;

  public class Stone : PickupItem
  {
      [SerializeField] GameObject thrownStonePrefab;

      public void Throw(Vector2 origin, Vector2 target)
      {
          GameObject thrown = Instantiate(thrownStonePrefab, origin, Quaternion.identity);
          thrown.GetComponent<ThrownStone>().Launch(target - origin);
          Destroy(gameObject);
      }
  }
  ```

- [ ] **Step 5: Create Stone pickup prefab**

  Create Sprite Circle → name `Stone`. Scale `(0.3, 0.3, 1)`. Tint grey. Add `CircleCollider2D` (Is Trigger = true). Set Layer `Items`. Attach `Stone.cs`. Assign `thrownStonePrefab` = `ThrownStone.prefab`. Drag to `Assets/Prefabs/Stone.prefab`.

- [ ] **Step 6: Verify in Play mode**

  Enter Play. Walk near Stone, press E — stone picked up. Press F, aim mouse near a guard — stone flies, hits wall, guard turns red (alerted), returns to yellow after alertDuration. Exit Play.

- [ ] **Step 7: Commit**

  ```bash
  git add Assets/Scripts/World/Stone.cs Assets/Scripts/World/ThrownStone.cs Assets/Scripts/World/NoiseSource.cs Assets/Prefabs/Stone.prefab Assets/Prefabs/ThrownStone.prefab Assets/Scenes/Room_01.unity
  git commit -m "feat: stone pickup, throw mechanic, noise alerts nearest guard"
  ```

---

## Task 10: ExitTrigger + Scene Transition

**Files:**
- Create: `Assets/Scripts/World/ExitTrigger.cs`

- [ ] **Step 1: Create ExitTrigger.cs**

  ```csharp
  using UnityEngine;

  public class ExitTrigger : MonoBehaviour
  {
      [SerializeField] bool isFinalExit;

      void OnTriggerEnter2D(Collider2D other)
      {
          if (!other.CompareTag("Player")) return;
          if (isFinalExit) GameManager.Instance.WinGame();
          else GameManager.Instance.LoadNextRoom();
      }
  }
  ```

- [ ] **Step 2: Place ExitTrigger in Room_01**

  Create Sprite Square → name `ExitTrigger`. Tint green. Scale `(1, 1, 1)`. Add `BoxCollider2D` (Is Trigger = true). Attach `ExitTrigger.cs`. Leave `isFinalExit` unchecked. Position at the intended exit of Room_01.

- [ ] **Step 3: Verify in Play mode**

  Enter Play in Room_01. Walk player into green exit — scene changes to Room_02. Exit Play.

- [ ] **Step 4: Commit**

  ```bash
  git add Assets/Scripts/World/ExitTrigger.cs Assets/Scenes/Room_01.unity
  git commit -m "feat: exit trigger loads next room via GameManager"
  ```

---

## Task 11: UI — HUD, MainMenu, GameOver

**Files:**
- Create: `Assets/Scripts/UI/HUDManager.cs`
- Create: `Assets/Scripts/UI/MainMenuUI.cs`
- Create: `Assets/Scripts/UI/GameOverUI.cs`

- [ ] **Step 1: Create HUDManager.cs**

  ```csharp
  using UnityEngine;
  using UnityEngine.UI;

  public class HUDManager : MonoBehaviour
  {
      [SerializeField] Image[] heartIcons;
      [SerializeField] Image inventoryIcon;
      [SerializeField] Sprite heartFull;
      [SerializeField] Sprite heartEmpty;

      PlayerInventory _inventory;

      void Start()
      {
          var player = FindFirstObjectByType<PlayerInventory>();
          if (player != null) _inventory = player;
      }

      void Update()
      {
          UpdateHearts();
          UpdateInventory();
      }

      void UpdateHearts()
      {
          if (GameManager.Instance == null) return;
          int lives = GameManager.Instance.Lives;
          for (int i = 0; i < heartIcons.Length; i++)
              heartIcons[i].sprite = i < lives ? heartFull : heartEmpty;
      }

      void UpdateInventory()
      {
          if (_inventory == null) { inventoryIcon.enabled = false; return; }
          bool hasItem = _inventory.HeldItem != null;
          inventoryIcon.enabled = hasItem;
          if (hasItem && _inventory.HeldItem.icon != null)
              inventoryIcon.sprite = _inventory.HeldItem.icon;
      }
  }
  ```

- [ ] **Step 2: Build HUD Canvas in Room_01**

  Create → UI → Canvas (Screen Space - Overlay). Add 3 Image children: `Heart_1`, `Heart_2`, `Heart_3`. Anchor top-left. Add 1 Image child `InventoryIcon` at bottom-left. Create empty child `HUD`, attach `HUDManager.cs`. Assign `heartIcons` array (Heart_1, Heart_2, Heart_3), `inventoryIcon` (InventoryIcon). Use default white sprites tinted red for hearts; tint grey for heartEmpty.

- [ ] **Step 3: Create MainMenuUI.cs**

  ```csharp
  using UnityEngine;

  public class MainMenuUI : MonoBehaviour
  {
      public void OnStartClicked() => GameManager.Instance.StartGame();
      public void OnQuitClicked() => Application.Quit();
  }
  ```

- [ ] **Step 4: Build MainMenu scene UI**

  Open `MainMenu.unity`. Create Canvas. Add TextMeshProUGUI "Dungeon Escape" (title, centered). Add Button "Jugar" → OnClick → `MainMenuUI.OnStartClicked`. Add Button "Salir" → `MainMenuUI.OnQuitClicked`. Attach `MainMenuUI.cs` to Canvas.

- [ ] **Step 5: Create GameOverUI.cs**

  ```csharp
  using UnityEngine;
  using TMPro;

  public class GameOverUI : MonoBehaviour
  {
      [SerializeField] TextMeshProUGUI messageText;

      void Start()
      {
          if (GameManager.Instance != null)
              messageText.text = GameManager.Instance.IsWin ? "¡Escapaste!" : "Game Over";
      }

      public void OnRestartClicked() => GameManager.Instance.StartGame();
      public void OnQuitClicked() => Application.Quit();
  }
  ```

- [ ] **Step 6: Build GameOver scene UI**

  Open `GameOver.unity`. Create Canvas. Add TextMeshProUGUI component → name `MessageText`. Add Button "Reintentar" → `GameOverUI.OnRestartClicked`. Add Button "Salir" → `GameOverUI.OnQuitClicked`. Attach `GameOverUI.cs` to Canvas. Assign `messageText` = MessageText in Inspector.

- [ ] **Step 7: Verify full game loop in Play mode**

  Start from MainMenu. Click Jugar → Room_01 loads, 3 hearts visible. Walk into guard cone → heart disappears. Reach 0 lives → GameOver loads with "Game Over" text. Click Reintentar → Room_01 reloads with 3 hearts. Exit Play.

- [ ] **Step 8: Commit**

  ```bash
  git add Assets/Scripts/UI/ Assets/Scenes/MainMenu.unity Assets/Scenes/GameOver.unity Assets/Scenes/Room_01.unity
  git commit -m "feat: HUD hearts + inventory icon, MainMenu, GameOver UI"
  ```

---

## Task 12: Room_01 Layout

**Files:**
- Modify: `Assets/Scenes/Room_01.unity`

Room_01 is the tutorial room. One static guard. Player learns: move, pick up key, open door, reach exit.

- [ ] **Step 1: Clear test objects**

  Delete any leftover test GameObjects from Room_01 (Guard_Test, test Wall, Guard_Patrol test). Keep Player prefab instance, Camera, and HUD Canvas.

- [ ] **Step 2: Build walls**

  Create Sprite Squares for room boundary. Layer `Walls`. Add `BoxCollider2D` to each. Color dark grey. Room size: 16×10 units centered at origin.
  - Top wall: position `(0, 5, 0)`, scale `(16, 1, 1)`
  - Bottom wall: position `(0, -5, 0)`, scale `(16, 1, 1)`
  - Left wall: position `(-8, 0, 0)`, scale `(1, 10, 1)`
  - Right wall: position `(8, 0, 0)`, scale `(1, 10, 1)`

- [ ] **Step 3: Place Guard_Static**

  Drag `Guard_Static.prefab` into scene. Position `(2, 0, 0)`. Set rotation speed 20, maxAngle 50, cone angle 55°, cone distance 4. Assign materials in Inspector.

- [ ] **Step 4: Place Key + locked Door**

  - Door prefab at `(4, -3, 0)` — blocks exit corridor.
  - Key prefab at `(-3, 2, 0)` — accessible without crossing guard path.
  - In Key Inspector, assign `linkedDoor` to the Door instance.

- [ ] **Step 5: Place ExitTrigger**

  ExitTrigger at `(6, -3, 0)` beyond the door. `isFinalExit` = false.

- [ ] **Step 6: Set Camera**

  Set Main Camera: Projection = Orthographic, Size = 6. Position `(0, 0, -10)`.

- [ ] **Step 7: Verify full Room_01 loop in Play mode**

  Enter Play. Navigate around guard, grab key (E), door opens, reach exit, Room_02 loads. No errors in Console. Exit Play.

- [ ] **Step 8: Commit**

  ```bash
  git add Assets/Scenes/Room_01.unity
  git commit -m "feat: Room_01 layout — walls, static guard, key-door, exit"
  ```

---

## Task 13: Room_02 Layout

**Files:**
- Modify: `Assets/Scenes/Room_02.unity`

Room_02: one patrolling guard + one static guard, stone for distraction, lever puzzle, final exit.

- [ ] **Step 1: Build walls for Room_02**

  Same boundary (16×10). Add inner dividing wall at `(0, 0, 0)`, scale `(1, 8, 1)` with a gap at `(0, -3, 0)` (leave 2-unit gap), forcing path near patrol guard.

- [ ] **Step 2: Place Guard_Patrol**

  Drag `Guard_Patrol.prefab`. Position `(-4, 1, 0)`. Create Waypoints: `Waypoint_A` at `(-4, 3, 0)`, `Waypoint_B` at `(-4, -2, 0)`. Assign to `waypoints` array. Speed 2.

- [ ] **Step 3: Place Guard_Static**

  Position `(3, 0, 0)`. Rotation speed 25, maxAngle 60, cone angle 65°, distance 4.5.

- [ ] **Step 4: Place Stone**

  Stone prefab at `(-5, -3, 0)` — reachable without crossing guard path.

- [ ] **Step 5: Place Lever + Door + ExitTrigger**

  - Door at `(5, 0, 0)` blocking exit corridor.
  - Lever at `(1, 2, 0)` — player must sneak past static guard.
  - ExitTrigger at `(7, 0, 0)`. **Set `isFinalExit` = true.**
  - Link Lever to Door in Inspector.

- [ ] **Step 6: Add HUD Canvas + Camera**

  Duplicate HUD Canvas setup from Room_01. Camera Orthographic Size 6, position `(0, 0, -10)`.

- [ ] **Step 7: Verify full game in Play mode**

  Start from MainMenu → Room_01 exit → Room_02. Pick up stone, throw to distract patrol guard, reach lever (E), door opens, reach exit — GameOver loads with "¡Escapaste!". Exit Play.

- [ ] **Step 8: Commit**

  ```bash
  git add Assets/Scenes/Room_02.unity
  git commit -m "feat: Room_02 layout — patrol + static guard, stone distraction, lever-door, win exit"
  ```

---

## Self-Review Checklist

| Spec Requirement | Task |
|-----------------|------|
| MainMenu scene | Task 11 |
| Room_01 (static guard) | Tasks 5, 12 |
| Room_02 (patrol guard) | Tasks 6, 13 |
| GameOver scene (win/lose) | Tasks 11, 2 |
| GameManager DontDestroyOnLoad, lives | Task 2 |
| Player WASD movement | Task 3 |
| Player 3 lives | Tasks 2, 11 |
| Player inventory 1 item | Task 7 |
| Interact E key | Task 7 |
| Throw F key | Task 9 |
| Vision cone mesh + raycast | Task 4 |
| Vision cone yellow → red | Tasks 4, 5 |
| Guard_Static oscillating cone | Task 5 |
| Guard_Patrol waypoints | Task 6 |
| Guard alert on noise | Task 9 (NoiseSource) |
| Detection → lose life → respawn | Task 5 (GuardBase.OnAlerted) |
| 0 lives → GameOver | Task 2 |
| Key picks up, opens door | Task 8 |
| Lever toggles door | Task 8 |
| Stone pickable, throwable | Task 9 |
| Stone distracts nearest guard | Task 9 |
| ExitTrigger loads next scene | Task 10 |
| HUD hearts + inventory icon | Task 11 |
| Win condition "¡Escapaste!" | Tasks 2, 10, 11 |
| Layers: Player, Guard, Walls, Items, Interactable | Task 1 |
| Player tagged "Player" | Task 3 |
