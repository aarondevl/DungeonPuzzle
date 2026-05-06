# DungeonPuzzle Quality Pass Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Cerrar bugs de colisiones, mejorar feel del gameplay, y subir notablemente el nivel visual del proyecto sin reescribirlo.

**Architecture:** El plan ataca en 4 fases ordenadas por riesgo y dependencia. Fase 1 son **bugs de correctness** (físicas/colisiones). Fase 2 es **feel** (timings, smoothing, tweens, spawn points). Fase 3 es **visual** (URP 2D lighting, sprite animator simple, cono con gradient, HUD con sprites). Fase 4 es **code quality** (cache, eventos, asmdef, dead code). Cada fase deja el proyecto jugable. Se puede pausar entre fases.

**Tech Stack:** Unity 6 + URP 2D Renderer, Input System (1.x), Rigidbody2D, TextMeshPro, C# (.NET Standard 2.1). Sin DOTween — usamos coroutines.

**Notas importantes para el ejecutor:**
- Este repo no tiene tests previos. La **Task 0** crea la asmdef de tests. Donde el cambio es **lógica C# pura**, hacemos TDD (EditMode test). Donde el cambio es **escena/visual/animator/lighting**, no se puede TDD razonablemente — usamos verificación manual con `mcp__ai-game-developer__screenshot-game-view` + `mcp__ai-game-developer__console-get-logs` y se documenta el método.
- Trabajamos en `main` directamente (proyecto solo, no equipo). Cada task = commit propio. Si algo se rompe, `git revert` por task.
- **Antes de cada commit**, ejecutar `mcp__ai-game-developer__console-get-logs` y verificar 0 errores. Si hay errores en consola que no son del cambio, mencionarlos y parar.
- Estilo de mensajes de commit del repo: `feat:`, `fix:`, `chore:`, `refactor:` en minúsculas, descripción corta.

---

## File Structure

### Nuevos archivos

```
Assets/
  Scripts/
    Tests/
      DungeonPuzzle.Tests.asmdef            # Task 0
      EditMode/
        ThrownStoneFilterTests.cs           # Task 2
        NoiseSourceTests.cs                 # Task 3
        PlayerMovementSmoothingTests.cs     # Task 7
    Core/
      SpawnPoint.cs                         # Task 9
    World/
      DoorTween.cs                          # Task 8 (logic separated for testability)
  Settings/
    DungeonGlobalLight.asset                # Task 10 (Light2D global asset, optional)
  Materials/
    VisionConeGradient.mat                  # Task 12
  Shaders/
    VisionConeGradient.shadergraph          # Task 12
  Animations/
    Player/
      Player.controller                     # Task 11
      PlayerIdle.anim                       # Task 11
      PlayerWalk.anim                       # Task 11
  Sprites/
    UI/
      heart_full.png + heart_empty.png      # Task 13 (generated procedurally if no art)
```

### Archivos modificados

```
Assets/Scripts/Guard/GuardBase.cs           # Tasks 1, 6
Assets/Scripts/Guard/GuardPatrol.cs         # Task 1
Assets/Scripts/Guard/GuardStatic.cs         # Task 1
Assets/Scripts/Guard/VisionCone.cs          # Tasks 6, 12, 14 (cleanup)
Assets/Scripts/World/ThrownStone.cs         # Task 2
Assets/Scripts/World/NoiseSource.cs         # Task 3
Assets/Scripts/World/Door.cs                # Tasks 4, 8
Assets/Scripts/Player/PlayerMovement.cs     # Task 7
Assets/Scripts/Player/PlayerInteraction.cs  # Task 14 (cache Camera.main)
Assets/Scripts/Core/GameManager.cs          # Task 9 (use SpawnPoint)
Assets/Scripts/UI/HUDManager.cs             # Tasks 13, 14 (event-based)
Assets/Scripts/Player/PlayerInventory.cs    # Task 14 (event for HUD)
Assets/Prefabs/Guard_Patrol.prefab          # Task 1 (add Rigidbody2D)
Assets/Prefabs/Guard_Static.prefab          # Task 1
Assets/Prefabs/ThrownStone.prefab           # Task 2 (set noise mask)
Assets/Prefabs/Door.prefab                  # Task 8 (DoorTween component)
Assets/Scenes/Room_01.unity                 # Tasks 9, 10 (spawn point + lights)
Assets/Scenes/Room_02.unity                 # Tasks 9, 10
```

---

## Phase 1 — Correctness (collision/physics bugs)

### Task 0: Bootstrap test assembly

**Files:**
- Create: `Assets/Scripts/Tests/DungeonPuzzle.Tests.asmdef`

- [ ] **Step 1: Create the test asmdef file**

Path: `Assets/Scripts/Tests/DungeonPuzzle.Tests.asmdef`

```json
{
    "name": "DungeonPuzzle.Tests",
    "rootNamespace": "",
    "references": [
        "GUID:27619889b8ba8c24980f49ee34dbb44a",
        "GUID:0acc523941302664db1f4e527237feb3"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "versionDefines": [],
    "noEngineReferences": false
}
```

The two GUID references are `UnityEngine.TestRunner` and `UnityEditor.TestRunner` (Unity stable IDs). If these GUIDs don't resolve in this Unity version, open Test Runner (Window > General > Test Runner) and click "Create EditMode Test Assembly Folder" in `Assets/Scripts/Tests/`, then move the file as needed.

- [ ] **Step 2: Create EditMode subfolder and a sanity test**

Path: `Assets/Scripts/Tests/EditMode/SanityTest.cs`

```csharp
using NUnit.Framework;

public class SanityTest
{
    [Test]
    public void OnePlusOne_EqualsTwo()
    {
        Assert.AreEqual(2, 1 + 1);
    }
}
```

- [ ] **Step 3: Verify tests run via MCP**

Use `mcp__ai-game-developer__tests-run` with `testMode: "EditMode"`.

Expected: 1 passed, 0 failed.

