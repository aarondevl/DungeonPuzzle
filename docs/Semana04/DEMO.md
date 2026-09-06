# Guion de demostración — Semana 04

Duración objetivo: **6–8 minutos**. Todo lo que se lista aquí es visible en
ejecución; no hace falta mostrar código durante la demo salvo en el punto 6.

## Dos caminos

**Camino corto (recomendado para sustentar): `Room_Demo`.** Es un banco de pruebas
con todas las mecánicas de la Semana 04 en una sola pantalla, así que la
demostración dura ~90 segundos en vez de recorrer cinco salas. Lo construye el
menú. El recorrido va de izquierda a derecha y cada tramo demuestra una cosa:

```
  spawn ─▶ bloque para deslizar ─▶ dos piedras ─▶ pasillo de 3 trampas
        ─▶ placa + palanca ─▶ espalda del guardia ─▶ puerta ─▶ salida
```

**Camino largo: `Room_02`.** Las salas numeradas, tal como se juegan. Úsalo si el
profesor pide ver el juego real y no el banco de pruebas.

## Preparación (antes de empezar a grabar/presentar)

1. Abrir el proyecto con Unity `6000.5.0b10`.
2. Menú **`DungeonPuzzle ▸ Semana 04 ▸ Construir todo`** y comprobar en la consola
   que el informe de validación no imprime ninguna línea `FALLA`.
3. Abrir `Assets/Scenes/Room_Demo.unity` (o `Room_02.unity` para el camino largo).
4. En la vista de Scene, activar **Gizmos** y, en `Edit ▸ Project Settings ▸
   Physics 2D`, marcar temporalmente **Always Show Colliders**: durante la demo
   se ven los triggers y los cuerpos, que es justo lo que se está evaluando.
5. Ya en Play, **`F1`** abre el panel de estado. Es la pieza que hace la demo
   legible para quien mira: escribe en pantalla la velocidad real del héroe, el
   estado de cada guardia, la fase de cada trampa y cuántos colliders hay sobre
   la placa. Casi todo lo que se está evaluando es invisible sin él.

---

## 1 · Organización del proyecto (1 min)

Con el proyecto abierto, recorrer el panel Project:

- `Scripts/` dividido en **Core / Player / Guard / World / UI / FX / Tests**.
- Señalar los tres `.asmdef` y decir la regla: *el Editor puede usar el Runtime,
  nunca al revés*.
- Abrir `Window ▸ General ▸ Test Runner ▸ EditMode` y lanzar **Run All**:
  45 casos en verde en menos de un segundo, sin entrar en Play Mode.

## 2 · Respuesta de colisión del jugador (1 min)

En Play, dentro de `Room_02`:

- Caminar **en diagonal contra un muro**: el héroe *desliza* a lo largo de la
  pared en vez de frenar en seco o engancharse en la esquina.
- Seguir empujando contra el muro: la animación de caminar **se detiene** aunque
  la tecla siga pulsada, porque el `Animator` recibe la velocidad real del
  cuerpo, no la deseada.

> Qué se está demostrando: el cambio de `MovePosition` a `linearVelocity`
> (sección 2.4 del documento técnico).

## 3 · Objetos interactivos por trigger (1,5 min)

- Acercarse a la **llave**: el sensor de interacción la registra al entrar en el
  radio. Pulsar `E` → pop, chispa, sonido y la puerta enlazada se abre.
- Acercarse a la **palanca** y pulsar `E` → la puerta conmuta.
- Situarse entre dos objetos accionables y pulsar `E`: se acciona **el más
  cercano**, no uno al azar. Antes de la Semana 4 la elección era arbitraria.

## 4 · La piedra: proyectil y ruido (1,5 min)

- Recoger la piedra con `E` y lanzarla con `F` hacia el lado opuesto de la sala.
- **Observar que la piedra no empuja al héroe al salir**: nace justo encima de él
  y antes lo desplazaba. Ahora `Player ✘ Projectile` en la matriz y además se
  ignora explícitamente el collider del lanzador.
