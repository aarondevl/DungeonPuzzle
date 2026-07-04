using UnityEngine;

/// <summary>
/// Ordena el sprite por su Y (los "pies"): menor Y => se dibuja delante.
/// Estándar top-down para que actores y props altos se ocluyan entre sí.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class YSort : MonoBehaviour
{
    [Tooltip("Offset desde el pivot hasta los pies (negativo si el pivot está al centro).")]
    [SerializeField] float feetOffset = 0f;

    SpriteRenderer _sr;

    void Awake() => _sr = GetComponent<SpriteRenderer>();

    void LateUpdate() => _sr.sortingOrder = ComputeOrder(transform.position.y + feetOffset);

    public static int ComputeOrder(float feetY) => -(int)Mathf.Round(feetY * 100f);
}
