using UnityEngine;

public class NoiseSource : MonoBehaviour
{
    [SerializeField] float noiseRadius = 8f;
    [SerializeField] LayerMask guardLayer;

    public void TriggerNoise()
    {
        Collider2D[] guards = Physics2D.OverlapCircleAll(transform.position, noiseRadius, guardLayer);
        GuardBase nearest = null;
        float minDist = float.MaxValue;
        foreach (var g in guards)
        {
            float d = Vector2.Distance(transform.position, g.transform.position);
            if (d < minDist) { minDist = d; nearest = g.GetComponent<GuardBase>(); }
        }
        nearest?.AlertAt(transform.position);
        Destroy(gameObject);
    }
}
