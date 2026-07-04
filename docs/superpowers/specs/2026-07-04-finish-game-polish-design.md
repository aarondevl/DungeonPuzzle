# Spec: Pulido final de DungeonPuzzle (guardias, texturas, sorting, audio)

**Fecha:** 2026-07-04
**Estado:** Aprobado por el usuario
**Alcance:** Pulir lo existente. Cero mecánicas ni contenido nuevo.

## Contexto

Juego de sigilo top-down 2D (URP, Unity 6000.5.0b10, Input System). 5 salas + menú + game over.
Diagnóstico actual:

- Los guardias no tienen `SpriteRenderer` (se ven como círculo placeholder) aunque su lógica
  de **traslación** (`MovePosition`) y **rotación** (`MoveRotation`) ya funciona.
- El suelo de las salas es negro (existe `floor.png` sin usar); la salida es un cuadrado verde.
- Solo existe la sorting layer `Default` → oclusiones incorrectas entre player/guardias/props.
- No hay ni un clip de audio en el proyecto. `AudioMaster.PlaySFX` y los campos
  `Key.pickupSfx` ya están cableados de la sesión anterior.
- El player ya tiene Animator top-down 4 direcciones con el pack `Character_base/Unarmed_*`.

## 1. Guardias

**Visual:** hijo `Visual` con `SpriteRenderer` en ambos prefabs (`Guard_Patrol`, `Guard_Static`),
usando el pack `Character_base` variante **Sword** (mismo estilo que el player).
Tinte: patrulla rojizo suave, estático azulado.

**Animación:** `Assets/Animations/Guard/GuardTopDown.controller` calcado al patrón del player:
parámetros `MoveX`, `MoveY`, `Speed`; clips `Idle_{up,down,left,right}` y `Walk_{up,down,left,right}`
construidos desde `Sword_Idle_full.png` y `Sword_Walk_full.png` (6 frames × 4 direcciones, ya rebanados).

**Script nuevo `GuardVisual.cs`** (en el hijo `Visual`):
- Lee posición/rotación del `Rigidbody2D` del padre; deriva velocidad y facing.
- Alimenta `MoveX/MoveY/Speed` del Animator.
- Contra-rota cada frame (`transform.rotation = Quaternion.identity`) para que el cuerpo se vea
  de pie mientras el rigidbody padre —y el cono de visión— rotan suavemente. La rotación visible
  y continua del guardia la da el **cono de visión**.
- Guardia estático: idle 4-direcciones según la dirección de su cono.

**No se toca:** `GuardBase`, `GuardPatrol`, `GuardStatic`, `VisionCone` (la IA queda igual).

## 2. Texturas faltantes

- **Suelo:** sprite con `floor.png` en Draw Mode Tiled cubriendo el interior de cada una de las
  5 salas, sorting layer `Floor`.
- **Salida:** reemplazar el cuadrado verde por sprite de trampilla/escalera; se extrae del atlas
  `Entorno_Dungeon_Texturas.png` si contiene algo útil, si no se genera un pixel-sprite acorde.
- Puerta, llave, muros y HUD no se tocan.

## 3. Sorting / oclusiones (enfoque aprobado: layers + Y-sort en actores)

- Sorting layers (orden de atrás a adelante): `Floor, FloorFX, Objects, Actors, WallsTop, FX`
  (más `Default` existente).
- **Script nuevo `YSort.cs`**: en `LateUpdate`, `sortingOrder = -(int)((pos.y + feetOffset) * 100)`.
  Se añade a player, guardias, puerta y piedra.
- Asignación: suelo→`Floor`; polvo/pop→`FloorFX`/`FX`; llave/piedra/palanca→`Objects`;
  player/guardias/puerta→`Actors`; cono de visión→`FX`.
- Aplica a los prefabs y a las 5 salas.

## 4. Audio (generado proceduralmente, estilo retro chiptune, WAV 44.1 kHz mono)

| Archivo | Uso | Gancho |
|---|---|---|
| `Audio/SFX/key_pickup.wav` | recoger llave | campo `Key.pickupSfx` (ya existe) |
| `Audio/SFX/door_open.wav` | puerta abre | `Door.Open()` |
| `Audio/SFX/lever.wav` | palanca | `Lever` |
| `Audio/SFX/stone_pickup.wav` | recoger piedra | `Stone.OnPickedUp` |
| `Audio/SFX/stone_throw.wav` | lanzar piedra | `PlayerInteraction.TryThrow` |
| `Audio/SFX/stone_land.wav` | piedra aterriza | `ThrownStone` |
| `Audio/SFX/detected.wav` | jugador detectado | `GameManager.PlayerDetected` |
| `Audio/SFX/alert.wav` | guardia pasa a Alerted | `GuardBase` |
| `Audio/SFX/exit.wav` | completar sala | `ExitTrigger` |
| `Audio/UI/click.wav` | botones de menú | `MainMenuUI` / `PauseMenu` |
| `Audio/Music/dungeon_ambient.wav` | loop ambiental ~20 s (drone grave + goteos) | `AudioMaster.PlayMusic` |

`AudioMaster` gana `PlayMusic(clip, volume)` con un segundo `AudioSource` en loop,
volumen menor que SFX, respetando `GameProgress.MasterVolume`.
Los SFX se asignan por Inspector (campos `[SerializeField] AudioClip`) en prefabs/escenas;
los que viven en singletons sin escena (GameManager/AudioMaster) se cargan vía `Resources`
o referencia serializada según lo que ya use el proyecto (decisión de implementación:
preferir campos serializados; `Resources/` solo si no hay dónde serializar).

## 5. Fuera de alcance

Mecánicas, niveles, IA, HUD, menús, vidas, timer, contenido nuevo.

## 6. Verificación

- Tests EditMode existentes en verde (`tests-run`).
- Play en Room_01: guardia visible y animado caminando (traslación), cono rotando (rotación),
  suelo texturizado, oclusiones correctas al pasar por delante/detrás de puerta y guardias,
  sonidos audibles.
- Screenshots antes/después por sala.

## Decisiones tomadas con el usuario

1. Alcance: pulir lo existente (no contenido nuevo).
2. Audio: generado proceduralmente estilo retro; SFX + loop ambiental.
3. Guardias: pack Sword con tinte; la rotación visible la da el cono de visión
   (el sprite contra-rota y anima por direcciones, como el player).
4. Oclusiones: enfoque A (sorting layers + YSort en actores).