If it fails to compile because the GUIDs don't resolve, fall back to creating the asmdef via Unity's Test Runner UI (Window > General > Test Runner > EditMode tab > "Create EditMode Test Assembly Folder").

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Tests/
git commit -m "chore: bootstrap EditMode test assembly"
```

---

### Task 1: Guard physics — use Rigidbody2D for movement and rotation

**Why:** `GuardPatrol.MoveToward` y `GuardStatic.Update` mueven y rotan via `transform.position` / `transform.rotation`, lo cual ignora la física 2D. El guard puede atravesar walls y no respeta el `Rigidbody2D` del player.

**Files:**
- Modify: `Assets/Scripts/Guard/GuardBase.cs`
- Modify: `Assets/Scripts/Guard/GuardPatrol.cs`
- Modify: `Assets/Scripts/Guard/GuardStatic.cs`
- Modify: `Assets/Prefabs/Guard_Patrol.prefab` (Rigidbody2D + Collider2D)
- Modify: `Assets/Prefabs/Guard_Static.prefab` (Rigidbody2D + Collider2D)

- [ ] **Step 1: Update `GuardBase.cs` to require Rigidbody2D and expose it**

Replace the file contents with:

```csharp
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public abstract class GuardBase : MonoBehaviour
{
    protected enum GuardState { Normal, Alerted }
    protected GuardState State = GuardState.Normal;

    [SerializeField] protected float alertDuration = 3f;

    protected VisionCone VisionCone;
    protected Rigidbody2D Rb;

    protected virtual void Awake()
    {
        Rb = GetComponent<Rigidbody2D>();
        Rb.bodyType = RigidbodyType2D.Kinematic;
        Rb.gravityScale = 0f;
        Rb.freezeRotation = false;
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

- [ ] **Step 2: Update `GuardPatrol.cs` to use Rigidbody2D**

Replace the file contents with:

```csharp
using UnityEngine;

public class GuardPatrol : GuardBase
{
    [SerializeField] Transform[] waypoints;
    [SerializeField] float moveSpeed = 2f;

    int _index;
    Vector2 _alertTarget;

    void FixedUpdate()
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
        Vector2 target = waypoints[_index].position;
        MoveToward(target);
        FaceDirection(target - Rb.position);

        if (Vector2.Distance(Rb.position, target) < 0.1f)
            _index = (_index + 1) % waypoints.Length;
    }

    void MoveToward(Vector2 target)
    {
        Vector2 next = Vector2.MoveTowards(Rb.position, target, moveSpeed * Time.fixedDeltaTime);
        Rb.MovePosition(next);
    }

    void FaceDirection(Vector2 dir)
    {
        if (dir == Vector2.zero) return;
        float angle = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
        Rb.MoveRotation(-angle);
    }

    protected override void OnNoiseAlerted(Vector2 position) => _alertTarget = position;
    protected override void OnReturnToNormal() => _alertTarget = Vector2.zero;
}
```

- [ ] **Step 3: Update `GuardStatic.cs` to use Rigidbody2D rotation**

Replace the file contents with:

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

    void FixedUpdate()
    {
        if (State == GuardState.Alerted) return;
        _time += Time.fixedDeltaTime * rotationSpeed * Mathf.Deg2Rad;
        float offset = Mathf.Sin(_time) * maxAngle;
        Rb.MoveRotation(_baseAngle + offset);
    }
}
```

- [ ] **Step 4: Wait for Unity to recompile, check console**

Use `mcp__ai-game-developer__console-get-logs` after the compile. Expected: 0 errors.

- [ ] **Step 5: Add Rigidbody2D to guard prefabs (if missing)**

For each prefab:
- `Assets/Prefabs/Guard_Patrol.prefab`
- `Assets/Prefabs/Guard_Static.prefab`

Use `mcp__ai-game-developer__assets-prefab-open` then `mcp__ai-game-developer__gameobject-component-list-all` to check if `Rigidbody2D` is present. If not, use `mcp__ai-game-developer__gameobject-component-add` with type `Rigidbody2D`. Configure: `bodyType = Kinematic`, `gravityScale = 0`. The `Awake` method already enforces this at runtime, but setting it on the asset avoids "Did not converge" warnings in editor.

Confirm there is a `Collider2D` (BoxCollider2D or CircleCollider2D). If absent, add one sized to the guard sprite (~0.4 radius for Circle).

Save with `mcp__ai-game-developer__assets-prefab-save` and close with `mcp__ai-game-developer__assets-prefab-close`.

- [ ] **Step 6: Manual verification in Room_01**

Open Room_01 with `mcp__ai-game-developer__scene-open`. Enter PlayMode with `mcp__ai-game-developer__editor-application-set-state` (action: enter playmode). Move the player into a wall — guard should still patrol/oscillate. If you have a guard near a wall, **lure it** (lever or noise) and confirm it does NOT pass through walls. Capture screenshot with `mcp__ai-game-developer__screenshot-game-view`.

Exit playmode.

- [ ] **Step 7: Commit**

```bash
git add Assets/Scripts/Guard/ Assets/Prefabs/Guard_Patrol.prefab Assets/Prefabs/Guard_Static.prefab
git commit -m "fix: guard movement and rotation through Rigidbody2D physics"
```

---

### Task 2: ThrownStone — filter collisions to walls only

**Why:** `OnCollisionEnter2D` actualmente dispara `TriggerNoise` con cualquier roce (otros pickups, guards, decor). Solo deberían contar paredes/superficies que detengan la piedra.

**Files:**
- Test: `Assets/Scripts/Tests/EditMode/ThrownStoneFilterTests.cs`
- Modify: `Assets/Scripts/World/ThrownStone.cs`
- Modify: `Assets/Prefabs/ThrownStone.prefab` (set new mask field)

- [ ] **Step 1: Write the failing test**

Path: `Assets/Scripts/Tests/EditMode/ThrownStoneFilterTests.cs`

```csharp
using NUnit.Framework;

public class ThrownStoneFilterTests
{
    [Test]
    public void ShouldTriggerNoise_WhenLayerMatchesMask_ReturnsTrue()
    {
        int mask = 1 << 8;        // "Walls" layer
        int hitLayer = 8;
        Assert.IsTrue(ThrownStone.ShouldTriggerNoise(mask, hitLayer));
    }

    [Test]
    public void ShouldTriggerNoise_WhenLayerOutsideMask_ReturnsFalse()
    {
        int mask = 1 << 8;
        int hitLayer = 9;        // "Items"
        Assert.IsFalse(ThrownStone.ShouldTriggerNoise(mask, hitLayer));
    }

    [Test]
    public void ShouldTriggerNoise_WhenMaskIsEmpty_ReturnsFalse()
    {
        Assert.IsFalse(ThrownStone.ShouldTriggerNoise(0, 8));
    }
}
```

- [ ] **Step 2: Run tests and verify they fail**

Use `mcp__ai-game-developer__tests-run` with `testMode: "EditMode"`.
Expected: 3 fails (`ShouldTriggerNoise` does not exist yet).

- [ ] **Step 3: Update `ThrownStone.cs`**

Replace the file contents with:

```csharp
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class ThrownStone : MonoBehaviour
{
    [SerializeField] float speed = 8f;
    [SerializeField] LayerMask noiseTriggerLayers;

    Rigidbody2D _rb;

    void Awake() => _rb = GetComponent<Rigidbody2D>();

    public void Launch(Vector2 direction) =>
        _rb.linearVelocity = direction.normalized * speed;

    void OnCollisionEnter2D(Collision2D col)
    {
        if (!ShouldTriggerNoise(noiseTriggerLayers.value, col.gameObject.layer)) return;
        GetComponent<NoiseSource>().TriggerNoise();
    }

    public static bool ShouldTriggerNoise(int mask, int layer) =>
        (mask & (1 << layer)) != 0;
}
```

- [ ] **Step 4: Run tests and verify they pass**

Use `mcp__ai-game-developer__tests-run` with `testMode: "EditMode"`.
Expected: 4 passed (the 3 new + the SanityTest).

- [ ] **Step 5: Configure the ThrownStone prefab mask**

Open `Assets/Prefabs/ThrownStone.prefab` with `mcp__ai-game-developer__assets-prefab-open`. Use `mcp__ai-game-developer__gameobject-component-modify` on the `ThrownStone` script: set `noiseTriggerLayers` to include the `Walls` layer (layer 8 according to `ProjectSettings/TagManager.asset`). Save and close.

- [ ] **Step 6: Manual playtest in Room_02**

Open Room_02, enter playmode, walk to player, throw a stone (F) at a wall — guard should react. Throw another stone past a guard at empty space — when it stops via friction it should NOT trigger again (it only collides on enter; verify no spurious alert). Screenshot.

- [ ] **Step 7: Commit**

```bash
git add Assets/Scripts/World/ThrownStone.cs Assets/Scripts/Tests/EditMode/ThrownStoneFilterTests.cs Assets/Prefabs/ThrownStone.prefab
git commit -m "fix: thrown stone only triggers noise on configured wall layers"
```

---

### Task 3: NoiseSource alerts ALL guards in radius

**Why:** El comportamiento actual de elegir solo el guard más cercano es contraintuitivo. Si el ruido suena cerca de 3 guardias, los 3 deberían reaccionar — eso hace más legible la mecánica.

**Files:**
- Test: `Assets/Scripts/Tests/EditMode/NoiseSourceTests.cs`
- Modify: `Assets/Scripts/World/NoiseSource.cs`

- [ ] **Step 1: Write the failing test**

This is a static logic test. The dispatch semantic is: "given an array of guards, call AlertAt on each that's within radius." We extract the dispatch into a pure-static method to test it.

Path: `Assets/Scripts/Tests/EditMode/NoiseSourceTests.cs`

```csharp
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class NoiseSourceTests
{
    [Test]
    public void GuardsWithinRadius_AllAreSelected()
    {
        Vector2 origin = Vector2.zero;
        var positions = new List<Vector2> {
            new Vector2(1f, 0f),
            new Vector2(0f, 2f),
            new Vector2(10f, 10f)        // out of radius 5
        };
        var indices = NoiseSource.SelectGuardsInRadius(origin, positions, 5f);
        Assert.AreEqual(2, indices.Count);
        Assert.Contains(0, indices);
        Assert.Contains(1, indices);
    }

    [Test]
    public void NoGuardsWithinRadius_ReturnsEmpty()
    {
        var indices = NoiseSource.SelectGuardsInRadius(Vector2.zero,
            new List<Vector2> { new Vector2(100f, 0f) }, 5f);
        Assert.AreEqual(0, indices.Count);
    }
}
```

- [ ] **Step 2: Run tests, expect fail**

Use `mcp__ai-game-developer__tests-run` EditMode. Expected: 2 fails.

- [ ] **Step 3: Update `NoiseSource.cs`**

Replace the file contents with:

```csharp
using System.Collections.Generic;
using UnityEngine;

public class NoiseSource : MonoBehaviour
{
    [SerializeField] float noiseRadius = 8f;
    [SerializeField] LayerMask guardLayer;

    public void TriggerNoise()
    {
        Collider2D[] guards = Physics2D.OverlapCircleAll(transform.position, noiseRadius, guardLayer);
        foreach (var col in guards)
        {
            var g = col.GetComponent<GuardBase>();
            if (g != null) g.AlertAt(transform.position);
        }
        Destroy(gameObject);
    }

    public static List<int> SelectGuardsInRadius(Vector2 origin, IList<Vector2> positions, float radius)
    {
        var result = new List<int>();
        for (int i = 0; i < positions.Count; i++)
        {
            if (Vector2.Distance(origin, positions[i]) <= radius)
                result.Add(i);
        }
        return result;
    }
}
```

- [ ] **Step 4: Run tests, expect pass**

Expected: all EditMode tests pass.

- [ ] **Step 5: Manual playtest**

Place 2 guards within range in a test scene (or use Room_02 if it has them), throw stone — both should turn alerted at the same time.

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/World/NoiseSource.cs Assets/Scripts/Tests/EditMode/NoiseSourceTests.cs
git commit -m "feat: noise alerts all guards in radius, not just nearest"
```

---

### Task 4: Door defensive components

**Why:** Si alguien arrastra el componente `Door` a un GameObject sin `SpriteRenderer` o `Collider2D`, hay un null-ref silencioso. Un `[RequireComponent]` lo previene en editor.

**Files:**
- Modify: `Assets/Scripts/World/Door.cs`

- [ ] **Step 1: Update `Door.cs` header**

Edit the file — change line 3 to add `[RequireComponent]`:

```csharp
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public class Door : MonoBehaviour
{
    // ... rest unchanged
}
```

(Keep the rest of the file as-is for now — Task 8 will add the tween.)

- [ ] **Step 2: Verify compile in Unity**

`mcp__ai-game-developer__console-get-logs`. Expected: no errors.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/World/Door.cs
git commit -m "fix: enforce Door dependencies via RequireComponent"
```

---

## Phase 2 — Feel / UX

### Task 5: Spotted detection timer

**Why:** Hoy la detección es instantánea y dura. Un pequeño delay (~0.4s) durante el cual el guard ya está visualmente alertado pero la vida no se pierde da al jugador chance de esconderse y se siente mucho mejor. Si player sigue visible al final del delay, recién entonces se llama a `GameManager.PlayerDetected`.

**Files:**
- Modify: `Assets/Scripts/Guard/VisionCone.cs`
- Modify: `Assets/Scripts/Guard/GuardBase.cs`

- [ ] **Step 1: Add a sustained-detection event to `VisionCone.cs`**

The existing `OnPlayerDetected` fires every frame the player is in cone+los. We'll keep that, but rename semantics: it's now "currently seeing player". GuardBase will gate the `GameManager.PlayerDetected` behind a sustain timer.

Edit `VisionCone.cs` — replace `CheckDetection` with:

```csharp
public bool IsSeeingPlayer { get; private set; }

void CheckDetection()
{
    bool sees = false;
    Collider2D hit = Physics2D.OverlapCircle(transform.position, distance, playerLayer);
    if (hit != null)
    {
        Vector2 toPlayer = hit.transform.position - transform.position;
        float angleTo = Vector2.Angle(transform.up, toPlayer);
        if (angleTo < angle / 2f)
        {
            RaycastHit2D los = Physics2D.Raycast(transform.position, toPlayer.normalized, distance, wallLayer);
            if (!los) sees = true;
        }
    }
    IsSeeingPlayer = sees;
    if (sees) OnPlayerDetected?.Invoke();
}
```

(The `OnPlayerDetected` event is preserved so existing code keeps compiling, but we now also expose `IsSeeingPlayer` per-frame.)

- [ ] **Step 2: Update `GuardBase.cs` to use a sustain timer**

Replace the body with:

```csharp
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public abstract class GuardBase : MonoBehaviour
{
    protected enum GuardState { Normal, Alerted }
    protected GuardState State = GuardState.Normal;

    [SerializeField] protected float alertDuration = 3f;
    [SerializeField] protected float spotSustainSeconds = 0.4f;

    protected VisionCone VisionCone;
    protected Rigidbody2D Rb;

    float _spotTimer;
    bool _spotConfirmed;

    protected virtual void Awake()
    {
        Rb = GetComponent<Rigidbody2D>();
        Rb.bodyType = RigidbodyType2D.Kinematic;
        Rb.gravityScale = 0f;
        VisionCone = GetComponentInChildren<VisionCone>();
        // Note: previously we subscribed to VisionCone.OnPlayerDetected here.
        // Now we poll VisionCone.IsSeeingPlayer in Update so we can apply a sustain timer.
    }

    void Update()
    {
        if (_spotConfirmed) return;
        if (VisionCone.IsSeeingPlayer)
        {
            _spotTimer += Time.deltaTime;
            // First sighting: switch material immediately for feedback
            if (State == GuardState.Normal)
            {
                State = GuardState.Alerted;
                VisionCone.SetAlerted(true);
            }
            if (_spotTimer >= spotSustainSeconds)
            {
                _spotConfirmed = true;
                OnAlerted();
            }
        }
        else
        {
            _spotTimer = Mathf.Max(0f, _spotTimer - Time.deltaTime);
            if (_spotTimer == 0f && State == GuardState.Alerted && !_spotConfirmed)
            {
                // Visual de-alert if we never confirmed
                StartCoroutine(ReturnToNormalAfter(alertDuration));
            }
        }
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
        if (_spotConfirmed) yield break;
        State = GuardState.Normal;
        VisionCone.SetAlerted(false);
        OnReturnToNormal();
    }

    protected virtual void OnAlerted() => GameManager.Instance.PlayerDetected();
    protected virtual void OnNoiseAlerted(Vector2 position) { }
    protected virtual void OnReturnToNormal() { }
}
```

Note: `GuardPatrol` already overrides `Update` (changed to `FixedUpdate` in Task 1 — verify this is the case before this task starts; if not, rename in `GuardPatrol.cs` so its movement uses `FixedUpdate` and the `Update` here owns the timer).

- [ ] **Step 3: Verify compile**

Console logs: 0 errors.

- [ ] **Step 4: Manual playtest**

Stand right at the edge of a guard's cone for under 0.4s, then duck behind a wall. Expected: cone goes red briefly, but you do NOT lose a life. Then walk fully into the cone and stand for 1s — life lost, scene reloads.

Capture before/after screenshots with `screenshot-game-view`.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Guard/VisionCone.cs Assets/Scripts/Guard/GuardBase.cs
git commit -m "feat: spotted detection requires 0.4s sustain before life loss"
```

---

### Task 6: Player movement smoothing

**Why:** El movimiento on/off se siente robótico. Un acceleration/deceleration corto da inercia sin alterar el game-feel del puzzle.

**Files:**
- Test: `Assets/Scripts/Tests/EditMode/PlayerMovementSmoothingTests.cs`
- Modify: `Assets/Scripts/Player/PlayerMovement.cs`

- [ ] **Step 1: Write the failing test**

Path: `Assets/Scripts/Tests/EditMode/PlayerMovementSmoothingTests.cs`

```csharp
using NUnit.Framework;
using UnityEngine;

public class PlayerMovementSmoothingTests
{
    [Test]
    public void StepVelocity_FromZeroTowardMax_RespectsAcceleration()
    {
        Vector2 current = Vector2.zero;
        Vector2 target  = new Vector2(4f, 0f);
        float accel = 20f;
        float dt    = 0.02f;

        Vector2 next = PlayerMovement.StepVelocity(current, target, accel, dt);
        // delta should be at most accel * dt = 0.4
        Assert.AreEqual(0.4f, next.x, 0.0001f);
        Assert.AreEqual(0f,   next.y, 0.0001f);
    }

    [Test]
    public void StepVelocity_AlreadyAtTarget_DoesNotOvershoot()
    {
        Vector2 current = new Vector2(4f, 0f);
        Vector2 target  = new Vector2(4f, 0f);
        Vector2 next = PlayerMovement.StepVelocity(current, target, 20f, 0.02f);
        Assert.AreEqual(target, next);
    }

    [Test]
    public void StepVelocity_DecelerationCase_DoesNotOvershootZero()
    {
        Vector2 current = new Vector2(0.3f, 0f);
        Vector2 target  = Vector2.zero;
        Vector2 next = PlayerMovement.StepVelocity(current, target, 20f, 0.02f);
        // accel*dt = 0.4 > 0.3 → must clamp to zero, not -0.1
        Assert.AreEqual(0f, next.x, 0.0001f);
    }
}
```

- [ ] **Step 2: Run tests, expect fail**

Expected: 3 fails (`StepVelocity` not defined).

- [ ] **Step 3: Update `PlayerMovement.cs`**

Replace the file contents with:

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] float speed = 4f;
    [SerializeField] float acceleration = 30f;

    Rigidbody2D _rb;
    Vector2 _velocity;

    void Awake() => _rb = GetComponent<Rigidbody2D>();

    void FixedUpdate()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        float h = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1f : 0f)
                - (kb.aKey.isPressed || kb.leftArrowKey.isPressed ? 1f : 0f);
        float v = (kb.wKey.isPressed || kb.upArrowKey.isPressed ? 1f : 0f)
                - (kb.sKey.isPressed || kb.downArrowKey.isPressed ? 1f : 0f);
        Vector2 target = new Vector2(h, v).normalized * speed;
        _velocity = StepVelocity(_velocity, target, acceleration, Time.fixedDeltaTime);
        _rb.MovePosition(_rb.position + _velocity * Time.fixedDeltaTime);
    }

    public static Vector2 StepVelocity(Vector2 current, Vector2 target, float acceleration, float deltaTime)
    {
        float maxDelta = acceleration * deltaTime;
        return Vector2.MoveTowards(current, target, maxDelta);
    }
}
```

- [ ] **Step 4: Run tests, expect pass**

Expected: all EditMode tests pass.

- [ ] **Step 5: Manual playtest**

Walk forward and stop — player should glide briefly to a stop, not freeze instantly. Screenshot.

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/Player/PlayerMovement.cs Assets/Scripts/Tests/EditMode/PlayerMovementSmoothingTests.cs
git commit -m "feat: smooth player acceleration/deceleration"
```

