# Spec: Castillo arcano interconectado — vertical slice

**Fecha:** 2026-09-10  
**Estado:** Aprobado por el usuario  
**Horizonte:** 3–4 semanas, desarrollado principalmente por una persona  
**Duración objetivo:** 20–30 minutos en una primera partida

## 1. Visión

Convertir las cinco salas actuales de DungeonPuzzle en un recorrido coherente por un
castillo arcano. Para esta entrega, el recorrido será principalmente lineal, con un único
desvío opcional. La base técnica permitirá añadir más adelante regresos, atajos y rutas
alternativas sin reconstruir el sistema de escenas.

El juego seguirá siendo 2D top-down. El atractivo vendrá de una identidad visual más fuerte,
continuidad espacial, mejor ritmo de puzzles y respuesta audiovisual clara; no de una
conversión a 3D.

## 2. Objetivos

- Hacer que las salas se perciban como partes del mismo castillo.
- Mantener una ruta principal clara y terminable en 20–30 minutos.
- Dar a cada zona una función jugable y una identidad audiovisual reconocible.
- Reutilizar los cinco niveles, mecánicas, enemigos y sprites existentes.
- Reiniciar únicamente la habitación actual cuando el jugador falla.
- Dejar preparados viajes bidireccionales y múltiples puertas por sala para una fase futura.

## 3. Fuera de alcance

- Conversión a 3D.
- Mundo abierto o carga aditiva de un castillo gigante.
- Generación procedural.
- Nuevos enemigos con IA compleja.
- Inventario persistente entre habitaciones.
- Narrativa ramificada.
- Mapa interactivo completo; esta entrega solo necesita nombre e indicador simple de zona.
- Varias rutas jugables completas. La exploración amplia corresponde a una fase posterior.

## 4. Estructura del recorrido

Se conservan las cinco escenas existentes:

| Escena | Identidad dentro del castillo | Función jugable |
|---|---|---|
| `Room_01` | Patio de entrada + Gran Salón | Tutorial seguro, presentación del castillo y objetivo visible: la Torre del Alcaide |
| `Room_02` | Ala de guardias | Dominar patrullas, conos de visión, ruido y piedras como distracción |
| `Room_03` | Biblioteca arcana | Resolver palancas mientras se aplica el sigilo ya aprendido |
| `Room_04` | Calabozos + pasadizo secreto | Combinar trampas, llave, ruido y observación; contiene el único desvío opcional |
| `Room_05` | Torre del Alcaide | Puzzle final compacto, escape y cierre del vertical slice |

El Patio y el Gran Salón son dos espacios dentro de `Room_01`, no dos escenas. El pasadizo
secreto es una zona lateral dentro de `Room_04`, no un nivel adicional.

### Bucle por habitación

1. Observar el espacio, patrullas e interactivos.
2. Entender un objetivo visible.
3. Planear y ejecutar una solución.
4. Recibir respuesta visual y sonora inmediata.
5. Abrir la puerta siguiente o descubrir el desvío opcional.

Cada sala introduce como máximo una idea nueva. Las salas posteriores combinan conceptos ya
aprendidos en vez de seguir agregando sistemas.

## 5. Arquitectura técnica

Cada habitación seguirá siendo una escena Unity independiente cargada en modo `Single`. No se
mantendrán simultáneamente varias escenas de juego.

### 5.1 `ExitTrigger`

El componente existente se ampliará con destino explícito opcional:

- `destinationScene`: escena a cargar.
- `destinationEntryId`: punto de entrada dentro de la escena destino.
- `isFinalExit`: mantiene el comportamiento de victoria actual.

Si no hay un destino explícito, conservará el comportamiento actual de `LoadNextRoom()`. Esto
permite migrar las escenas gradualmente y mantiene compatibles las referencias serializadas.
El pestillo `_used` seguirá evitando cargas dobles causadas por los dos colliders del jugador.

### 5.2 `SpawnPoint`

El componente existente ganará un identificador y la posibilidad de marcarse como punto
predeterminado. Al cargar una escena, `GameManager` buscará el identificador solicitado. Si no
existe, usará el punto predeterminado; si tampoco existe, conservará la posición del jugador y
registrará un error claro.

