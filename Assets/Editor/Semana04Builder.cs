#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Construcción reproducible del avance de la Semana 04.
///
/// Decisión de arquitectura: el contenido de las salas NO se coloca a mano en el
/// editor, se genera con código (igual que <see cref="RoomBuilder"/>). Así el
/// avance es reproducible por cualquier integrante del equipo, el diff de git es
/// legible y no dependemos de que cada quien recuerde qué capa, qué máscara o qué
/// referencia había que arrastrar en el Inspector.
/// </summary>
public static class Semana04Builder
{
    const string PlatePrefabPath = "Assets/Prefabs/PressurePlate.prefab";
    const string SpikePrefabPath = "Assets/Prefabs/SpikeTrap.prefab";
    static readonly string[] TargetRooms = { "Room_02", "Room_03", "Room_04", "Room_05" };

    [MenuItem("DungeonPuzzle/Semana 04/Construir todo")]
    public static void BuildAll()
    {
        CreatePrefabs();
        PopulateRooms();
        UpgradePlayerPrefab();
        Debug.Log(Validate());
    }

    // ---------------------------------------------------------------- prefabs

    [MenuItem("DungeonPuzzle/Semana 04/1. Crear prefabs (placa y pinchos)")]
    public static void CreatePrefabs()
    {
        CreatePlatePrefab();
        CreateSpikePrefab();
        AssetDatabase.SaveAssets();
        Debug.Log("[Semana04] Prefabs creados/actualizados.");
    }

    static void CreatePlatePrefab()
    {
        var go = new GameObject("PressurePlate") { layer = CollisionLayers.Interactable };
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadSprite("plate_up");
        sr.sortingLayerName = "Objects";

        var box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(0.45f, 0.45f);

        var plate = go.AddComponent<PressurePlate>();
        var so = new SerializedObject(plate);
        // La piedra lanzada también pesa: permite resolver el puzle sin quedarse encima.
        so.FindProperty("acceptedLayers").intValue =
            CollisionLayers.PlayerMask | CollisionLayers.ProjectileMask | CollisionLayers.GuardMask;
        so.FindProperty("plateRenderer").objectReferenceValue = sr;
        so.FindProperty("releasedSprite").objectReferenceValue = LoadSprite("plate_up");
        so.FindProperty("pressedSprite").objectReferenceValue = LoadSprite("plate_down");
        so.ApplyModifiedPropertiesWithoutUndo();

        SavePrefab(go, PlatePrefabPath);
    }

    static void CreateSpikePrefab()
    {
        var go = new GameObject("SpikeTrap") { layer = CollisionLayers.Hazard };
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadSprite("spikes_hidden");
        sr.sortingLayerName = "Objects";

        var box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(0.4f, 0.4f);

        var trap = go.AddComponent<SpikeTrap>();
        var so = new SerializedObject(trap);
        so.FindProperty("hiddenSprite").objectReferenceValue = LoadSprite("spikes_hidden");
        so.FindProperty("risingSprite").objectReferenceValue = LoadSprite("spikes_rising");
        so.FindProperty("extendedSprite").objectReferenceValue = LoadSprite("spikes_out");
        so.FindProperty("victimLayers").intValue = CollisionLayers.PlayerMask;
        so.ApplyModifiedPropertiesWithoutUndo();

        SavePrefab(go, SpikePrefabPath);
    }

    static void SavePrefab(GameObject go, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
    }

    static Sprite LoadSprite(string name) =>
        AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Sprites/Game/{name}.png");

    // ------------------------------------------------------------------ salas

