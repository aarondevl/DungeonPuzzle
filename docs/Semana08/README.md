# Semana 08 — Avance del proyecto: mecánicas secundarias y parallax

**Proyecto:** DungeonPuzzle — juego de sigilo y puzles top-down 2D
**Motor:** Unity `6000.5.0b10` · Universal Render Pipeline (2D Renderer) · Input System
**Rama:** `feat/semana08-parallax`

Este documento cubre los dos puntos de la Semana 8. La primera parte inventaría las
mecánicas secundarias que ya existen en el prototipo y explica cómo se apoyan en la
mecánica principal (sigilo + distracción). La segunda documenta el sistema de
parallax, que es lo nuevo de esta semana.

> **Antes de probar:** las texturas y las capas de parallax se generan con el menú
> **`DungeonPuzzle ▸ Semana 08 ▸ Construir todo`** (ver §4). Es el mismo criterio
> de las Semanas 04 y 05: en git van los scripts, las pruebas y el builder; los
> binarios los produce el editor.

---

## 1. Mecánicas secundarias

La mecánica principal es **moverse por la sala sin ser detectado**. Todo lo demás
son mecánicas secundarias: no son el objetivo, pero cambian cómo se resuelve cada
sala y le dan al jugador decisiones que tomar.

| Mecánica | Script | Qué aporta a la principal |
|---|---|---|
| Lanzar una piedra | `World/Stone.cs`, `World/ThrownStone.cs`, `World/NoiseSource.cs` | Convierte el sigilo en algo activo: el ruido mueve al guardia y abre una ventana de paso. |
| Llave y puerta | `World/Key.cs`, `World/Door.cs`, `Player/PlayerInventory.cs` | Obliga a recorrer la sala en un orden: primero la llave, luego la puerta. |
| Palanca | `World/Lever.cs` | Mecanismo que se acciona a distancia de la puerta: hay que exponerse para abrirla. |
| Placa de presión | `World/PressurePlate.cs` | Reacciona al peso de cuerpos físicos (héroe o guardia). Modo mantenido o con enclavamiento. |
| Trampa de pinchos | `World/SpikeTrap.cs` | Obstáculo con ciclo propio (oculta → sube → extendida → baja). Hay que leer el ritmo. |
| Pasaje secreto | `World/SecretDiscovery.cs` | Ruta opcional de Room_04 que se registra en el progreso sin ser obligatoria. |
| Inventario de una ranura | `Player/PlayerInventory.cs` | Recurso escaso: una piedra ocupa la ranura hasta lanzarla. |

### 1.1 Cómo se relacionan

```
  héroe ──E──▶ recoger piedra ──F──▶ lanzar ──▶ ruido ──▶ guardia investiga
                                                              │
  héroe ──E──▶ palanca / placa ──▶ puerta ──▶ ruta libre ◀────┘
  héroe ──pisa──▶ trampa / cono de visión ──▶ pierde una vida ──▶ recarga la sala
```

Ninguna mecánica secundaria funciona sola: la piedra solo sirve porque hay guardias
con cono de visión; la placa solo sirve porque hay una puerta; la trampa solo
importa porque cuesta una vida. Esa dependencia es lo que las convierte en
secundarias y no en minijuegos aparte.

### 1.2 Cobertura por pruebas

Cada mecánica tiene su lógica pura separada del `MonoBehaviour` y cubierta en
EditMode: `PressurePlateTests`, `SpikeTrapCycleTests`, `ThrownStoneBounceTests`,
`ThrownStoneFilterTests`, `NoiseSourceTests`, `PlayerInventoryTests`,
`InteractionSensorTests`. Son parte de la suite que ya pasaba en la Semana 05.

---

## 2. Parallax

### 2.1 Qué es y por qué aquí es distinto

El parallax es el efecto por el cual, al moverse el observador, los objetos lejanos
se desplazan menos en pantalla que los cercanos. En un juego de plataformas se
consigue moviendo capas de fondo a una fracción de la velocidad de la cámara.

DungeonPuzzle tiene dos particularidades que cambian la receta:

1. **La cámara de las salas es fija.** Si nada se mueve, no hay parallax. La
   solución es tratar al **héroe** como "cámara virtual": las capas reaccionan a su
   desplazamiento. El componente admite las tres referencias (cámara, héroe o un
   transform cualquiera), así que en escenas con cámara que sigue al jugador, como
   la galería de biomas, se usa la cámara real.
2. **El suelo cubre toda la vista.** Un fondo detrás del suelo no se vería. Por
   eso las capas van *encima* del suelo: una de grietas que se mueve menos que el
   héroe (el suelo parece hundido, con profundidad) y una de niebla que se mueve
   en sentido contrario (está entre el observador y el suelo).

### 2.2 El componente `ParallaxLayer`

`Assets/Scripts/FX/ParallaxLayer.cs`. Un único componente por capa, con la
matemática en funciones estáticas para poder probarla sin escena.

