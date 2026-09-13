using System.Collections;
using UnityEngine;

/// <summary>Palanca de pared: abre o cierra su puerta. Al accionarla la palanca se vuelca visiblemente.</summary>
public class Lever : MonoBehaviour, IInteractable
{
    [SerializeField] Door linkedDoor;

    bool _thrown;
    Coroutine _anim;

    public void Interact(PlayerInventory inventory)
    {
        SfxLibrary.Play("SFX/lever");
        Vfx.Spark(transform.position);
        inventory.GetComponent<PlayerFeedback>()?.Punch(1.1f, 0.15f);
        _thrown = !_thrown;
        if (_anim != null) StopCoroutine(_anim);
        _anim = StartCoroutine(Flip(_thrown));
        if (linkedDoor != null) linkedDoor.Toggle();
    }

    /// <summary>Vuelca el sprite hacia el otro lado con un golpe de escala.</summary>
    IEnumerator Flip(bool thrown)
    {
        var sr = GetComponent<SpriteRenderer>();
        float from = transform.localEulerAngles.z;
        float to = thrown ? -35f : 0f;
        Vector3 baseScale = transform.localScale;
        const float seconds = 0.22f;
        float t = 0f;
        while (t < seconds)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / seconds);
            float e = 1f - (1f - u) * (1f - u);
            transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.LerpAngle(from, to, e));
            transform.localScale = baseScale * (1f + 0.25f * Mathf.Sin(u * Mathf.PI));
            if (sr != null) sr.flipX = thrown;
            yield return null;
        }
        transform.localRotation = Quaternion.Euler(0f, 0f, to);
        transform.localScale = baseScale;
    }
}
