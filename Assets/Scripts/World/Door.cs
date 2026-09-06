using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public class Door : MonoBehaviour
{
    [SerializeField] Sprite closedSprite;
    [SerializeField] Sprite openSprite;
    [SerializeField] float tweenSeconds = 0.25f;

    SpriteRenderer _sr;
    Collider2D _col;
    bool _open;
    Coroutine _running;

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _col = GetComponent<Collider2D>();
    }

    public void Open()
    {
        if (_open) return;
        _open = true;
        SfxLibrary.Play("SFX/door_open");
        Vfx.Spark(transform.position);
        _col.enabled = false;
        if (_running != null) StopCoroutine(_running);
        _running = StartCoroutine(Tween(closedSprite, openSprite));
    }

    public void Toggle() { if (_open) Close(); else Open(); }

    /// <summary>Estado explícito, para mecanismos que mantienen la puerta abierta mientras se cumplan (placas de presión).</summary>
    public void SetOpen(bool open)
    {
        if (open) Open();
        else if (_open) Close();
    }

    public bool IsOpen => _open;

    public void Close()
    {
        _open = false;
        SfxLibrary.Play("SFX/door_open", 0.8f);
        _col.enabled = true;
        if (_running != null) StopCoroutine(_running);
        _running = StartCoroutine(Tween(openSprite, closedSprite));
    }

    IEnumerator Tween(Sprite from, Sprite to)
    {
        Vector3 startScale = transform.localScale;
        Vector3 punch = startScale * 1.1f;
        float t = 0f;
        while (t < tweenSeconds * 0.5f)
        {
            t += Time.deltaTime;
            float u = t / (tweenSeconds * 0.5f);
            transform.localScale = Vector3.Lerp(startScale, punch, u);
            yield return null;
        }
        _sr.sprite = to;
        t = 0f;
        while (t < tweenSeconds * 0.5f)
        {
            t += Time.deltaTime;
            float u = t / (tweenSeconds * 0.5f);
            transform.localScale = Vector3.Lerp(punch, startScale, u);
            yield return null;
        }
        transform.localScale = startScale;
    }
}