| Función | Cálculo | Para qué |
|---|---|---|
| `Evaluate(origen, Δref, factor, deriva)` | `origen + Δref ⊙ factor + deriva` | Posición de la capa. `⊙` es producto por componentes: cada eje tiene su profundidad. |
| `Wrap(desplazamiento, periodo)` | lleva el valor a `[-periodo/2, periodo/2)` | Una capa en mosaico puede avanzar sin límite sin alejarse de su origen (repetición infinita). |
| `FactorFromDepth(d)` | `d / (1 + d)` | Convierte una profundidad aparente en factor, con el observador a distancia 1 del suelo. |

Significado del factor:

| Factor | Interpretación |
|---|---|
| `0` | Pegado al mundo: es el plano del suelo. |
| `0 < f < 1` | Fondo. Cuanto más cerca de 1, más lejano. |
| `1` | Pegado a la referencia: un cielo infinito. |
| `f < 0` | Primer plano: en pantalla se mueve más deprisa y al revés. |

Los parámetros expuestos en el Inspector son la referencia, el factor por eje, la
**deriva** propia (unidades/segundo; para niebla y polvo que se mueven solos) y el
**tamaño de mosaico** para el envolvimiento. Con el objeto seleccionado, el gizmo
naranja dibuja el vector desde el origen de la capa hasta su posición actual.

### 2.3 Las dos capas generadas

| Capa | Sorting layer | Factor | Deriva | Mosaico | Textura |
|---|---|---|---|---|---|
| `Parallax_Depth` | FloorFX (+5) | `0.12` | ninguna | 2 u | `parallax_cracks.png`, 128 px a 64 px/u, grietas con caminatas aleatorias que envuelven en los bordes. Opacidad 45 %. |
| `Parallax_Fog` | FX (−5) | `−0.25` | `(0.12, 0.04)` u/s | 8 u | `parallax_fog.png`, 256 px a 32 px/u, ruido de valor en tres octavas, periódico. Opacidad 9 %. |

Las dos texturas se generan por código en `Semana08Builder` para que el mosaico
no tenga costuras: las grietas avanzan con aritmética modular y la niebla usa una
retícula que envuelve. Se importan como sprite con `FullRect`, imprescindible para
el `DrawMode.Tiled`, y `Wrap = Repeat`.

Las dos capas usan el material **`Sprite-Unlit-Default`** de URP. Con el material
iluminado, las luces 2D de cada sala realzaban la niebla hasta volverla casi opaca;
sin iluminación, su opacidad es exactamente la del color de la capa. La niebla usa
32 píxeles por unidad para que el mosaico mida 8 unidades y la repetición no se note.

Cada sprite se dimensiona un mosaico más grande que la zona que debe cubrir en cada
lado. Así, cuando `Wrap` devuelve la capa a su origen, el borde nunca entra en
pantalla y el movimiento parece continuo.

### 2.4 Pruebas

`Assets/Scripts/Tests/EditMode/ParallaxLayerTests.cs` (13 casos):

- Factor 0 no mueve la capa; factor 1 copia el desplazamiento completo.
- Factor intermedio se mueve menos (fondo); factor negativo se mueve al revés (primer plano).
- Cada eje usa su propio factor; la deriva se suma.
- `Wrap`: sin periodo no cambia nada; dentro del intervalo no cambia; fuera vuelve al
  mosaico equivalente; siempre queda en `[-p/2, p/2)` en un barrido de 270 valores.
- `FactorFromDepth`: 0 en el suelo, tiende a 1 en el infinito, negativo en primer plano.
- Tras 1000 unidades de recorrido una capa envuelta sigue a menos de medio mosaico de su origen.

Resultado en Unity `6000.5.0b10`: suite EditMode completa **139/139** en verde, incluidos
los 13 casos de `ParallaxLayerTests`. El informe de `Construir todo` da **16/16 OK**.

---

## 3. Vectores aplicados

Continúa la línea de la Semana 07: el parallax es una **transformación de
traslación** dependiente de otra traslación.

- `Δref = posición_actual − posición_inicial` de la referencia es un vector.
- `Δref ⊙ factor` lo escala por eje: es una matriz diagonal `diag(fx, fy)` aplicada
  al vector.
- `Wrap` es la reducción módulo un periodo, la misma idea que llevar un ángulo a
  `[-180°, 180°)`.
- `FactorFromDepth` sale de triángulos semejantes: si el observador está a altura 1
  sobre el suelo y un punto a profundidad `d` por debajo, su desplazamiento aparente
  es `d / (1 + d)` del desplazamiento del observador.

---

## 4. Cómo reproducir el avance

1. Abrir el proyecto con Unity `6000.5.0b10`.
2. Menú **`DungeonPuzzle ▸ Semana 08 ▸ Construir todo`**. Hace tres cosas:
   genera las dos texturas en `Assets/Sprites/Game/Parallax/`, coloca el objeto
   `Parallax` con sus dos capas en `Room_01..05` y `Room_Demo`, e imprime el
   informe de validación. No debe haber ninguna línea `FALLA`.
