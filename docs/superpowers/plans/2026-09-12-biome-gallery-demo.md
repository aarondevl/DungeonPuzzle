# Biome Gallery Demo Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build one independent, continuously explorable gallery scene with a central plaza and three distinct biomes that demonstrate the repository's directional characters, lateral heroes, ranger artwork and fire/ice/energy/poison VFX.

**Architecture:** Keep campaign scenes untouched and compose the gallery from a new scene plus reusable gallery prefabs. Reuse the current player, `Flipbook`, `YSort`, `GlowPulse` and `FloatBob`; add focused runtime components for bounded camera following, physical-player trigger filtering, proximity activation, foreground fading and return portals.

**Tech Stack:** Unity 6000.5.0b10, C#, Unity Test Framework/NUnit, URP 2D Renderer and Light2D, Input System, Unity MCP.

**Spec:** `docs/superpowers/specs/2026-09-12-biome-gallery-demo-design.md`

## Global Constraints

- Create `Assets/Scenes/Prototypes/BiomeGallery_Demo.unity`; do not modify Room_01–Room_04 or campaign progression.
- Keep the gallery out of the shipping Build Settings until human visual review approves it.
- Use only art already present in the repository; do not import or generate replacement art.
- Use `Character_base` for the freely moving player, lateral Knight/Rogue/Mage sprites for stationary vignettes, Forest Ranger as large-format display art, and the explosion pack for fire, ice, energy and poison exhibits.
- Do not modify the source sprite import settings or copy raw art into new folders.
- Do not prune any art pack in this implementation.
- Preserve the known local-noise changes in `Assets/Settings/UniversalRP.asset`, the Anton and Oswald SDF assets, `DungeonPuzzle.slnx`, and `ProjectSettings/EditorSettings.asset`; never stage them.
- Do not touch `Assets/_Recovery/` or `Assets/_Recovery.meta`.
- After creating assets through Unity, include their adjacent `.meta` files in the same focused commit.
- Before claiming a visual result, inspect screenshots; before completion, run both full test suites and review the Unity console.
- Do not push any commit unless the user explicitly asks.

## File Map

| File | Responsibility |
|---|---|
| `Assets/Scripts/Gallery/GalleryPlayerContact.cs` | One shared rule that accepts the player's physical collider and rejects its interaction sensor |
| `Assets/Scripts/Gallery/GalleryCameraFollow.cs` | Smoothly follow the player while clamping the orthographic camera to gallery bounds |
| `Assets/Scripts/Gallery/GalleryProximityActivator.cs` | Enable and optionally disable exhibit visuals when the player approaches |
| `Assets/Scripts/Gallery/GalleryReturnPortal.cs` | Return the physical player body to the central plaza without affecting campaign state |
| `Assets/Scripts/Gallery/ForegroundOccluder.cs` | Fade tall foreground renderers while they cover the player |
| `Assets/Scripts/Tests/EditMode/GalleryRuntimeTests.cs` | Pure and component-level coverage for camera, triggers, fades and portals |
| `Assets/Scripts/Tests/EditMode/GalleryAssetTests.cs` | Editor validation of prefab wiring and scene hierarchy |
| `Assets/Prefabs/Gallery/*.prefab` | Reusable character displays, ranger sign and four VFX exhibits |
| `Assets/Scenes/Prototypes/BiomeGallery_Demo.unity` | Central plaza, forest, ruins, marsh, player, camera, lighting, paths and boundaries |
| `Assets/Screenshots/biome-gallery-*.png` | Ignored visual evidence reviewed during implementation |

---

### Task 1: Shared player-contact rule and bounded gallery camera

**Files:**
- Create: `Assets/Scripts/Gallery/GalleryPlayerContact.cs`
- Create: `Assets/Scripts/Gallery/GalleryCameraFollow.cs`
- Create: `Assets/Scripts/Tests/EditMode/GalleryRuntimeTests.cs`

**Interfaces:**
- Consumes: the existing `Player` tag, physical `CircleCollider2D`, interaction-sensor trigger, and an orthographic `Camera`.
- Produces: `GalleryPlayerContact.IsPhysicalPlayer(Collider2D)`, `GalleryCameraFollow.ClampCenter(...)`, and automatic camera target discovery.

- [ ] **Step 1: Write failing contact and camera-bound tests**

Create `GalleryRuntimeTests.cs` with:

```csharp
using NUnit.Framework;
using UnityEngine;

public class GalleryRuntimeTests
{
    GameObject _player;

    [TearDown]
    public void TearDown()
    {
        if (_player != null) Object.DestroyImmediate(_player);
    }

    Collider2D NewPlayerCollider(bool trigger)
    {
        _player = new GameObject("GalleryTestPlayer") { tag = "Player" };
        var collider = _player.AddComponent<CircleCollider2D>();
        collider.isTrigger = trigger;
        return collider;
    }

    [Test]
    public void PhysicalPlayerRule_RejectsInteractionSensor()
    {
        Assert.That(GalleryPlayerContact.IsPhysicalPlayer(NewPlayerCollider(true)), Is.False);
    }

    [Test]
    public void PhysicalPlayerRule_AcceptsSolidPlayerBody()
    {
        Assert.That(GalleryPlayerContact.IsPhysicalPlayer(NewPlayerCollider(false)), Is.True);
    }

    [Test]
    public void CameraClamp_AccountsForOrthographicViewport()
    {
        var result = GalleryCameraFollow.ClampCenter(
            new Vector2(50f, -50f), new Rect(-32f, -20f, 64f, 42f), 6.5f, 16f / 9f);
        Assert.That(result.x, Is.EqualTo(32f - 6.5f * 16f / 9f).Within(0.001f));
        Assert.That(result.y, Is.EqualTo(-13.5f).Within(0.001f));
    }
}
```

- [ ] **Step 2: Run the focused EditMode tests**

Run through Unity MCP:

```text
run_tests(mode="EditMode", test_names=[
  "GalleryRuntimeTests.PhysicalPlayerRule_RejectsInteractionSensor",
  "GalleryRuntimeTests.PhysicalPlayerRule_AcceptsSolidPlayerBody",
  "GalleryRuntimeTests.CameraClamp_AccountsForOrthographicViewport"
])
```

Expected: compilation fails because the two gallery types do not exist.

- [ ] **Step 3: Implement the contact rule**

Create `GalleryPlayerContact.cs`:

```csharp
using UnityEngine;

public static class GalleryPlayerContact
{
    public static bool IsPhysicalPlayer(Collider2D collider) =>
        collider != null && !collider.isTrigger && collider.CompareTag("Player");
}
```

- [ ] **Step 4: Implement the bounded camera**

Create `GalleryCameraFollow.cs`:

```csharp
using UnityEngine;

[RequireComponent(typeof(Camera))]
public sealed class GalleryCameraFollow : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] Rect worldBounds = new(-32f, -20f, 64f, 42f);
    [SerializeField, Min(0.01f)] float sharpness = 6f;

    Camera _camera;

    void Awake() => _camera = GetComponent<Camera>();

    void LateUpdate()
    {
        if (target == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;
            target = player.transform;
        }

        Vector2 center = ClampCenter(target.position, worldBounds,
            _camera.orthographicSize, _camera.aspect);
        var desired = new Vector3(center.x, center.y, transform.position.z);
        float t = 1f - Mathf.Exp(-sharpness * Time.unscaledDeltaTime);
        transform.position = Vector3.Lerp(transform.position, desired, t);
    }

    public static Vector2 ClampCenter(Vector2 desired, Rect bounds, float halfHeight, float aspect)
    {
        float halfWidth = halfHeight * Mathf.Max(0.01f, aspect);
        float minX = bounds.xMin + halfWidth;
        float maxX = bounds.xMax - halfWidth;
        float minY = bounds.yMin + halfHeight;
        float maxY = bounds.yMax - halfHeight;
        float x = minX <= maxX ? Mathf.Clamp(desired.x, minX, maxX) : bounds.center.x;
        float y = minY <= maxY ? Mathf.Clamp(desired.y, minY, maxY) : bounds.center.y;
        return new Vector2(x, y);
    }
}
```

- [ ] **Step 5: Re-run focused tests, inspect console, and commit**

Expected: all three focused tests pass and Unity reports no compilation error.

```bash
git add Assets/Scripts/Gallery Assets/Scripts/Gallery.meta Assets/Scripts/Tests/EditMode/GalleryRuntimeTests.cs Assets/Scripts/Tests/EditMode/GalleryRuntimeTests.cs.meta
git commit -m "feat: add bounded biome gallery camera"
```

---

### Task 2: Proximity exhibits and central return portals

