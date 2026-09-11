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
    Coroutine _returnRoutine;

    /// <summary>Estado legible desde fuera (HUD de demostración, herramientas).</summary>
    public bool IsAlerted => State == GuardState.Alerted;
    public bool IsSeeingPlayer => VisionCone != null && VisionCone.IsSeeingPlayer;

    protected virtual void Awake()
    {
        Rb = GetComponent<Rigidbody2D>();
        Rb.bodyType = RigidbodyType2D.Kinematic;
        Rb.gravityScale = 0f;
        Rb.freezeRotation = false;
        Rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        // Un cuerpo cinemático solo reporta contactos si se le pide explícitamente:
        // sin esto, chocar de frente con un guardia no disparaba ningún callback.
        Rb.useFullKinematicContacts = true;
        VisionCone = GetComponentInChildren<VisionCone>();
    }

    void Update()
    {
        if (_spotConfirmed) return;
        if (VisionCone.IsSeeingPlayer)
        {
            CancelReturn();
            OnVisionAlerted(VisionCone.LastSeenPlayerPosition);
            _spotTimer += Time.deltaTime;
            if (State == GuardState.Normal)
            {
                State = GuardState.Alerted;
                SfxLibrary.Play("SFX/alert");
                VisionCone.SetAlerted(true);
                Vfx.Alert(transform.position);
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
            if (ShouldScheduleReturn(_spotTimer, State == GuardState.Alerted,
                    _spotConfirmed, _returnRoutine != null))
                ScheduleReturn();
        }
    }

    /// <summary>
    /// Contacto físico: si el héroe choca con el guardia queda descubierto al
    /// instante, sin esperar al cono de visión. Cubre el punto ciego de pegarse a
    /// su espalda, donde el cono nunca llega.
    /// </summary>
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (_spotConfirmed) return;
        if (!CollisionLayers.Contains(CollisionLayers.PlayerMask, collision.gameObject.layer)) return;
        _spotConfirmed = true;
        State = GuardState.Alerted;
        VisionCone.SetAlerted(true);
        Vfx.Alert(transform.position);
        OnAlerted();
    }

    public void AlertAt(Vector2 noisePosition)
    {
        if (State == GuardState.Alerted) return;
        State = GuardState.Alerted;
        CancelReturn();
        SfxLibrary.Play("SFX/alert");
        VisionCone.SetAlerted(true);
        OnNoiseAlerted(noisePosition);
        ScheduleReturn();
    }

    static bool ShouldScheduleReturn(float spotTimer, bool isAlerted,
        bool spotConfirmed, bool returnPending) =>
        spotTimer <= 0f && isAlerted && !spotConfirmed && !returnPending;

    void ScheduleReturn()
    {
        if (_returnRoutine == null)
            _returnRoutine = StartCoroutine(ReturnToNormalAfter(alertDuration));
    }

    void CancelReturn()
    {
        if (_returnRoutine == null) return;
        StopCoroutine(_returnRoutine);
        _returnRoutine = null;
    }

    IEnumerator ReturnToNormalAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        _returnRoutine = null;
        if (_spotConfirmed) yield break;
        State = GuardState.Normal;
        VisionCone.SetAlerted(false);
        OnReturnToNormal();
    }

    protected virtual void OnAlerted() => GameManager.Instance.PlayerDetected();
    protected virtual void OnVisionAlerted(Vector2 position) { }
    protected virtual void OnNoiseAlerted(Vector2 position) { }
    protected virtual void OnReturnToNormal() { }
}