3. `Window ▸ General ▸ Test Runner ▸ EditMode ▸ Run All`: los 13 casos nuevos de
   `ParallaxLayerTests` en verde junto al resto de la suite.
4. Play en `Room_02`. Mover al héroe de un extremo al otro: las grietas se quedan
   atrás (profundidad) y la niebla cruza por delante en sentido contrario, además
   de derivar sola aunque el héroe esté quieto.
5. Con `Parallax_Fog` seleccionado en la jerarquía, el gizmo naranja de la vista
   Scene muestra el vector de desplazamiento acumulado de la capa.

Los pasos 1 y 2 del menú también existen por separado para regenerar solo las
texturas o solo las salas.

### 4.1 Grabar el gameplay sin intervención

Menú **`DungeonPuzzle ▸ Semana 08 ▸ Demo automática (para grabar)`**:

1. ejecuta `Construir todo`, abre `Room_Demo` y entra en Play;
2. `DemoAutopilot` recorre la sala solo: recoge la piedra (E), la lanza al rincón
   lejano (F) para que el guardia de patrulla investigue, camina de un lado a otro
   para mostrar el parallax, pasa por la espalda del guardia fijo, acciona la
   palanca y sale por la puerta hasta la pantalla de victoria;
3. Unity graba la vista Game fotograma a fotograma con `Time.captureFramerate = 24`,
   así el video sale fluido aunque el editor vaya lento, y nunca captura nada fuera
   del juego. Los fotogramas quedan en `Vídeos/DungeonPuzzle/frames_FECHA/`.

Luego **`DungeonPuzzle ▸ Semana 08 ▸ Codificar última grabación a MP4`** genera el
video H.264 1920×1080 con el codificador integrado del editor (`MediaEncoder`), sin
instalar nada. El piloto no modifica el gameplay: fija la velocidad del Rigidbody2D
después de `PlayerMovement` y usa las mismas acciones que las teclas E y F.

### 4.2 Corrección en `Room_Demo`

Al grabar apareció un fallo heredado del builder de la Semana 04: los muros de
`Room_Demo` se copiaban de una plantilla que ya es un sprite en mosaico con su
collider en unidades de mundo, y además se les aplicaba escala. El tamaño se
multiplicaba (16×3 por 16×0,5 = 256×1,5 unidades): los muros tapaban la sala y
bloqueaban al héroe. `Semana04Builder.Wall` ahora aplica el tamaño al sprite y al
collider con escala 1, y la escena está corregida.

---

## 5. Guion de demostración (3–4 min)

| Tiempo | Acción | Evidencia |
|---|---|---|
| 0:00–0:40 | Recorrer la tabla de §1 con el panel Project abierto. | Cada mecánica secundaria es un script en `World/` con su prueba en `Tests/EditMode/`. |
| 0:40–1:30 | En `Room_02`, recoger la piedra, lanzarla, abrir la puerta con la palanca. | Encadenamiento de mecánicas secundarias al servicio de la principal. |
| 1:30–2:30 | Mover al héroe de izquierda a derecha y detenerse. | Grietas que se quedan atrás, niebla que cruza al revés y sigue derivando. |
| 2:30–3:15 | Seleccionar `Parallax_Fog` y cambiar el factor en el Inspector a `0`, `0.5` y `1` en Play. | La misma capa pasa de primer plano a fondo a cielo pegado al observador. |
| 3:15–4:00 | Abrir `ParallaxLayer.cs` y correr `ParallaxLayerTests`. | Fórmula `origen + Δref ⊙ factor + deriva` y 13 casos en verde. |

---

## 6. Archivos entregables de la Semana 8

| Archivo | Contenido |
|---|---|
| `Assets/Scripts/FX/ParallaxLayer.cs` | Componente de capa de parallax con matemática estática. |
| `Assets/Scripts/Tests/EditMode/ParallaxLayerTests.cs` | 13 pruebas EditMode de `Evaluate`, `Wrap` y `FactorFromDepth`. |
| `Assets/Editor/Semana08Builder.cs` | Generación de texturas, colocación en salas y validación, desde el menú del editor. |
| `Assets/Scripts/Demo/DemoAutopilot.cs` | Recorrido automático de `Room_Demo` y grabación de la vista Game. |
| `Assets/Editor/Semana08Demo.cs` | Menú de la demo automática: construye, abre la sala, entra en Play y sale al terminar. |
| `Assets/Editor/Semana08VideoEncoder.cs` | Convierte los fotogramas en MP4 H.264 con el codificador del editor. |
| `docs/Semana08/README.md` | Este documento. |
| `Assets/Sprites/Game/Parallax/*.png` | Texturas generadas por el builder, versionadas para que las salas abran con el parallax listo. |