**Files:**
- Create: `Assets/Scripts/Gallery/GalleryProximityActivator.cs`
- Create: `Assets/Scripts/Gallery/GalleryReturnPortal.cs`
- Modify: `Assets/Scripts/Tests/EditMode/GalleryRuntimeTests.cs`

**Interfaces:**
- Consumes: `GalleryPlayerContact.IsPhysicalPlayer(...)` from Task 1.
- Produces: `GalleryProximityActivator.SetInside(bool)` and `GalleryReturnPortal.TryTeleport(Collider2D, float)` for scene wiring and tests.

- [ ] **Step 1: Add failing behavior tests**

Append to `GalleryRuntimeTests`:

```csharp
[Test]
public void ProximityActivator_ChangesAllConfiguredTargets()
{
    var root = new GameObject("Activator");
    var a = new GameObject("A");
    var b = new GameObject("B");
    var activator = root.AddComponent<GalleryProximityActivator>();
    activator.Configure(new[] { a, b }, true);
    activator.SetInside(true);
    Assert.That(a.activeSelf && b.activeSelf, Is.True);
    activator.SetInside(false);
    Assert.That(a.activeSelf || b.activeSelf, Is.False);
    Object.DestroyImmediate(root);
    Object.DestroyImmediate(a);
    Object.DestroyImmediate(b);
}

[Test]
public void ReturnPortal_MovesBodyAndClearsVelocity()
{
    var collider = NewPlayerCollider(false);
    var body = _player.AddComponent<Rigidbody2D>();
    body.gravityScale = 0f;
    body.linearVelocity = new Vector2(3f, 2f);
    var portalObject = new GameObject("Portal");
    var destinationObject = new GameObject("Destination");
    destinationObject.transform.position = new Vector3(4f, -3f);
    var portal = portalObject.AddComponent<GalleryReturnPortal>();
    portal.Configure(destinationObject.transform, 0.35f);
    Assert.That(portal.TryTeleport(collider, 1f), Is.True);
    Assert.That(body.position, Is.EqualTo(new Vector2(4f, -3f)));
    Assert.That(body.linearVelocity, Is.EqualTo(Vector2.zero));
    Object.DestroyImmediate(portalObject);
    Object.DestroyImmediate(destinationObject);
}
```

- [ ] **Step 2: Run both new tests**

Expected: compilation fails because `GalleryProximityActivator` and `GalleryReturnPortal` do not exist.

- [ ] **Step 3: Implement proximity activation**

Create `GalleryProximityActivator.cs`:

```csharp
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class GalleryProximityActivator : MonoBehaviour
{
    [SerializeField] GameObject[] targets;
    [SerializeField] bool deactivateOnExit = true;

    public void Configure(GameObject[] controlledTargets, bool turnOffOnExit)
    {
        targets = controlledTargets;
        deactivateOnExit = turnOffOnExit;
    }

    public void SetInside(bool inside)
    {
        if (!inside && !deactivateOnExit) return;
        if (targets == null) return;
        foreach (var target in targets)
            if (target != null) target.SetActive(inside);
    }

    void Awake() => GetComponent<Collider2D>().isTrigger = true;
    void OnTriggerEnter2D(Collider2D other)
    {
        if (GalleryPlayerContact.IsPhysicalPlayer(other)) SetInside(true);
    }
    void OnTriggerExit2D(Collider2D other)
    {
        if (GalleryPlayerContact.IsPhysicalPlayer(other)) SetInside(false);
    }
}
```

- [ ] **Step 4: Implement the local return portal**

Create `GalleryReturnPortal.cs`:

```csharp
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class GalleryReturnPortal : MonoBehaviour
{
    [SerializeField] Transform destination;
    [SerializeField, Min(0f)] float cooldownSeconds = 0.35f;
    float _lastUse = float.NegativeInfinity;

    public void Configure(Transform target, float cooldown)
    {
        destination = target;
        cooldownSeconds = Mathf.Max(0f, cooldown);
    }

    public bool TryTeleport(Collider2D playerCollider, float now)
    {
        if (!GalleryPlayerContact.IsPhysicalPlayer(playerCollider) || destination == null ||
            now - _lastUse < cooldownSeconds) return false;
        _lastUse = now;
        var body = playerCollider.attachedRigidbody;
        if (body != null)
        {
            body.position = destination.position;
            body.linearVelocity = Vector2.zero;
        }
        else playerCollider.transform.position = destination.position;
        return true;
    }

    void Awake() => GetComponent<Collider2D>().isTrigger = true;
    void OnTriggerEnter2D(Collider2D other) => TryTeleport(other, Time.unscaledTime);
}
```

