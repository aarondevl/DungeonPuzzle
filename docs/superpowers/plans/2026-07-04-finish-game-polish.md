# Pulido final DungeonPuzzle — Plan de implementación

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Guardias visibles/animados (traslación + rotación), suelo y salida texturizados, oclusiones correctas vía sorting layers + Y-sort, y audio retro completo (SFX + loop ambiental).

**Architecture:** Todo es pulido sobre sistemas existentes. Los guardias ganan un hijo `Visual` (sprite pack Sword + Animator clonado del player + contra-rotación); el orden visual se resuelve con 6 sorting layers nuevas + script `YSort`; el audio se genera proceduralmente (Python → WAV) en `Assets/Resources/Audio/` y se reproduce vía `SfxLibrary` estático + `AudioMaster` ampliado.

**Tech Stack:** Unity 6000.5.0b10, URP 2D, Input System, unity-mcp-cli (`script-execute` para operaciones de editor), Python 3 (generación WAV), NUnit EditMode.

## Global Constraints

- Spec: `docs/superpowers/specs/2026-07-04-finish-game-polish-design.md`. NO tocar mecánicas/IA/HUD/menús/niveles.
- El editor Unity está abierto y conectado por MCP: TODO cambio de assets nativos (.controller, .anim, .prefab, escenas) se hace vía `npx unity-mcp-cli run-tool script-execute` (código editor C#), nunca editando YAML a mano — EXCEPTO `ProjectSettings/TagManager.asset` (Task 1) que se edita como YAML con Unity notificado vía `assets-refresh`.
- Después de crear/editar archivos `.cs` desde fuera: `npx unity-mcp-cli run-tool assets-refresh --input '{}'` y verificar `console-get-logs` sin errores de compilación.
- Los WAV van bajo `Assets/Resources/Audio/` (los singletons se auto-crean por código y no pueden serializar clips).
- Convención de commits del repo: prefijos `feat:`/`fix:`/`chore:` en español, con `Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>`.
- Tests EditMode viven en `Assets/Scripts/Tests/EditMode/` (infra ya funciona; ver `PlayerMovementSmoothingTests.cs` como referencia de estilo).
- Ejecutar tests: `npx unity-mcp-cli run-tool tests-run --input '{"testMode":"EditMode"}'` — todas las escenas deben estar guardadas antes.

---

### Task 1: Sorting layers + YSort

**Files:**
- Modify: `ProjectSettings/TagManager.asset` (bloque `m_SortingLayers`)
- Create: `Assets/Scripts/FX/YSort.cs`
- Test: `Assets/Scripts/Tests/EditMode/YSortTests.cs`

**Interfaces:**
- Produces: sorting layers `Floor, FloorFX, Objects, Actors, WallsTop, FX`; clase `YSort` con `static int ComputeOrder(float feetY)` y campo serializado `feetOffset`. Tasks 4 y 8 asignan estas layers y añaden `YSort` a prefabs/escenas.

- [ ] **Step 1: Test que falla** — `Assets/Scripts/Tests/EditMode/YSortTests.cs`:

```csharp
using NUnit.Framework;

public class YSortTests
{
    [Test]
    public void ObjetoMasAbajo_SeDibujaEncima()
    {
        Assert.Greater(YSort.ComputeOrder(-2f), YSort.ComputeOrder(1f));
    }

    [Test]
    public void Orden_EscalaPor100_YRedondea()
    {
        Assert.AreEqual(-150, YSort.ComputeOrder(1.5f));
        Assert.AreEqual(230, YSort.ComputeOrder(-2.3f));
        Assert.AreEqual(0, YSort.ComputeOrder(0f));
    }
}
```

- [ ] **Step 2: Correr y ver fallo de compilación** (`YSort` no existe): `assets-refresh` → `console-get-logs` debe mostrar CS0103/CS0246 en YSortTests.

- [ ] **Step 3: Implementación** — `Assets/Scripts/FX/YSort.cs`:

```csharp
using UnityEngine;

/// <summary>
/// Ordena el sprite por su Y (los "pies"): menor Y => se dibuja delante.
/// Estándar top-down para que actores y props altos se ocluyan entre sí.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class YSort : MonoBehaviour
{
    [Tooltip("Offset desde el pivot hasta los pies (negativo si el pivot está al centro).")]
    [SerializeField] float feetOffset = 0f;

    SpriteRenderer _sr;

    void Awake() => _sr = GetComponent<SpriteRenderer>();

    void LateUpdate() => _sr.sortingOrder = ComputeOrder(transform.position.y + feetOffset);

    public static int ComputeOrder(float feetY) => -(int)Mathf.Round(feetY * 100f);
}
```

- [ ] **Step 4: Tests en verde**: `tests-run` EditMode → `YSortTests` PASS (y el resto sigue verde).

- [ ] **Step 5: Añadir sorting layers.** En `ProjectSettings/TagManager.asset`, reemplazar el bloque:

```yaml
  m_SortingLayers:
  - name: Default
    uniqueID: 0
    locked: 0
```

por (uniqueIDs arbitrarios pero fijos):

```yaml
  m_SortingLayers:
  - name: Floor
    uniqueID: 3141592001
    locked: 0
  - name: FloorFX
    uniqueID: 3141592002
    locked: 0
  - name: Default
    uniqueID: 0
    locked: 0
  - name: Objects
    uniqueID: 3141592003
    locked: 0
  - name: Actors
    uniqueID: 3141592004
    locked: 0
  - name: WallsTop
    uniqueID: 3141592005
    locked: 0
  - name: FX
    uniqueID: 3141592006
    locked: 0
```

Luego `assets-refresh`. Verificar vía `script-execute` (body-only): `Debug.Log(string.Join(",", UnityEngine.SortingLayer.layers.Select(l=>l.name)))` → debe listar las 7.

- [ ] **Step 6: Commit** — `git add ProjectSettings/TagManager.asset Assets/Scripts/FX/YSort.cs* Assets/Scripts/Tests/EditMode/YSortTests.cs*` ; mensaje: `feat: sorting layers + YSort para oclusiones top-down`.

---

### Task 2: GuardVisual (lógica de facing + contra-rotación)

**Files:**
- Create: `Assets/Scripts/Guard/GuardVisual.cs`
- Test: `Assets/Scripts/Tests/EditMode/GuardVisualFacingTests.cs`

**Interfaces:**
- Consumes: convención de rotación de `GuardPatrol.FaceDirection` (0° = arriba; `target = -Atan2(x,y)`).
- Produces: componente `GuardVisual` (va en el hijo `Visual`, Task 4) con estáticos `Vector2 ComputeFacing(Vector2 velocity, float bodyRotationDeg, float walkThreshold)` y `Vector2 RotationToDirection(float rotationDeg)`. Alimenta parámetros de Animator `MoveX/MoveY/Speed` (mismos nombres que el player).

- [ ] **Step 1: Test que falla** — `Assets/Scripts/Tests/EditMode/GuardVisualFacingTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;

public class GuardVisualFacingTests
{
    [Test]
    public void RotacionCero_MiraArriba()
    {
        var d = GuardVisual.RotationToDirection(0f);
        Assert.AreEqual(0f, d.x, 1e-4f);
        Assert.AreEqual(1f, d.y, 1e-4f);
    }

    [Test]
    public void RotacionMenos90_MiraDerecha()
    {
        var d = GuardVisual.RotationToDirection(-90f);
        Assert.AreEqual(1f, d.x, 1e-4f);
        Assert.AreEqual(0f, d.y, 1e-4f);
    }

    [Test]
    public void EnMovimiento_FacingSigueVelocidad_CardinalDominante()
    {
        var f = GuardVisual.ComputeFacing(new Vector2(2f, 0.5f), 0f, 0.05f);
        Assert.AreEqual(new Vector2(1f, 0f), f);
    }

    [Test]
    public void Quieto_FacingSigueRotacionDelCuerpo()
    {
        var f = GuardVisual.ComputeFacing(Vector2.zero, 180f, 0.05f);
        Assert.AreEqual(new Vector2(0f, -1f), f);
    }
}
```

- [ ] **Step 2: Ver fallo de compilación** (`GuardVisual` no existe) vía `assets-refresh` + logs.

- [ ] **Step 3: Implementación** — `Assets/Scripts/Guard/GuardVisual.cs`:

```csharp
using UnityEngine;

/// <summary>
/// Visual del guardia: anima idle/walk 4-direcciones y contra-rota para que el
/// cuerpo se vea de pie mientras el Rigidbody2D del padre (y su cono de visión)
/// rotan con MoveRotation. La ROTACIÓN visible del guardia la da el cono.
/// </summary>
[RequireComponent(typeof(Animator))]
public class GuardVisual : MonoBehaviour
{
    [SerializeField] Rigidbody2D body;           // rigidbody del guardia (padre)
    [SerializeField] float walkThreshold = 0.05f;

    Animator _animator;
    Vector2 _lastPos;

    void Awake()
    {
        _animator = GetComponent<Animator>();
        if (body == null) body = GetComponentInParent<Rigidbody2D>();
        _lastPos = body.position;
    }

    void LateUpdate()
    {
        // Contra-rotación: anula la rotación heredada del padre.
        transform.rotation = Quaternion.identity;

        Vector2 vel = (body.position - _lastPos) / Mathf.Max(Time.deltaTime, 0.0001f);
        _lastPos = body.position;

        Vector2 facing = ComputeFacing(vel, body.rotation, walkThreshold);
        _animator.SetFloat("MoveX", facing.x);
        _animator.SetFloat("MoveY", facing.y);
        _animator.SetFloat("Speed", vel.magnitude);
    }

    /// <summary>Si se mueve, mira hacia la velocidad; si no, hacia donde apunta el cuerpo.</summary>
    public static Vector2 ComputeFacing(Vector2 velocity, float bodyRotationDeg, float walkThreshold)
    {
        Vector2 dir = velocity.magnitude > walkThreshold
            ? velocity
            : RotationToDirection(bodyRotationDeg);
        if (dir == Vector2.zero) return Vector2.down;
        return Mathf.Abs(dir.x) >= Mathf.Abs(dir.y)
            ? new Vector2(Mathf.Sign(dir.x), 0f)
            : new Vector2(0f, Mathf.Sign(dir.y));
    }

    /// <summary>Inversa de GuardPatrol.FaceDirection: 0° = arriba, -90° = derecha.</summary>
    public static Vector2 RotationToDirection(float rotationDeg)
    {
        float rad = rotationDeg * Mathf.Deg2Rad;
        return new Vector2(-Mathf.Sin(rad), Mathf.Cos(rad));
    }
}
```

- [ ] **Step 4: Tests en verde** (`tests-run` EditMode).

- [ ] **Step 5: Commit** — `feat: GuardVisual con facing 4-direcciones y contra-rotación`.

---

### Task 3: Animaciones del guardia (clips Sword + controller clonado)

**Files:**
- Create (generados por editor script): `Assets/Animations/Guard/Guard_Idle_{up,down,left,right}.anim`, `Assets/Animations/Guard/Guard_Walk_{up,down,left,right}.anim`, `Assets/Animations/Guard/GuardTopDown.controller`

**Interfaces:**
- Consumes: clips del player en `Assets/Animations/Player/TopDown/` (fuente de mapeo frame→dirección); sheets `Assets/Sprites/Character_base/PNG/Sword_Idle/Sword_Idle_full.png` y `Sword_Walk/Sword_Walk_full.png`.
- Produces: `GuardTopDown.controller` con parámetros `MoveX/MoveY/Speed` y estados equivalentes al player pero con clips Sword. Task 4 lo asigna al Animator del guardia.

**Estrategia:** no adivinar qué fila del sheet es cada dirección: leerlo de los clips del player (que referencian `Unarmed_*_full`), extraer el índice `_N` del nombre de cada sprite keyframe, y usar el sprite `Sword_*_full_N` equivalente. El controller se clona con `AssetDatabase.CopyAsset` y se le intercambian los motions por nombre de estado.

- [ ] **Step 1: Ejecutar editor script** vía `script-execute` (isMethodBody=true, guardar JSON en scratchpad y pasar con `--input-file`):

```csharp
using System.Linq;
using System.Collections.Generic;
using UnityEditor.Animations;

string dstDir = "Assets/Animations/Guard";
if (!AssetDatabase.IsValidFolder(dstDir)) AssetDatabase.CreateFolder("Assets/Animations", "Guard");

// Índice de sprites Sword por (sheet, N)
Sprite[] LoadSheet(string path) => AssetDatabase.LoadAllAssetRepresentationsAtPath(path).OfType<Sprite>().ToArray();
Dictionary<int, Sprite> ByIndex(Sprite[] arr) => arr.ToDictionary(
    s => int.Parse(s.name.Substring(s.name.LastIndexOf('_') + 1)), s => s);

var swordIdle = ByIndex(LoadSheet("Assets/Sprites/Character_base/PNG/Sword_Idle/Sword_Idle_full.png"));
var swordWalk = ByIndex(LoadSheet("Assets/Sprites/Character_base/PNG/Sword_Walk/Sword_Walk_full.png"));

string srcDir = "Assets/Animations/Player/TopDown";
var map = new (string src, string dst, Dictionary<int, Sprite> sheet)[] {
    ("Idle_down", "Guard_Idle_down", swordIdle), ("Idle_up", "Guard_Idle_up", swordIdle),
    ("Idle_left", "Guard_Idle_left", swordIdle), ("Idle_right", "Guard_Idle_right", swordIdle),
    ("Walk_down", "Guard_Walk_down", swordWalk), ("Walk_up", "Guard_Walk_up", swordWalk),
    ("Walk_left", "Guard_Walk_left", swordWalk), ("Walk_right", "Guard_Walk_right", swordWalk),
};

var made = new Dictionary<string, AnimationClip>();
foreach (var (src, dst, sheet) in map)
{
    var srcClip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{srcDir}/{src}.anim");
    var binding = AnimationUtility.GetObjectReferenceCurveBindings(srcClip)
        .First(b => b.propertyName == "m_Sprite");
    var srcKeys = AnimationUtility.GetObjectReferenceCurve(srcClip, binding);

    var clip = new AnimationClip { frameRate = srcClip.frameRate };
    var settings = AnimationUtility.GetAnimationClipSettings(srcClip);
    AnimationUtility.SetAnimationClipSettings(clip, settings); // loopTime etc.

    var keys = srcKeys.Select(k => {
        var name = ((Sprite)k.value).name;                 // p.ej. Unarmed_Walk_full_7
        int n = int.Parse(name.Substring(name.LastIndexOf('_') + 1));
        return new ObjectReferenceKeyframe { time = k.time, value = sheet[n] };
    }).ToArray();

    var dstBinding = new EditorCurveBinding { type = typeof(SpriteRenderer), path = "", propertyName = "m_Sprite" };
    AnimationUtility.SetObjectReferenceCurve(clip, dstBinding, keys);
    AssetDatabase.CreateAsset(clip, $"{dstDir}/{dst}.anim");
    made[src] = clip;
}

// Clonar controller y swapear motions por nombre de estado
string ctrlPath = $"{dstDir}/GuardTopDown.controller";
AssetDatabase.CopyAsset($"{srcDir}/PlayerTopDown.controller", ctrlPath);
var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath);
int swapped = 0;
foreach (var layer in ctrl.layers)
{
    void Walk(AnimatorStateMachine sm)
    {
        foreach (var cs in sm.states)
        {
            if (cs.state.motion is AnimationClip c && made.ContainsKey(c.name))
            { cs.state.motion = made[c.name]; swapped++; }
            else if (cs.state.motion is BlendTree bt)
            {
                var children = bt.children;
                for (int i = 0; i < children.Length; i++)
                    if (children[i].motion is AnimationClip cc && made.ContainsKey(cc.name))
                    { children[i].motion = made[cc.name]; swapped++; }
                bt.children = children;
            }
        }
        foreach (var sub in sm.stateMachines) Walk(sub.stateMachine);
    }
    Walk(layer.stateMachine);
}
EditorUtility.SetDirty(ctrl);
AssetDatabase.SaveAssets();
Debug.Log($"[GUARDANIM] clips={made.Count} swapped={swapped}");
```

**Nota:** los clips del player pueden llamarse distinto al nombre de archivo — si `swapped` < 8, inspeccionar `ctrl` con `object-get-data` y ajustar `made`'s keys a los nombres reales de los motions (`c.name`).

- [ ] **Step 2: Verificar** — log `[GUARDANIM] clips=8 swapped=8` (o justificar si el controller usa menos estados). `console-get-logs` sin errores.

- [ ] **Step 3: Commit** — `feat: animaciones GuardTopDown (pack Sword) clonadas del player`.

---

### Task 4: Prefabs de guardias — hijo Visual + tinte + YSort

**Files:**
- Modify: `Assets/Prefabs/Guard_Patrol.prefab`, `Assets/Prefabs/Guard_Static.prefab` (vía editor script)

**Interfaces:**
- Consumes: `GuardTopDown.controller` (Task 3), `GuardVisual` (Task 2), `YSort` + layer `Actors` (Task 1).
- Produces: guardias con hijo `Visual` renderizando. Task 8 confía en que las instancias en escena heredan esto.

- [ ] **Step 1: Editor script** vía `script-execute` (un solo body, ambos prefabs):

```csharp
using System.Linq;
var jobs = new (string path, Color tint)[] {
    ("Assets/Prefabs/Guard_Patrol.prefab", new Color(1f, 0.72f, 0.72f, 1f)),   // rojizo
    ("Assets/Prefabs/Guard_Static.prefab", new Color(0.72f, 0.84f, 1f, 1f)),  // azulado
};
var ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/Guard/GuardTopDown.controller");
var idleSheet = AssetDatabase.LoadAllAssetRepresentationsAtPath(
    "Assets/Sprites/Character_base/PNG/Sword_Idle/Sword_Idle_full.png").OfType<Sprite>().OrderBy(s => s.name).First();

foreach (var (path, tint) in jobs)
{
    var root = PrefabUtility.LoadPrefabContents(path);

    // Si el root tuviera SpriteRenderer placeholder (círculo), quitarlo.
    var oldSr = root.GetComponent<SpriteRenderer>();
    if (oldSr != null) Object.DestroyImmediate(oldSr);

    var visual = root.transform.Find("Visual")?.gameObject;
    if (visual == null) { visual = new GameObject("Visual"); visual.transform.SetParent(root.transform, false); }

    var sr = visual.GetComponent<SpriteRenderer>() ?? visual.AddComponent<SpriteRenderer>();
    sr.sprite = idleSheet; sr.color = tint;
    sr.sortingLayerName = "Actors";

    var anim = visual.GetComponent<Animator>() ?? visual.AddComponent<Animator>();
    anim.runtimeAnimatorController = ctrl;

    if (visual.GetComponent<GuardVisual>() == null) visual.AddComponent<GuardVisual>();
    var ys = visual.GetComponent<YSort>() ?? visual.AddComponent<YSort>();
    var so = new SerializedObject(ys); so.FindProperty("feetOffset").floatValue = -0.35f; so.ApplyModifiedPropertiesWithoutUndo();

    PrefabUtility.SaveAsPrefabAsset(root, path);
    PrefabUtility.UnloadPrefabContents(root);
    Debug.Log($"[GUARDPFB] {path} ok");
}
```

- [ ] **Step 2: Verificar en Room_01** — abrir escena, screenshot de Scene/Game view: guardia con cuerpo visible tintado. Revisar que las instancias en escena no tengan overrides de SpriteRenderer añadidos (se limpian en Task 8 si los hay).

- [ ] **Step 3: Smoke play** — `editor-application-set-state` playmode ~5 s en Room_01, screenshot: el guardia patrulla animado (traslación) con cono rotando (rotación). Salir de playmode.

- [ ] **Step 4: Commit** — `feat: guardias con cuerpo visible, animación 4-dir y tinte por tipo`.

---

### Task 5: Generación de audio (Python → WAV en Resources)

**Files:**
- Create: `Assets/Resources/Audio/SFX/{key_pickup,door_open,lever,stone_pickup,stone_throw,stone_land,detected,alert,exit}.wav`, `Assets/Resources/Audio/UI/click.wav`, `Assets/Resources/Audio/Music/dungeon_ambient.wav`
- Create (temporal en scratchpad, NO commitear): `gen_audio.py`

**Interfaces:**
- Produces: 11 clips cargables con `Resources.Load<AudioClip>("Audio/SFX/door_open")` etc. Tasks 6–7 los consumen por esos paths exactos (sin extensión).

- [ ] **Step 1: Escribir `gen_audio.py` en el scratchpad** (síntesis: ondas cuadradas/triangulares con envolvente para SFX; para música, drones sinusoidales graves en Dm + goteos aleatorios suaves; 44100 Hz, 16-bit mono, loop de 20 s sin click en el borde — fade cruzado en los últimos 0.5 s):

```python
import math, random, struct, wave, os

SR = 44100
def env(n, a=0.01, d=0.15):
    at, dt = int(a*SR), int(d*SR)
    out = []
    for i in range(n):
        if i < at: out.append(i/at)
        elif i > n-dt: out.append(max(0.0, (n-i)/dt))
        else: out.append(1.0)
    return out

def square(f, t): return 1.0 if math.sin(2*math.pi*f*t) >= 0 else -1.0
def tri(f, t):    return 2/math.pi*math.asin(math.sin(2*math.pi*f*t))
def noise():      return random.uniform(-1, 1)

def render(dur, fn, vol=0.5):
    n = int(dur*SR); e = env(n)
    return [max(-1, min(1, fn(i/SR, i/n)*e[i]*vol)) for i in range(n)]

def save(path, samples):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with wave.open(path, 'w') as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR)
        w.writeframes(b''.join(struct.pack('<h', int(s*32767)) for s in samples))
    print("ok", path)

A = "C:/www/DungeonPuzzle/Assets/Resources/Audio"

# SFX (p = progreso 0..1, t = segundos)
save(f"{A}/SFX/key_pickup.wav",  render(0.35, lambda t,p: square(660+880*p, t)*0.8 + tri(1320*p+660, t)*0.2))
save(f"{A}/SFX/door_open.wav",   render(0.5,  lambda t,p: tri(120+60*p, t)*0.7 + noise()*0.25*(1-p)))
save(f"{A}/SFX/lever.wav",       render(0.18, lambda t,p: square(300 if p<0.5 else 220, t)))
save(f"{A}/SFX/stone_pickup.wav",render(0.15, lambda t,p: tri(400+100*p, t)))
save(f"{A}/SFX/stone_throw.wav", render(0.25, lambda t,p: noise()*(1-p)*0.8 + tri(600-350*p, t)*0.3))
save(f"{A}/SFX/stone_land.wav",  render(0.2,  lambda t,p: noise()*(1-p)**2 + tri(90, t)*0.5))
save(f"{A}/SFX/detected.wav",    render(0.6,  lambda t,p: square(440-200*p, t)*0.6 + square(220-100*p, t)*0.4))
save(f"{A}/SFX/alert.wav",       render(0.3,  lambda t,p: square(880 if (t*8)%1<0.5 else 1100, t)))
save(f"{A}/SFX/exit.wav",        render(0.7,  lambda t,p: tri(523*(1+0.5*int(p*3)/2), t)))  # arpegio asc.
save(f"{A}/UI/click.wav",        render(0.06, lambda t,p: square(1200, t)))

# Música: loop ambiental 20 s — drones Dm (D2, A2, D3, F3) + goteos
DUR = 20.0; n = int(DUR*SR)
freqs = [(73.42, .30), (110.0, .22), (146.83, .18), (174.61, .12)]
drops = sorted(random.uniform(0, DUR) for _ in range(14))
buf = []
for i in range(n):
    t = i/SR
    s = sum(v*math.sin(2*math.pi*f*t + 0.3*math.sin(2*math.pi*0.07*t)) for f, v in freqs)
    for d in drops:
        dt_ = t－d if False else t-d
        if 0 <= dt_ < 0.25:
            f = 900 - 2400*dt_
            s += 0.12*math.sin(2*math.pi*max(f,200)*dt_)*math.exp(-dt_*18)
    buf.append(s*0.45)
# loop sin click: crossfade de los últimos 0.5 s con el inicio
xf = int(0.5*SR)
for i in range(xf):
    a = i/xf
    buf[n-xf+i] = buf[n-xf+i]*(1-a) + buf[i]*a
save(f"{A}/Music/dungeon_ambient.wav", [max(-1, min(1, s)) for s in buf])
```

(OJO: la línea del `dt_` tiene un guion raro de ejemplo — escribir simplemente `dt_ = t-d`.)

- [ ] **Step 2: Ejecutar** `python gen_audio.py` → 11 líneas `ok ...`.

- [ ] **Step 3: Importar** — `assets-refresh`; verificar vía `script-execute`: `Debug.Log(Resources.Load<AudioClip>("Audio/SFX/door_open") != null)` → True.

- [ ] **Step 4: Commit** — `feat: SFX retro y loop ambiental generados (Resources/Audio)` (incluir `.meta`).

---

### Task 6: SfxLibrary + AudioMaster con música y volúmenes

**Files:**
- Create: `Assets/Scripts/Core/SfxLibrary.cs`
- Modify: `Assets/Scripts/Core/AudioMaster.cs`

**Interfaces:**
- Consumes: clips de Task 5; `GameProgress.MasterVolume/MusicVolume/SfxVolume` (ya existen, `GameProgress.cs:52-64`); `AudioMaster.PlaySFX(AudioClip, float)` (ya existe).
- Produces: `SfxLibrary.Play(string path, float volume = 1f)` (path relativo a `Resources/Audio/`, p.ej. `"SFX/door_open"`), `SfxLibrary.Get(string path)`; `AudioMaster.PlayMusic(AudioClip, float baseVolume = 0.6f)`. Task 7 usa `SfxLibrary.Play` en todos los ganchos.

- [ ] **Step 1: Crear `SfxLibrary.cs`:**

```csharp
using System.Collections.Generic;
using UnityEngine;

/// <summary>Carga perezosa de clips desde Resources/Audio y reproducción vía AudioMaster.</summary>
public static class SfxLibrary
{
    static readonly Dictionary<string, AudioClip> Cache = new();

    public static AudioClip Get(string path)
    {
        if (!Cache.TryGetValue(path, out var clip))
        {
            clip = Resources.Load<AudioClip>("Audio/" + path);
            if (clip == null) Debug.LogWarning($"SfxLibrary: falta Resources/Audio/{path}");
            Cache[path] = clip;
        }
        return clip;
    }

    public static void Play(string path, float volume = 1f)
    {
        if (AudioMaster.Instance != null)
            AudioMaster.Instance.PlaySFX(Get(path), volume);
    }
}
```

- [ ] **Step 2: Ampliar `AudioMaster.cs`** — música + volúmenes por canal. Reemplazar la clase para que quede así (conservando `PlaySFX` con su firma):

```csharp
using UnityEngine;

public class AudioMaster : MonoBehaviour
{
    public static AudioMaster Instance { get; private set; }

    AudioSource _sfx;
    AudioSource _music;
    float _musicBaseVolume = 0.6f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _sfx = gameObject.AddComponent<AudioSource>();
        _sfx.playOnAwake = false;
        _sfx.spatialBlend = 0f;

        _music = gameObject.AddComponent<AudioSource>();
        _music.playOnAwake = false;
        _music.spatialBlend = 0f;
        _music.loop = true;

        Apply();
        PlayMusic(SfxLibrary.Get("Music/dungeon_ambient"));
    }

    public void Apply()
    {
        AudioListener.volume = GameProgress.MasterVolume;
        Screen.fullScreen = GameProgress.Fullscreen;
        if (_music != null) _music.volume = _musicBaseVolume * GameProgress.MusicVolume;
    }

    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null || _sfx == null) return;
        _sfx.PlayOneShot(clip, volume * GameProgress.SfxVolume);
    }

    public void PlayMusic(AudioClip clip, float baseVolume = 0.6f)
    {
        if (clip == null || _music == null) return;
        _musicBaseVolume = baseVolume;
        _music.clip = clip;
        _music.volume = _musicBaseVolume * GameProgress.MusicVolume;
        _music.Play();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AutoCreate()
    {
        if (Instance != null) return;
        var go = new GameObject("AudioMaster");
        go.AddComponent<AudioMaster>();
    }
}
```

(Se elimina el `GetComponent<AudioSource>()` previo: el GO siempre es auto-creado sin AudioSource.)

- [ ] **Step 3: Compila limpio** — `assets-refresh` + logs sin errores.

- [ ] **Step 4: Smoke play 3 s** — entrar a playmode: la música ambiental debe sonar (verificar con `script-execute`: `Debug.Log(Object.FindAnyObjectByType<AudioMaster>().GetComponents<AudioSource>()[1].isPlaying)` → True). Salir.

- [ ] **Step 5: Commit** — `feat: SfxLibrary y AudioMaster con música ambiental y volúmenes por canal`.

---

### Task 7: Ganchos de SFX en gameplay y UI

**Files:**
- Modify: `Assets/Scripts/World/Door.cs` (método `Open` y `Close`), `Assets/Scripts/World/Lever.cs`, `Assets/Scripts/World/Stone.cs`, `Assets/Scripts/World/ThrownStone.cs`, `Assets/Scripts/World/ExitTrigger.cs`, `Assets/Scripts/Core/GameManager.cs` (`PlayerDetected`), `Assets/Scripts/Guard/GuardBase.cs`, `Assets/Scripts/UI/MainMenuUI.cs`, `Assets/Scripts/UI/PauseMenu.cs`
- Modify: `Assets/Prefabs/Key.prefab` (asignar `pickupSfx`)

**Interfaces:**
- Consumes: `SfxLibrary.Play(path)` (Task 6); paths de Task 5.

- [ ] **Step 1: Una línea por gancho** (exactamente estas):
  - `Door.Open()` tras `_open = true;` → `SfxLibrary.Play("SFX/door_open");`
  - `Door.Close()` tras `_open = false;` → `SfxLibrary.Play("SFX/door_open", 0.8f);`
  - `Lever.Interact` → convertir a bloque: `{ SfxLibrary.Play("SFX/lever"); linkedDoor.Toggle(); }`
  - `Stone.cs`: añadir `public override void OnPickedUp(PlayerInventory inventory) => SfxLibrary.Play("SFX/stone_pickup");` y en `Throw` antes de `Destroy`: `SfxLibrary.Play("SFX/stone_throw");`
  - `ThrownStone.OnCollisionEnter2D` antes de `TriggerNoise()` → `SfxLibrary.Play("SFX/stone_land");`
  - `ExitTrigger.OnTriggerEnter2D` tras el early-return del tag → `SfxLibrary.Play("SFX/exit");`
  - `GameManager.PlayerDetected()` primera línea → `SfxLibrary.Play("SFX/detected");`
  - `GuardBase`: en `Update` dentro de `if (State == GuardState.Normal)` (al pasar a Alerted) y en `AlertAt` tras el early-return → `SfxLibrary.Play("SFX/alert");`
  - `MainMenuUI` y `PauseMenu`: primera línea de cada método público `On*Clicked()` → `SfxLibrary.Play("UI/click");`

- [ ] **Step 2: Asignar `pickupSfx` de la llave** vía `script-execute`:

```csharp
var path = "Assets/Prefabs/Key.prefab";
var root = PrefabUtility.LoadPrefabContents(path);
var so = new SerializedObject(root.GetComponent<Key>());
so.FindProperty("pickupSfx").objectReferenceValue =
    AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Audio/SFX/key_pickup.wav");
so.ApplyModifiedPropertiesWithoutUndo();
PrefabUtility.SaveAsPrefabAsset(root, path);
PrefabUtility.UnloadPrefabContents(root);
Debug.Log("[KEYSFX] ok");
```

- [ ] **Step 3: Compila + tests** — `assets-refresh`, logs limpios, `tests-run` EditMode verde.

- [ ] **Step 4: Commit** — `feat: SFX en puerta, palanca, piedra, alerta, detección, salida y UI`.

---

### Task 8: Escenas — suelo tileado, sprite de salida, layers y limpieza

**Files:**
- Modify: `Assets/Scenes/Room_01..05.unity` (vía editor script por escena)
- Create (si el atlas no sirve): `Assets/Sprites/Game/exit_hatch.png` (generado con Python, 32×32 pixel-art de trampilla/escalera, mismo tono que `floor.png`)

**Interfaces:**
- Consumes: layers de Task 1, prefabs de Task 4.

- [ ] **Step 1: Decidir sprite de salida** — `Read` de `Assets/Sprites/Game/Entorno_Dungeon_Texturas.png`; si contiene trampilla/escalera clara, rebanar esa región (editar `.meta` vía `assets-modify` o SpriteEditor por script). Si no: generar `exit_hatch.png` con Python (32×32, paleta tomada de `floor.png`: marco marrón oscuro + peldaños descendentes en degradado) e importarlo con `spritePixelsToUnits` igual al de `floor.png` (ver su `.meta`).

- [ ] **Step 2: Script por escena** vía `script-execute` (repetir para Room_01..05, cambiando el path):

```csharp
using System.Linq;
using UnityEditor.SceneManagement;
var scenePath = "Assets/Scenes/Room_01.unity"; // ← iterar 01..05
var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

// 1) Bounds interiores: unión de bounds de todos los SpriteRenderers de muros (sprite wall)
var all = Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
var walls = all.Where(s => s.sprite != null && s.sprite.name.ToLower().Contains("wall")).ToArray();
var b = walls[0].bounds; foreach (var w in walls) b.Encapsulate(w.bounds);

// 2) Suelo tileado
var floorSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Game/floor.png");
var floorGO = GameObject.Find("FloorTiled") ?? new GameObject("FloorTiled");
var fsr = floorGO.GetComponent<SpriteRenderer>() ?? floorGO.AddComponent<SpriteRenderer>();
fsr.sprite = floorSprite; fsr.drawMode = SpriteDrawMode.Tiled;
fsr.size = new Vector2(b.size.x, b.size.y);
fsr.sortingLayerName = "Floor";
floorGO.transform.position = new Vector3(b.center.x, b.center.y, 0f);

// 3) Salida: ExitTrigger con sprite nuevo
foreach (var ex in Object.FindObjectsByType<ExitTrigger>(FindObjectsSortMode.None))
{
    var esr = ex.GetComponent<SpriteRenderer>();
    if (esr != null)
    {
        esr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Game/exit_hatch.png");
        esr.color = Color.white; esr.drawMode = SpriteDrawMode.Simple;
        esr.sortingLayerName = "FloorFX";
        esr.transform.localScale = Vector3.one; // ajustar si queda demasiado grande/pequeña
    }
}

// 4) Layers del resto + limpiar overrides de guardias
foreach (var s in all)
{
    if (s == fsr) continue;
    var go = s.gameObject; var n = s.sprite ? s.sprite.name.ToLower() : "";
    bool isGuardInstance = go.GetComponentInParent<GuardBase>() != null && go.transform.parent == null == false;
    if (go.GetComponent<GuardBase>() != null && s != null) { Object.DestroyImmediate(s); continue; } // círculo placeholder en root del guardia
    if (n.Contains("wall")) s.sortingLayerName = "WallsTop";
    else if (n.Contains("floor")) s.sortingLayerName = "Floor";
    else if (go.GetComponent<PickupItem>() != null || go.GetComponent<Lever>() != null) s.sortingLayerName = "Objects";
    else if (go.GetComponent<Door>() != null || go.CompareTag("Player")) {
        s.sortingLayerName = "Actors";
        if (go.GetComponent<YSort>() == null) go.AddComponent<YSort>();
    }
}

EditorSceneManager.MarkSceneDirty(scene);
EditorSceneManager.SaveScene(scene);
Debug.Log($"[SCENEPASS] {scenePath} ok walls={walls.Length}");
```

**Nota:** este script es orientativo — el ejecutor DEBE inspeccionar la primera escena (`scene-get-data`) antes de correrlo y ajustar los criterios de match (nombres reales de sprites/objetos), y tras Room_01 validar con screenshot antes de correr las otras 4. El cono de visión usa MeshRenderer (no SpriteRenderer): setear su `sortingLayerName = "FX"` vía `GetComponent<MeshRenderer>()` en los guardias.

- [ ] **Step 3: Screenshot de cada sala** (Game view) — suelo texturizado, salida con trampilla, guardias visibles, sin círculos placeholder.

- [ ] **Step 4: Prefab del Player también con YSort** — si el Player es prefab (lo es: `Assets/Prefabs/Player.prefab`), añadir `YSort` + layer `Actors` al prefab en vez de por escena (mismo patrón del script de Task 4; `feetOffset` -0.4). Las piedras/llaves prefab → layer `Objects` en el prefab.

- [ ] **Step 5: Commit** — `feat: suelo tileado, sprite de salida, sorting layers en salas y prefabs`.

---

### Task 9: Verificación final end-to-end

**Files:** ninguno nuevo.

- [ ] **Step 1: Tests** — `tests-run` EditMode: TODO verde.
- [ ] **Step 2: Play run Room_01** — entrar a playmode ~10 s: guardia patrullando animado (traslación visible), cono rotando suave (rotación visible), música ambiental sonando, suelo texturizado. Screenshot. Salir.
- [ ] **Step 3: Revisión de oclusiones** — mover al player (o teletransportarlo vía `script-execute` en playmode) por encima y por debajo de un guardia y de la puerta; screenshots: el de menor Y tapa al de mayor Y en ambos casos.
- [ ] **Step 4: Logs limpios** — `console-get-logs` sin errores ni warnings nuevos.
- [ ] **Step 5: Commit final si quedó algo suelto** + resumen al usuario con screenshots antes/después.

---

## Self-Review (hecho)

- **Cobertura del spec:** guardias (Tasks 2-4), texturas (Task 8 + exit sprite), sorting (Tasks 1, 4, 8), audio (Tasks 5-7), verificación (Task 9). Música respeta MusicVolume (Task 6). ✓
- **Sin placeholders:** todos los pasos tienen código o comando concreto; los dos scripts de escena/animación marcan explícitamente qué debe validar el ejecutor contra el estado real (nombres de motions, estructura de escena) — inspección, no invención. ✓
- **Consistencia de tipos:** `SfxLibrary.Play(string,float)`, `AudioMaster.PlaySFX(AudioClip,float)`, `YSort.ComputeOrder(float)`, `GuardVisual.ComputeFacing(Vector2,float,float)` usados idénticos en todas las tasks. ✓
