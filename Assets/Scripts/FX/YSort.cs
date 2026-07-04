using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Ordena el sprite por su Y (los "pies"): menor Y => se dibuja delante.
/// Estándar top-down para que actores y props altos se ocluyan entre sí.
/// Soporta SortingGroup (visuales por capas) o SpriteRenderer simple.
/// </summary>
public class YSort : MonoBehaviour
{
    [Tooltip("Offset desde el pivot hasta los pies (negativo si el pivot está al centro).")]
    [SerializeField] float feetOffset = 0f;

    SortingGroup _group;
    SpriteRenderer _sr;

    void Awake()
    {
        _group = GetComponent<SortingGroup>();
        _sr = GetComponent<SpriteRenderer>();
    }

    void LateUpdate()
    {
        int order = ComputeOrder(transform.position.y + feetOffset);
        if (_group != null) _group.sortingOrder = order;
        else if (_sr != null) _sr.sortingOrder = order;
    }

    public static int ComputeOrder(float feetY) => -(int)Mathf.Round(feetY * 100f);
}
