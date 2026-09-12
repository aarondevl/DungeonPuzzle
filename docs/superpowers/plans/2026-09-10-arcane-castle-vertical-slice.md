# Arcane Castle Vertical Slice Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Convert the five existing rooms into a continuous 20–30 minute journey through a visually distinct arcane castle, while preserving the current mechanics and enabling future branching routes.

**Architecture:** Keep one Unity scene per room and extend the existing `ExitTrigger`, `SpawnPoint`, `GameManager`, and `GameProgress` flow with explicit destinations, named entry points, safe fallbacks, room metadata, and a persistent transition overlay. Migrate the five existing scenes first, then reshape only `Room_01` and `Room_04`, and finish with a reusable arcane lighting/audio kit applied across all rooms.

**Tech Stack:** Unity 6000.5.0b10, C#, Unity Test Framework/NUnit, URP 2D Renderer and Light2D, TextMesh Pro, Input System, PlayerPrefs, Unity MCP.

**Spec:** `docs/superpowers/specs/2026-09-10-arcane-castle-vertical-slice-design.md`

## Global Constraints

- Keep the game 2D top-down; do not add 3D gameplay or additive world streaming.
- Reuse `Room_01` through `Room_05`; do not add another playable room scene.
- `Room_01` contains both Patio and Gran Salón; `Room_04` contains the optional secret.
- Preserve current scene references and old PlayerPrefs keys.
- Inventory items remain local to their room.
- Failure reloads only the current room and preserves castle progress.
- The normal run goes from Patio to Tower without returning to the menu.
- Target first-play duration is 20–30 minutes.
- Keep every puzzle and danger readable under the arcane lighting.
- Prefer existing sprites, prefabs, FX scripts, and audio infrastructure.
- Run focused tests after each code task and the full EditMode suite before every scene/content commit.
- After Unity imports any new asset, include its adjacent `.meta` file in the same commit.

## File Map

| File | Responsibility |
|---|---|
| `Assets/Scripts/Core/SpawnPoint.cs` | Named/default scene entry selection |
| `Assets/Scripts/Core/CastleTravelRules.cs` | Pure validation and linear fallback rules |
| `Assets/Scripts/Core/GameManager.cs` | Own travel state, loading, lives, timers, and player placement |
| `Assets/Scripts/World/ExitTrigger.cs` | Convert player overlap into final, explicit, or linear travel |
| `Assets/Scripts/Core/RoomIdentity.cs` | Describe room ID, display name, accent color, and ambience key |
| `Assets/Scripts/UI/SceneTransition.cs` | Persistent fade and room-title presentation |
| `Assets/Resources/UI/SceneTransition.prefab` | CanvasGroup, fade image, and title text wiring |
| `Assets/Scripts/Core/GameProgress.cs` | Existing progress plus completed-room and secret state |
| `Assets/Scripts/World/SecretDiscovery.cs` | Mark the Room_04 secret once and provide feedback |
| `Assets/Scripts/UI/HUDManager.cs` | Show the room identity instead of only a numeric label |
| `Assets/Scripts/Core/AudioMaster.cs` | Crossfade room ambience and preserve volume settings |
| `Assets/Scripts/Guard/GuardBase.cs` | Keep suspicion stingers bounded and non-stacking |
| `Assets/Editor/GenerateArcaneAmbience.cs` | Deterministically generate the five loopable ambience clips |
| `Assets/Prefabs/FX/ArcaneSigil.prefab` | Reusable glowing sigil decoration |
| `Assets/Prefabs/FX/ArcaneBrazier.prefab` | Reusable local light and particles |
| `Assets/Scenes/Room_01.unity` | Patio + Gran Salón |
| `Assets/Scenes/Room_02.unity` | Ala de guardias |
| `Assets/Scenes/Room_03.unity` | Biblioteca arcana |
| `Assets/Scenes/Room_04.unity` | Calabozos + optional passage |
| `Assets/Scenes/Room_05.unity` | Torre del Alcaide |
| `Assets/Scripts/Tests/EditMode/CastleTravelTests.cs` | Entry resolution, destination validation, and fallbacks |
| `Assets/Scripts/Tests/EditMode/RoomIdentityTests.cs` | Metadata fallbacks |
| `Assets/Scripts/Tests/EditMode/GameProgressCastleTests.cs` | Persistent completion/secret state |
| `Assets/Scripts/Tests/EditMode/AudioMasterTests.cs` | Ambience switch rules |
| `Assets/Scripts/Tests/PlayMode/DungeonPuzzle.PlayModeTests.asmdef` | Runtime-capable Unity test assembly |
| `Assets/Scripts/Tests/PlayMode/SceneTransitionPlayModeTests.cs` | Fade coroutine behavior |

---

### Task 1: Named spawn-point resolution

**Files:**
- Modify: `Assets/Scripts/Core/SpawnPoint.cs:1-13`
- Create: `Assets/Scripts/Tests/EditMode/CastleTravelTests.cs`

**Interfaces:**
- Consumes: Existing scene `SpawnPoint` components with no serialized ID.
- Produces: `SpawnPoint.Id`, `SpawnPoint.IsDefault`, and `SpawnPoint.Resolve(SpawnPoint[] points, string requestedId)` for Task 2.

- [ ] **Step 1: Write the failing entry-resolution tests**

Create `CastleTravelTests.cs` with these tests and helper:

```csharp
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class CastleTravelTests
{
    static SpawnPoint NewSpawn(string name, string id, bool isDefault)
    {
        var go = new GameObject(name);
        var spawn = go.AddComponent<SpawnPoint>();
        typeof(SpawnPoint).GetField("spawnId", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(spawn, id);
        typeof(SpawnPoint).GetField("isDefault", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(spawn, isDefault);
        return spawn;
    }

    [Test]
    public void Resolve_PrefersRequestedId()
    {
        var fallback = NewSpawn("Fallback", "South", true);
        var north = NewSpawn("North", "North", false);
        Assert.That(SpawnPoint.Resolve(new[] { fallback, north }, "North"), Is.SameAs(north));
        Object.DestroyImmediate(fallback.gameObject);
        Object.DestroyImmediate(north.gameObject);
    }

    [Test]
    public void Resolve_UsesDefaultWhenRequestedIdIsMissing()
    {
        var fallback = NewSpawn("Fallback", "South", true);
        var north = NewSpawn("North", "North", false);
        Assert.That(SpawnPoint.Resolve(new[] { north, fallback }, "Secret"), Is.SameAs(fallback));
        Object.DestroyImmediate(fallback.gameObject);
        Object.DestroyImmediate(north.gameObject);
    }

    [Test]
    public void Resolve_ReturnsNullForEmptyInput()
    {
        Assert.That(SpawnPoint.Resolve(System.Array.Empty<SpawnPoint>(), "South"), Is.Null);
    }
}
```

- [ ] **Step 2: Run the focused tests and confirm the expected failure**

Run through Unity MCP:

```text
run_tests(mode="EditMode", test_names=[
  "CastleTravelTests.Resolve_PrefersRequestedId",
  "CastleTravelTests.Resolve_UsesDefaultWhenRequestedIdIsMissing",
  "CastleTravelTests.Resolve_ReturnsNullForEmptyInput"
])
```

Expected: compilation fails because `SpawnPoint.Resolve` does not exist.

- [ ] **Step 3: Implement the minimal `SpawnPoint` contract**

Replace `SpawnPoint.cs` with:

```csharp
using System;
using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    [SerializeField] string spawnId = "Default";
    [SerializeField] bool isDefault = true;

    public string Id => spawnId;
    public bool IsDefault => isDefault;

    public static SpawnPoint Resolve(SpawnPoint[] points, string requestedId)
    {
        if (points == null || points.Length == 0) return null;

        if (!string.IsNullOrWhiteSpace(requestedId))
            foreach (var point in points)
                if (point != null && string.Equals(point.Id, requestedId, StringComparison.OrdinalIgnoreCase))
                    return point;

        foreach (var point in points)
            if (point != null && point.IsDefault) return point;

        return points[0];
    }

    void OnDrawGizmos()
    {
        Gizmos.color = isDefault ? Color.cyan : new Color(0.55f, 0.35f, 1f);
        Gizmos.DrawWireSphere(transform.position, 0.3f);
        Gizmos.DrawLine(transform.position, transform.position + transform.up * 0.6f);
    }
}
```

- [ ] **Step 4: Run the focused tests and full EditMode suite**

Expected: the three focused tests pass; the existing EditMode suite remains green.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Core/SpawnPoint.cs Assets/Scripts/Tests/EditMode/CastleTravelTests.cs Assets/Scripts/Tests/EditMode/CastleTravelTests.cs.meta
git commit -m "feat: add named castle spawn points"
```

---

### Task 2: Explicit room travel with safe linear fallback

**Files:**
- Create: `Assets/Scripts/Core/CastleTravelRules.cs`
- Modify: `Assets/Scripts/Core/GameManager.cs:65-173`
- Modify: `Assets/Scripts/World/ExitTrigger.cs:1-32`
- Modify: `Assets/Scripts/Tests/EditMode/CastleTravelTests.cs`

**Interfaces:**
- Consumes: `SpawnPoint.Resolve(...)` from Task 1 and current `GameManager.LoadNextRoom()` behavior.
- Produces: `GameManager.TravelTo(string destinationScene, string destinationEntryId)`, `GameManager.IsTransitioning`, and `CastleTravelRules.CanStart(...)` for later transition work.

- [ ] **Step 1: Add failing destination-rule tests**

Append:

```csharp
[Test]
public void CanStart_RejectsBlankSceneAndActiveTransition()
{
    Assert.That(CastleTravelRules.CanStart(false, "", _ => true), Is.False);
    Assert.That(CastleTravelRules.CanStart(true, "Room_02", _ => true), Is.False);
}

[Test]
public void CanStart_UsesSceneLoadabilityProbe()
{
    Assert.That(CastleTravelRules.CanStart(false, "Room_02", s => s == "Room_02"), Is.True);
    Assert.That(CastleTravelRules.CanStart(false, "Missing", s => s == "Room_02"), Is.False);
}

[Test]
public void NextScene_StopsAfterFinalRoom()
{
    Assert.That(CastleTravelRules.NextScene(4, 5), Is.EqualTo("Room_05"));
    Assert.That(CastleTravelRules.NextScene(5, 5), Is.Null);
}
```

- [ ] **Step 2: Run focused tests**

Expected: compilation fails because `CastleTravelRules` does not exist.

- [ ] **Step 3: Implement the pure rules**

Create:

```csharp
using System;

public static class CastleTravelRules
{
    public static bool CanStart(bool isTransitioning, string sceneName, Func<string, bool> canLoad)
    {
        return !isTransitioning
            && !string.IsNullOrWhiteSpace(sceneName)
            && canLoad != null
            && canLoad(sceneName);
    }

    public static string NextScene(int currentLevel, int totalLevels)
    {
        int next = currentLevel + 1;
        return next <= totalLevels ? $"Room_{next:00}" : null;
    }
}
```

- [ ] **Step 4: Add explicit travel state to `GameManager`**

Add fields/properties:

```csharp
string _pendingSpawnId;
bool _isTransitioning;
public bool IsTransitioning => _isTransitioning;
```

Add the new method:

```csharp
public void TravelTo(string destinationScene, string destinationEntryId)
{
    if (!CastleTravelRules.CanStart(_isTransitioning, destinationScene,
            Application.CanStreamedLevelBeLoaded))
    {
        Debug.LogError($"Castle travel rejected: '{destinationScene}'.");
        return;
    }

    _isTransitioning = true;
    _pendingSpawnId = destinationEntryId;
    _currentRoomScene = destinationScene;
    ResumeTime();
    SceneManager.LoadScene(destinationScene);
}
```

Add `if (_isTransitioning) return;` as the first line of `PlayerDetected()` so a guard cannot
reload the destination scene while a travel request is already in flight.

In `OnSceneLoaded`, replace the single `FindFirstObjectByType<SpawnPoint>()` lookup with:

```csharp
var spawn = SpawnPoint.Resolve(
    Object.FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None),
    _pendingSpawnId);
_pendingSpawnId = null;
_isTransitioning = false;

if (spawn == null)
{
    Debug.LogError($"No SpawnPoint exists in scene '{scene.name}'.");
    return;
}
```

Keep the existing player positioning and rigidbody velocity reset after this block.

- [ ] **Step 5: Extend `ExitTrigger` without breaking old scenes**

Add serialized fields:

```csharp
[SerializeField] string destinationScene;
[SerializeField] string destinationEntryId = "Default";
```

Replace the final dispatch with:

```csharp
if (isFinalExit)
    GameManager.Instance.WinGame();
else if (!string.IsNullOrWhiteSpace(destinationScene))
    GameManager.Instance.TravelTo(destinationScene, destinationEntryId);
else
    GameManager.Instance.LoadNextRoom();
```

Do not remove `_used` or change the player tag check.

- [ ] **Step 6: Run tests and manually smoke the fallback**

Run the focused tests, then all EditMode tests. Open `Room_01`, leave its new destination blank,
enter Play Mode, complete it, and verify it still loads `Room_02` through `LoadNextRoom()`.

- [ ] **Step 7: Commit**

```bash
git add Assets/Scripts/Core/CastleTravelRules.cs Assets/Scripts/Core/CastleTravelRules.cs.meta Assets/Scripts/Core/GameManager.cs Assets/Scripts/World/ExitTrigger.cs Assets/Scripts/Tests/EditMode/CastleTravelTests.cs
git commit -m "feat: support explicit castle room travel"
```

---

### Task 3: Room identity and HUD presentation

**Files:**
- Create: `Assets/Scripts/Core/RoomIdentity.cs`
- Create: `Assets/Scripts/Tests/EditMode/RoomIdentityTests.cs`
- Modify: `Assets/Scripts/UI/HUDManager.cs:35-47`

**Interfaces:**
- Consumes: Active scene name and existing HUD room label.
- Produces: `RoomIdentity.Current`, `DisplayName`, `AccentColor`, and `AmbienceKey` for Tasks 4, 8, and 9.

- [ ] **Step 1: Write failing metadata-fallback tests**

```csharp
using NUnit.Framework;
using UnityEngine;