- [ ] **Step 5: Run focused and full EditMode tests, then commit**

Expected: the new tests pass and the baseline EditMode suite remains green.

```bash
git add Assets/Scripts/Gallery/GalleryProximityActivator.cs Assets/Scripts/Gallery/GalleryProximityActivator.cs.meta Assets/Scripts/Gallery/GalleryReturnPortal.cs Assets/Scripts/Gallery/GalleryReturnPortal.cs.meta Assets/Scripts/Tests/EditMode/GalleryRuntimeTests.cs
git commit -m "feat: add gallery exhibits and return portals"
```

---

### Task 3: Foreground occlusion fading

**Files:**
- Create: `Assets/Scripts/Gallery/ForegroundOccluder.cs`
- Modify: `Assets/Scripts/Tests/EditMode/GalleryRuntimeTests.cs`

**Interfaces:**
- Consumes: `GalleryPlayerContact.IsPhysicalPlayer(...)` and one or more child `SpriteRenderer` instances.
- Produces: `ForegroundOccluder.StepAlpha(...)` and automatic fade state from trigger overlap.

- [ ] **Step 1: Add failing fade tests**

Append:

```csharp
[Test]
public void OccluderAlpha_MovesTowardTargetWithoutOvershoot()
{
    Assert.That(ForegroundOccluder.StepAlpha(1f, 0.3f, 4f, 0.1f), Is.EqualTo(0.6f).Within(0.001f));
    Assert.That(ForegroundOccluder.StepAlpha(0.35f, 0.3f, 4f, 0.1f), Is.EqualTo(0.3f).Within(0.001f));
}
```

- [ ] **Step 2: Run the new test**

Expected: compilation fails because `ForegroundOccluder` does not exist.

- [ ] **Step 3: Implement the occluder**

Create `ForegroundOccluder.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class ForegroundOccluder : MonoBehaviour
{
    [SerializeField] SpriteRenderer[] renderers;
    [SerializeField, Range(0f, 1f)] float occludedAlpha = 0.3f;
    [SerializeField, Min(0.01f)] float fadeSpeed = 4f;
    readonly HashSet<int> _overlaps = new();

    void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<SpriteRenderer>();
    }

    void Update()
    {
        float target = _overlaps.Count > 0 ? occludedAlpha : 1f;
        foreach (var renderer in renderers)
        {
            if (renderer == null) continue;
            Color color = renderer.color;
            color.a = StepAlpha(color.a, target, fadeSpeed, Time.deltaTime);
            renderer.color = color;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (GalleryPlayerContact.IsPhysicalPlayer(other)) _overlaps.Add(other.GetInstanceID());
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (GalleryPlayerContact.IsPhysicalPlayer(other)) _overlaps.Remove(other.GetInstanceID());
    }

    public static float StepAlpha(float current, float target, float speed, float deltaTime) =>
        Mathf.MoveTowards(current, target, Mathf.Max(0f, speed) * Mathf.Max(0f, deltaTime));
}
```

- [ ] **Step 4: Run focused and full EditMode tests, then commit**

```bash
git add Assets/Scripts/Gallery/ForegroundOccluder.cs Assets/Scripts/Gallery/ForegroundOccluder.cs.meta Assets/Scripts/Tests/EditMode/GalleryRuntimeTests.cs
git commit -m "feat: fade biome gallery foreground props"
```

---

### Task 4: Reusable sprite-pack exhibits

**Files:**
- Create: `Assets/Prefabs/Gallery/Gallery_Knight.prefab`
- Create: `Assets/Prefabs/Gallery/Gallery_Rogue.prefab`
- Create: `Assets/Prefabs/Gallery/Gallery_Mage.prefab`
- Create: `Assets/Prefabs/Gallery/Gallery_RangerDisplay.prefab`
- Create: `Assets/Prefabs/Gallery/Gallery_Fire.prefab`
- Create: `Assets/Prefabs/Gallery/Gallery_EnergyBarrier.prefab`
- Create: `Assets/Prefabs/Gallery/Gallery_IceCrystal.prefab`
- Create: `Assets/Prefabs/Gallery/Gallery_PoisonCloud.prefab`
- Create: `Assets/Scripts/Tests/EditMode/GalleryAssetTests.cs`

