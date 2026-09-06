using UnityEngine;

/// <summary>
/// Salida de la sala. Se blinda contra el disparo doble: el jugador lleva DOS
/// colliders (el sólido del cuerpo y el trigger del <see cref="InteractionSensor"/>),
/// así que el motor emite dos <c>OnTriggerEnter2D</c> en el mismo frame. Sin el
/// pestillo, la segunda llamada cargaba la sala siguiente por segunda vez y el
/// jugador se saltaba un nivel.
/// </summary>
public class ExitTrigger : MonoBehaviour
{
    [SerializeField] bool isFinalExit;

    bool _used;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_used) return;
        if (!other.CompareTag("Player")) return;
        _used = true;

        SfxLibrary.Play("SFX/exit");
        Vfx.Spark(transform.position);
        if (isFinalExit) GameManager.Instance.WinGame();
        else GameManager.Instance.LoadNextRoom();
    }
}