public class RoomIdentityTests
{
    [Test]
    public void ResolvedDisplayName_UsesSceneNameWhenDisplayNameIsBlank()
    {
        var go = new GameObject("Identity");
        var identity = go.AddComponent<RoomIdentity>();
        Assert.That(identity.ResolvedDisplayName("Room_03"), Is.EqualTo("Room_03"));
        Object.DestroyImmediate(go);
    }

    [Test]
    public void ResolvedAmbienceKey_UsesDungeonDefaultWhenBlank()
    {
        var go = new GameObject("Identity");
        var identity = go.AddComponent<RoomIdentity>();
        Assert.That(identity.ResolvedAmbienceKey, Is.EqualTo("Music/dungeon_ambient"));
        Object.DestroyImmediate(go);
    }
}
```

- [ ] **Step 2: Run focused tests**

Expected: compilation fails because `RoomIdentity` does not exist.

- [ ] **Step 3: Implement `RoomIdentity`**

```csharp
using UnityEngine;

public sealed class RoomIdentity : MonoBehaviour
{
    public static RoomIdentity Current { get; private set; }

    [SerializeField] string roomId;
    [SerializeField] string displayName;
    [SerializeField] Color accentColor = new(0.31f, 0.91f, 0.86f, 1f);
    [SerializeField] string ambienceKey = "Music/dungeon_ambient";

    public string RoomId => roomId;
    public string DisplayName => displayName;
    public Color AccentColor => accentColor;
    public string AmbienceKey => ambienceKey;
    public string ResolvedAmbienceKey => string.IsNullOrWhiteSpace(ambienceKey)
        ? "Music/dungeon_ambient" : ambienceKey;

    public string ResolvedDisplayName(string sceneName) =>
        string.IsNullOrWhiteSpace(displayName) ? sceneName : displayName;

    void Awake() => Current = this;
    void OnDestroy() { if (Current == this) Current = null; }
}
```

- [ ] **Step 4: Use room identity in `HUDManager.UpdateLabels()`**

Replace the numeric assignment with:

```csharp
if (levelLabel != null)
{
    var identity = RoomIdentity.Current;
    levelLabel.text = identity != null
        ? identity.ResolvedDisplayName(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name)
        : $"ROOM {GameManager.Instance.CurrentLevel:00}";
    if (identity != null) levelLabel.color = identity.AccentColor;
}
```

- [ ] **Step 5: Run focused and full EditMode tests**

Expected: both room identity tests and the full suite pass.

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/Core/RoomIdentity.cs Assets/Scripts/Core/RoomIdentity.cs.meta Assets/Scripts/UI/HUDManager.cs Assets/Scripts/Tests/EditMode/RoomIdentityTests.cs Assets/Scripts/Tests/EditMode/RoomIdentityTests.cs.meta
git commit -m "feat: add castle room identity metadata"
```

---

### Task 4: Persistent fade and room-title transition

**Files:**
- Create: `Assets/Scripts/UI/SceneTransition.cs`
- Create: `Assets/Resources/UI/SceneTransition.prefab`
- Create: `Assets/Scripts/Tests/PlayMode/DungeonPuzzle.PlayModeTests.asmdef`
- Create: `Assets/Scripts/Tests/PlayMode/SceneTransitionPlayModeTests.cs`
- Modify: `Assets/Scripts/Core/GameManager.cs`

**Interfaces:**
- Consumes: `RoomIdentity.Current`, `GameManager.TravelTo(...)`, and a Resources prefab.
- Produces: `SceneTransition.Instance`, `FadeOut()`, and `FadeIn(string title, Color accent)`.

- [ ] **Step 1: Create the PlayMode assembly and write a failing fade test**

Create `DungeonPuzzle.PlayModeTests.asmdef` so the new test does not inherit the existing
Editor-only `DungeonPuzzle.Tests.asmdef`:

```json
{
    "name": "DungeonPuzzle.PlayModeTests",
    "rootNamespace": "",
    "references": ["DungeonPuzzle.Runtime"],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": false,
    "defineConstraints": ["UNITY_INCLUDE_TESTS"],
    "versionDefines": [],
    "noEngineReferences": false,
    "optionalUnityReferences": ["TestAssemblies"]
}
```

Then create the test:

```csharp
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class SceneTransitionPlayModeTests
{
    [UnityTest]
    public IEnumerator FadeOutAndIn_ReachesExactEndpointAlphas()
    {
        var go = new GameObject("TransitionTest");
        var group = go.AddComponent<CanvasGroup>();
        var labelObject = new GameObject("Label");
        var label = labelObject.AddComponent(
            System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro", true));
        label.transform.SetParent(go.transform);
        var transition = go.AddComponent<SceneTransition>();

        void Set(string field, object value) => typeof(SceneTransition)
            .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(transition, value);
        Set("canvasGroup", group);
        Set("roomTitle", label);
        Set("fadeDuration", 0.01f);
        Set("titleHoldDuration", 0f);

        yield return transition.FadeOut();
        Assert.That(group.alpha, Is.EqualTo(1f));
        yield return transition.FadeIn("Biblioteca Arcana", Color.cyan);
        Assert.That(group.alpha, Is.EqualTo(0f));
        Object.Destroy(go);
    }
}
```

- [ ] **Step 2: Run the PlayMode test**

Expected: compilation fails because `SceneTransition` does not exist.

- [ ] **Step 3: Implement `SceneTransition`**

The script must expose these exact methods and set exact endpoint alpha values:

```csharp
public IEnumerator FadeOut()
{
    yield return Fade(0f, 1f);
    canvasGroup.alpha = 1f;
}

public IEnumerator FadeIn(string title, Color accent)
{
    roomTitle.text = title;
    roomTitle.color = accent;
    roomTitle.gameObject.SetActive(true);
    canvasGroup.alpha = 1f;
    if (titleHoldDuration > 0f)
        yield return new WaitForSecondsRealtime(titleHoldDuration);
    yield return Fade(1f, 0f);
    canvasGroup.alpha = 0f;
    roomTitle.gameObject.SetActive(false);
}

IEnumerator Fade(float from, float to)
{
    float elapsed = 0f;
    canvasGroup.blocksRaycasts = true;
    while (elapsed < fadeDuration)
    {
        elapsed += Time.unscaledDeltaTime;
        canvasGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / fadeDuration));
        yield return null;
    }
    canvasGroup.blocksRaycasts = to > 0.5f;
}
```