**Interfaces:**
- Consumes: existing `Flipbook`, `YSort`, `GlowPulse`, `FloatBob`, `SpriteRenderer`, source PNGs and sorting layers.
- Produces: eight reusable prefabs for Task 5; no new Animator Controller.

- [ ] **Step 1: Build the three lateral character prefabs in Unity**

Use Unity MCP prefab/component tools. Each root has `SpriteRenderer`, looping `Flipbook`, `YSort`, no Rigidbody2D and no gameplay AI:

- Knight: `Knight/Idle/idle1.png` through `idle12.png`, 8 fps, scale calibrated to approximately 1.6 world units high.
- Rogue: `Rogue/Idle/idle1.png` through `idle18.png`, 10 fps, same apparent height as Knight.
- Mage: `Mage/Idle/idle1.png` through `idle14.png`, 8 fps, same apparent height as Knight.

All source paths begin with `Assets/Sprites/assassin-mage-viking-free-pixel-art-game-heroes/PNG/`. Set `Flipbook.loop = true`, leave `destroyOnEnd` unused, and put renderers on the existing `Objects` sorting layer.

- [ ] **Step 2: Build the ranger display**

Use `Assets/Sprites/forest-ranger-chibi-character-sprites/Forest_Ranger_1/PNG/PNG Sequences/Idle/0_Forest_Ranger_Idle_000.png` as a static renderer on `Objects`. Frame it with tinted `Assets/Sprites/Square.png` renderers so it reads as a sign or carved display, not a world-scale actor. Add `FloatBob` only to a small title ornament, not to the portrait.

- [ ] **Step 3: Build the four looping VFX prefabs**

Use `Flipbook.loop = true`, no collider, and sorting layer `FX`:

- Fire: `Explosion_1/Explosion_1.png` through `Explosion_10.png`, 12 fps.
- Energy barrier: `Explosion_7/1/Explosion_1.png` through `Explosion_5.png`, 8 fps.
- Ice crystal: `Explosion_5/Explosion_1.png` through `Explosion_10.png`, 12 fps, plus `GlowPulse` on a cyan base sprite.
- Poison cloud: `Explosion_9/Explosion_1.png` through `Explosion_10.png`, 10 fps, tinted pale green only if the original frames remain legible.

All VFX paths begin with `Assets/Sprites/animated-explosion-sprite-pack/PNG/`. Adjust prefab root scale instead of changing any source importer.

- [ ] **Step 4: Add editor asset validation tests**

Create `GalleryAssetTests.cs`:

```csharp
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class GalleryAssetTests
{
    static readonly string[] AnimatedPrefabs =
    {
        "Gallery_Knight", "Gallery_Rogue", "Gallery_Mage", "Gallery_Fire",
        "Gallery_EnergyBarrier", "Gallery_IceCrystal", "Gallery_PoisonCloud"
    };

    [Test]
    public void AnimatedGalleryPrefabs_HaveRendererAndFlipbook()
    {
        foreach (string name in AnimatedPrefabs)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/Gallery/{name}.prefab");
            Assert.That(prefab, Is.Not.Null, name);
            Assert.That(prefab.GetComponentInChildren<SpriteRenderer>(), Is.Not.Null, name);
            Assert.That(prefab.GetComponentInChildren<Flipbook>(), Is.Not.Null, name);
        }
    }

    [Test]
    public void RangerDisplay_IsPresentationOnly()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefabs/Gallery/Gallery_RangerDisplay.prefab");
        Assert.That(prefab, Is.Not.Null);
        Assert.That(prefab.GetComponentInChildren<SpriteRenderer>(), Is.Not.Null);
        Assert.That(prefab.GetComponentInChildren<Rigidbody2D>(), Is.Null);
    }
}
```

- [ ] **Step 5: Run tests, inspect all prefabs in isolation, and commit**

Capture isolated previews while calibrating scale. Run both `GalleryAssetTests`, then the full EditMode suite.

```bash
git add Assets/Prefabs/Gallery Assets/Prefabs/Gallery.meta Assets/Scripts/Tests/EditMode/GalleryAssetTests.cs Assets/Scripts/Tests/EditMode/GalleryAssetTests.cs.meta
git commit -m "feat: add reusable sprite gallery exhibits"
```

---

### Task 5: Build the continuous three-biome scene

**Files:**
- Create: `Assets/Scenes/Prototypes/BiomeGallery_Demo.unity`
- Modify: `Assets/Scripts/Tests/EditMode/GalleryAssetTests.cs`

