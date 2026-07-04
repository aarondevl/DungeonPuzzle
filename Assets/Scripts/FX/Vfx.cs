using UnityEngine;

/// <summary>
/// API estática para lanzar efectos visuales puntuales, en el mismo espíritu que
/// <see cref="SfxLibrary"/>: carga un prefab de <c>Resources/VFX/&lt;nombre&gt;</c> y lo
/// instancia en una posición del mundo. Así ningún objeto necesita cablear referencias.
/// El prefab lleva un <see cref="Flipbook"/> que anima la secuencia y se autodestruye.
/// </summary>
public static class Vfx
{
    public static void Play(string name, Vector3 position, float scale = 1f)
    {
        var prefab = Resources.Load<GameObject>("VFX/" + name);
        if (prefab == null)
        {
            Debug.LogWarning($"[Vfx] Prefab no encontrado: Resources/VFX/{name}");
            return;
        }
        var go = Object.Instantiate(prefab, position, Quaternion.identity);
        if (!Mathf.Approximately(scale, 1f))
            go.transform.localScale *= scale;
    }

    /// <summary>Chispa dorada breve: impactos, palancas, puertas, recogidas.</summary>
    public static void Spark(Vector3 position) => Play("Spark", position);

    /// <summary>Fogonazo de alerta (rojo, más grande): cuando un guardia te detecta.</summary>
    public static void Alert(Vector3 position) => Play("Alert", position);
}
