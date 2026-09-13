using System.Collections;
using UnityEngine;

/// <summary>
/// Reacciones visibles del héroe: rebote al recoger algo, secuencia de captura
/// (parpadeo rojo, sacudida y encogimiento) y salida por la trampilla (giro y
/// desaparición). También corta el control del jugador durante esas secuencias.
///
/// Se añade en caliente desde <see cref="PlayerMovement"/> si el prefab no lo trae,
/// igual que el sensor de interacción, para no depender de editar la escena.
/// Las animaciones usan tiempo NO escalado porque la captura va en cámara lenta.
/// </summary>
public class PlayerFeedback : MonoBehaviour
{
    static readonly Color HitColor = new Color(1f, 0.25f, 0.25f, 1f);

    Transform _visual;
    SpriteRenderer[] _renderers;
    Color[] _baseColors;
    Vector3 _baseScale = Vector3.one;
    Rigidbody2D _rb;
    PlayerMovement _movement;
    PlayerInteraction _interaction;
    Animator _animator;
    Coroutine _punch;

    public bool HasControl { get; private set; } = true;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _movement = GetComponent<PlayerMovement>();
        _interaction = GetComponent<PlayerInteraction>();
        _animator = GetComponentInChildren<Animator>();
        _visual = _animator != null ? _animator.transform : transform;
        _baseScale = _visual.localScale;
        _renderers = GetComponentsInChildren<SpriteRenderer>();
        _baseColors = new Color[_renderers.Length];
        for (int i = 0; i < _renderers.Length; i++) _baseColors[i] = _renderers[i].color;
    }

    /// <summary>Activa o corta el control del jugador (movimiento e interacción).</summary>
    public void SetControl(bool enabled)
    {
        HasControl = enabled;
        if (_movement != null) _movement.enabled = enabled;
        if (_interaction != null) _interaction.enabled = enabled;
        if (!enabled && _rb != null) _rb.linearVelocity = Vector2.zero;
        if (!enabled && _animator != null) _animator.SetFloat("Speed", 0f);
    }

    /// <summary>Rebote de escala corto: recoger un objeto, accionar algo.</summary>
    public void Punch(float scale = 1.18f, float seconds = 0.2f)
    {
        if (_punch != null) StopCoroutine(_punch);
        _punch = StartCoroutine(PunchRoutine(scale, seconds));
    }

    IEnumerator PunchRoutine(float scale, float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / seconds);
            // Sube rápido y vuelve con ease-out.
            float s = u < 0.3f ? Mathf.Lerp(1f, scale, u / 0.3f)
                               : Mathf.Lerp(scale, 1f, 1f - (1f - (u - 0.3f) / 0.7f) * (1f - (u - 0.3f) / 0.7f));
            _visual.localScale = _baseScale * s;
            yield return null;
        }
        _visual.localScale = _baseScale;
        _punch = null;
    }

    /// <summary>Secuencia de captura: sin control, tres parpadeos rojos con sacudida y encogimiento final.</summary>
    public Coroutine PlayCaptured(float seconds) => StartCoroutine(CapturedRoutine(seconds));

    IEnumerator CapturedRoutine(float seconds)
    {
        SetControl(false);
        Vector3 basePos = _visual.localPosition;

        float flashPhase = seconds * 0.6f;
        float t = 0f;
        while (t < flashPhase)
        {
            t += Time.unscaledDeltaTime;
            float u = t / flashPhase;
            bool red = Mathf.FloorToInt(u * 6f) % 2 == 0;           // 3 parpadeos
            Tint(red ? HitColor : Color.white);
            float shake = Mathf.Sin(t * 60f) * 0.06f * (1f - u);
            _visual.localPosition = basePos + new Vector3(shake, 0f, 0f);
            yield return null;
        }
        _visual.localPosition = basePos;
        Tint(HitColor);

        float shrinkPhase = seconds - flashPhase;
        t = 0f;
        while (t < shrinkPhase)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / shrinkPhase);
            _visual.localScale = _baseScale * Mathf.Lerp(1f, 0.55f, u * u);
            _visual.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, 25f, u));
            yield return null;
        }
    }

    /// <summary>Salida por la trampilla: se desliza hasta el hueco, gira y se encoge hasta desaparecer.</summary>
    public Coroutine PlayEscape(float seconds, Vector3? target = null) => StartCoroutine(EscapeRoutine(seconds, target));

    IEnumerator EscapeRoutine(float seconds, Vector3? target)
    {
        SetControl(false);
        // Sin física durante la salida: el hueco no es un obstáculo y el cuerpo no debe rebotar.
        if (_rb != null) _rb.simulated = false;
        Vector3 from = transform.position;
        Vector3 to = target.HasValue ? new Vector3(target.Value.x, target.Value.y, from.z) : from;
        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / seconds);
            float e = u * u;
            transform.position = Vector3.Lerp(from, to, 1f - (1f - u) * (1f - u));   // llega pronto al hueco
            _visual.localScale = _baseScale * (1f - e);
            _visual.localRotation = Quaternion.Euler(0f, 0f, 540f * e);
            Tint(Color.Lerp(Color.white, new Color(0.3f, 0.3f, 0.4f, 1f), u));
            yield return null;
        }
        _visual.localScale = Vector3.zero;
    }

    void Tint(Color c)
    {
        for (int i = 0; i < _renderers.Length; i++)
            if (_renderers[i] != null) _renderers[i].color = _baseColors[i] * c;
    }
}