**Interfaces:**
- Consumes: `Assets/Prefabs/Player.prefab`, all Task 4 prefabs, and all Task 1–3 runtime components.
- Produces: a single traversable gallery scene with no dependency on `GameProgress` or `RoomIdentity`.

- [ ] **Step 1: Create the scene shell and camera**

Create the scene through Unity MCP and save immediately at the exact path. Add these roots:

Create these seven direct scene roots; do not wrap them in an additional parent:

```text
Systems
CentralPlaza
Biome_Forest
Biome_ArcaneRuins
Biome_AlchemyMarsh
Foreground
Boundaries
```

Under `Systems`, instantiate `Player.prefab` at `(0,-1,0)`, add `GalleryReturnDestination` at `(0,-2,0)`, and add `Main Camera` at `(0,0,-10)` with orthographic size `6.5`, background `#111421`, and `GalleryCameraFollow.worldBounds = (-32,-20,64,42)`. Add one URP Global Light 2D at intensity `0.72`, color `#B9C7E6`. Do not add `RoomIdentity`, exits, HUD progression objects or campaign triggers.

- [ ] **Step 2: Compose the plaza and organic routes**

Use tinted instances of `Square.png`, `floor.png`, `floor_stone.png`, `wall.png` and `stone.png`; scale and overlap them to avoid rectangular room silhouettes. Keep all walkable ground on `Floor`, boundaries on the existing solid environment layer, and decorative actors on `Objects` with `YSort`.

Use these centers and extents:

- plaza centered `(0,0)`, walkable radius about `6`;
- forest center `(-20,5)`, extents about `12×14`;
- ruins center `(15,11)`, extents about `15×13`;
- marsh center `(19,-11)`, extents about `15×12`.

Connect them with paths at least `2.2` units wide. Give each biome one secondary branch at least `4` units long. Form the outer collision boundary with static `BoxCollider2D` segments; verify there is no gap large enough for the player's `0.4`-radius body.

- [ ] **Step 3: Dress the enchanted forest**

Use a green/brown floor palette, irregular stone/tree silhouettes, the Ranger Display as the landmark, Rogue as a stationary ambient figure, and Fire as the campfire. Place one proximity trigger around the campfire vignette with radius about `3.2`; it enables the Rogue and fire and disables them after exit. Add at least two tall foreground clusters using `ForegroundOccluder`.

- [ ] **Step 4: Dress the arcane ruins**

Use desaturated blue/violet stone, broken wall columns, Knight beside the main route, Energy Barrier across a non-blocking alcove, and Ice Crystal at the secondary branch endpoint. Use proximity triggers around barrier and crystal; neither receives a solid collider. Apply local cyan/violet Light2D accents without lowering player readability.

- [ ] **Step 5: Dress the alchemy marsh**

Use dark green/teal islands, narrow dry paths wider than `2.2` units, Mage beside a cauldron assembled from existing stone/square sprites, and Poison Cloud over a clearly decorative basin. Trigger Mage and poison effects by proximity. The effect area has no damage script and cannot trap the player.

- [ ] **Step 6: Add returns and labels**

At each far endpoint, create a visible portal trigger with `GalleryReturnPortal.destination = Systems/GalleryReturnDestination`. Use three small TextMesh Pro world labels: `BOSQUE ENCANTADO`, `RUINAS ARCANAS`, and `PANTANO ALQUÍMICO`; keep them outside the movement path and subordinate to the art.

- [ ] **Step 7: Add structural scene tests**

Add `using UnityEditor.SceneManagement;` and `using UnityEngine.SceneManagement;` beside the
existing imports at the top of `GalleryAssetTests.cs`, then append this test inside the class:

```csharp
[Test]
public void BiomeGallery_HasRequiredRootsAndThreeReturnPortals()
{
    const string path = "Assets/Scenes/Prototypes/BiomeGallery_Demo.unity";
    Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
    try
    {
        string[] roots = { "Systems", "CentralPlaza", "Biome_Forest",
            "Biome_ArcaneRuins", "Biome_AlchemyMarsh", "Foreground", "Boundaries" };
        foreach (string root in roots)
            Assert.That(System.Array.Exists(scene.GetRootGameObjects(), go => go.name == root),
                Is.True, root);
        Assert.That(Object.FindObjectsByType<GalleryReturnPortal>(FindObjectsSortMode.None).Length,
            Is.EqualTo(3));
        Assert.That(GameObject.FindGameObjectWithTag("Player"), Is.Not.Null);
    }
    finally { EditorSceneManager.CloseScene(scene, true); }
}
```

