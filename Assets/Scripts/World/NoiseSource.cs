using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Fuente de ruido puntual (la piedra al caer). Avisa a los guardias dentro del
/// radio y lo hace VISIBLE: un anillo que se expande hasta ese radio y un "?" sobre
/// cada guardia que va a mirar.
/// </summary>
public class NoiseSource : MonoBehaviour
{
    [SerializeField] float noiseRadius = 8f;
    [SerializeField] LayerMask guardLayer;

    public void TriggerNoise()
    {
        int mask = CollisionLayers.Resolve(guardLayer, CollisionLayers.GuardMask);
        Vfx.NoiseRing(transform.position, noiseRadius);
        Collider2D[] guards = Physics2D.OverlapCircleAll(transform.position, noiseRadius, mask);
        foreach (var col in guards)
        {
            var g = col.GetComponentInParent<GuardBase>();
            if (g != null) g.AlertAt(transform.position);
        }
        Destroy(gameObject);
    }

    public static List<int> SelectGuardsInRadius(Vector2 origin, IList<Vector2> positions, float radius)
    {
        var result = new List<int>();
        for (int i = 0; i < positions.Count; i++)
        {
            if (Vector2.Distance(origin, positions[i]) <= radius)
                result.Add(i);
        }
        return result;
    }
}