    [MenuItem("DungeonPuzzle/Semana 04/2. Colocar en las salas 02-05")]
    public static void PopulateRooms()
    {
        var plate = AssetDatabase.LoadAssetAtPath<GameObject>(PlatePrefabPath);
        var spike = AssetDatabase.LoadAssetAtPath<GameObject>(SpikePrefabPath);
        if (plate == null || spike == null)
        {
            Debug.LogError("[Semana04] Faltan los prefabs. Ejecuta primero el paso 1.");
            return;
        }

        foreach (string room in TargetRooms)
        {
            Scene s = EditorSceneManager.OpenScene($"Assets/Scenes/{room}.unity", OpenSceneMode.Single);
            RemovePrevious(s);
            Physics2D.SyncTransforms();

            var spawn = Object.FindFirstObjectByType<SpawnPoint>();
            var exit = Object.FindFirstObjectByType<ExitTrigger>();
            var door = Object.FindFirstObjectByType<Door>();
            if (spawn == null || exit == null)
            {
                Debug.LogWarning($"[Semana04] {room}: sin SpawnPoint o ExitTrigger, se omite.");
                continue;
            }

            PlaceSpikes(spike, spawn.transform.position, exit.transform.position, room);
            if (door != null) PlacePlate(plate, door, spawn.transform.position, room);

            EditorSceneManager.MarkSceneDirty(s);
            EditorSceneManager.SaveScene(s);
        }
        Debug.Log("[Semana04] Salas 02-05 actualizadas.");
    }

    static void RemovePrevious(Scene s)
    {
        foreach (var go in s.GetRootGameObjects())
            if (go.name.StartsWith("SpikeTrap") || go.name.StartsWith("PressurePlate"))
                Object.DestroyImmediate(go);
    }