---

### Task 7: Door open/close tween

**Why:** El cambio de sprite/collider es instantáneo. Un fade + escala punch sobre 0.25s es suficiente para que se sienta físico.

**Files:**
- Modify: `Assets/Scripts/World/Door.cs`

- [ ] **Step 1: Update `Door.cs` with a coroutine tween**

Replace the file contents with:

```csharp
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public class Door : MonoBehaviour
{
    [SerializeField] Sprite closedSprite;
    [SerializeField] Sprite openSprite;
    [SerializeField] float tweenSeconds = 0.25f;

    SpriteRenderer _sr;
    Collider2D _col;
    bool _open;
    Coroutine _running;

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _col = GetComponent<Collider2D>();
    }

    public void Open()
    {
        if (_open) return;
        _open = true;
        _col.enabled = false;
        if (_running != null) StopCoroutine(_running);
        _running = StartCoroutine(Tween(closedSprite, openSprite));
    }

    public void Toggle() { if (_open) Close(); else Open(); }

    void Close()
    {
        _open = false;
        _col.enabled = true;
        if (_running != null) StopCoroutine(_running);
        _running = StartCoroutine(Tween(openSprite, closedSprite));
    }

    IEnumerator Tween(Sprite from, Sprite to)
    {
        Vector3 startScale = transform.localScale;
        Vector3 punch = startScale * 1.1f;
        float t = 0f;
        // Punch up
        while (t < tweenSeconds * 0.5f)
        {
            t += Time.deltaTime;
            float u = t / (tweenSeconds * 0.5f);
            transform.localScale = Vector3.Lerp(startScale, punch, u);
            yield return null;
        }
        _sr.sprite = to;
        // Punch down
        t = 0f;
        while (t < tweenSeconds * 0.5f)
        {
            t += Time.deltaTime;
            float u = t / (tweenSeconds * 0.5f);
            transform.localScale = Vector3.Lerp(punch, startScale, u);
            yield return null;
        }
        transform.localScale = startScale;
    }
}
```

