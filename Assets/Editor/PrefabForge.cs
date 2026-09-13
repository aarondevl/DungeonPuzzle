#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Crea por código los prefabs que no existían en el proyecto, para que el
/// constructor de niveles pueda colocarlos. Reproducible: se puede volver a
/// ejecutar y sobreescribe el prefab.
/// </summary>
public static class PrefabForge
{
    public const string PrisonerPath = "Assets/Prefabs/Prisoner.prefab";

    [MenuItem("DungeonPuzzle/Niveles/Crear prefab del prisionero")]
    public static void CreatePrisoner()
    {
        var root = new GameObject("Prisoner") { layer = CollisionLayers.Interactable };
        var rb = root.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        var col = root.AddComponent<CircleCollider2D>();
        col.radius = 0.35f;
        root.AddComponent<Prisoner>();
        var ysort = root.AddComponent<YSort>();
        new SerializedObject(ysort).FindProperty("feetOffset").floatValue = -0.4f;

        // Cuerpo: el mismo sheet del héroe (otro preso) con animación por código.
        var visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);
        visual.transform.localScale = Vector3.one * 4.25f;    // misma escala que el héroe
        var sr = visual.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = "Actors";

        Sprite[] idle = Sprites("Assets/Sprites/Game/Characters/hero_green_idle.png", "Unarmed_Idle_full_", 24, 30);
        Sprite[] walk = Sprites("Assets/Sprites/Game/Characters/hero_green_walk.png", "right_", 0, 6);
        if (idle.Length > 0) sr.sprite = idle[0];

        var body = visual.AddComponent<GuardBody>();
        var so = new SerializedObject(body);
        so.FindProperty("body").objectReferenceValue = rb;
        Fill(so.FindProperty("idleFrames"), idle);
        Fill(so.FindProperty("walkFrames"), walk);
        so.FindProperty("fps").floatValue = 8f;
        so.FindProperty("spritesFaceLeft").boolValue = false;
        so.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, PrisonerPath);
        Object.DestroyImmediate(root);
        Debug.Log($"[PrefabForge] {PrisonerPath} creado ({idle.Length} idle, {walk.Length} walk).");
    }

    /// <summary>Sub-sprites de una hoja por prefijo e índice (nombre = prefijo + índice).</summary>
    static Sprite[] Sprites(string path, string prefix, int from, int to)
    {
        var all = AssetDatabase.LoadAllAssetRepresentationsAtPath(path).OfType<Sprite>().ToDictionary(s => s.name);
        var list = new System.Collections.Generic.List<Sprite>();
        for (int i = from; i < to; i++)
            if (all.TryGetValue(prefix + i, out var s)) list.Add(s);
        return list.ToArray();
    }

    static void Fill(SerializedProperty array, Sprite[] sprites)
    {
        if (array == null) return;
        array.arraySize = sprites.Length;
        for (int i = 0; i < sprites.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
    }

    public static GameObject EnsurePrisoner()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrisonerPath);
        if (prefab == null) { CreatePrisoner(); prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrisonerPath); }
        return prefab;
    }
}
#endif
