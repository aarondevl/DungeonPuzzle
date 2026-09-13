using UnityEngine;

/// <summary>
/// Salida de la sala: la trampilla del recorrido lineal o una puerta con destino
/// explícito dentro del castillo. La sala termina cuando el CUERPO del héroe está
/// encima del hueco, no cuando lo roza.
///
/// Tres blindajes:
///   * El héroe lleva DOS colliders: el sólido del cuerpo y el trigger del
///     <see cref="InteractionSensor"/> (radio 1). Antes el sensor disparaba la salida a
///     una unidad y media del hueco: la sala "se acababa" sin llegar a la trampilla.
///     Ahora solo cuenta el collider sólido y, además, el centro del héroe debe estar
///     a menos de <see cref="enterRadius"/> del centro del hueco.
///   * Pestillo <c>_used</c> contra el disparo doble en el mismo frame.
///   * Una puerta con destino inválido no consume el pestillo: se puede volver a
///     intentar y el error queda registrado con el nombre de la puerta.
///
/// Sin <see cref="destinationScene"/> conserva el comportamiento lineal
/// (<see cref="GameManager.LoadNextRoom"/>), así las escenas antiguas siguen valiendo.
/// </summary>
public class ExitTrigger : MonoBehaviour
{
    [SerializeField] bool isFinalExit;
    [SerializeField] string destinationScene;
    [SerializeField] string destinationEntryId = "Default";
    [Tooltip("Distancia máxima entre el centro del héroe y el centro del hueco para salir.")]
    [SerializeField] float enterRadius = 0.45f;

    bool _used;

    void OnTriggerEnter2D(Collider2D other) => TryExit(other);

    void OnTriggerStay2D(Collider2D other) => TryExit(other);

    void TryExit(Collider2D other)
    {
        if (_used) return;
        if (!IsPlayerBody(other)) return;
        if (!IsOverHatch(other.bounds.center, transform.position, enterRadius)) return;
        _used = true;

        if (!string.IsNullOrWhiteSpace(destinationScene) && !isFinalExit)
        {
            if (!GameManager.Instance.TravelTo(destinationScene, destinationEntryId, gameObject))
            {
                _used = false;
                return;
            }

            SfxLibrary.Play("SFX/exit");
            Vfx.Spark(transform.position);
            return;
        }

        SfxLibrary.Play("SFX/exit");
        Vfx.Spark(transform.position);
        if (isFinalExit) GameManager.Instance.WinGame(transform.position);
        else GameManager.Instance.LoadNextRoom(transform.position);
    }

    static bool IsPlayerBody(Collider2D other) =>
        other != null && !other.isTrigger && other.CompareTag("Player");

    /// <summary>true si el punto del héroe está dentro del radio del hueco (distancia entre puntos).</summary>
    public static bool IsOverHatch(Vector2 playerCenter, Vector2 hatchCenter, float radius) =>
        VectorMath.Distance(playerCenter, hatchCenter) <= radius;

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.4f, 1f, 0.6f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, enterRadius);
    }
}
