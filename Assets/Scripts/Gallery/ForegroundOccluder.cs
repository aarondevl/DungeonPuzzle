using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public sealed class ForegroundOccluder : MonoBehaviour
{
    [SerializeField] SpriteRenderer[] renderers;
    [SerializeField, Range(0f, 1f)] float occludedAlpha = 0.3f;
    [SerializeField, Min(0.01f)] float fadeSpeed = 4f;
    readonly HashSet<Collider2D> _overlaps = new();

    void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<SpriteRenderer>();
    }

    void Update()
    {
        float target = _overlaps.Count > 0 ? occludedAlpha : 1f;
        foreach (var renderer in renderers)
        {
            if (renderer == null) continue;
            Color color = renderer.color;
            color.a = StepAlpha(color.a, target, fadeSpeed, Time.deltaTime);
            renderer.color = color;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (GalleryPlayerContact.IsPhysicalPlayer(other)) _overlaps.Add(other);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (GalleryPlayerContact.IsPhysicalPlayer(other)) _overlaps.Remove(other);
    }

    public static float StepAlpha(float current, float target, float speed, float deltaTime) =>
        Mathf.MoveTowards(current, target, Mathf.Max(0f, speed) * Mathf.Max(0f, deltaTime));
}