- [ ] **Step 8: Validate and commit the scene**

Use Unity MCP scene validation to check missing scripts/references, overlapping cameras, collider issues and duplicate lights. Run `GalleryAssetTests` and the full EditMode suite. Review the `.unity` diff and stage only the new gallery scene and test change.

```bash
git add Assets/Scenes/Prototypes/BiomeGallery_Demo.unity Assets/Scenes/Prototypes/BiomeGallery_Demo.unity.meta Assets/Scripts/Tests/EditMode/GalleryAssetTests.cs
git commit -m "feat: build explorable biome gallery"
```

---

### Task 6: Play traversal and visual review

**Files:**
- Modify only when inspection reveals a concrete defect: `Assets/Scenes/Prototypes/BiomeGallery_Demo.unity` or a gallery-owned script/prefab.
- Create ignored evidence: `Assets/Screenshots/biome-gallery-overview.png`, `biome-gallery-forest.png`, `biome-gallery-ruins.png`, `biome-gallery-marsh.png`, `biome-gallery-occlusion.png`.

**Interfaces:**
- Consumes: completed gallery scene.
- Produces: human-reviewable visual evidence and a scene that can be traversed without editor teleportation.

- [ ] **Step 1: Traverse every route in Play Mode**

Open the gallery directly and enter Play Mode. Starting at the plaza, walk the main route and secondary branch of Forest, Ruins and Marsh. For each biome, verify its far portal returns to `(0,-2)` and leaves the player able to move. Confirm no exhibit changes lives, campaign completion or saved progress.

- [ ] **Step 2: Verify the oblique composition**

Check that the player remains readable at the top, bottom and lateral extremes; the camera never exposes outside the `(-32,-20,64,42)` bounds; lateral characters read as staged vignettes; Ranger reads as a display; and fire, barrier, ice and poison read as four distinct effects.

- [ ] **Step 3: Capture and inspect five screenshots**

Capture a scene overview from Scene View and four Game View images: Forest, Ruins, Marsh and an overlap behind a tall foreground prop. Inspect each image rather than relying on scene hierarchy. Fix only concrete scale, contrast, framing, sorting or obstruction defects, then recapture the affected image.

- [ ] **Step 4: Run focused checks after visual corrections and commit**

Run `GalleryRuntimeTests` and `GalleryAssetTests`. Review the scene/prefab diff to ensure no unrelated serialized project settings entered it.

```bash
git add Assets/Scenes/Prototypes/BiomeGallery_Demo.unity Assets/Prefabs/Gallery Assets/Scripts/Gallery Assets/Scripts/Tests/EditMode/GalleryRuntimeTests.cs Assets/Scripts/Tests/EditMode/GalleryAssetTests.cs
git commit -m "fix: polish biome gallery readability"
```

Skip this commit when inspection required no tracked correction.

---

### Task 7: Full regression verification and handoff

**Files:**
- Do not update `docs/qa/2026-09-10-arcane-castle-playtest.md`; this isolated gallery is not the final campaign/build closure.
- Source fixes belong in the earlier task that owns the affected component or scene.

**Interfaces:**
- Consumes: all gallery tasks.
- Produces: verified prototype and a concise inventory of displayed source-pack families.

- [ ] **Step 1: Run the full Unity test suites**

Run complete EditMode and PlayMode suites through Unity MCP. Expected baseline plus gallery tests: zero failures. Record exact passed/failed totals in the final handoff message.

- [ ] **Step 2: Review the Unity console**

Clear logs, open and play the gallery, traverse one biome, stop Play Mode, then request Errors and Warnings. Accept only known MCP/Test Runner infrastructure noise; fix new missing-reference, importer, physics or runtime exceptions before proceeding.

- [ ] **Step 3: Run final repository checks**

Run `git diff --check`, `git status --short`, and `git log -5 --oneline`. Confirm the five known noise files, previous controller deletions and `Art_QuarterView_Probe` work were not accidentally included in gallery commits. Confirm no remote was pushed.

- [ ] **Step 4: Deliver the review package**

Report the scene path, five screenshot paths, exact test totals, console status, which sprite families were demonstrated, and any visual limitation found. Explicitly state that the gallery is not in Build Settings and that campaign scenes remain untouched.
