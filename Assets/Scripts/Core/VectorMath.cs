using UnityEngine;

/// <summary>
/// Puntos, vectores y coordenadas del juego, en un solo sitio.
///
/// Convenciones que usa todo DungeonPuzzle:
///   * Un PUNTO es una posición en el mundo (x, y). Se representa con Vector2.
///   * Un VECTOR es la diferencia entre dos puntos: destino - origen. Tiene
///     dirección (hacia dónde) y magnitud (cuánto). Normalizarlo deja solo la
///     dirección (magnitud 1).
///   * El SISTEMA DE COORDENADAS del mundo es el de Unity 2D: +X derecha, +Y arriba.
///   * Los guardias miden su rotación en grados sobre Z con 0° = mirando arriba y
///     giro antihorario positivo (igual que transform.rotation). Así, -90° mira a la
///     derecha y +90° a la izquierda.
///
/// Todas las funciones son puras (sin estado ni motor) para poder probarlas en
/// EditMode: ver VectorMathTests.
/// </summary>
public static class VectorMath
{
    /// <summary>Vector unitario que apunta de <paramref name="from"/> a <paramref name="to"/>.</summary>
    public static Vector2 Direction(Vector2 from, Vector2 to)
    {
        Vector2 delta = to - from;             // vector desplazamiento entre dos puntos
        return delta.sqrMagnitude < 1e-8f ? Vector2.zero : delta.normalized;
    }

    /// <summary>Distancia euclídea entre dos puntos: magnitud del vector que los une.</summary>
    public static float Distance(Vector2 a, Vector2 b) => (b - a).magnitude;

    /// <summary>
    /// Ángulo (0..180°) entre dos vectores, vía producto punto:
    /// cos(θ) = (a · b) / (|a| · |b|).
    /// </summary>
    public static float AngleBetween(Vector2 a, Vector2 b)
    {
        float denom = a.magnitude * b.magnitude;
        if (denom < 1e-8f) return 0f;
        float cos = Mathf.Clamp(Vector2.Dot(a, b) / denom, -1f, 1f);
        return Mathf.Acos(cos) * Mathf.Rad2Deg;
    }

    /// <summary>
    /// ¿Está el punto dentro de un cono? Dos condiciones vectoriales:
    ///   1. distancia(origen, punto) ≤ alcance;
    ///   2. ángulo(forward, punto - origen) ≤ mitad de la apertura.
    /// </summary>
    public static bool IsInsideCone(Vector2 origin, Vector2 forward, Vector2 point, float halfAngleDeg, float range)
    {
        Vector2 toPoint = point - origin;
        if (toPoint.sqrMagnitude > range * range) return false;
        if (toPoint.sqrMagnitude < 1e-8f) return true;       // sobre el origen: dentro
        return AngleBetween(forward, toPoint) <= halfAngleDeg;
    }

    /// <summary>
    /// Proyecta una dirección libre sobre las 4 direcciones cardinales de la animación.
    /// Se mide cuánto del vector cae sobre cada eje con el producto punto contra la
    /// base (right, up) y gana el eje dominante; en empate gana la horizontal.
    /// </summary>
    public static Vector2 ToCardinal(Vector2 direction, Vector2 fallback)
    {
        if (direction == Vector2.zero) return fallback;
        float onX = Vector2.Dot(direction, Vector2.right);   // componente horizontal
        float onY = Vector2.Dot(direction, Vector2.up);      // componente vertical
        return Mathf.Abs(onX) >= Mathf.Abs(onY)
            ? new Vector2(Mathf.Sign(onX), 0f)
            : new Vector2(0f, Mathf.Sign(onY));
    }

    /// <summary>Convierte un vector dirección a la rotación Z del guardia (0° = arriba).</summary>
    public static float DirectionToAngle(Vector2 direction) =>
        -Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;

    /// <summary>Inversa de <see cref="DirectionToAngle"/>: rotación Z → vector unitario.</summary>
    public static Vector2 AngleToDirection(float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        return new Vector2(-Mathf.Sin(rad), Mathf.Cos(rad));
    }

    /// <summary>
    /// Cambio de sistema de coordenadas: expresa un punto del mundo en el marco local
    /// de un observador (origen + dirección forward). Devuelve (lateral, frontal):
    /// x &gt; 0 está a su derecha, y &gt; 0 está delante de él.
    /// </summary>
    public static Vector2 WorldToLocal(Vector2 origin, Vector2 forward, Vector2 worldPoint)
    {
        Vector2 f = forward.normalized;
        Vector2 right = new Vector2(f.y, -f.x);               // perpendicular, giro horario
        Vector2 delta = worldPoint - origin;
        return new Vector2(Vector2.Dot(delta, right), Vector2.Dot(delta, f));
    }

    /// <summary>
    /// Un paso de movimiento hacia un punto sin pasarse: posición + dirección · velocidad · Δt,
    /// recortado al destino si el paso es mayor que la distancia restante.
    /// </summary>
    public static Vector2 StepTowards(Vector2 position, Vector2 target, float speed, float deltaTime)
    {
        float step = speed * deltaTime;
        float remaining = Distance(position, target);
        if (remaining <= step) return target;
        return position + Direction(position, target) * step;
    }
}