Also implement singleton lifetime, `DontDestroyOnLoad`, and:

```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
static void AutoCreate()
{
    if (Instance != null) return;
    var prefab = Resources.Load<SceneTransition>("UI/SceneTransition");
    if (prefab != null) Object.Instantiate(prefab);
    else Debug.LogWarning("Missing Resources/UI/SceneTransition prefab; travel will cut.");
}
```

- [ ] **Step 4: Build the prefab in Unity**

Create `SceneTransition.prefab` with:

- root `SceneTransition`, `Canvas`, `CanvasGroup`, and `SceneTransition` script;
- CanvasGroup starts with alpha `0`, `interactable = false`, and `blocksRaycasts = false`;
- Canvas render mode `Screen Space - Overlay`, sorting order `500`;
- full-screen black `Image` at alpha `1`;
- centered TextMesh Pro label, 36 pt, uppercase disabled, wrapping disabled;
- `fadeDuration = 0.22`, `titleHoldDuration = 0.35`;
- root marked inactive in the Project prefab is **not** allowed; it must instantiate active.

- [ ] **Step 5: Route `GameManager.TravelTo` through a coroutine**

Keep `TravelTo` as the public entry and make it call `StartCoroutine(TravelRoutine(...))`.
The coroutine must:

```csharp
IEnumerator TravelRoutine(string sceneName, string entryId)
{
    _isTransitioning = true;
    _pendingSpawnId = entryId;
    _currentRoomScene = sceneName;
    PauseTime();

    if (SceneTransition.Instance != null)
        yield return SceneTransition.Instance.FadeOut();

    yield return SceneManager.LoadSceneAsync(sceneName);

    var identity = RoomIdentity.Current;
    string title = identity != null
        ? identity.ResolvedDisplayName(sceneName)
        : sceneName;
    Color accent = identity != null ? identity.AccentColor : Color.cyan;

    if (SceneTransition.Instance != null)
        yield return SceneTransition.Instance.FadeIn(title, accent);

    _isTransitioning = false;
    ResumeTime();
}
```

`OnSceneLoaded` must no longer clear `_isTransitioning`; it still resolves and clears the
pending spawn ID. Change its unconditional `ResumeTime()` call to
`if (!_isTransitioning) ResumeTime();` so gameplay stays frozen through the fade-in. Invalid
destinations must be rejected before this coroutine starts.

- [ ] **Step 6: Run PlayMode, focused EditMode, and full EditMode tests**

Expected: fade endpoint test passes; travel and all existing tests remain green.

- [ ] **Step 7: Manual transition smoke test**

Open `Room_01`, complete the room, and verify: fade to black, `Room_02` load, player at spawn,
“Ala de guardias” appears, fade clears, input is usable, and no duplicate EventSystem exists.

- [ ] **Step 8: Commit**

```bash
git add Assets/Scripts/UI/SceneTransition.cs Assets/Scripts/UI/SceneTransition.cs.meta Assets/Resources/UI.meta Assets/Resources/UI/SceneTransition.prefab Assets/Resources/UI/SceneTransition.prefab.meta Assets/Scripts/Tests/PlayMode.meta Assets/Scripts/Tests/PlayMode/DungeonPuzzle.PlayModeTests.asmdef Assets/Scripts/Tests/PlayMode/DungeonPuzzle.PlayModeTests.asmdef.meta Assets/Scripts/Tests/PlayMode/SceneTransitionPlayModeTests.cs Assets/Scripts/Tests/PlayMode/SceneTransitionPlayModeTests.cs.meta Assets/Scripts/Core/GameManager.cs
git commit -m "feat: add castle room transitions"
```

---

### Task 5: Persistent room completion and optional secret

**Files:**
- Modify: `Assets/Scripts/Core/GameProgress.cs:1-95`
- Modify: `Assets/Scripts/Core/GameManager.cs`
- Modify: `Assets/Scripts/World/ExitTrigger.cs`
- Create: `Assets/Scripts/World/SecretDiscovery.cs`
- Create: `Assets/Scripts/Tests/EditMode/GameProgressCastleTests.cs`

**Interfaces:**
- Consumes: PlayerPrefs and `RoomIdentity.RoomId`.
- Produces: `GameProgress.MarkRoomCompleted(string)`, `IsRoomCompleted(string)`, `DiscoverSecret(string)`, and `IsSecretDiscovered(string)`.

- [ ] **Step 1: Write failing persistence tests**

```csharp
using NUnit.Framework;

public class GameProgressCastleTests
{
    const string Room = "guard_wing_test";
    const string Secret = "room_04_passage_test";

    [TearDown]
    public void Cleanup()
    {
        GameProgress.ClearRoomCompleted(Room);
        GameProgress.ClearSecret(Secret);
    }

    [Test]
    public void CompletedRoom_RoundTripsThroughPlayerPrefs()
    {
        Assert.That(GameProgress.IsRoomCompleted(Room), Is.False);
        GameProgress.MarkRoomCompleted(Room);
        Assert.That(GameProgress.IsRoomCompleted(Room), Is.True);
    }

    [Test]
    public void Secret_RoundTripsThroughPlayerPrefs()
    {
        Assert.That(GameProgress.IsSecretDiscovered(Secret), Is.False);
        GameProgress.DiscoverSecret(Secret);
        Assert.That(GameProgress.IsSecretDiscovered(Secret), Is.True);
    }
}
```

- [ ] **Step 2: Run focused tests**

Expected: compilation fails because the castle progress methods do not exist.

- [ ] **Step 3: Add scoped PlayerPrefs keys and methods**

Add:

```csharp
const string KeyRoomCompleteFmt = "DP.Castle.Room.{0}.Complete";
const string KeySecretFmt = "DP.Castle.Secret.{0}";

static string SafeId(string id) => string.IsNullOrWhiteSpace(id) ? "unknown" : id.Trim();

public static bool IsRoomCompleted(string roomId) =>
    PlayerPrefs.GetInt(string.Format(KeyRoomCompleteFmt, SafeId(roomId)), 0) == 1;

public static void MarkRoomCompleted(string roomId)
{
    PlayerPrefs.SetInt(string.Format(KeyRoomCompleteFmt, SafeId(roomId)), 1);
    PlayerPrefs.Save();
}

public static void ClearRoomCompleted(string roomId)
{
    PlayerPrefs.DeleteKey(string.Format(KeyRoomCompleteFmt, SafeId(roomId)));
}

public static bool IsSecretDiscovered(string secretId) =>
    PlayerPrefs.GetInt(string.Format(KeySecretFmt, SafeId(secretId)), 0) == 1;

public static void DiscoverSecret(string secretId)
{
    PlayerPrefs.SetInt(string.Format(KeySecretFmt, SafeId(secretId)), 1);
    PlayerPrefs.Save();
}

public static void ClearSecret(string secretId)
{
    PlayerPrefs.DeleteKey(string.Format(KeySecretFmt, SafeId(secretId)));
}
```