- [ ] **Step 2: Verify compile and playtest**

Open Room_01 — pick up key, watch door punch+swap. The collider disables instantly (so the player can walk through), but the visual takes 0.25s, which is fine.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/World/Door.cs
git commit -m "feat: door open/close tween (scale punch + sprite swap)"
```

---

### Task 8: Spawn point system

**Why:** Cuando el player muere, `SceneManager.LoadScene` recarga la escena y player vuelve al origen del prefab — no hay control sobre dónde respawnea. Un `SpawnPoint` por escena hace explícita esa posición y permite mover el origen sin tocar prefabs.

**Files:**
- Create: `Assets/Scripts/Core/SpawnPoint.cs`
- Modify: `Assets/Scripts/Core/GameManager.cs`
- Modify: `Assets/Scenes/Room_01.unity` (add SpawnPoint GO)
- Modify: `Assets/Scenes/Room_02.unity` (add SpawnPoint GO)

- [ ] **Step 1: Create `SpawnPoint.cs`**

Path: `Assets/Scripts/Core/SpawnPoint.cs`

```csharp
using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
        Gizmos.DrawLine(transform.position, transform.position + transform.up * 0.6f);
    }
}
```

- [ ] **Step 2: Update `GameManager.cs` to position the player on scene load**

Add this method and hook it to `SceneManager.sceneLoaded`. Replace `GameManager.cs` with:

```csharp
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

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
        _currentRoomScene = SceneManager.GetActiveScene().name;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AutoCreate()
    {
        if (Instance != null) return;
        var go = new GameObject("GameManager");
        go.AddComponent<GameManager>();

        if (Object.FindObjectOfType<EventSystem>() == null)
        {
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<InputSystemUIInputModule>();
            Object.DontDestroyOnLoad(esGO);
        }
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

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        var spawn = Object.FindFirstObjectByType<SpawnPoint>();
        if (spawn == null) return;
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;
        player.transform.SetPositionAndRotation(spawn.transform.position, spawn.transform.rotation);
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }
}
```

- [ ] **Step 3: Add SpawnPoint GameObject to each room scene**

For each scene (`Room_01.unity`, `Room_02.unity`):
- `mcp__ai-game-developer__scene-open`
- `mcp__ai-game-developer__gameobject-create` — name `"SpawnPoint"`, position the player's current spawn (open the scene first via MCP and copy the player's current `transform.position`).
- `mcp__ai-game-developer__gameobject-component-add` — type `SpawnPoint`.
- `mcp__ai-game-developer__scene-save`.

- [ ] **Step 4: Manual verification**

Move the SpawnPoint in Room_01 to a different corner and enter playmode — player should appear at that corner.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Core/SpawnPoint.cs Assets/Scripts/Core/GameManager.cs Assets/Scenes/Room_01.unity Assets/Scenes/Room_02.unity
git commit -m "feat: per-scene SpawnPoint controls player respawn position"
```

