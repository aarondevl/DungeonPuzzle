using UnityEngine;

public class ExitTrigger : MonoBehaviour
{
    [SerializeField] bool isFinalExit;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (isFinalExit) GameManager.Instance.WinGame();
        else GameManager.Instance.LoadNextRoom();
    }
}
