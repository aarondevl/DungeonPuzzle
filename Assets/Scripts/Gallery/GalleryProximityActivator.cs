using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public sealed class GalleryProximityActivator : MonoBehaviour
{
    [SerializeField] GameObject[] targets;
    [SerializeField] bool deactivateOnExit = true;

    public void Configure(GameObject[] controlledTargets, bool turnOffOnExit)
    {
        targets = controlledTargets;
        deactivateOnExit = turnOffOnExit;
    }

    public void SetInside(bool inside)
    {
        if (!inside && !deactivateOnExit) return;
        if (targets == null) return;
        foreach (var target in targets)
            if (target != null) target.SetActive(inside);
    }

    void Awake() => GetComponent<Collider2D>().isTrigger = true;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (GalleryPlayerContact.IsPhysicalPlayer(other)) SetInside(true);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (GalleryPlayerContact.IsPhysicalPlayer(other)) SetInside(false);
    }
}
