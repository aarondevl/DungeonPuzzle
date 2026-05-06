using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public abstract class GuardBase : MonoBehaviour
{
    protected enum GuardState { Normal, Alerted }
    protected GuardState State = GuardState.Normal;

    [SerializeField] protected float alertDuration = 3f;
    [SerializeField] protected float spotSustainSeconds = 0.4f;

    protected VisionCone VisionCone;
    protected Rigidbody2D Rb;

    float _spotTimer;
    bool _spotConfirmed;

    protected virtual void Awake()
    {
        Rb = GetComponent<Rigidbody2D>();
        Rb.bodyType = RigidbodyType2D.Kinematic;
        Rb.gravityScale = 0f;
        Rb.freezeRotation = false;
        VisionCone = GetComponentInChildren<VisionCone>();
    }

    void Update()
    {
        if (_spotConfirmed) return;
        if (VisionCone.IsSeeingPlayer)
        {
            _spotTimer += Time.deltaTime;
            if (State == GuardState.Normal)
            {
                State = GuardState.Alerted;
                VisionCone.SetAlerted(true);
            }
            if (_spotTimer >= spotSustainSeconds)
            {
                _spotConfirmed = true;
                OnAlerted();
            }
        }
        else
        {
            _spotTimer = Mathf.Max(0f, _spotTimer - Time.deltaTime);
            if (_spotTimer == 0f && State == GuardState.Alerted && !_spotConfirmed)
            {
                StartCoroutine(ReturnToNormalAfter(alertDuration));
            }
        }
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
        if (_spotConfirmed) yield break;
        State = GuardState.Normal;
        VisionCone.SetAlerted(false);
        OnReturnToNormal();
    }

    protected virtual void OnAlerted() => GameManager.Instance.PlayerDetected();
    protected virtual void OnNoiseAlerted(Vector2 position) { }
    protected virtual void OnReturnToNormal() { }
}
