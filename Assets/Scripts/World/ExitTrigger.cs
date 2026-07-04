using UnityEngine;

public class ExitTrigger : MonoBehaviour
{
    [SerializeField] bool isFinalExit;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        SfxLibrary.Play("SFX/exit");
        Vfx.Spark(transform.position);
        if (isFinalExit) GameManager.Instance.WinGame();
        else GameManager.Instance.LoadNextRoom();
    }
}
