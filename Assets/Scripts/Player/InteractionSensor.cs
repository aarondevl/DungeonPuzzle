using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sensor de proximidad por TRIGGERS.
///
/// Antes <see cref="PlayerInteraction"/> resolvía "¿qué tengo cerca?" con un
/// <c>Physics2D.OverlapCircle</c> en el momento de pulsar E: una consulta ciega que
/// se ejecutaba fuera del ciclo de física, devolvía un único collider arbitrario y
/// no permitía dar feedback al jugador antes de pulsar.
///
/// Este componente mantiene el conjunto de candidatos usando los eventos del motor
/// (<c>OnTriggerEnter2D</c> / <c>OnTriggerExit2D</c>), que Unity ya calcula durante
/// la simulación. Ventajas:
///   * coste amortizado: no hay consulta espacial por pulsación;
///   * el HUD puede mostrar "pulsa E" en cuanto el objeto entra en el radio;
///   * al salir de rango o destruirse el objeto, el candidato desaparece solo.
///
/// El collider de detección se crea en <see cref="Awake"/> si no se asigna uno, de
/// forma que el prefab del jugador conserva su collider sólido intacto.
/// </summary>
public class InteractionSensor : MonoBehaviour
{
    [SerializeField] float radius = 1f;
    [SerializeField] CircleCollider2D sensorCollider;

    readonly List<Collider2D> _inRange = new();

    public float Radius => radius;
    public IReadOnlyList<Collider2D> InRange => _inRange;

    void Awake()
    {
        if (sensorCollider == null)
        {
            sensorCollider = gameObject.AddComponent<CircleCollider2D>();
            sensorCollider.isTrigger = true;
            sensorCollider.radius = radius;
        }
        sensorCollider.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!_inRange.Contains(other)) _inRange.Add(other);
    }

    void OnTriggerExit2D(Collider2D other) => _inRange.Remove(other);

    /// <summary>Candidato más cercano que expone el componente <typeparamref name="T"/>.</summary>
    public T Closest<T>(int layerMask = ~0) where T : class
    {
        T best = null;
        float bestSqr = float.MaxValue;
        Vector2 origin = transform.position;

        for (int i = _inRange.Count - 1; i >= 0; i--)
        {
            Collider2D col = _inRange[i];
            if (col == null) { _inRange.RemoveAt(i); continue; }   // destruido mientras estaba en rango
            if (!col.gameObject.activeInHierarchy) continue;
            if (!CollisionLayers.Contains(layerMask, col.gameObject.layer)) continue;

            var candidate = col.GetComponent<T>();
            if (candidate == null) continue;

            float sqr = ((Vector2)col.transform.position - origin).sqrMagnitude;
            if (sqr >= bestSqr) continue;
            bestSqr = sqr;
            best = candidate;
        }
        return best;
    }

    /// <summary>Elige el índice del candidato más cercano. Extraída para poder testearla sin motor.</summary>
    public static int ClosestIndex(Vector2 origin, IList<Vector2> candidates)
    {
        int best = -1;
        float bestSqr = float.MaxValue;
        for (int i = 0; i < candidates.Count; i++)
        {
            float sqr = (candidates[i] - origin).sqrMagnitude;
            if (sqr >= bestSqr) continue;
            bestSqr = sqr;
            best = i;
        }
        return best;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