- La piedra **impacta el muro sin atravesarlo** (detección continua) → chispa,
  sonido y el guardia cercano gira hacia el ruido.
- Lanzar la segunda piedra contra la **puerta cerrada**: no la atraviesa, choca y
  hace ruido ahí (`Projectile ✔ Interactable`). Con la puerta abierta la cruza,
  porque `Door.Open()` apaga su collider.

## 5 · Obstáculo animado y placa de presión (2 min)

- Mostrar la fila de **trampas de pinchos** en el trayecto hacia la salida:
  los tres pinchos suben y bajan **desfasados**, nunca los tres a la vez.
- Cruzar leyendo el ritmo → se pasa.
- Quedarse quieto sobre una trampa desarmada y esperar a que suba: **también
  mata** (es el caso del `Overlap` al armarse, no solo del `OnTriggerEnter2D`).
- Ir a la **placa de presión** junto a la puerta: pisarla → la runa se enciende y
  la puerta se abre; bajarse → se cierra.
- **El caso de los dos dueños:** con la puerta ya abierta por la palanca, pisar
  la placa y bajarse la **cierra**. No es un fallo: es exactamente por qué en las
  salas 02-05 el constructor pone la placa en modo `latching` cuando la puerta ya
  tiene otro mecanismo. Decirlo en voz alta, es un punto a favor.
- Pegarse a la **espalda de un guardia**: aunque el cono no llegue, el contacto
  físico lo descubre al instante.

## 6 · Arquitectura (1 min, sobre el editor)

- Abrir `Assets/Scripts/Core/CollisionLayers.cs`: una sola fuente de verdad para
  capas y máscaras.
- Abrir `Edit ▸ Project Settings ▸ Physics 2D` y mostrar la **matriz de
  colisiones** ya recortada, contrastándola con la tabla del documento técnico.
- Ejecutar `DungeonPuzzle ▸ Semana 04 ▸ 4. Validar física y capas` y leer el
  informe OK/FALLA en consola.
- Cerrar con la idea de crecimiento: un enemigo nuevo es una subclase de
  `GuardBase`; un objeto accionable nuevo solo implementa `IInteractable` y el
  jugador no se toca.

---

## Recorrido de `Room_Demo` en 90 segundos

| # | Dónde | Qué se demuestra |
|---|---|---|
| 1 | Al salir, el bloque de piedra | El héroe **desliza** al rozarlo y la animación de caminar se detiene contra él |
| 2 | Las dos piedras del inicio | Una al muro lejano: **ruido** → el guardia de arriba gira. Otra a la puerta cerrada: **no la atraviesa** |
| 3 | Pasillo central | Tres **trampas desfasadas**; con `F1` se lee la fase de cada una |
| 4 | Placa junto a la puerta | Pisarla abre, salirse **cierra**. La palanca de al lado abre a mano |
| 5 | Guardia estático | Acercarse por su **espalda**: el contacto físico te descubre aunque el cono mire al norte |
| 6 | Puerta y salida | Cruzar termina la demo en victoria |

> La palanca junto a la placa es también el **rescate**: si algo se tuerce en vivo,
> abre la puerta a mano y la demostración sigue.

## Checklist de captura (si se entrega en vídeo)

- [ ] Deslizamiento contra muro y animación que se detiene
- [ ] Recogida de llave y accionamiento de palanca con `E`
- [ ] Lanzamiento de piedra sin empujar al héroe
- [ ] Impacto en muro + guardia reaccionando al ruido
- [ ] Rebote de la piedra
- [ ] Ciclo completo de los pinchos, desfasados
- [ ] Muerte por pinchos estando quieto encima
- [ ] Placa: pisar abre, salirse cierra; la palanca abre a mano
- [ ] Panel `F1` visible en al menos una toma
- [ ] Detección por contacto a la espalda del guardia
- [ ] Test Runner en verde
- [ ] Matriz de colisiones en Project Settings