---

## Phase 3 — Visuals

### Task 9: URP 2D global lighting + wall shadows

**Why:** El proyecto está en URP 2D Renderer pero no usa Light2D. Agregar un Global Light tenue + dos Spot/Point lights por sala + Shadow Caster 2D en walls da el salto visual más grande con menos esfuerzo.

**Files:**
- Modify: `Assets/Scenes/Room_01.unity`
- Modify: `Assets/Scenes/Room_02.unity`
- Modify: walls within each scene (Shadow Caster 2D component)

This task is **scene/asset-only** — no script changes. There is no automated test path; we verify with the screenshot tool.

- [ ] **Step 1: Confirm URP 2D Renderer is active**

Open `Assets/Settings/Renderer2D.asset` with `mcp__ai-game-developer__assets-get-data`. Confirm it's a `Renderer2DData`. Open `Assets/Settings/UniversalRP.asset` and confirm `m_RendererDataList` references `Renderer2D.asset`. If yes, lights will work.

- [ ] **Step 2: Add Global Light 2D to Room_01**

`mcp__ai-game-developer__scene-open` Room_01.

`mcp__ai-game-developer__gameobject-create` — name `"GlobalLight2D"`, position `(0,0,0)`.

`mcp__ai-game-developer__gameobject-component-add` — type `UnityEngine.Rendering.Universal.Light2D`.

`mcp__ai-game-developer__gameobject-component-modify` — set:
- `m_LightType = 1` (Global)
- `m_Color = (0.18, 0.18, 0.28, 1)` (cool dim)
- `m_Intensity = 0.5`