Keep all old keys unchanged. Extend `ResetAll()` to clear the five approved room IDs
(`patio_hall`, `guard_wing`, `arcane_library`, `dungeons`, `warden_tower`) and
`room_04_passage`.

- [ ] **Step 4: Mark completion during forward travel**

Before `GameManager` leaves a numbered room through a non-final `ExitTrigger`, call:

```csharp
if (RoomIdentity.Current != null)
    GameProgress.MarkRoomCompleted(RoomIdentity.Current.RoomId);
```

For the final exit, mark the current room immediately before `WinGame()`.

- [ ] **Step 5: Implement `SecretDiscovery`**

```csharp
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class SecretDiscovery : MonoBehaviour
{
    [SerializeField] string secretId = "room_04_passage";
    [SerializeField] GameObject revealEffect;
    bool _used;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_used || !other.CompareTag("Player")) return;
        _used = true;
        bool firstDiscovery = !GameProgress.IsSecretDiscovered(secretId);
        GameProgress.DiscoverSecret(secretId);
        if (firstDiscovery)
        {
            SfxLibrary.Play("SFX/key_pickup", 0.45f);
            if (revealEffect != null) revealEffect.SetActive(true);
            Vfx.Spark(transform.position);
        }
    }
}
```

- [ ] **Step 6: Run focused and full EditMode tests**

Expected: persistence tests pass without deleting or changing prior level/time preferences.

- [ ] **Step 7: Commit**

```bash
git add Assets/Scripts/Core/GameProgress.cs Assets/Scripts/Core/GameManager.cs Assets/Scripts/World/ExitTrigger.cs Assets/Scripts/World/SecretDiscovery.cs Assets/Scripts/World/SecretDiscovery.cs.meta Assets/Scripts/Tests/EditMode/GameProgressCastleTests.cs Assets/Scripts/Tests/EditMode/GameProgressCastleTests.cs.meta
git commit -m "feat: persist castle room and secret progress"
```

---

### Task 6: Migrate all five scenes to the castle route

**Files:**
- Modify: `Assets/Scenes/Room_01.unity`
- Modify: `Assets/Scenes/Room_02.unity`
- Modify: `Assets/Scenes/Room_03.unity`
- Modify: `Assets/Scenes/Room_04.unity`
- Modify: `Assets/Scenes/Room_05.unity`

**Interfaces:**
- Consumes: `RoomIdentity`, named `SpawnPoint`, and explicit `ExitTrigger` fields.
- Produces: A complete Patio → Tower route with exact metadata and destinations.

- [ ] **Step 1: Record a clean baseline**

Run all EditMode tests. For each room, open it without saving unrelated changes and capture a
Game View screenshot in Play Mode. Store screenshots under the already ignored
`Assets/Screenshots/` folder.

- [ ] **Step 2: Add exact room metadata**

Create one root GameObject named `RoomIdentity` in each scene and configure:

| Scene | roomId | displayName | accentColor RGBA | ambienceKey |
|---|---|---|---|---|
| Room_01 | `patio_hall` | `Patio del Gran Salón` | `(0.31, 0.91, 0.86, 1)` | `Music/patio_hall_ambient` |
| Room_02 | `guard_wing` | `Ala de Guardias` | `(0.95, 0.30, 0.28, 1)` | `Music/guard_wing_ambient` |
| Room_03 | `arcane_library` | `Biblioteca Arcana` | `(0.60, 0.35, 1.00, 1)` | `Music/library_ambient` |
| Room_04 | `dungeons` | `Calabozos Espectrales` | `(0.35, 0.95, 0.55, 1)` | `Music/dungeons_ambient` |
| Room_05 | `warden_tower` | `Torre del Alcaide` | `(0.95, 0.65, 0.20, 1)` | `Music/tower_ambient` |

- [ ] **Step 3: Configure default entries**

On the existing `SpawnPoint` in every room set `spawnId = "South"` and `isDefault = true`.
Do not move it during this task.

- [ ] **Step 4: Configure the forward route**

Set the existing `ExitTrigger` fields exactly:

| From | destinationScene | destinationEntryId | isFinalExit |
|---|---|---|---|
| Room_01 | `Room_02` | `South` | false |
| Room_02 | `Room_03` | `South` | false |
| Room_03 | `Room_04` | `South` | false |
| Room_04 | `Room_05` | `South` | false |
| Room_05 | blank | `Default` | true |

- [ ] **Step 5: Save and validate each scene independently**

For each scene: save, run Unity scene validation, enter Play Mode, verify the identity appears in
the HUD, and exit Play Mode. Check Console before moving to the next scene.

- [ ] **Step 6: Run a route smoke test**

Start from `MainMenu`, choose New Game, and use safe editor teleportation only to move the player
onto each exit. Verify the sequence is exactly Room_01 → 02 → 03 → 04 → 05 → GameOver, every
spawn is correct, and no exit fires twice.

- [ ] **Step 7: Run the full EditMode suite and commit**

```bash
git add Assets/Scenes/Room_01.unity Assets/Scenes/Room_02.unity Assets/Scenes/Room_03.unity Assets/Scenes/Room_04.unity Assets/Scenes/Room_05.unity
git commit -m "feat: connect rooms as an arcane castle route"
```

---

### Task 7: Shape Room_01 hub and Room_04 optional passage

**Files:**
- Modify: `Assets/Scenes/Room_01.unity`
- Modify: `Assets/Scenes/Room_04.unity`

**Interfaces:**
- Consumes: Existing wall/floor sprites, doors, lever/key mechanics, `SecretDiscovery`, and the
  existing collision/sorting layers.
- Produces: A visible castle objective in Room_01 and one optional secret in Room_04.

- [ ] **Step 1: Protect the current solutions**

Play and record the required solution for both rooms before editing. In `Room_01`, verify the
current tutorial path still teaches movement/interact/door. In `Room_04`, record every required
key, lever, pressure plate, stone, trap, and guard used by its main solution.

- [ ] **Step 2: Divide `Room_01` into Patio and Gran Salón**

Keep the current tutorial objects in the lower half as `Patio`. Beyond its existing door, create
the `GranSalon` parent containing:

- a central safe aisle at least two player-collider widths wide;
- the existing forward exit centered on the north wall;
- two sealed side-door silhouettes named `FutureDoor_West` and `FutureDoor_East`, with no
  collider and no `ExitTrigger`;
- three cyan sigil markers leading toward the north exit;
- a non-interactive tower emblem above the north exit;
- no new guard in the Gran Salón.

The player must be able to stop in the hall, understand that the tower is the goal, and reach the
exit without learning a second new mechanic.

- [ ] **Step 3: Create the optional Room_04 passage**

Add a side alcove off the existing main path, under root `SecretPassage`, with:

- entrance controlled by one existing-style `Lever` named `SecretLever`;
- one `Door` named `SecretDoor`, controlled only by that lever;
- no required item from the main solution placed inside;
- one trigger child named `SecretDiscovery`, tagged/layered so only the player activates it;
- `SecretDiscovery.secretId = "room_04_passage"`;
- one glowing sigil as the reward and a short route reconnecting before the main exit;
- no additional guard and at most one existing spike-trap prefab inside.

The main solution must remain completable without entering the passage.

- [ ] **Step 4: Verify both routes**

Run Room_04 twice from a reset save:

1. Ignore the side lever and complete the main route.
2. Open the side door, enter the trigger, confirm one stinger/one spark, return to the main path,
   and complete the room.

Reload after discovery and verify `GameProgress.IsSecretDiscovered("room_04_passage")` is true
without replaying the first-discovery feedback.

- [ ] **Step 5: Run all EditMode tests and commit**

```bash
git add Assets/Scenes/Room_01.unity Assets/Scenes/Room_04.unity
git commit -m "feat: add grand hall and optional secret passage"
```

---

### Task 8: Build and apply the reusable arcane visual kit

**Files:**
- Create: `Assets/Prefabs/FX/ArcaneSigil.prefab`
- Create: `Assets/Prefabs/FX/ArcaneBrazier.prefab`
- Modify: `Assets/Scenes/Room_01.unity`
- Modify: `Assets/Scenes/Room_02.unity`
- Modify: `Assets/Scenes/Room_03.unity`
- Modify: `Assets/Scenes/Room_04.unity`
- Modify: `Assets/Scenes/Room_05.unity`

**Interfaces:**
- Consumes: `RoomIdentity.AccentColor`, existing `GlowPulse`, `FloatBob`, `YSort`, sprite sorting
  layers, and URP 2D lights.
- Produces: Two reusable decoration prefabs and a readable per-room lighting pass.

- [ ] **Step 1: Build `ArcaneSigil.prefab`**

Use four copies of Unity's square sprite, rotated 45 degrees and arranged as a hollow diamond.
Configure:

- root name `ArcaneSigil`;
- child renderers on sorting layer `FloorFX`;
- shared room accent tint at 70% alpha;
- `GlowPulse.frequency = 0.65`;
- a child 2D Point Light using the same tint, intensity `0.32`, outer radius `1.8`;
- no collider and no gameplay script.

- [ ] **Step 2: Build `ArcaneBrazier.prefab`**

Configure:

- a stone/metal base assembled from existing environment sprites;
- a ParticleSystem with 12–18 particles, lifetime `0.45–0.8`, start size `0.05–0.12`, upward
  velocity `0.15–0.35`, no collision;
- a 2D Point Light, intensity `0.55`, inner radius `0.35`, outer radius `2.7`;
- renderer sorting layer `Objects`, particles on `FX`;
- no collider unless placed against an existing wall collider.

- [ ] **Step 3: Establish a readable lighting baseline in Room_01**

Set the Global Light 2D color to dark blue-grey `(0.18, 0.22, 0.32, 1)` and intensity `0.72`.
If the scene lacks a Global Light 2D, add exactly one. Assign `Sprite-Lit-Default` to floor,
walls, doors, keys, and environmental props; keep `PlayerSprite_Unlit.mat` on the player and
keep the custom vision-cone material unchanged. Place braziers symmetrically at important
doorways and sigils on the safe route. In Play Mode,
verify the player, floor, walls, door, key, guard, and exit remain identifiable without relying on
the HUD.

- [ ] **Step 4: Apply accents without changing gameplay geometry**

Use the Task 6 room colors. For every room:

- place 2–4 braziers at entrances, exits, or major puzzle nodes;
- place 2–5 sigils to reinforce the intended route or objective;
- keep red exclusively for active danger/guard alert outside Room_02's controlled accents;
- keep floor brightness variation below 20% so walkability stays legible;
- do not put particles over interaction prompts or vision cones.

- [ ] **Step 5: Give every room one visual landmark**

- Room_01: tower emblem over the north exit.
- Room_02: crossed red/cyan guard banners beside the guarded route.
- Room_03: violet central sigil ring around the lever puzzle.
- Room_04: green spectral bars/chain cluster at the secret entrance.
- Room_05: gold/cyan alcaide seal behind the final puzzle.

Build landmarks from existing sprites, tinted SpriteRenderers, and the two new prefabs; do not
add a new texture pack.

- [ ] **Step 6: Capture and compare every room**

For each room, capture a Game View screenshot at its spawn and main puzzle. Check foreground
occlusion, hazard contrast, vision cone contrast, player silhouette, and consistent cyan arcane
language. Fix a room before proceeding to the next.

- [ ] **Step 7: Run tests and commit**

Run the full EditMode suite and one Play Mode smoke in every room.

```bash
git add Assets/Prefabs/FX.meta Assets/Prefabs/FX/ArcaneSigil.prefab Assets/Prefabs/FX/ArcaneSigil.prefab.meta Assets/Prefabs/FX/ArcaneBrazier.prefab Assets/Prefabs/FX/ArcaneBrazier.prefab.meta Assets/Scenes/Room_01.unity Assets/Scenes/Room_02.unity Assets/Scenes/Room_03.unity Assets/Scenes/Room_04.unity Assets/Scenes/Room_05.unity
git commit -m "feat: apply arcane castle visual identity"
```

---

### Task 9: Generate and crossfade room ambience

**Files:**
- Create: `Assets/Editor/GenerateArcaneAmbience.cs`
- Create: `Assets/Resources/Audio/Music/patio_hall_ambient.wav`
- Create: `Assets/Resources/Audio/Music/guard_wing_ambient.wav`
- Create: `Assets/Resources/Audio/Music/library_ambient.wav`
- Create: `Assets/Resources/Audio/Music/dungeons_ambient.wav`
- Create: `Assets/Resources/Audio/Music/tower_ambient.wav`
- Modify: `Assets/Scripts/Core/AudioMaster.cs:1-83`
- Modify: `Assets/Scripts/Guard/GuardBase.cs:37-77`
- Create: `Assets/Scripts/Tests/EditMode/AudioMasterTests.cs`

**Interfaces:**
- Consumes: `RoomIdentity.ResolvedAmbienceKey` and current music volume preferences.
- Produces: `AudioMaster.ApplyRoom(RoomIdentity identity)` and five loopable ambience clips.

- [ ] **Step 1: Add failing ambience-switch tests**

Create:

```csharp
using NUnit.Framework;
using UnityEngine;

public class AudioMasterTests
{
    [Test]
    public void ShouldSwitchAmbience_RejectsMissingAndAlreadyPlayingClip()
    {
        var current = AudioClip.Create("Current", 8, 1, 8000, false);
        Assert.That(AudioMaster.ShouldSwitchAmbience(current, null, true), Is.False);
        Assert.That(AudioMaster.ShouldSwitchAmbience(current, current, true), Is.False);
        Object.DestroyImmediate(current);
    }

    [Test]
    public void ShouldSwitchAmbience_AcceptsDifferentOrStoppedClip()
    {
        var current = AudioClip.Create("Current", 8, 1, 8000, false);
        var next = AudioClip.Create("Next", 8, 1, 8000, false);
        Assert.That(AudioMaster.ShouldSwitchAmbience(current, next, true), Is.True);
        Assert.That(AudioMaster.ShouldSwitchAmbience(current, current, false), Is.True);
        Object.DestroyImmediate(current);
        Object.DestroyImmediate(next);
    }
}
```

