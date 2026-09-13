using System.Collections;
using UnityEngine;

/// <summary>
/// Máquina de estados común a todos los guardias.
///
///   Normal ──ve al héroe──▶ Alerted (cono rojo, sospecha) ──lo sigue viendo 0.4 s──▶ Chasing
///   Chasing: corre hacia el héroe (o hacia donde lo vio por última vez). Lo ATRAPA
///            solo si lo alcanza (contacto o distancia menor que captureRadius).
///   Chasing ──pierde de vista y llega al último punto──▶ Searching (mira alrededor)
///   Searching ──no lo encuentra──▶ Returning (vuelve a su puesto o ruta) ──▶ Normal
///   Normal ──oye una piedra──▶ Alerted (investiga el ruido) ──alertDuration──▶ Normal
///
/// Antes ver al héroe medio segundo era captura instantánea. Ahora hay persecución:
/// el héroe es un poco más rápido que el guardia, así que puede escapar cortando la
/// línea de visión tras una esquina o una puerta.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public abstract class GuardBase : MonoBehaviour
{
    protected enum GuardState { Normal, Alerted, Chasing, Searching, Returning }
    protected GuardState State = GuardState.Normal;

    [SerializeField] protected float alertDuration = 3f;
    [SerializeField] protected float spotSustainSeconds = 0.4f;

    [Header("Persecución")]
    [Tooltip("Velocidad al perseguir. El héroe corre a 4: algo menos para que pueda escapar.")]
    [SerializeField] protected float chaseSpeed = 3.4f;
    [SerializeField] protected float chaseTurnSpeed = 540f;
    [Tooltip("Distancia a la que el guardia atrapa al héroe.")]
    [SerializeField] protected float captureRadius = 0.6f;
    [Tooltip("Segundos sin verlo antes de darse por vencido al llegar al último punto visto.")]
    [SerializeField] protected float loseSightSeconds = 1.5f;
    [Tooltip("Segundos mirando alrededor antes de volver.")]
    [SerializeField] protected float searchSeconds = 1.6f;

    protected VisionCone VisionCone;
    protected Rigidbody2D Rb;

    float _spotTimer;
    bool _captured;
    Coroutine _returnRoutine;
    Vector2 _lastSeen;
    float _unseenTimer;
    float _searchTimer;
    float _searchDir = 1f;

    /// <summary>Estado legible desde fuera (HUD de demostración, herramientas).</summary>
    public bool IsAlerted => State != GuardState.Normal;
    public bool IsChasing => State == GuardState.Chasing;
    public bool IsSeeingPlayer => VisionCone != null && VisionCone.IsSeeingPlayer;
    public string StateName => State.ToString();

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
        if (_captured || VisionCone == null) return;
        bool sees = VisionCone.IsSeeingPlayer;
        if (sees) _lastSeen = VisionCone.LastSeenPlayerPosition;

        switch (State)
        {
            case GuardState.Normal:
            case GuardState.Alerted:
                if (sees)
                {
                    CancelReturn();
                    OnVisionAlerted(_lastSeen);
                    _spotTimer += Time.deltaTime;
                    if (State == GuardState.Normal)
                    {
                        State = GuardState.Alerted;
                        SfxLibrary.Play("SFX/alert");
                        VisionCone.SetAlerted(true);
                        Vfx.Alert(transform.position);
                    }
                    if (_spotTimer >= spotSustainSeconds) StartChase();
                }
                else
                {
                    _spotTimer = Mathf.Max(0f, _spotTimer - Time.deltaTime);
                    if (ShouldScheduleReturn(_spotTimer, State == GuardState.Alerted, _captured, _returnRoutine != null))
                        ScheduleReturn();
                }
                break;

            case GuardState.Chasing:
                _unseenTimer = sees ? 0f : _unseenTimer + Time.deltaTime;
                if (VectorMath.Distance(Rb.position, _lastSeen) <= captureRadius && sees) { Capture(); return; }
                if (!sees && _unseenTimer >= loseSightSeconds && VectorMath.Distance(Rb.position, _lastSeen) <= 0.6f)
                {
                    State = GuardState.Searching;
                    _searchTimer = searchSeconds;
                    _searchDir = Random.value < 0.5f ? -1f : 1f;
                }
                break;

            case GuardState.Searching:
                if (sees) { StartChase(); break; }
                _searchTimer -= Time.deltaTime;
                if (_searchTimer <= 0f) BeginReturn();
                break;

            case GuardState.Returning:
                if (sees) StartChase();
                break;
        }
    }

    void FixedUpdate()
    {
        if (_captured) return;
        switch (State)
        {
            case GuardState.Chasing:
                MoveToward(_lastSeen, chaseSpeed);
                FaceDirection(_lastSeen - Rb.position, chaseTurnSpeed);
                break;
            case GuardState.Searching:
                // Mira a un lado y a otro buscando.
                Rb.MoveRotation(Rb.rotation + _searchDir * 140f * Time.fixedDeltaTime);
                if (Mathf.Repeat(_searchTimer, 0.8f) < Time.fixedDeltaTime) _searchDir = -_searchDir;
                break;
            default:
                OnFixedUpdate();
                break;
        }
    }

    /// <summary>Movimiento propio de cada tipo de guardia en Normal, Alerted y Returning.</summary>
    protected abstract void OnFixedUpdate();

    // ---------- transiciones ----------

    void StartChase()
    {
        if (State == GuardState.Chasing) return;
        CancelReturn();
        State = GuardState.Chasing;
        _unseenTimer = 0f;
        _spotTimer = 0f;
        SfxLibrary.Play("SFX/alert", 0.9f);
        VisionCone.SetAlerted(true);
        Vfx.Alert(transform.position + Vector3.up * 0.7f);
        OnChaseStarted();
    }

    void BeginReturn()
    {
        State = GuardState.Returning;
        VisionCone.SetAlerted(false);
        OnReturnToNormal();
        OnStartReturning();
    }

    /// <summary>La subclase avisa de que ya está en su puesto o ruta: vuelve a Normal.</summary>
    protected void FinishReturn()
    {
        if (State != GuardState.Returning) return;
        State = GuardState.Normal;
        _spotTimer = 0f;
    }

    void Capture()
    {
        if (_captured) return;
        _captured = true;
        State = GuardState.Chasing;
        VisionCone.SetAlerted(true);
        if (GameManager.Instance != null) GameManager.Instance.PlayerDetected();
        OnAlerted();
    }

    /// <summary>
    /// Contacto físico: chocar con el guardia es captura inmediata en cualquier estado.
    /// Cubre el punto ciego de pegarse a su espalda, donde el cono nunca llega.
    /// </summary>
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (_captured) return;
        if (!CollisionLayers.Contains(CollisionLayers.PlayerMask, collision.gameObject.layer)) return;
        Vfx.Alert(transform.position);
        Capture();
    }

    public void AlertAt(Vector2 noisePosition)
    {
        if (State == GuardState.Chasing || State == GuardState.Searching) return;
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
        if (_captured || State != GuardState.Alerted) yield break;
        State = GuardState.Normal;
        VisionCone.SetAlerted(false);
        OnReturnToNormal();
    }

    // ---------- movimiento compartido ----------

    /// <summary>Un paso hacia el punto sin pasarse: posición + dirección · rapidez · Δt.</summary>
    protected void MoveToward(Vector2 target, float speed)
    {
        Rb.MovePosition(VectorMath.StepTowards(Rb.position, target, speed, Time.fixedDeltaTime));
    }

    /// <summary>Gira el cuerpo (y con él el cono) hacia la dirección dada.</summary>
    protected void FaceDirection(Vector2 dir, float turnSpeed)
    {
        if (dir.sqrMagnitude < 1e-6f) return;
        float target = VectorMath.DirectionToAngle(dir);
        float maxTurn = turnSpeed * Time.fixedDeltaTime;
        Rb.MoveRotation(Mathf.MoveTowardsAngle(Rb.rotation, target, maxTurn));
    }

    protected bool Arrived(Vector2 target, float radius) => VectorMath.Distance(Rb.position, target) <= radius;

    // ---------- ganchos ----------

    protected virtual void OnAlerted() { }
    protected virtual void OnVisionAlerted(Vector2 position) { }
    protected virtual void OnNoiseAlerted(Vector2 position) { }
    protected virtual void OnReturnToNormal() { }
    protected virtual void OnChaseStarted() { }
    /// <summary>Empieza la vuelta al puesto. Por defecto no hay que moverse: se acaba al instante.</summary>
    protected virtual void OnStartReturning() => FinishReturn();
}