- [ ] **Step 3: Add 2 Point Lights to Room_01 for atmosphere**

For each, create a GameObject, add `Light2D`, configure:
- `m_LightType = 0` (Point)
- `m_Color = (1.0, 0.85, 0.55, 1)` (warm torch)
- `m_Intensity = 1.4`
- `m_OuterRadius = 4.5`
- `m_InnerRadius = 0.5`
- `m_FalloffIntensity = 0.5`
- `m_ShadowsEnabled = true`

Place at corners of the room aesthetically (open the scene visually first, screenshot, decide).

- [ ] **Step 4: Add ShadowCaster2D to wall objects in Room_01**

`mcp__ai-game-developer__gameobject-find` the walls (likely tagged `"Walls"` or in layer 8). For each:

`mcp__ai-game-developer__gameobject-component-add` — type `UnityEngine.Rendering.Universal.ShadowCaster2D`.

Modify: `m_SelfShadows = false`, `m_CastsShadows = true`. Walls usually have BoxCollider2D — `ShadowCaster2D.useRendererSilhouette` should be off; we'll let it auto-generate from the collider shape.

(If the walls are a single tilemap, add the ShadowCaster2D to a parent and use `CompositeShadowCaster2D` on the parent; check if walls in this scene are individual or tilemap before deciding.)

- [ ] **Step 5: Save Room_01, screenshot for review**

`mcp__ai-game-developer__scene-save`. Enter playmode, screenshot. Compare with pre-lighting screenshot.

- [ ] **Step 6: Repeat for Room_02**

Same steps. Use slightly different light colors/positions to differentiate the rooms.

- [ ] **Step 7: Commit**

```bash
git add Assets/Scenes/Room_01.unity Assets/Scenes/Room_02.unity
git commit -m "feat: URP 2D lighting with shadow casters in rooms"
```

---

### Task 10: Player simple animation (idle/walk)

**Why:** El player es un sprite estático. Una animación simple de 2-4 frames de idle y walk se nota inmediatamente. Si no hay arte disponible, generamos sprites por código (escala oscilante en SpriteRenderer + tint sutil) usando un Animator.

**Files:**
- Create: `Assets/Animations/Player/Player.controller`
- Create: `Assets/Animations/Player/PlayerIdle.anim`
- Create: `Assets/Animations/Player/PlayerWalk.anim`
- Modify: `Assets/Prefabs/Player.prefab` (add Animator)
- Modify: `Assets/Scripts/Player/PlayerMovement.cs` (set `Speed` parameter)

**Honest constraint:** Sin sprite sheet real, la animación va a usar el mismo sprite en todos los frames pero con cambios en `localScale` (rebote sutil tipo "bob") como AnimationClip. Es feo en gif aislado pero en juego top-down, contundente.

- [ ] **Step 1: Create Animator Controller via Unity**

Use `mcp__ai-game-developer__script-execute` with this Roslyn snippet to create the controller:

```csharp
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using System.IO;

public static class CreatePlayerController
{
    public static void Run()
    {
        const string folder = "Assets/Animations/Player";
        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
        AssetDatabase.Refresh();

        var ctrl = AnimatorController.CreateAnimatorControllerAtPath($"{folder}/Player.controller");
        ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);

        // Idle clip — barely-visible bob
        var idle = new AnimationClip { name = "PlayerIdle", frameRate = 12 };
        var curve = new AnimationCurve(
            new Keyframe(0f, 1f), new Keyframe(0.5f, 1.02f), new Keyframe(1f, 1f));
        idle.SetCurve("", typeof(Transform), "m_LocalScale.x", curve);
        idle.SetCurve("", typeof(Transform), "m_LocalScale.y", curve);
        idle.wrapMode = WrapMode.Loop;
        var idleSettings = AnimationUtility.GetAnimationClipSettings(idle);
        idleSettings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(idle, idleSettings);
        AssetDatabase.CreateAsset(idle, $"{folder}/PlayerIdle.anim");

        // Walk clip — stronger bob
        var walk = new AnimationClip { name = "PlayerWalk", frameRate = 12 };
        var walkCurve = new AnimationCurve(
            new Keyframe(0f, 0.95f), new Keyframe(0.15f, 1.05f),
            new Keyframe(0.30f, 0.95f), new Keyframe(0.45f, 1.05f),
            new Keyframe(0.60f, 0.95f));
        walk.SetCurve("", typeof(Transform), "m_LocalScale.x", walkCurve);
        walk.SetCurve("", typeof(Transform), "m_LocalScale.y", walkCurve);
        walk.wrapMode = WrapMode.Loop;
        var walkSettings = AnimationUtility.GetAnimationClipSettings(walk);
        walkSettings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(walk, walkSettings);
        AssetDatabase.CreateAsset(walk, $"{folder}/PlayerWalk.anim");

        // States
        var sm = ctrl.layers[0].stateMachine;
        var idleState = sm.AddState("Idle");
        idleState.motion = idle;
        var walkState = sm.AddState("Walk");
        walkState.motion = walk;
        sm.defaultState = idleState;

        // Transitions
        var toWalk = idleState.AddTransition(walkState);
        toWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        toWalk.hasExitTime = false;
        toWalk.duration = 0.05f;

        var toIdle = walkState.AddTransition(idleState);
        toIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        toIdle.hasExitTime = false;
        toIdle.duration = 0.05f;

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[CreatePlayerController] Done");
    }
}
```

Method to call: `CreatePlayerController.Run`.

Verify console for `"[CreatePlayerController] Done"` and absence of errors.

- [ ] **Step 2: Add Animator to Player prefab**

`mcp__ai-game-developer__assets-prefab-open` `Assets/Prefabs/Player.prefab`.

`mcp__ai-game-developer__gameobject-component-add` — type `Animator`.

`mcp__ai-game-developer__gameobject-component-modify` — set `m_Controller` to point at `Assets/Animations/Player/Player.controller` (use the asset GUID from `mcp__ai-game-developer__assets-find` with filter `Player.controller`).

Save and close prefab.

- [ ] **Step 3: Update `PlayerMovement.cs` to set `Speed` param**