Run these two tests. Expected: compilation fails because `ShouldSwitchAmbience` does not exist.

- [ ] **Step 2: Create a deterministic editor generator**

Add menu item `DungeonPuzzle/Audio/Generate Arcane Ambience`. It must generate five 12-second,
mono, 44.1 kHz, 16-bit PCM WAV files with deterministic seed `42817`:

- Patio: filtered wind noise plus sparse low fire crackle.
- Guard wing: patio bed plus quiet low drum pulse every 2 seconds.
- Library: sine drones at A2/E3 plus sparse high glass partials.
- Dungeons: low noise bed plus water-drop impulses and distant chain-like metallic partials.
- Tower: library drone plus a slow A2/E3/A3 pulse that peaks below `0.45`.

The generator must use only `System.IO`, `System.Math`, and `System.Random`, write to the exact
Resources paths above, normalize each clip to peak `0.45`, and call `AssetDatabase.Refresh()`.
The final 0.25 seconds must crossfade into the first 0.25 seconds to avoid a loop click.

Use this implementation shape; keep the paths and synthesis values exact:

```csharp
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class GenerateArcaneAmbience
{
    const int Rate = 44100;
    const int Seconds = 12;
    const int Seed = 42817;

    enum Mood { Patio, Guard, Library, Dungeons, Tower }

    [MenuItem("DungeonPuzzle/Audio/Generate Arcane Ambience")]
    public static void Generate()
    {
        Write("Assets/Resources/Audio/Music/patio_hall_ambient.wav", Build(Mood.Patio, Seed));
        Write("Assets/Resources/Audio/Music/guard_wing_ambient.wav", Build(Mood.Guard, Seed + 1));
        Write("Assets/Resources/Audio/Music/library_ambient.wav", Build(Mood.Library, Seed + 2));
        Write("Assets/Resources/Audio/Music/dungeons_ambient.wav", Build(Mood.Dungeons, Seed + 3));
        Write("Assets/Resources/Audio/Music/tower_ambient.wav", Build(Mood.Tower, Seed + 4));
        AssetDatabase.Refresh();
        ConfigureImports();
    }

    static float[] Build(Mood mood, int seed)
    {
        var random = new System.Random(seed);
        var samples = new float[Rate * Seconds];
        float wind = 0f;
        for (int i = 0; i < samples.Length; i++)
        {
            float t = i / (float)Rate;
            wind = Mathf.Lerp(wind, (float)(random.NextDouble() * 2.0 - 1.0), 0.004f);
            float sample = mood switch
            {
                Mood.Patio => wind * 0.22f + Crackle(random),
                Mood.Guard => wind * 0.13f + Pulse(t, 2f, 55f, 0.16f),
                Mood.Library => Drone(t, 0.16f) + Glass(t, 3.7f, 0.08f),
                Mood.Dungeons => wind * 0.10f + Drop(t, 3.1f, 0.13f),
                Mood.Tower => Drone(t, 0.12f) + TowerPulse(t),
                _ => 0f
            };
            samples[i] = sample;
        }
        CrossfadeLoop(samples, Rate / 4);
        Normalize(samples, 0.45f);
        return samples;
    }

    static float Crackle(System.Random random) =>
        random.NextDouble() < 0.0009 ? (float)(random.NextDouble() * 0.18 - 0.09) : 0f;

    static float Pulse(float t, float period, float hz, float gain)
    {
        float phase = t % period;
        return phase < 0.45f
            ? Mathf.Sin(2f * Mathf.PI * hz * phase) * Mathf.Exp(-phase * 9f) * gain
            : 0f;
    }

    static float Drone(float t, float gain) => gain *
        (Mathf.Sin(2f * Mathf.PI * 110f * t) * 0.55f
        + Mathf.Sin(2f * Mathf.PI * 164.81f * t) * 0.35f);

    static float Glass(float t, float period, float gain)
    {
        float phase = t % period;
        return phase < 0.8f
            ? Mathf.Sin(2f * Mathf.PI * 1318.51f * phase) * Mathf.Exp(-phase * 5f) * gain
            : 0f;
    }

    static float Drop(float t, float period, float gain)
    {
        float phase = t % period;
        float frequency = Mathf.Lerp(900f, 420f, Mathf.Clamp01(phase / 0.35f));
        return phase < 0.35f
            ? Mathf.Sin(2f * Mathf.PI * frequency * phase) * Mathf.Exp(-phase * 11f) * gain
            : 0f;
    }

    static float TowerPulse(float t)
    {
        int note = Mathf.FloorToInt(t / 2f) % 3;
        float hz = note == 0 ? 55f : note == 1 ? 82.41f : 110f;
        return Pulse(t, 2f, hz, 0.18f);
    }

    static void CrossfadeLoop(float[] samples, int count)
    {
        int start = samples.Length - count;
        for (int i = 0; i < count; i++)
        {
            float t = (i + 1f) / count;
            samples[start + i] = Mathf.Lerp(samples[start + i], samples[i], t);
        }
    }

    static void Normalize(float[] samples, float peak)
    {
        float max = 0f;
        foreach (float sample in samples) max = Mathf.Max(max, Mathf.Abs(sample));
        if (max <= 0f) return;
        float scale = peak / max;
        for (int i = 0; i < samples.Length; i++) samples[i] *= scale;
    }

    static void Write(string path, float[] samples)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        using var writer = new BinaryWriter(File.Create(path));
        writer.Write(new[] { 'R', 'I', 'F', 'F' });
        writer.Write(36 + samples.Length * 2);
        writer.Write(new[] { 'W', 'A', 'V', 'E' });
        writer.Write(new[] { 'f', 'm', 't', ' ' });
        writer.Write(16); writer.Write((short)1); writer.Write((short)1);
        writer.Write(Rate); writer.Write(Rate * 2); writer.Write((short)2); writer.Write((short)16);
        writer.Write(new[] { 'd', 'a', 't', 'a' });
        writer.Write(samples.Length * 2);
        foreach (float sample in samples)
            writer.Write((short)(Mathf.Clamp(sample, -1f, 1f) * short.MaxValue));
    }

    static void ConfigureImports()
    {
        string[] paths = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Resources/Audio/Music" });
        foreach (string guid in paths)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.Contains("_ambient")) continue;
            var importer = (AudioImporter)AssetImporter.GetAtPath(path);
            importer.forceToMono = true;
            importer.preloadAudioData = false;
            importer.defaultSampleSettings = new AudioImporterSampleSettings
            {
                loadType = AudioClipLoadType.Streaming,
                compressionFormat = AudioCompressionFormat.Vorbis,
                quality = 0.55f
            };
            importer.SaveAndReimport();
        }
    }
}
```

