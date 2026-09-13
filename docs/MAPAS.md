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
| `G` | guardia fijo (barre hacia el espacio abierto) |
| `m` `n` `q` `r` | rutas de patrulla: las celdas de una misma letra son sus puntos |
| `T` | trampa de pinchos (desfasadas entre sí) |
| `S` | piedra lanzable |
| `*` | antorcha (luz puntual) |

## Recorrido previsto

1. **La celda**: solo sigilo. La reja de la celda está rota; una patrulla rodea el bloque de arriba y un guardia fijo vigila la salida. El corredor entre los dos bloques es la ruta segura.
2. **La llave**: primera puerta. La llave está en la zona de partida y vuela sola hasta la reja del pasillo; una patrulla rodea el bloque central y un guardia cubre la salida.
3. **Los pasillos**: piedra y palanca. La palanca abre la reja doble de la celda, el guardia del puesto central mira por su ventana y la patrulla de la derecha se puede atraer con la piedra. Pinchos en el tramo bajo.
4. **La armería**: placa y dos puertas. La placa de la celda abre el pasillo de pinchos que patrulla un guardia; la llave está en la esquina opuesta y abre la reja doble de la sala de la salida. Hay que ir y volver.
5. **El patio**: final. Placa en la celda, dos piedras, dos patrullas que se cruzan, dos guardias fijos y la llave del portón doble junto a los pinchos.

Cada sala tiene su propio tinte de muros, suelo y antorchas (piedra fría, arenisca, musgo, ladrillo rojizo, noche azul) para reconocerla de un vistazo.

Para cambiar un nivel: edita el mapa en el archivo, valida con **Niveles → Validar mapas**
(comprueba anchos, que haya un `P` y un `E`, que cada puerta tenga forma de abrirse y que
la salida sea alcanzable) y vuelve a construir.
