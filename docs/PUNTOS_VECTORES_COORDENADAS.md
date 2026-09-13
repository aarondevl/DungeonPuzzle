# Puntos, vectores y coordenadas en Dungeon Puzzle

Dónde se aplica cada concepto del tema dentro del juego. Toda la matemática pura
vive en `Assets/Scripts/Core/VectorMath.cs` y está cubierta por
`Assets/Scripts/Tests/EditMode/VectorMathTests.cs` (Window → General → Test Runner → EditMode).

## Convenciones

| Concepto | En el juego |
|---|---|
| Sistema de coordenadas | Unity 2D: +X derecha, +Y arriba. Origen (0,0) en el centro de cada sala. |
| Punto | Posición de un objeto: `Rigidbody2D.position`, `transform.position`, waypoints, punto del ruido. |
| Vector | Diferencia entre dos puntos: `destino - origen`. Tiene dirección y magnitud. |
| Vector unitario | `Direction(from, to)` normaliza el vector: solo dirección, magnitud 1. |
| Rotación de un guardia | Grados sobre Z, 0° = arriba, antihorario positivo (−90° = derecha). `DirectionToAngle` / `AngleToDirection` convierten entre vector y ángulo. |

## Jugador (`PlayerMovement`, `PlayerInteraction`)

1. **Entrada → vector dirección.** WASD produce `(h, v)` y se normaliza para que la
   diagonal no sea √2 veces más rápida.
2. **Velocidad = dirección · rapidez.** La velocidad actual se acerca a la deseada
   con `StepVelocity` (aceleración · Δt) y el Rigidbody integra
   `posición += velocidad · Δt`.
3. **Facing cardinal.** `SnapFacing` proyecta el vector sobre los ejes con producto
   punto (`ToCardinal`) y elige el dominante; eso alimenta `MoveX`/`MoveY` del
   Animator, que ahora usa las filas correctas del sprite sheet (antes arriba y
   abajo estaban invertidas al caminar).
4. **Coordenadas de pantalla → mundo.** Al lanzar una piedra, la posición del ratón
   (píxeles) se convierte con `Camera.ScreenToWorldPoint` y el vector
   `objetivo - héroe` da la dirección de tiro. Sin ratón se lanza en la dirección
   de mirada.
5. **Gizmos.** Con el héroe seleccionado, la vista de escena dibuja el vector
   velocidad (amarillo) y el facing (cian).

## Cono de visión (`VisionCone`)

La detección ya no depende de la malla dibujada. Un héroe es visto si:

1. `|héroe − guardia| ≤ alcance` (magnitud del vector).
2. `ángulo(forward, héroe − guardia) ≤ apertura / 2`, calculado con producto punto:
   `cos θ = (a · b) / (|a||b|)`.
3. Un raycast a lo largo de ese vector no toca ningún muro (línea de visión).

`DistanceToPlayer` y `AngleToPlayer` quedan expuestos para depurar, y el gizmo
dibuja los bordes del cono, la mirada y el vector hacia el héroe.

## Guardias (`GuardPatrol`, `GuardStatic`)

- **Patrulla:** `dirección = normalizar(waypoint − posición)`; el paso es
  `dirección · rapidez · Δt` recortado al destino (`StepTowards`), y la rotación
  del cuerpo es `DirectionToAngle(dirección)`.
- **Investigar:** al ver al héroe u oír una piedra, el guardia guarda ese punto,
  camina hacia él y se detiene a `investigateRadius` sin vibrar alrededor.
- **Guardia fijo:** barre con una senoide alrededor de su ángulo base
  (`SweepAngle`). Al alertarse gira hacia el punto del ruido usando el ángulo del
  vector `ruido − guardia`, y al calmarse vuelve al barrido girando, no saltando.

## Cambio de sistema de coordenadas

`WorldToLocal(origen, forward, punto)` expresa un punto del mundo en el marco del
observador: la componente lateral es `punto · right` y la frontal `punto · forward`.
Es la misma operación que hace `transform.InverseTransformPoint` al construir la
malla del cono en coordenadas locales.
