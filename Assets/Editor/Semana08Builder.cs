#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Construcción reproducible del avance de la Semana 08: capas de parallax en las
/// salas de la campaña y en la sala de demostración.
///
/// Igual que <see cref="Semana04Builder"/>, nada se arrastra a mano en el Inspector:
/// las texturas se generan por código, las capas se crean y configuran por código y
/// el resultado se valida por código. Así el avance se reproduce en cualquier
/// máquina y el diff de git sigue siendo legible.
///
/// Decisión de diseño: la cámara de las salas es fija y el suelo cubre casi toda la
/// vista, así que un fondo "detrás del suelo" no se vería. Las dos capas van, por
/// tanto, por encima del suelo:
///   · Parallax_Depth  (FloorFX): grietas y escombros que se desplazan MENOS que el
///     héroe (factor 0.12) → el suelo parece estar hundido, con profundidad.
///   · Parallax_Fog    (FX):      niebla translúcida que se desplaza EN CONTRA del
///     héroe (factor -0.25) y deriva sola → está entre el observador y el suelo.
/// Ambas usan al héroe como referencia (ParallaxLayer.ReferenceMode.Player) porque
/// con cámara fija la "cámara virtual" que se mueve es el propio jugador.
/// </summary>
public static class Semana08Builder
{
    const string TextureFolder = "Assets/Sprites/Game/Parallax";
    const string CracksPath = TextureFolder + "/parallax_cracks.png";
    const string FogPath = TextureFolder + "/parallax_fog.png";
    const string RootName = "Parallax";
    const string DepthName = "Parallax_Depth";
    const string FogName = "Parallax_Fog";
    const float PixelsPerUnit = 64f;
    // La niebla usa menos píxeles por unidad: un mosaico de 8 u hace que la repetición
    // no se note y que las manchas se lean como bancos de niebla, no como píxeles.
    const float FogPixelsPerUnit = 32f;
    // Material sin iluminación de URP: las luces 2D de las salas no deben realzar la
    // niebla; su opacidad tiene que ser exactamente la del color de la capa.
    const string UnlitMaterialPath =
        "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

    static readonly string[] TargetRooms =
        { "Room_01", "Room_02", "Room_03", "Room_04", "Room_05", "Room_Demo" };

    [MenuItem("DungeonPuzzle/Semana 08/Construir todo")]
    public static void BuildAll()
    {
        GenerateTextures();
        PopulateRooms();
        Debug.Log(Validate());
    }

    // --------------------------------------------------------------- texturas

    [MenuItem("DungeonPuzzle/Semana 08/1. Generar texturas de parallax")]
    public static void GenerateTextures()
    {
        Directory.CreateDirectory(TextureFolder);
        WriteTexture(CracksPath, BuildCracks(128, seed: 8041));
        WriteTexture(FogPath, BuildFog(256, seed: 8042));
        AssetDatabase.Refresh();
        ConfigureImporter(CracksPath, FilterMode.Point, PixelsPerUnit);
        ConfigureImporter(FogPath, FilterMode.Bilinear, FogPixelsPerUnit);
        AssetDatabase.SaveAssets();
        Debug.Log("[Semana08] Texturas de parallax generadas.");
    }

    /// <summary>Grietas oscuras y guijarros sobre fondo transparente. Mosaico sin costuras.</summary>
    static Texture2D BuildCracks(int size, int seed)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var rng = new System.Random(seed);
        var px = new Color32[size * size];
        for (int i = 0; i < px.Length; i++) px[i] = new Color32(0, 0, 0, 0);

        // Grietas: caminatas aleatorias que envuelven en los bordes (módulo) para que
        // el mosaico no tenga costura.
        var crack = new Color32(8, 6, 14, 190);
        for (int c = 0; c < 9; c++)
        {
            int x = rng.Next(size), y = rng.Next(size);
            int dx = rng.Next(2) == 0 ? 1 : -1, dy = rng.Next(2) == 0 ? 1 : -1;
            int length = 18 + rng.Next(26);
            for (int s = 0; s < length; s++)
            {
                px[y * size + x] = crack;
                if (rng.Next(3) == 0) x = Mod(x + dx, size); else y = Mod(y + dy, size);
                if (rng.Next(9) == 0) dx = -dx;
                if (rng.Next(11) == 0) dy = -dy;
            }
        }

        // Guijarros: puntos de 1-2 px, algo más claros.
        var pebble = new Color32(70, 62, 84, 150);
        for (int p = 0; p < 70; p++)
        {
            int x = rng.Next(size), y = rng.Next(size);
            px[y * size + x] = pebble;
            if (rng.Next(2) == 0) px[y * size + Mod(x + 1, size)] = pebble;
        }