Los IDs serán nombres estables y descriptivos, como `South`, `North` o `Secret`, no índices de
la jerarquía.

### 5.3 `GameManager`

`GameManager` seguirá siendo persistente y conservará vidas, tiempo y sala actual. Ganará una
operación de viaje que:

1. Valida el destino antes de iniciar la transición.
2. Guarda el identificador de entrada pendiente.
3. Bloquea temporalmente nuevas solicitudes de viaje.
4. Ejecuta un fundido de salida.
5. Carga la escena destino.
6. Coloca al jugador en el punto de entrada solicitado.
7. Muestra el nombre de la zona y ejecuta el fundido de entrada.

`StartLevel()` y el selector de niveles seguirán disponibles para pruebas y repetición. El
flujo normal desde “Nueva partida” recorrerá el castillo sin regresar al menú entre salas.

### 5.4 Identidad de habitación

Cada escena tendrá un componente pequeño `RoomIdentity` con:

- identificador estable;
- nombre mostrado al entrar;
- color arcano de acento;
- clave del ambiente sonoro.

El componente solo describe la escena. La transición, HUD y audio consumen esos datos sin
conocer la lógica interna del puzzle.

### 5.5 Progreso

`GameProgress` seguirá usando `PlayerPrefs` y mantendrá las claves actuales para niveles,
tiempos, muertes, volumen y pantalla completa. Se añadirán únicamente los datos necesarios
para recordar habitaciones completadas y el pasadizo descubierto.

No habrá objetos de inventario que deban cruzar entre habitaciones en esta entrega. Llaves y
piedras siguen perteneciendo a su escena. Esto evita acoplar la migración del castillo a una
reescritura del inventario.

## 6. Flujo de datos

```text
Player entra en ExitTrigger
    -> ExitTrigger valida uso y pide viajar
    -> GameManager guarda destinationEntryId
    -> TransitionOverlay funde a negro
    -> SceneManager carga destinationScene
    -> GameManager busca SpawnPoint coincidente
    -> RoomIdentity configura título, color y ambiente
    -> TransitionOverlay revela la nueva habitación
```

Al completar una habitación, `GameProgress` actualiza desbloqueo y mejor tiempo antes de cargar
la siguiente escena. Al ser detectado, se descuenta una vida y se recarga solo la escena
actual. El progreso anterior del castillo no se borra.

## 7. Dirección audiovisual: castillo arcano

La base visual será piedra oscura con iluminación azul/cian, sigilos luminosos y efectos
mágicos contenidos. Cada ala tendrá un acento secundario para facilitar orientación:

- Patio y Gran Salón: cian y oro tenue.
- Ala de guardias: cian con rojo de peligro.
- Biblioteca: turquesa y violeta.
- Calabozos: verde espectral.
- Torre: cian, carmesí y oro como culminación.

La legibilidad manda sobre la atmósfera. Suelos transitables, peligros, puertas e interactivos
deben distinguirse aun cuando las luces locales estén apagadas. No se oscurecerá información
necesaria para resolver un puzzle.

El pase visual reutilizará los sprites actuales y se concentrará en:

- iluminación 2D global y local;
- sigilos, partículas y pulsos de emisión;
- props repetibles para romper superficies vacías;
- una paleta consistente por zona;
- transiciones y títulos de habitación;
- contraste claro para visión, trampas e interactivos.

El audio tendrá un ambiente base y capas discretas por zona: viento/fuego, biblioteca,
cadenas/calabozos y energía arcana. La alerta debe tener tres estados perceptibles —calma,
sospecha y detección— sin volver a producir picos de volumen. Puertas, piedras, pasos y
mecanismos deben comunicar peso y estado.

## 8. Fallos y recuperación

- Una puerta sin destino válido no cambia de escena, libera el bloqueo de viaje y registra un
  error con el nombre de la puerta y el destino.
- Un `destinationEntryId` inexistente usa el `SpawnPoint` predeterminado y registra una
  advertencia.
