using UnityEngine;

/// <summary>
/// Efecto visual de "pop" al recoger un objeto: crece y se desvanece, luego se
/// autodestruye. Se instancia como un GameObject independiente porque el objeto
/// recogido (p. ej. la llave) se desactiva de inmediato en PlayerInventory.
/// </summary>
public class PickupPop : MonoBehaviour
{
    /// <summary>Crea el efecto copiando el sprite de la fuente en su posición.</summary>
    public static void Spawn(SpriteRenderer source, float growTo = 1.8f, float duration = 0.35f)
    {
        if (source == null || source.sprite == null) return;

        var go = new GameObject("PickupPop");
        go.transform.position = source.transform.position;
        go.transform.rotation = source.transform.rotation;
        go.transform.localScale = source.transform.lossyScale;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = source.sprite;
        sr.color = source.color;
        sr.sharedMaterial = source.sharedMaterial;
        sr.sortingLayerID = source.sortingLayerID;
        sr.sortingOrder = source.sortingOrder + 1;

        var pop = go.AddComponent<PickupPop>();
        pop._sr = sr;
        pop._startScale = go.transform.localScale;
        pop._endScale = pop._startScale * growTo;
        pop._duration = Mathf.Max(0.01f, duration);
    }

    SpriteRenderer _sr;
    Vector3 _startScale;
    Vector3 _endScale;
    float _duration;
    float _t;

    void Update()
    {
        _t += Time.deltaTime;
        float u = Mathf.Clamp01(_t / _duration);

        // Escala con ease-out para un "pop" ágil.
        transform.localScale = Vector3.LerpUnclamped(_startScale, _endScale, 1f - (1f - u) * (1f - u));

        // Fade lineal a transparente.
        var c = _sr.color;
        c.a = 1f - u;
        _sr.color = c;

        if (u >= 1f) Destroy(gameObject);
    }
}
