using System.Collections.Generic;
using UnityEngine;

public class NoiseSource : MonoBehaviour
{
    [SerializeField] float noiseRadius = 8f;
    [SerializeField] LayerMask guardLayer;

    public void TriggerNoise()
    {
        Collider2D[] guards = Physics2D.OverlapCircleAll(transform.position, noiseRadius, guardLayer);
        foreach (var col in guards)
        {
            var g = col.GetComponent<GuardBase>();
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
