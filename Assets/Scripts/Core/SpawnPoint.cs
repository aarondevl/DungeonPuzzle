using System;
using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    [SerializeField] string spawnId = "Default";
    [SerializeField] bool isDefault = true;

    public string Id => spawnId;
    public bool IsDefault => isDefault;

    public static SpawnPoint Resolve(SpawnPoint[] points, string requestedId)
    {
        if (points == null || points.Length == 0) return null;

        if (!string.IsNullOrWhiteSpace(requestedId))
            foreach (var point in points)
                if (point != null && string.Equals(point.Id, requestedId, StringComparison.OrdinalIgnoreCase))
                    return point;

        foreach (var point in points)
            if (point != null && point.IsDefault) return point;

        return points[0];
    }

    void OnDrawGizmos()
    {
        Gizmos.color = isDefault ? Color.cyan : new Color(0.55f, 0.35f, 1f);
        Gizmos.DrawWireSphere(transform.position, 0.3f);
        Gizmos.DrawLine(transform.position, transform.position + transform.up * 0.6f);
    }
}
