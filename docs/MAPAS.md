# Mapas de las 5 salas

Las salas se generan desde mapas ASCII en `Assets/Editor/LevelDesigner.cs`
(menú **DungeonPuzzle → Niveles → Construir Room_01-05 desde los mapas**).
Una celda = una unidad del mundo; cada sala mide 26x14 y la cámara se ajusta sola.

![Vista previa](mapas_preview.png)

## Leyenda

| Símbolo | Significado |
|---|---|
| `#` / `.` | muro / suelo |
| `P` / `E` | aparición del héroe / salida (trampilla) |
| `A`-`D` | puertas (letras contiguas = una puerta más ancha) |
| `a`-`d` | llave de la puerta A-D |
| `1`-`4` | palanca de la puerta A-D |
| `5`-`8` | placa de presión de la puerta A-D (se queda abierta al pisarla) |
| `E`-letrero | al acercarte a algo usable aparece "E  RECOGER", "E  ACCIONAR PALANCA" o "PISA LA PLACA"; con piedra en mano, "F  LANZAR" |
| `G` | guardia fijo que barre hacia el espacio abierto |
| `^` `v` `<` `>` | centinela: guardia fijo que no barre y mira en esa dirección; solo se pasa aturdiéndolo o distrayéndolo |
| `Z` / `z` | prisionero encadenado (E lo libera y hace de señuelo) y los puntos de la ruta por la que huye |
| `m` `n` `q` `r` | rutas de patrulla: las celdas de una misma letra son sus puntos |
| `T` | trampa de pinchos (desfasadas entre sí) |
| `S` | piedra lanzable |
| `*` | antorcha (luz puntual) |

## Mecánicas y cómo se ven

| Mecánica | Qué hace | Cómo se ve |
|---|---|---|
| Piedra (F) | Al llevarla aparece una línea de puntería hasta donde caerá. Si golpea a un guardia lo **aturde 4 s**; si cae al suelo hace **ruido** y los guardias cercanos van a mirar. Queda en el suelo para recogerla otra vez. | Línea y círculo de puntería; estrellas girando y cono apagado en el guardia aturdido; anillo de ruido y "?" sobre los que lo oyen. |
| Prisionero señuelo (E) | Al liberarlo corre por su ruta. Los guardias que lo ven **lo persiguen a él** y, si lo alcanzan, se lo llevan. | "¡CORRE!" al liberarlo, "!" en los guardias que lo ven, "JA" cuando lo atrapan. |
| Centinela | Guardia fijo que no barre. Bloquea un paso hasta que lo aturdes o lo distraes. | Cono fijo; al aturdirlo se apaga. |
| Persecución | Verte medio segundo no es perder: el guardia corre tras de ti y solo te atrapa si te alcanza. Cortar la línea de visión permite escapar. | "!" y cono rojo al empezar; busca girando al perderte; vuelve a su puesto. |
| Llave, palanca, placa | Abren la puerta a la que están enlazadas (la llave vuela sola hasta la suya). | Letrero con la tecla al acercarse; palanca que se vuelca; puerta que se abre con chispa. |

## Recorrido previsto

Cada herramienta que aparece es necesaria para salir; no hay objetos decorativos.

1. **La celda**: solo sigilo. La reja de la celda está rota; una patrulla rodea el bloque de arriba y un guardia fijo vigila la salida.
2. **La piedra y la llave**: la llave está en un cuarto cuya única entrada vigila un centinela. Acércate por fuera de su cono, aturdirlo con la piedra y entra mientras ve estrellas. La llave abre la reja de la salida.
3. **El señuelo**: la palanca de la salida está entre un centinela y una patrulla. En la celda hay otro preso: libéralo y corre por el patio, los guardias van a por él y dejan libre el camino a la palanca.
4. **La armería**: corredor de pinchos con patrulla; la placa del fondo abre la sala de la salida. Un centinela da la espalda a la placa: se llega por arriba, fuera de su cono, o con la piedra.
5. **El patio**: la placa vigilada abre el cuarto de la llave (custodiado por un centinela); la llave abre el portón. Dos patrullas, dos piedras y un preso para apartarlas.

Al escapar de la última sala, el héroe cruza la pantalla, se reúne con su familia y aparece "FIN" con las estadísticas.

Para cambiar un nivel: edita el mapa en el archivo, valida con **Niveles → Validar mapas**
(comprueba anchos, que haya un `P` y un `E`, que cada puerta tenga forma de abrirse y que
la salida sea alcanzable) y vuelve a construir.