Edit the file. After `_velocity = ...` line, before `_rb.MovePosition`, add an Animator hook:

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] float speed = 4f;
    [SerializeField] float acceleration = 30f;

    Rigidbody2D _rb;
    Animator _animator;
    Vector2 _velocity;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _animator = GetComponentInChildren<Animator>();
    }

    void FixedUpdate()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        float h = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1f : 0f)
                - (kb.aKey.isPressed || kb.leftArrowKey.isPressed ? 1f : 0f);
        float v = (kb.wKey.isPressed || kb.upArrowKey.isPressed ? 1f : 0f)
                - (kb.sKey.isPressed || kb.downArrowKey.isPressed ? 1f : 0f);
        Vector2 target = new Vector2(h, v).normalized * speed;
        _velocity = StepVelocity(_velocity, target, acceleration, Time.fixedDeltaTime);
        _rb.MovePosition(_rb.position + _velocity * Time.fixedDeltaTime);

        if (_animator != null) _animator.SetFloat("Speed", _velocity.magnitude);
    }

    public static Vector2 StepVelocity(Vector2 current, Vector2 target, float acceleration, float deltaTime)
    {
        float maxDelta = acceleration * deltaTime;
        return Vector2.MoveTowards(current, target, maxDelta);
    }
}
```

Note: animator is on a child if needed (sprite child), or on the same GO as the SpriteRenderer. `GetComponentInChildren` covers both.

- [ ] **Step 4: Run EditMode tests, then playtest**

EditMode tests should all still pass. Then playtest — player should bob slightly when idle and stronger while moving. Screenshot.

- [ ] **Step 5: Commit**

```bash
git add Assets/Animations/ Assets/Prefabs/Player.prefab Assets/Scripts/Player/PlayerMovement.cs
git commit -m "feat: simple bob animator for player idle/walk"
```

---

### Task 11: Vision cone gradient material

**Why:** Hoy el cono es un triángulo opaco. Un gradient hacia los bordes (alpha cae con la distancia) lee mucho mejor.

**Files:**
- Create: `Assets/Shaders/VisionConeGradient.shadergraph` (or fallback Unlit material with vertex color)
- Create: `Assets/Materials/VisionConeGradient.mat`
- Modify: `Assets/Scripts/Guard/VisionCone.cs` (vertex colors with alpha falloff)

**Approach:** En vez de Shader Graph (complejo de versionar), usamos un Unlit material con `Sprite-Default` o `Universal Render Pipeline/2D/Sprite-Lit-Default` y le aplicamos vertex colors desde el script. El mesh ya tiene vértices controlados — solo agregamos colors con alpha que decrece desde el origen hacia los extremos.

- [ ] **Step 1: Create the cone material**

`mcp__ai-game-developer__assets-material-create`:
- path: `Assets/Materials/VisionConeNormal.mat`
- shaderName: `Sprites/Default` (vertex color supported and transparent)

Modify the material:
- `_Color = (1, 1, 0.3, 1)` (yellow base for normal)

Repeat for `Assets/Materials/VisionConeAlert.mat` with `_Color = (1, 0.2, 0.2, 1)` (red).

- [ ] **Step 2: Update `VisionCone.cs` to assign vertex colors with alpha falloff**

Replace the file with:

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
    [SerializeField, Range(0f, 1f)] float originAlpha = 0.6f;
    [SerializeField, Range(0f, 1f)] float edgeAlpha = 0.05f;

    MeshFilter _mf;
    MeshRenderer _mr;
    Mesh _mesh;

    public bool IsSeeingPlayer { get; private set; }
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
        Color[] colors = new Color[rayCount + 2];

        vertices[0] = Vector3.zero;
        colors[0]   = new Color(1, 1, 1, originAlpha);

        for (int i = 0; i <= rayCount; i++)
        {
            float currentAngle = -halfAngle + angleStep * i;
            float rad = currentAngle * Mathf.Deg2Rad;
            Vector2 localDir = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad));
            Vector2 worldDir = transform.TransformDirection(localDir);
            RaycastHit2D hit = Physics2D.Raycast(transform.position, worldDir, distance, wallLayer);
            Vector3 point = hit ? transform.InverseTransformPoint(hit.point)
                                : (Vector3)(localDir * distance);
            vertices[i + 1] = point;
            colors[i + 1]   = new Color(1, 1, 1, edgeAlpha);
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
        _mesh.colors = colors;
        _mesh.RecalculateNormals();
    }

    void CheckDetection()
    {
        bool sees = false;
        Collider2D hit = Physics2D.OverlapCircle(transform.position, distance, playerLayer);
        if (hit != null)
        {
            Vector2 toPlayer = hit.transform.position - transform.position;
            float angleTo = Vector2.Angle(transform.up, toPlayer);
            if (angleTo < angle / 2f)
            {
                RaycastHit2D los = Physics2D.Raycast(transform.position, toPlayer.normalized, distance, wallLayer);
                if (!los) sees = true;
            }
        }
        IsSeeingPlayer = sees;
        if (sees) OnPlayerDetected?.Invoke();
    }

    public void SetAlerted(bool alerted) =>
        _mr.material = alerted ? alertMaterial : normalMaterial;
}
```

Note: `DirFromAngle` removed (was dead code).

- [ ] **Step 3: Assign new materials in vision cone instances**

For each guard prefab (`Guard_Patrol`, `Guard_Static`):
- Open prefab
- Find the VisionCone child GameObject
- Modify the `VisionCone` component:
  - `normalMaterial` → `Assets/Materials/VisionConeNormal.mat`
  - `alertMaterial` → `Assets/Materials/VisionConeAlert.mat`
- Save & close

- [ ] **Step 4: Manual playtest + screenshot**

Run Room_01. Cono debería tener fade hacia los bordes. Screenshot.

- [ ] **Step 5: Commit**

```bash
git add Assets/Materials/VisionConeNormal.mat Assets/Materials/VisionConeAlert.mat Assets/Scripts/Guard/VisionCone.cs Assets/Prefabs/Guard_Patrol.prefab Assets/Prefabs/Guard_Static.prefab
git commit -m "feat: vision cone gradient via vertex colors and removed dead code"
```

---

### Task 12: HUD heart sprites + alert flash

**Why:** El HUD usa color tint sobre el mismo `Image`. Mejor usar dos sprites (heart_full / heart_empty) para que la silueta sea reconocible. También un flash rojo de pantalla cuando se pierde una vida da feedback fuerte.

**Files:**
- Create: `Assets/Sprites/UI/heart_full.png` (or generated via script)
- Create: `Assets/Sprites/UI/heart_empty.png`
- Modify: `Assets/Scripts/UI/HUDManager.cs`

**Honest constraint:** Sin arte real, generamos los sprites de corazón vía script (igual que se hizo en commit `3e0be31`). El asset se guarda como PNG en `Assets/Sprites/UI/`.

- [ ] **Step 1: Generate heart sprites via Roslyn script**

Use `mcp__ai-game-developer__script-execute`:

