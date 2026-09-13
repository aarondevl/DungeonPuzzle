using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// API estática para lanzar efectos visuales puntuales, en el mismo espíritu que
/// <see cref="SfxLibrary"/>: carga un prefab de <c>Resources/VFX/&lt;nombre&gt;</c> y lo
/// instancia en una posición del mundo. Así ningún objeto necesita cablear referencias.
/// El prefab lleva un <see cref="Flipbook"/> que anima la secuencia y se autodestruye.
///
/// Además hay efectos generados por código (sin prefab ni sprites): el anillo de
/// ruido, el "?" de los guardias que oyen algo y las estrellas de aturdimiento.
/// </summary>
public static class Vfx
{
    public static void Play(string name, Vector3 position, float scale = 1f)
    {
        var prefab = Resources.Load<GameObject>("VFX/" + name);
        if (prefab == null)
        {
            Debug.LogWarning($"[Vfx] Prefab no encontrado: Resources/VFX/{name}");
            return;
        }
        var go = Object.Instantiate(prefab, position, Quaternion.identity);
        if (!Mathf.Approximately(scale, 1f))
            go.transform.localScale *= scale;
    }

    /// <summary>Chispa dorada breve: impactos, palancas, puertas, recogidas.</summary>
    public static void Spark(Vector3 position) => Play("Spark", position);

    /// <summary>Fogonazo de alerta (rojo, más grande): cuando un guardia te detecta.</summary>
    public static void Alert(Vector3 position) => Play("Alert", position);

    /// <summary>Anillo que se expande hasta el radio del ruido: muestra hasta dónde llega.</summary>
    public static void NoiseRing(Vector3 position, float radius, float seconds = 0.6f)
    {
        var go = new GameObject("NoiseRing");
        go.transform.position = position;
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop = true;
        lr.positionCount = 40;
        lr.widthMultiplier = 0.08f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.sortingLayerName = "FX";
        lr.sortingOrder = 50;
        var ring = go.AddComponent<RingAnimation>();
        ring.Init(lr, radius, seconds, new Color(1f, 0.85f, 0.4f, 0.9f));
    }

    /// <summary>Texto flotante en el mundo ("?", "!", "zzz") que sube y se desvanece.</summary>
    public static void FloatingText(Vector3 position, string text, Color color, float seconds = 1.1f, float size = 6f)
    {
        var go = new GameObject("FloatingText");
        go.transform.position = position;
        var tmp = go.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        tmp.sortingLayerID = SortingLayer.NameToID("FX");
        tmp.sortingOrder = 60;
        var rt = (RectTransform)go.transform;
        rt.sizeDelta = new Vector2(3f, 1.5f);
        var anim = go.AddComponent<FloatUpAndFade>();
        anim.Init(tmp, seconds);
    }

    /// <summary>"?" sobre un guardia que ha oído algo.</summary>
    public static void Question(Vector3 position) =>
        FloatingText(position + Vector3.up * 0.9f, "?", new Color(1f, 0.9f, 0.5f, 1f), 1.2f, 7f);

    /// <summary>Estrellas girando sobre la cabeza mientras dura el aturdimiento.</summary>
    public static GameObject StunStars(Transform target, float seconds)
    {
        var go = new GameObject("StunStars");
        go.transform.SetParent(target, false);
        go.transform.localPosition = Vector3.zero;
        var stars = go.AddComponent<OrbitingStars>();
        stars.Init(seconds);
        return go;
    }
}

/// <summary>Anillo que crece hasta un radio y se desvanece. Solo lo usa <see cref="Vfx.NoiseRing"/>.</summary>
public class RingAnimation : MonoBehaviour
{
    LineRenderer _lr; float _radius, _seconds, _t; Color _color;

    public void Init(LineRenderer lr, float radius, float seconds, Color color)
    {
        _lr = lr; _radius = radius; _seconds = seconds; _color = color;
        Apply(0f);
    }

    void Update()
    {
        _t += Time.deltaTime;
        float u = Mathf.Clamp01(_t / _seconds);
        Apply(u);
        if (u >= 1f) Destroy(gameObject);
    }

    void Apply(float u)
    {
        float r = Mathf.Lerp(0.3f, _radius, 1f - (1f - u) * (1f - u));
        int n = _lr.positionCount;
        for (int i = 0; i < n; i++)
        {
            float a = i / (float)n * Mathf.PI * 2f;
            _lr.SetPosition(i, new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f));
        }
        var c = _color; c.a = _color.a * (1f - u);
        _lr.startColor = _lr.endColor = c;
    }
}

/// <summary>Sube despacio y se desvanece. Solo lo usa <see cref="Vfx.FloatingText"/>.</summary>
public class FloatUpAndFade : MonoBehaviour
{
    TextMeshPro _tmp; float _seconds, _t; Color _base;

    public void Init(TextMeshPro tmp, float seconds) { _tmp = tmp; _seconds = seconds; _base = tmp.color; }

    void Update()
    {
        _t += Time.deltaTime;
        float u = Mathf.Clamp01(_t / _seconds);
        transform.position += Vector3.up * 0.8f * Time.deltaTime;
        var c = _base; c.a = 1f - u * u;
        _tmp.color = c;
        if (u >= 1f) Destroy(gameObject);
    }
}

/// <summary>Tres "×" girando alrededor de la cabeza. Solo lo usa <see cref="Vfx.StunStars"/>.</summary>
public class OrbitingStars : MonoBehaviour
{
    readonly TextMeshPro[] _stars = new TextMeshPro[3];
    float _seconds, _t;

    public void Init(float seconds)
    {
        _seconds = seconds;
        for (int i = 0; i < _stars.Length; i++)
        {
            var go = new GameObject($"Star{i}");
            go.transform.SetParent(transform, false);
            var tmp = go.AddComponent<TextMeshPro>();
            tmp.text = "×";
            tmp.fontSize = 6f;
            tmp.color = new Color(1f, 0.95f, 0.4f, 1f);
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;
            tmp.sortingLayerID = SortingLayer.NameToID("FX");
            tmp.sortingOrder = 60;
            ((RectTransform)go.transform).sizeDelta = new Vector2(1f, 1f);
            _stars[i] = tmp;
        }
    }

    void Update()
    {
        _t += Time.deltaTime;
        // El padre (el guardia) rota: se anula para que las estrellas giren en su propio plano.
        transform.rotation = Quaternion.identity;
        for (int i = 0; i < _stars.Length; i++)
        {
            float a = _t * 5f + i * Mathf.PI * 2f / _stars.Length;
            _stars[i].transform.localPosition = new Vector3(Mathf.Cos(a) * 0.45f, 0.75f + Mathf.Sin(a) * 0.12f, 0f);
        }
        if (_t >= _seconds) Destroy(gameObject);
    }
}
