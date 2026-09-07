# Guía de capturas — Semana 04

Las seis capturas que pide la presentación, con el encuadre exacto y cómo dejar
Unity listo para cada una. Se suben desde la propia presentación: cada lámina
tiene un botón **Añadir captura**; la imagen se comprime en el navegador y queda
guardada para quien abra el enlace.

> **Ya tomadas.** Las seis capturas están en `docs/Semana04/capturas/` y
> referenciadas desde `PRESENTACION.md`. Esta guía queda como referencia para
> repetirlas o mejorar alguna.

## Preparación común (una sola vez)

1. Abrir el proyecto con Unity `6000.5.0b10`.
2. Menú **`DungeonPuzzle ▸ Semana 04 ▸ Construir todo`**. Comprobar en la consola
   que el informe de validación no imprime ninguna línea `FALLA`.
3. `Edit ▸ Project Settings ▸ Physics 2D` → marcar **Always Show Colliders**
   (se desmarca al terminar; solo hace falta para la captura 4).
4. Cerrar paneles que no aporten. Una captura con quince ventanas abiertas no se
   lee proyectada.
5. Tema del editor: da igual cuál, pero **el mismo en las seis**. Mezclar claro y
   oscuro en el mismo deck se nota.

**Cómo capturar:** `Shift + Cmd + 4` en macOS o la Herramienta Recortes en
Windows, recortando la región concreta. No capturar la pantalla completa: la
presentación reduce la imagen a 1400 px de lado mayor, y lo que sobra roba
legibilidad a lo que importa.

---

## Captura 1 · Panel Project
**Lámina:** «Una carpeta por responsabilidad, tres assemblies por dependencia»

- Panel **Project** en modo lista (no iconos grandes).
- Desplegar `Assets/Scripts` hasta ver las seis subcarpetas: `Core`, `Player`,
  `Guard`, `World`, `UI`, `FX`, `Tests`.
- Que se vean los tres archivos `.asmdef`: `DungeonPuzzle.Runtime`,
  `DungeonPuzzle.Editor` y `DungeonPuzzle.Tests`.
- Ensanchar el panel lo suficiente para que ningún nombre quede cortado con «…».

## Captura 2 · Inspector del Player
**Lámina:** «Composición sobre herencia»

- Seleccionar el `Player` en la jerarquía de cualquier sala.
- Capturar el **Inspector** completo. Tienen que verse:
  - `Rigidbody 2D` (Body Type *Dynamic*, Gravity Scale 0),
  - **dos** `Circle Collider 2D`, uno de ellos con *Is Trigger* marcado — es el
    sensor de interacción, y es justo lo que explica la lámina,
  - `Player Movement`, `Player Interaction`, `Player Inventory`,
    `Interaction Sensor`.
- Colapsar el `Animator` y el `Sprite Renderer` si no cabe todo.

> Si `Interaction Sensor` no aparece, falta ejecutar el paso 3 del menú
> (`Añadir sensor al prefab del jugador`). En ejecución se añade solo, pero para
> la captura conviene verlo en el prefab.

## Captura 3 · Matriz de colisiones
**Lámina:** «La matriz, recortada a lo que el juego necesita»

- `Edit ▸ Project Settings ▸ Physics 2D`.
- Desplazar hasta **Layer Collision Matrix** y desplegarla.
- Encuadrar la matriz entera, con las capas `Projectile` y `Hazard` visibles.
- Si entra en el mismo recorte, incluir el campo **Gravity** en `(0, 0)`.

Es la captura que se contrasta en vivo con la tabla de la lámina, así que tiene
que leerse casilla por casilla.

## Captura 4 · Colliders en la Scene view
**Lámina:** «La respuesta al muro: de empujón correctivo a deslizamiento»

- Con **Always Show Colliders** activo, entrar en **Play**.
- Llevar al héroe contra un muro y **pausar** (`Ctrl/Cmd + Shift + P`).
- Capturar la **Scene view** encuadrando al héroe y el muro. Deben distinguirse:
  - el círculo del collider sólido,
  - el círculo mayor del trigger del sensor,
  - la malla del cono de visión de un guardia, si cabe en el encuadre.
- Gizmos activados; vista 2D.

## Captura 5 · Las trampas en Game view
**Lámina:** «SpikeTrap: cuatro fases, tres trampas desfasadas»

- En **Play**, en `Room_02` o `Room_03`.
- Encuadrar la **Game view** con las tres trampas del trayecto y el héroe cerca.
- El instante ideal: **una trampa clavada y las otras dos ocultas**. Es
  exactamente lo que demuestra el desfase, y es lo que la lámina afirma.
- Pausar en el momento justo en vez de intentar acertar el clic.

> Alternativa mejor si da tiempo: grabar 4-5 segundos de vídeo del ciclo
> completo. Una captura fija demuestra el estado; el vídeo demuestra el ritmo.

## Captura 6 · Test Runner
**Lámina:** «Lo que se puede comprobar sin abrir el juego»

- `Window ▸ General ▸ Test Runner`, pestaña **EditMode**.
- **Run All** y esperar a que termine (tarda menos de un segundo).
- Desplegar el árbol y capturar con los checks verdes visibles.
- Que se lean los nombres de las clases: `SpikeTrapCycleTests`,
  `PressurePlateTests`, `ThrownStoneBounceTests`, `CollisionLayersTests`,
  `InteractionSensorTests`, `PlayerFacingTests`.
- El contador total debe decir **45**.

---

## Después de capturar

- Desmarcar **Always Show Colliders** en `Project Settings ▸ Physics 2D`.
- Salir de Play antes de guardar nada: los cambios hechos en Play se pierden, y
  los hechos en Play sobre un prefab pueden ensuciarlo.

## Si algo no coincide con lo que dice la presentación

Es información, no un problema de la captura. Anotarlo y decirlo en la
sustentación: `DungeonPuzzle ▸ Semana 04 ▸ 4. Validar física y capas` imprime en
consola qué capa o qué par de la matriz está fuera de sitio.
