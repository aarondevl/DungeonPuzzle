using UnityEngine;

/// <summary>
/// Línea de puntería mientras el héroe lleva una piedra: del héroe al punto donde
/// caerá (el cursor, recortado al alcance), con un pequeño círculo en el destino.
/// Así el lanzamiento deja de ser a ciegas.
///
/// Vectores: destino = origen + normalizar(cursor - origen) · min(|cursor - origen|, alcance).
/// </summary>
public class ThrowAim : MonoBehaviour
{
    LineRenderer _line;
    LineRenderer _ring;
    Camera _cam;

    void Awake()
    {
        _line = NewLine("AimLine", 2, 0.05f, new Color(1f, 0.9f, 0.5f, 0.75f));
        _ring = NewLine("AimRing", 24, 0.04f, new Color(1f, 0.9f, 0.5f, 0.9f));
        _ring.loop = true;
        Hide();
    }

    LineRenderer NewLine(string name, int points, float width, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = points;
        lr.widthMultiplier = width;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lr.endColor = color;
        lr.sortingLayerName = "FX";
        lr.sortingOrder = 40;
        return lr;
    }

    /// <summary>Punto de caída recortado al alcance. Función pura, cubierta por tests.</summary>
    public static Vector2 ClampTarget(Vector2 origin, Vector2 wanted, float maxRange)
    {
        Vector2 delta = wanted - origin;
        float len = delta.magnitude;
        if (len <= maxRange || len < 1e-4f) return wanted;
        return origin + delta / len * maxRange;
    }

    public void Show(Vector2 origin, Vector2 target)
    {
        _line.enabled = true;
        _ring.enabled = true;
        _line.SetPosition(0, origin);
        _line.SetPosition(1, target);
        int n = _ring.positionCount;
        for (int i = 0; i < n; i++)
        {
            float a = i / (float)n * Mathf.PI * 2f;
            _ring.SetPosition(i, new Vector3(target.x + Mathf.Cos(a) * 0.3f, target.y + Mathf.Sin(a) * 0.3f, 0f));
        }
    }

    public void Hide()
    {
        if (_line != null) _line.enabled = false;
        if (_ring != null) _ring.enabled = false;
    }
}