- Una escena sin `RoomIdentity` usa el nombre técnico de la escena y la configuración visual
  y sonora predeterminada.
- Solicitudes de viaje durante un fundido se ignoran.
- El guardado anterior seguirá siendo válido; las claves nuevas tendrán valores iniciales
  seguros.
- Al agotarse las vidas se conserva el flujo actual de Game Over; continuar reinicia la
  habitación con vidas restauradas, no el castillo completo.

## 9. Plan de entrega por semanas

### Semana 1 — Conectar

- Destinos explícitos en `ExitTrigger`.
- Entradas identificadas en `SpawnPoint`.
- Viaje y recuperación segura en `GameManager`.
- `RoomIdentity`, fundido y título de zona.
- Compatibilidad con flujo y guardado actuales.

### Semana 2 — Diseñar

- Patio + Gran Salón dentro de `Room_01`.
- Ritmo y objetivos de las cinco escenas.
- Pasadizo opcional dentro de `Room_04`.
- Revisión de distancias, patrullas, trampas y tiempos de resolución.

### Semana 3 — Vestir

- Paleta arcana por zona.
- Luces 2D, sigilos, partículas y props reutilizables.
- Ambientes y respuesta sonora por zona y estado de alerta.
- Legibilidad de puertas, peligros e interactivos.

### Semana 4 — Pulir

- Recorrido completo y corrección de bloqueos.
- Equilibrio de guardias y puzzles.
- Rendimiento y build de entrega.
- Capturas y material breve de presentación.

Si solo hay tres semanas, el orden de recorte será: reducir props decorativos, usar un solo
ambiente con variaciones de volumen y simplificar el pasadizo secreto. No se recortan la
continuidad entre salas, la recuperación segura ni la legibilidad.

## 10. Verificación

### Pruebas automáticas

- Destino explícito y fallback lineal de `ExitTrigger`.
- Bloqueo contra activación doble.
- Selección del `SpawnPoint` por ID y fallback predeterminado.
- Persistencia y compatibilidad de `GameProgress`.
- Viaje inválido sin cambio de escena ni bloqueo permanente.
- Regresiones existentes de movimiento, guardias, inventario, trampas y Y-sort.

### Pruebas manuales

- Partida nueva completa desde Patio hasta Torre sin regresar al menú.
- Entrada correcta a cada escena y presentación de nombre/color/ambiente.
- Detección y pérdida de las tres vidas en cada habitación.
- Continuación desde Game Over en la habitación correcta.
- Resolución normal y alternativa de `Room_04` sin softlocks.
- Partida de 20–30 minutos para un jugador que no conoce los puzzles.
- Lectura clara de caminos, peligros e interactivos en todas las zonas.
- Build ejecutable sin errores de consola relevantes ni referencias faltantes.

## 11. Definición de terminado

El vertical slice está terminado cuando:

- las cinco escenas forman un recorrido continuo y coherente;
- cada zona se reconoce por nombre, color, decoración y sonido;
- fallar reinicia solo la habitación actual;
- el recorrido completo no presenta puertas dobles, destinos inválidos ni softlocks;
- los tests automáticos están en verde;
- se completa un recorrido manual de principio a fin en el build de entrega;
- la duración observada de una primera partida queda dentro de 20–30 minutos.

## 12. Preparación para la fase B

La fase futura podrá añadir múltiples `ExitTrigger` por escena, entradas Norte/Sur/Secreto,
retorno al Gran Salón y atajos persistentes usando las mismas interfaces. No se implementarán
esas rutas ahora; únicamente se evita diseñar la fase A de una forma que las vuelva
imposibles.

## 13. Decisiones aprobadas

1. Ruta principalmente lineal ahora; exploración más amplia después.
2. Una escena Unity por habitación, sin castillo gigante ni carga aditiva.
3. Cinco escenas reutilizadas; Patio + Gran Salón comparten `Room_01`.
4. Un único desvío opcional dentro de `Room_04`.
5. Reinicio por habitación al fallar.
6. Dirección audiovisual de castillo arcano.
7. Alcance de 3–4 semanas y duración objetivo de 20–30 minutos.