```csharp
using UnityEngine;
using UnityEditor;
using System.IO;

public static class GenerateHearts
{
    public static void Run()
    {
        const string folder = "Assets/Sprites/UI";
        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

        WriteHeart($"{folder}/heart_full.png",  new Color(1f, 0.15f, 0.15f, 1f));
        WriteHeart($"{folder}/heart_empty.png", new Color(0.25f, 0.25f, 0.25f, 0.5f));

        AssetDatabase.Refresh();
        foreach (var path in new[] { $"{folder}/heart_full.png", $"{folder}/heart_empty.png" })
        {
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = TextureImporterType.Sprite;
            imp.spritePixelsPerUnit = 64;
            imp.SaveAndReimport();
        }
        Debug.Log("[GenerateHearts] Done");
    }

    static void WriteHeart(string path, Color c)
    {
        const int W = 64, H = 64;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        var clear = new Color(0, 0, 0, 0);
        for (int y = 0; y < H; y++)
        for (int x = 0; x < W; x++)
        {
            float fx = (x - W / 2f) / (W / 2f);
            float fy = (y - H / 2f) / (H / 2f);
            // Heart implicit: ((x^2 + y^2 - 1)^3 - x^2*y^3) <= 0  (with y flipped)
            float yy = -fy + 0.2f;
            float lhs = (fx * fx + yy * yy - 1f);
            float val = lhs * lhs * lhs - fx * fx * yy * yy * yy;
            tex.SetPixel(x, y, val <= 0 ? c : clear);
        }
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
    }
}
```

Method to call: `GenerateHearts.Run`.

Verify `Assets/Sprites/UI/heart_full.png` and `heart_empty.png` exist via `mcp__ai-game-developer__assets-find` filter `heart_`.

- [ ] **Step 2: Update `HUDManager.cs` to use sprite swap + flash**

Replace contents:

```csharp
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HUDManager : MonoBehaviour
{
    [SerializeField] Image[] heartIcons;
    [SerializeField] Sprite heartFullSprite;
    [SerializeField] Sprite heartEmptySprite;
    [SerializeField] Image inventoryIcon;
    [SerializeField] Image damageFlash;

    PlayerInventory _inventory;
    int _lastLives = -1;

    void Start()
    {
        var player = FindFirstObjectByType<PlayerInventory>();
        if (player != null) _inventory = player;
        if (damageFlash != null)
        {
            var c = damageFlash.color; c.a = 0; damageFlash.color = c;
        }
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
            heartIcons[i].sprite = i < lives ? heartFullSprite : heartEmptySprite;

        if (_lastLives > -1 && lives < _lastLives && damageFlash != null)
            StartCoroutine(Flash());
        _lastLives = lives;
    }

    IEnumerator Flash()
    {
        var c = damageFlash.color;
        c.a = 0.5f; damageFlash.color = c;
        float t = 0f;
        while (t < 0.4f)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(0.5f, 0f, t / 0.4f);
            damageFlash.color = c;
            yield return null;
        }
        c.a = 0; damageFlash.color = c;
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

- [ ] **Step 3: Wire heart sprites + damage flash in scenes**

For each scene with HUD (`Room_01.unity`, `Room_02.unity`):
- Find the HUD GameObject (has `HUDManager` component)
- Set `heartFullSprite` and `heartEmptySprite` to the new assets via `mcp__ai-game-developer__gameobject-component-modify`
- For each Image in `heartIcons`, ensure `Image.sprite` is `heart_full` initially
- Create a fullscreen `Image` (red, alpha 0) under Canvas covering the screen, and assign to `damageFlash` field

- [ ] **Step 4: Manual playtest**

Walk into a guard cone, lose a life — should see red flash. Hearts should switch sprite (not just tint).

- [ ] **Step 5: Commit**

```bash
git add Assets/Sprites/UI/ Assets/Scripts/UI/HUDManager.cs Assets/Scenes/Room_01.unity Assets/Scenes/Room_02.unity
git commit -m "feat: HUD heart sprites and damage screen flash"
```

---

## Phase 4 — Code Quality

### Task 13: Cache Camera.main + remove dead code + minor cleanup

**Why:** `Camera.main` busca por tag cada llamada — barato pero innecesariamente repetido. `VisionCone.DirFromAngle` ya quedó eliminado en Task 12. Aquí limpiamos `PlayerInteraction` y verificamos el resto.

**Files:**
- Modify: `Assets/Scripts/Player/PlayerInteraction.cs`

- [ ] **Step 1: Cache Camera.main in PlayerInteraction**

Replace contents:

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInventory))]
public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] float interactRadius = 1f;
    [SerializeField] LayerMask interactLayer;
    [SerializeField] LayerMask itemLayer;

    PlayerInventory _inventory;
    Camera _cam;

    void Awake()
    {
        _inventory = GetComponent<PlayerInventory>();
        _cam = Camera.main;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.eKey.wasPressedThisFrame) TryInteract();
        if (kb.fKey.wasPressedThisFrame) TryThrow();
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
        if (_cam == null) _cam = Camera.main;        // re-fetch if scene reload swapped cameras
        if (_cam == null) return;
        var stone = _inventory.TakeItem() as Stone;
        Vector2 mouseWorld = _cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        stone.Throw(transform.position, mouseWorld);
    }
}
```

(Bonus: gizmo now visualizes the interact radius — helps level design.)

- [ ] **Step 2: Compile, run all EditMode tests**

`tests-run` EditMode. Expected: all pass.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Player/PlayerInteraction.cs
git commit -m "refactor: cache Camera.main and add interact gizmo"
```

---

## Verification (end-to-end)

After each phase the project must remain playable. After all 4 phases:

1. `mcp__ai-game-developer__tests-run` `testMode: "EditMode"` — all tests green.
2. `mcp__ai-game-developer__scene-open` MainMenu → click Jugar → finish Room_01 → finish Room_02 → see win.
3. `mcp__ai-game-developer__console-get-logs` after a full run — no errors.
4. Visual checks (compare screenshots before/after Phase 3 lighting change in `Room_01` and `Room_02`).
5. Manual physics check: throw a stone at a wall (Phase 1, Task 2), get spotted briefly then escape (Phase 2, Task 5), watch door tween (Task 7), die and respawn at SpawnPoint (Task 8).

## Out of scope (explicit non-goals)

- Audio / SFX (separate plan; was an alternative on the menu earlier).
- New rooms / Room_03 (separate plan).
- Cleanup of "todo"-named commits (separate, history-rewriting plan).
- Multi-floor layouts, save system, settings menu.
- Removing `SampleScene.unity` (trivial, can be a one-line follow-up).

## Self-review notes

- Spec coverage: cada uno de los 13 puntos del status review tiene su task. ✓
- Placeholders: ningún paso dice "TODO" o "implement later". Cada step tiene comando o código. ✓
- Type consistency: `IsSeeingPlayer` se introduce en Task 5 dentro de `VisionCone.cs` y se mantiene cuando se reescribe en Task 11 (verificado). `Rb` se introduce en Task 1 en `GuardBase` y se reusa en Task 5 (también verificado).
- Limitación honesta: las tasks visuales (Lighting, Animator, HUD sprites) no son TDD — son verificadas con screenshot + console. Esto está documentado al inicio.