        tex.SetPixels32(px);
        tex.Apply();
        return tex;
    }

    /// <summary>Niebla suave: ruido de valor en varias octavas, periódico en ambos ejes.</summary>
    static Texture2D BuildFog(int size, int seed)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var px = new Color[size * size];
        int grid = 8;
        float[,] lattice = new float[grid, grid];
        var rng = new System.Random(seed);
        for (int y = 0; y < grid; y++)
            for (int x = 0; x < grid; x++)
                lattice[x, y] = (float)rng.NextDouble();

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = (float)x / size, v = (float)y / size;
                float n = 0.55f * PeriodicValueNoise(lattice, u, v, 1f)
                        + 0.30f * PeriodicValueNoise(lattice, u, v, 2f)
                        + 0.15f * PeriodicValueNoise(lattice, u, v, 4f);
                // Umbral suave: solo las zonas densas son visibles, el resto queda transparente.
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.42f, 0.78f, n));
                px[y * size + x] = new Color(0.78f, 0.74f, 0.92f, a);
            }
        }

        tex.SetPixels(px);
        tex.Apply();
        return tex;
    }

    /// <summary>Ruido de valor interpolado sobre una retícula que envuelve (periódico).</summary>
    static float PeriodicValueNoise(float[,] lattice, float u, float v, float frequency)
    {
        int grid = lattice.GetLength(0);
        float fx = u * frequency * grid, fy = v * frequency * grid;
        int x0 = Mathf.FloorToInt(fx), y0 = Mathf.FloorToInt(fy);
        float tx = Mathf.SmoothStep(0f, 1f, fx - x0), ty = Mathf.SmoothStep(0f, 1f, fy - y0);
        float a = lattice[Mod(x0, grid), Mod(y0, grid)];
        float b = lattice[Mod(x0 + 1, grid), Mod(y0, grid)];
        float c = lattice[Mod(x0, grid), Mod(y0 + 1, grid)];
        float d = lattice[Mod(x0 + 1, grid), Mod(y0 + 1, grid)];
        return Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), ty);
    }

    static int Mod(int a, int m) => ((a % m) + m) % m;

    static void WriteTexture(string path, Texture2D tex)
    {
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    /// <summary>Sprite en modo mosaico: FullRect es obligatorio para que DrawMode.Tiled funcione.</summary>
    static void ConfigureImporter(string path, FilterMode filter, float pixelsPerUnit)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer == null) { Debug.LogError($"[Semana08] No se pudo importar {path}"); return; }
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.filterMode = filter;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }

    // ------------------------------------------------------------------ salas

    [MenuItem("DungeonPuzzle/Semana 08/2. Colocar parallax en las salas")]
    public static void PopulateRooms()
    {
        var cracks = AssetDatabase.LoadAssetAtPath<Sprite>(CracksPath);
        var fog = AssetDatabase.LoadAssetAtPath<Sprite>(FogPath);
        if (cracks == null || fog == null)
        {
            Debug.LogError("[Semana08] Faltan las texturas. Ejecuta primero el paso 1.");
            return;
        }

        foreach (string room in TargetRooms)
        {
            string scenePath = $"Assets/Scenes/{room}.unity";
            if (!File.Exists(scenePath)) { Debug.LogWarning($"[Semana08] {room}: no existe, se omite."); continue; }

            Scene s = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            RemovePrevious(s);

            if (!TryFindFloor(s, out SpriteRenderer floor))
            {
                Debug.LogWarning($"[Semana08] {room}: sin FloorTiled, se omite.");
                continue;
            }

            Vector2 center = floor.transform.position;
            Vector2 floorSize = floor.size;

            var root = new GameObject(RootName);
            root.transform.position = center;

            // Capa de profundidad: cubre el suelo con un mosaico de margen para que el
            // envolvimiento (wrap) nunca deje ver un borde.
            float cracksTile = cracks.rect.width / cracks.pixelsPerUnit;
            var depth = MakeLayer(root.transform, DepthName, cracks, center,
                floorSize + Vector2.one * cracksTile * 2f,
                "FloorFX", 5, new Color(1f, 1f, 1f, 0.45f));
            depth.Configure(ParallaxLayer.ReferenceMode.Player,
                new Vector2(0.12f, 0.12f), Vector2.zero, Vector2.one * cracksTile);

            // Niebla en primer plano: cubre la vista completa de la cámara más un mosaico.
            float fogTile = fog.rect.width / fog.pixelsPerUnit;
            Vector2 viewSize = ViewSize(s);
            var fogLayer = MakeLayer(root.transform, FogName, fog, center,
                Vector2.Max(viewSize, floorSize) + Vector2.one * fogTile * 2f,
                "FX", -5, new Color(1f, 1f, 1f, 0.09f));
            fogLayer.Configure(ParallaxLayer.ReferenceMode.Player,
                new Vector2(-0.25f, -0.25f), new Vector2(0.12f, 0.04f), Vector2.one * fogTile);

            EditorSceneManager.MarkSceneDirty(s);
            EditorSceneManager.SaveScene(s);
        }
        Debug.Log("[Semana08] Parallax colocado en las salas.");
    }

    static ParallaxLayer MakeLayer(Transform parent, string name, Sprite sprite, Vector2 pos,
        Vector2 size, string sortingLayer, int order, Color tint)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.tileMode = SpriteTileMode.Continuous;
        sr.size = size;
        sr.sortingLayerName = sortingLayer;
        sr.sortingOrder = order;
        sr.color = tint;
        var unlit = AssetDatabase.LoadAssetAtPath<Material>(UnlitMaterialPath);
        if (unlit != null) sr.sharedMaterial = unlit;
        else Debug.LogWarning("[Semana08] No se encontró Sprite-Unlit-Default; la capa usará el material iluminado.");
        return go.AddComponent<ParallaxLayer>();
    }

    static void RemovePrevious(Scene s)
    {
        foreach (var go in s.GetRootGameObjects())
            if (go.name == RootName || go.name.StartsWith("Parallax_"))
                Object.DestroyImmediate(go);
    }

    static bool TryFindFloor(Scene s, out SpriteRenderer floor)
    {
        floor = null;
        foreach (var root in s.GetRootGameObjects())
        {
            foreach (var sr in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (sr.name != "FloorTiled") continue;
                floor = sr;
                return true;
            }
        }
        return false;
    }

    static Vector2 ViewSize(Scene s)
    {
        foreach (var root in s.GetRootGameObjects())
        {
            var cam = root.GetComponentInChildren<Camera>(true);
            if (cam == null || !cam.orthographic) continue;
            float h = cam.orthographicSize * 2f;
            return new Vector2(h * 16f / 9f, h);
        }
        return new Vector2(21.4f, 12f);
    }

    // ------------------------------------------------------------- validación

    [MenuItem("DungeonPuzzle/Semana 08/Validar")]
    public static void ValidateMenu() => Debug.Log(Validate());

    /// <summary>
    /// Comprueba texturas, capas de orden y que cada sala tenga las dos capas con los
    /// signos de factor correctos (fondo positivo, primer plano negativo).
    /// </summary>
    public static string Validate()
    {
        var sb = new StringBuilder("[Semana08] Validación de parallax\n");

        void Asset(string path)
        {
            bool ok = AssetDatabase.LoadAssetAtPath<Sprite>(path) != null;
            sb.AppendLine($"  {(ok ? "OK   " : "FALLA")} textura {Path.GetFileName(path)}");
        }
        Asset(CracksPath);
        Asset(FogPath);

        void CheckSorting(string name)
        {
            bool ok = UnityEngine.SortingLayer.NameToID(name) != 0 || name == "Default";
            sb.AppendLine($"  {(ok ? "OK   " : "FALLA")} sorting layer '{name}'");
        }
        CheckSorting("FloorFX");
        CheckSorting("FX");

        string current = SceneManager.GetActiveScene().path;
        foreach (string room in TargetRooms)
        {
            string scenePath = $"Assets/Scenes/{room}.unity";
            if (!File.Exists(scenePath)) continue;
            Scene s = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            // Se recorre la escena recién abierta por sus raíces (igual que PopulateRooms):
            // FindObjectsByType no es fiable justo después de OpenScene en batch mode.
            ParallaxLayer depth = null, fog = null;
            foreach (var root in s.GetRootGameObjects())
            {
                foreach (var l in root.GetComponentsInChildren<ParallaxLayer>(true))
                {
                    if (l.name == DepthName) depth = l;
                    if (l.name == FogName) fog = l;
                }
            }
            bool depthOk = depth != null && depth.Factor.x > 0f && depth.Factor.x < 1f
                           && depth.Mode == ParallaxLayer.ReferenceMode.Player && depth.WrapSize.x > 0f;
            bool fogOk = fog != null && fog.Factor.x < 0f && fog.Drift != Vector2.zero
                         && fog.Mode == ParallaxLayer.ReferenceMode.Player && fog.WrapSize.x > 0f;
            sb.AppendLine($"  {(depthOk ? "OK   " : "FALLA")} {room}: capa de profundidad (0 < factor < 1, referencia héroe, mosaico){Describe(depth, depthOk)}");
            sb.AppendLine($"  {(fogOk ? "OK   " : "FALLA")} {room}: capa de niebla (factor < 0, deriva, referencia héroe, mosaico){Describe(fog, fogOk)}");
        }
        if (!string.IsNullOrEmpty(current) && File.Exists(current))
            EditorSceneManager.OpenScene(current, OpenSceneMode.Single);

        return sb.ToString();
    }

    static string Describe(ParallaxLayer l, bool ok)
    {
        if (ok) return string.Empty;
        if (l == null) return " → no se encontró la capa";
        return $" → factor={l.Factor} deriva={l.Drift} mosaico={l.WrapSize} ref={l.Mode}";
    }
}
#endif
