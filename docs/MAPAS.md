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
| `G` | guardia fijo (barre hacia el espacio abierto) |
| `m` `n` `q` `r` | rutas de patrulla: las celdas de una misma letra son sus puntos |
| `T` | trampa de pinchos (desfasadas entre sí) |
| `S` | piedra lanzable |
| `*` | antorcha (luz puntual) |

## Recorrido previsto

Cada herramienta que aparece es necesaria para salir; no hay objetos decorativos.

1. **La celda**: solo sigilo. La reja de la celda está rota; una patrulla rodea el bloque de arriba y un guardia fijo vigila la salida. El corredor entre los dos bloques es la ruta segura.
2. **La llave**: la salida está tras una reja abajo a la derecha. La llave está arriba a la derecha, dentro de la ronda de la patrulla, y un guardia fijo cubre la mitad baja.
3. **Los pasillos**: la reja doble de la salida la abre la palanca de la esquina superior derecha, en plena ronda de la patrulla. La piedra sirve para atraer a la patrulla al lado contrario antes de entrar. El guardia del puesto central vigila el corredor y hay pinchos en el tramo bajo.
4. **La armería**: la celda da al corredor de pinchos que patrulla un guardia. La placa del fondo (abajo a la derecha) abre la reja doble de la sala de la salida, arriba a la derecha: hay que cruzar el patio dos veces.
5. **El patio**: la placa de abajo a la izquierda, vigilada por un guardia fijo, abre el cuarto de la llave; la llave abre el portón doble de la salida. Dos patrullas se cruzan en el patio y las dos piedras sirven para apartarlas.

Los guardias ya no atrapan con solo verte: al verte medio segundo salen a perseguirte y solo te atrapan si te alcanzan. El héroe corre algo más rápido, así que cortar la línea de visión tras una esquina o una reja permite escapar; después buscan un momento y vuelven a su puesto o ruta.

Cada sala tiene su propio tinte de muros, suelo y antorchas (piedra fría, arenisca, musgo, ladrillo rojizo, noche azul) para reconocerla de un vistazo.

Para cambiar un nivel: edita el mapa en el archivo, valida con **Niveles → Validar mapas**
(comprueba anchos, que haya un `P` y un `E`, que cada puerta tenga forma de abrirse y que
la salida sea alcanzable) y vuelve a construir.
