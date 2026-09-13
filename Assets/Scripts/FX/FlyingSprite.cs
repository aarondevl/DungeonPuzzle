using UnityEngine;

/// <summary>
/// Copia de un sprite que vuela en arco (curva de Bézier cuadrática) desde un punto
/// hasta otro, girando y encogiéndose, y avisa al llegar. Se usa para que la llave
/// "viaje" hasta su puerta al recogerla, en vez de que la puerta se abra sola a
/// distancia sin que el jugador entienda por qué.
///
/// Puntos y vectores en acción: el punto de control del arco es el punto medio
/// desplazado hacia arriba una fracción de la distancia entre origen y destino.
/// </summary>
public class FlyingSprite : MonoBehaviour
{
    SpriteRenderer _sr;
    Vector3 _p0, _p1, _p2;
    float _seconds;
    float _t;
    float _spinDeg;
    Vector3 _startScale;
    System.Action _onArrive;

    /// <summary>Lanza una copia de <paramref name="source"/> hacia <paramref name="target"/>.</summary>
    public static FlyingSprite Launch(SpriteRenderer source, Vector3 target, float seconds, System.Action onArrive,
        float arcHeight = 0.35f, float spinTurns = 1.5f)
    {
        if (source == null || source.sprite == null) { onArrive?.Invoke(); return null; }

        var go = new GameObject("FlyingSprite");
        Vector3 p0 = source.transform.position;
        go.transform.position = p0;
        go.transform.localScale = source.transform.lossyScale;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = source.sprite;
        sr.color = source.color;
        sr.sharedMaterial = source.sharedMaterial;
        sr.sortingLayerID = source.sortingLayerID;
        sr.sortingOrder = source.sortingOrder + 5;

        var fly = go.AddComponent<FlyingSprite>();
        fly._sr = sr;
        fly._p0 = p0;
        fly._p2 = target;
        fly._p1 = ControlPoint(p0, target, arcHeight);
        fly._seconds = Mathf.Max(0.05f, seconds);
        fly._spinDeg = 360f * spinTurns;
        fly._startScale = go.transform.localScale;
        fly._onArrive = onArrive;
        return fly;
    }

    /// <summary>Punto de control del arco: punto medio elevado según la distancia recorrida.</summary>
    public static Vector3 ControlPoint(Vector3 from, Vector3 to, float arcHeight)
    {
        Vector3 mid = (from + to) * 0.5f;
        float span = Vector3.Distance(from, to);
        return mid + Vector3.up * (span * arcHeight + 0.5f);
    }

    /// <summary>Bézier cuadrática: B(t) = (1-t)²·P0 + 2(1-t)t·P1 + t²·P2.</summary>
    public static Vector3 Bezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        float u = 1f - t;
        return u * u * p0 + 2f * u * t * p1 + t * t * p2;
    }

    void Update()
    {
        _t += Time.deltaTime / _seconds;
        float u = Mathf.Clamp01(_t);
        // Ease-in-out para que arranque suave y llegue con intención.
        float e = u * u * (3f - 2f * u);
        transform.position = Bezier(_p0, _p1, _p2, e);
        transform.rotation = Quaternion.Euler(0f, 0f, _spinDeg * e);
        transform.localScale = _startScale * Mathf.Lerp(1f, 0.55f, e);

        if (u >= 1f)
        {
            var cb = _onArrive;
            _onArrive = null;
            Destroy(gameObject);
            cb?.Invoke();
        }
    }
}
