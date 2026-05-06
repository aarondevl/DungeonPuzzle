# Guía para añadir sprites externos

Esta guía es el procedimiento que el equipo debe seguir para reemplazar o añadir sprites en el juego. El **Player** ya está hecho como ejemplo (`Assets/Sprites/Game/player_walk.png` con 4 frames y animación). Replica este flujo para Guard, Key, Stone, Door y nuevos enemigos.

## 1. Fuentes de sprites recomendadas (CC0 / libres)

Todas estas fuentes son legales para uso académico y comercial.

| Pack | URL | Licencia | Estilo |
|------|-----|----------|--------|
| **Kenney — Tiny Dungeon** | <https://kenney.nl/assets/tiny-dungeon> | CC0 | 16x16 fantasía pixel |
| **Kenney — 1-Bit Pack** | <https://kenney.nl/assets/1-bit-pack> | CC0 | Monocromo retro |
| **Kenney — Roguelike Pack** | <https://kenney.nl/assets/roguelike-rpg-pack> | CC0 | Top-down RPG |
| **OpenGameArt — Dungeon Tileset** | <https://opengameart.org/content/dungeon-tileset> | CC-BY 3.0 (atribución) | Variedad |
| **itch.io free assets** | <https://itch.io/game-assets/free> | Variable, leer cada uno | Variedad |

Después del PR cada autor debe agregarse a `docs/CREDITS.md` con la línea: `<NombreSprite> — <Autor> — <URL> — <Licencia>`.

## 2. Convención de carpetas

```
Assets/Sprites/
├── Game/                # sprites de juego (player, guard, key, etc.)
│   ├── player_walk.png  # ejemplo de sheet 64x16, 4 frames
│   └── ...
├── UI/                  # iconos HUD (heart_full, heart_empty, etc.)
└── External/            # sheets descargados sin modificar (opcional, para tracking)
```

Nombrado: `<entidad>_<estado>.png` para individuales, `<entidad>_<estado>.png` con frames horizontales para sheets (ej `player_walk.png` = 4 frames).

## 3. Import settings (críticos para pixel art)

Después de copiar el PNG, selecciónalo en Unity y configura el inspector así:

| Campo | Valor |
|-------|-------|
| **Texture Type** | Sprite (2D and UI) |
| **Sprite Mode** | Single (un solo frame) **o** Multiple (sheet animado) |
| **Pixels Per Unit** | 16 (debe coincidir con el tamaño de tile del juego) |
| **Filter Mode** | Point (no filter) — sin esto se ve borroso |
| **Compression** | None — o el alpha se rompe |
| **Mip Maps** | Off |
| **Alpha Is Transparency** | On |

Apply y comprobar.

## 4. Slicing (sheets multi-frame)

Si el PNG es un sheet (varios frames), tras configurar `Sprite Mode = Multiple`:

1. **Sprite Editor** (botón en el inspector).
2. **Slice → Type: Grid By Cell Size** → Pixel Size 16 x 16 → Slice → Apply.
3. Renombra cada sub-sprite con sufijo numérico: `player_walk_0`, `player_walk_1`, `player_walk_2`, `player_walk_3`.

## 5. Animation Clip

Existe un AnimationClip por estado (`Assets/Animations/Player/PlayerWalk.anim`, `PlayerIdle.anim`).

Para reemplazar frames:

1. Abre el `.anim` con doble click → ventana **Animation**.
2. En la línea de tiempo, borra los keyframes de `Sprite` viejos.
3. Selecciona los nuevos sub-sprites (multi-select desde Project) y arrástralos a la timeline.
4. Ajusta sample rate (la del Player es 8 fps).
5. Asegura `Loop Time` = ON para Walk.

El `Player.controller` ya tiene la transición Idle ↔ Walk con condición `Speed > 0.1`. **No hay que tocar el controller** si reemplazas frames de los clips existentes.

## 6. Conectar al prefab

1. Abre `Assets/Prefabs/<Entidad>.prefab` (Open Prefab).
2. Selecciona el GameObject root.
3. **SpriteRenderer.Sprite** = primer frame (idle).
4. **Material** = `Assets/Materials/PlayerSprite_Unlit.mat` (sprite no recibe luz 2D — uniforme).
5. Save Prefab.

## 7. Verificación

- Entra a Play en `Room_01`.
- Camina con WASD: la animación Walk debe correr a 8 fps.
- Suelta teclas: vuelve a Idle (frame 0 estático).
- Sin warnings de Animator en consola.

## 8. Trabajo asignado al equipo

Cada miembro toma una entidad y replica el flujo completo (descarga → import → slice → animación → prefab → test).

| Entidad | Frames recomendados | Estados |
|---------|---------------------|---------|
| Guard_Static | 1 | idle |
| Guard_Patrol | 4 | walk loop |
| Key | 4 | spin/float |
| Stone | 1 | idle |
| Door | 2 | closed, open |
| Lever | 2 | down, up |
| (Opcional) Pickup item nuevo | 4 | spin/float |

Cuando termines tu entidad, abre PR contra `main` con título `feat(sprites): <entidad>`. Adjunta screenshot in-game.

## 9. Crear un nivel nuevo (opcional)

`Assets/Editor/RoomBuilder.cs` ya contiene el patrón: `BuildRoom03`, `BuildRoom04`, `BuildRoom05`. Para añadir Room_06:

1. Duplica `Assets/Scenes/Room_05.unity` → `Room_06.unity`.
2. Añádelo al **File → Build Settings → Scenes In Build** (después de Room_05).
3. En `Assets/Scripts/Core/GameManager.cs`, sube `GameProgress.TotalLevels` a 6 (`Assets/Scripts/Core/GameProgress.cs:5`).
4. En `RoomBuilder.cs`, copia `BuildRoom05` como `BuildRoom06` y ajusta layout (walls, guards, key, lever, exit).
5. Ejecuta el menú **Tools → DungeonPuzzle → Build Room 06**.
6. Test: jugar desde `Room_05` debe llevar a `Room_06` y luego a `GameOver`.

## 10. Errores comunes

| Síntoma | Causa | Fix |
|---------|-------|-----|
| Sprite borroso | Filter Mode = Bilinear | Ponlo a Point |
| Sprite gris en escena | Material URP 2D recibe luz | Asignar `PlayerSprite_Unlit.mat` |
| Animación se traba en frame 0 | Loop Time = OFF | Activar Loop Time |
| Frames invertidos | Sheet ordenado de derecha a izquierda | Renombrar sub-sprites |
| Player muy grande/chico | PixelsPerUnit no coincide | Poner a 16 (resto del juego) |
