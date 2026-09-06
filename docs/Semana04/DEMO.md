# Guion de demostración — Semana 04

Duración objetivo: **6–8 minutos**. Todo lo que se lista aquí es visible en
ejecución; no hace falta mostrar código durante la demo salvo en el punto 6.

## Preparación (antes de empezar a grabar/presentar)

1. Abrir el proyecto con Unity `6000.5.0b10`.
2. Menú **`DungeonPuzzle ▸ Semana 04 ▸ Construir todo`** y comprobar en la consola
   que el informe de validación no imprime ninguna línea `FALLA`.
3. Abrir `Assets/Scenes/Room_02.unity`.
4. En la vista de Scene, activar **Gizmos** y, en `Edit ▸ Project Settings ▸
   Physics 2D`, marcar temporalmente **Always Show Colliders**: durante la demo
   se ven los triggers y los cuerpos, que es justo lo que se está evaluando.

---

## 1 · Organización del proyecto (1 min)

Con el proyecto abierto, recorrer el panel Project:

- `Scripts/` dividido en **Core / Player / Guard / World / UI / FX / Tests**.
- Señalar los tres `.asmdef` y decir la regla: *el Editor puede usar el Runtime,
  nunca al revés*.
- Abrir `Window ▸ General ▸ Test Runner ▸ EditMode` y lanzar **Run All**:
  42 casos en verde en menos de un segundo, sin entrar en Play Mode.

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
- Lanzarla en ángulo contra una superficie que no genera ruido para ver el
  **rebote con pérdida de energía**.

## 5 · Obstáculo animado y placa de presión (2 min)

- Mostrar la fila de **trampas de pinchos** en el trayecto hacia la salida:
  los tres pinchos suben y bajan **desfasados**, nunca los tres a la vez.
- Cruzar leyendo el ritmo → se pasa.
- Quedarse quieto sobre una trampa desarmada y esperar a que suba: **también
  mata** (es el caso del `Overlap` al armarse, no solo del `OnTriggerEnter2D`).
- Ir a la **placa de presión** junto a la puerta: pisarla → la runa se enciende y
  la puerta se abre; bajarse → se cierra.
- **El truco del puzle:** lanzar la piedra sobre la placa. La piedra pesa, la
  placa sigue accionada y la puerta queda abierta sin el héroe encima.
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

## Checklist de captura (si se entrega en vídeo)

- [ ] Deslizamiento contra muro y animación que se detiene
- [ ] Recogida de llave y accionamiento de palanca con `E`
- [ ] Lanzamiento de piedra sin empujar al héroe
- [ ] Impacto en muro + guardia reaccionando al ruido
- [ ] Rebote de la piedra
- [ ] Ciclo completo de los pinchos, desfasados
- [ ] Muerte por pinchos estando quieto encima
- [ ] Placa accionada por el héroe y por la piedra
- [ ] Detección por contacto a la espalda del guardia
- [ ] Test Runner en verde
- [ ] Matriz de colisiones en Project Settings