    /// <summary>Tres trampas desfasadas sobre el trayecto natural entrada → salida.</summary>
    static void PlaceSpikes(GameObject prefab, Vector2 from, Vector2 to, string room)
    {
        float[] ts = { 0.35f, 0.5f, 0.65f };
        float period = 3f;                       // hidden + rising + extended + falling
        for (int i = 0; i < ts.Length; i++)
        {
            Vector2 pos = Vector2.Lerp(from, to, ts[i]);
            if (!TryFreeSpot(pos, out pos))
            {
                Debug.LogWarning($"[Semana04] {room}: sin sitio libre para SpikeTrap #{i}, se omite.");
                continue;
            }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = $"SpikeTrap_{i + 1}";
            go.transform.position = pos;

            // Desfase alterno: nunca están las tres fuera a la vez, así siempre hay paso.
            var so = new SerializedObject(go.GetComponent<SpikeTrap>());
            so.FindProperty("startOffset").floatValue = i * (period / ts.Length);
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    /// <summary>Placa junto a la puerta, del lado por el que llega el jugador.</summary>
    static void PlacePlate(GameObject prefab, Door door, Vector2 spawnPos, string room)
    {
        Vector2 doorPos = door.transform.position;
        Vector2 toSpawn = (spawnPos - doorPos).normalized;
        if (toSpawn == Vector2.zero) toSpawn = Vector2.down;

        Vector2 pos = doorPos + toSpawn * 1.5f;
        if (!TryFreeSpot(pos, out pos))
        {
            Debug.LogWarning($"[Semana04] {room}: sin sitio libre para PressurePlate, se omite.");
            return;
        }

        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = "PressurePlate";
        go.transform.position = pos;

        // Si la puerta ya la controla una palanca o una llave, la placa solo puede
        // ABRIR (latching): si no, al bajarse de ella cerraría lo que otro abrió.
        bool doorHasOwner = HasOtherController(door);

        var so = new SerializedObject(go.GetComponent<PressurePlate>());
        so.FindProperty("linkedDoor").objectReferenceValue = door;
        so.FindProperty("latching").boolValue = doorHasOwner;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static bool HasOtherController(Door door)
    {
        foreach (var lever in Object.FindObjectsByType<Lever>(FindObjectsSortMode.None))
            if (LinkedDoorOf(lever, "linkedDoor") == door) return true;
        foreach (var key in Object.FindObjectsByType<Key>(FindObjectsSortMode.None))
            if (LinkedDoorOf(key, "linkedDoor") == door) return true;
        return false;
    }

    static Door LinkedDoorOf(Object component, string field) =>
        new SerializedObject(component).FindProperty(field)?.objectReferenceValue as Door;

    /// <summary>Busca un hueco sin muros cerca de <paramref name="wanted"/>, en espiral corta.</summary>
    static bool TryFreeSpot(Vector2 wanted, out Vector2 free)
    {
        var offsets = new List<Vector2> { Vector2.zero };
        for (float r = 0.5f; r <= 2f; r += 0.5f)
            for (int a = 0; a < 8; a++)
            {
                float rad = a * Mathf.PI / 4f;
                offsets.Add(new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * r);
            }

        int blocking = CollisionLayers.WallsMask | CollisionLayers.InteractableMask | CollisionLayers.HazardMask;
        foreach (Vector2 o in offsets)
        {
            Vector2 p = wanted + o;
            if (Physics2D.OverlapBox(p, Vector2.one * 0.55f, 0f, blocking) == null)
            {
                free = p;
                return true;
            }
        }
        free = wanted;
        return false;
    }

    // ---------------------------------------------------------------- jugador

    [MenuItem("DungeonPuzzle/Semana 04/3. Añadir sensor al prefab del jugador")]
    public static void UpgradePlayerPrefab()
    {
        const string path = "Assets/Prefabs/Player.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        if (root == null) { Debug.LogError($"[Semana04] No se encontró {path}"); return; }

        if (root.GetComponent<InteractionSensor>() == null)
            root.AddComponent<InteractionSensor>();

        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
        Debug.Log("[Semana04] Player.prefab: InteractionSensor añadido.");
    }

    // -------------------------------------------------------------- validación

    [MenuItem("DungeonPuzzle/Semana 04/4. Validar física y capas")]
    public static void ValidateMenu() => Debug.Log(Validate());

    /// <summary>
    /// Comprueba que el proyecto tiene las capas y la matriz de colisiones que los
    /// scripts dan por hechas. Un fallo aquí explica el 90 % de los bugs de
    /// colisiones "que no se ven" en el Inspector.
    /// </summary>
    public static string Validate()
    {
        var sb = new StringBuilder("[Semana04] Validación de física 2D\n");
        void Layer(int index, string expected)
        {
            string actual = LayerMask.LayerToName(index);
            sb.AppendLine(actual == expected
                ? $"  OK   capa {index} = {expected}"
                : $"  FALLA capa {index}: se esperaba '{expected}' y hay '{actual}'");
        }
        Layer(CollisionLayers.Player, "Player");
        Layer(CollisionLayers.Guard, "Guard");
        Layer(CollisionLayers.Walls, "Walls");
        Layer(CollisionLayers.Items, "Items");
        Layer(CollisionLayers.Interactable, "Interactable");
        Layer(CollisionLayers.Projectile, "Projectile");
        Layer(CollisionLayers.Hazard, "Hazard");

        void Pair(int a, int b, bool shouldCollide)
        {
            bool ignored = Physics2D.GetIgnoreLayerCollision(a, b);
            bool ok = ignored != shouldCollide;
            sb.AppendLine($"  {(ok ? "OK  " : "FALLA")} {LayerMask.LayerToName(a)} x {LayerMask.LayerToName(b)}: " +
                          $"{(shouldCollide ? "deben chocar" : "NO deben chocar")}");
        }
        Pair(CollisionLayers.Player, CollisionLayers.Walls, true);
        Pair(CollisionLayers.Player, CollisionLayers.Guard, true);
        Pair(CollisionLayers.Player, CollisionLayers.Hazard, true);
        Pair(CollisionLayers.Player, CollisionLayers.Projectile, false);
        Pair(CollisionLayers.Projectile, CollisionLayers.Walls, true);
        Pair(CollisionLayers.Projectile, CollisionLayers.Guard, false);
        Pair(CollisionLayers.Guard, CollisionLayers.Guard, false);

        sb.AppendLine($"  Gravedad 2D = {Physics2D.gravity} (debe ser (0,0) en un top-down)");
        return sb.ToString();
    }
}
#endif