- [ ] **Step 3: Run the generator and verify imports**

Run the menu item once. Inspect every generated clip and verify the generator set:

- Force To Mono: true;
- Load Type: Streaming;
- Compression Format: Vorbis;
- Quality: 55;
- Preload Audio Data: false.

- [ ] **Step 4: Extend `AudioMaster` with room ambience crossfade**

Add a second looping `AudioSource _nextMusic`, create it beside `_music`, and implement:

```csharp
public static bool ShouldSwitchAmbience(AudioClip current, AudioClip next, bool currentPlaying)
{
    return next != null && (current != next || !currentPlaying);
}

public void ApplyRoom(RoomIdentity identity)
{
    if (identity == null) return;
    AudioClip next = SfxLibrary.Get(identity.ResolvedAmbienceKey);
    if (!ShouldSwitchAmbience(_music.clip, next, _music.isPlaying)) return;
    StopAllCoroutines();
    StartCoroutine(CrossfadeMusic(next, 0.6f));
}

IEnumerator CrossfadeMusic(AudioClip next, float seconds)
{
    _nextMusic.clip = next;
    _nextMusic.loop = true;
    _nextMusic.volume = 0f;
    _nextMusic.Play();
    float elapsed = 0f;
    while (elapsed < seconds)
    {
        elapsed += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(elapsed / seconds);
        _music.volume = (1f - t) * _musicBaseVolume * GameProgress.MusicVolume;
        _nextMusic.volume = t * _musicBaseVolume * GameProgress.MusicVolume;
        yield return null;
    }
    _music.Stop();
    (_music, _nextMusic) = (_nextMusic, _music);
    Apply();
}
```

Call `ApplyRoom(RoomIdentity.Current)` after one frame so the new scene's
`RoomIdentity.Awake()` has run:

```csharp
void OnSceneLoaded(Scene scene, LoadSceneMode mode)
{
    EnsureListener();
    StartCoroutine(ApplyRoomNextFrame());
}

IEnumerator ApplyRoomNextFrame()
{
    yield return null;
    ApplyRoom(RoomIdentity.Current);
}
```

Add `using System.Collections;`. Keep `PlaySFX` and all volume preferences intact.

- [ ] **Step 5: Calibrate alert loudness**

Keep the existing three perceptible states:

- calm: room ambience;
- suspicion: `SFX/alert` at `0.35` maximum;
- detection: `SFX/detected` at `0.45` maximum.

Change both `SfxLibrary.Play("SFX/alert")` calls in `GuardBase` to
`SfxLibrary.Play("SFX/alert", 0.35f)`. Do not add a continuous chase loop. The existing
`State == GuardState.Alerted` guards remain in place so repeated suspicion cannot stack clips.

- [ ] **Step 6: Test every room transition with headphones and speakers**

Verify no loop click, no silence longer than the 0.6-second crossfade, no volume spike, and the
Master/Music/SFX sliders still affect the expected channels.

- [ ] **Step 7: Run tests and commit**

Run the two focused `AudioMasterTests`, then the full EditMode suite. Expected: both focused
tests and all existing EditMode tests pass.

```bash
git add Assets/Editor/GenerateArcaneAmbience.cs Assets/Editor/GenerateArcaneAmbience.cs.meta Assets/Resources/Audio/Music Assets/Scripts/Core/AudioMaster.cs Assets/Scripts/Guard/GuardBase.cs Assets/Scripts/Tests/EditMode/AudioMasterTests.cs Assets/Scripts/Tests/EditMode/AudioMasterTests.cs.meta
git commit -m "feat: add arcane room ambience"
```

---

### Task 10: Full-run verification, timing, and delivery build

**Files:**
- Create: `docs/qa/2026-09-10-arcane-castle-playtest.md`
- Source fixes are not made opportunistically here: return any failure to the task that owns the affected file, rerun that task's focused verification, and then resume Task 10.

**Interfaces:**
- Consumes: The complete vertical slice.
- Produces: Passing automated tests, a clean build, and recorded acceptance evidence.

- [ ] **Step 1: Run all automated tests**

Run the full EditMode suite, then the full PlayMode suite. Record total/passed/failed/duration in
the QA document. Expected: zero failed tests.

- [ ] **Step 2: Run the fresh-save critical path**

Reset progress from the menu. Play without editor teleportation:

1. Complete Patio + Gran Salón.
2. Complete Ala de Guardias using at least one thrown stone.
3. Complete Biblioteca using its lever solution.
4. Complete Calabozos without the secret.
5. Complete Torre and reach the win screen.

Record total time and per-room time. Accept when total is 20–30 minutes; if outside that range,
adjust guard wait times, route length, or puzzle clue strength before adding content.

- [ ] **Step 3: Run the failure/recovery matrix**

In every room, get detected three times. Verify each of the first two detections reloads that
room at its default spawn and the third opens Game Over. Continue/retry and verify the same room
restarts with three lives while prior castle completion/secret data remains intact.

- [ ] **Step 4: Run optional-route and configuration failure checks**

- Complete Room_04 with and without the passage.
- Temporarily set a test exit to `MissingScene`, confirm no load and one clear error, then restore
  the serialized value before saving.
- Temporarily set a test entry to `MissingEntry`, confirm default spawn and one warning, then
  restore it before saving.
- Stand on each exit long enough for both player colliders to overlap; confirm one travel only.

- [ ] **Step 5: Perform audiovisual and performance review**

At 1920×1080, inspect every room for player silhouette, puzzle readability, vision cone contrast,
particle obstruction, audio peaks, and transition consistency. Capture one spawn and one puzzle
screenshot per room. Capture profiler rendering/script/memory snapshots during Room_02 and
Room_05; accept when gameplay stays at or above 60 FPS on the development machine without
repeating console errors or increasing memory every room transition.

- [ ] **Step 6: Produce the delivery build**

Build the configured desktop target to `Builds/ArcaneCastleVerticalSlice/`. Launch the executable,
run MainMenu → Room_01 → Room_02, close, relaunch, and verify settings/progress load correctly.
The ignored `Builds/` output is delivery evidence, not source control content.

- [ ] **Step 7: Write the QA record**

The QA document must contain:

- commit tested;
- Unity version and target platform;
- automated test counts;
- first-run total and per-room times;
- pass/fail result for critical, failure/recovery, optional, audiovisual, and build checks;
- paths to the ten ignored screenshots;
- any accepted limitation, restricted to non-blocking cosmetic issues.

- [ ] **Step 8: Final verification commit**

Run `git diff --check`, the full EditMode suite, and the full PlayMode suite once more.

```bash
git add docs/qa/2026-09-10-arcane-castle-playtest.md
git commit -m "test: verify arcane castle vertical slice"
```

The implementation is complete only when the final worktree is clean and the QA record shows all
acceptance checks passing.
