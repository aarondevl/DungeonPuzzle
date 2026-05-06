using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public abstract class GuardBase : MonoBehaviour
{
    protected enum GuardState { Normal, Alerted }
    protected GuardState State = GuardState.Normal;

    [SerializeField] protected float alertDuration = 3f;

    protected VisionCone VisionCone;
    protected Rigidbody2D Rb;

    protected virtual void Awake()
    {
        Rb = GetComponent<Rigidbody2D>();
        Rb.bodyType = RigidbodyType2D.Kinematic;
        Rb.gravityScale = 0f;
        Rb.freezeRotation = false;
        VisionCone = GetComponentInChildren<VisionCone>();
        VisionCone.OnPlayerDetected += HandlePlayerDetected;
    }

    void HandlePlayerDetected()
    {
        if (State == GuardState.Alerted) return;
        State = GuardState.Alerted;
        VisionCone.SetAlerted(true);
        OnAlerted();
        StartCoroutine(ReturnToNormalAfter(alertDuration));
    }

    public void AlertAt(Vector2 noisePosition)
    {
        if (State == GuardState.Alerted) return;
        State = GuardState.Alerted;
        VisionCone.SetAlerted(true);
        OnNoiseAlerted(noisePosition);
        StartCoroutine(ReturnToNormalAfter(alertDuration));
    }

    IEnumerator ReturnToNormalAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        State = GuardState.Normal;
        VisionCone.SetAlerted(false);
        OnReturnToNormal();
    }

    protected virtual void OnAlerted() => GameManager.Instance.PlayerDetected();
    protected virtual void OnNoiseAlerted(Vector2 position) { }
    protected virtual void OnReturnToNormal() { }
}
