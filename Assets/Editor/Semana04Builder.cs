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
        BuildDemoRoom();
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
        // Solo cuerpos que pueden REPOSAR encima. Una piedra en vuelo la sobrevuela
        // (un trigger no frena a un cuerpo dinámico) y solo causaría un parpadeo de
        // la puerta al entrar y salir en el mismo instante.
        so.FindProperty("acceptedLayers").intValue =
            CollisionLayers.PlayerMask | CollisionLayers.GuardMask;
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


    // ------------------------------------------------------- sala de demostración

    /// <summary>
    /// Construye <c>Room_Demo</c>: un banco de pruebas con TODAS las mecánicas de la
    /// Semana 04 en una sola pantalla, para sustentar en 90 segundos en vez de
    /// recorrer cinco salas.
    ///
    /// Se clona Room_02 y se vacía su contenido, igual que hace <see cref="RoomBuilder"/>,
    /// para heredar cámara, HUD, luz global y post-proceso sin reconstruirlos.
    ///
    /// Recorrido previsto, de izquierda a derecha:
    ///   1. salida del spawn y rodeo del bloque → DESLIZAMIENTO contra el muro
    ///   2. dos piedras: una al muro del fondo (RUIDO → el guardia gira) y otra
    ///      contra la puerta cerrada (el proyectil NO la atraviesa y hace ruido ahí)
    ///   3. pasillo de tres TRAMPAS desfasadas
    ///   4. PLACA DE PRESIÓN que abre la puerta; palanca al lado como rescate manual
    ///   5. espalda del guardia estático → DETECCIÓN POR CONTACTO
    ///   6. puerta y salida
    /// </summary>
    [MenuItem("DungeonPuzzle/Semana 04/5. Construir sala de demostración")]
    public static void BuildDemoRoom()
    {
        var platePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlatePrefabPath);
        var spikePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SpikePrefabPath);
        var doorPrefab  = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Door.prefab");
        var leverPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Lever.prefab");
        var stonePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Stone.prefab");
        var guardStaticPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Guard_Static.prefab");
        var guardPatrolPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Guard_Patrol.prefab");
        if (platePrefab == null || spikePrefab == null)
        {
            Debug.LogError("[Semana04] Faltan los prefabs de la Semana 04. Ejecuta primero el paso 1.");
            return;
        }

        EditorSceneManager.OpenScene("Assets/Scenes/Room_02.unity", OpenSceneMode.Single);
        var src = EditorSceneManager.GetActiveScene();
        const string dst = "Assets/Scenes/Room_Demo.unity";
        EditorSceneManager.SaveScene(src, dst, false);
        Scene s = EditorSceneManager.OpenScene(dst, OpenSceneMode.Single);

        // Plantilla de muro capturada antes de vaciar, para heredar textura y capa.
        var topWall = GameObject.Find("Wall_Top");
        GameObject wallTemplate = topWall != null ? Object.Instantiate(topWall) : null;
        if (wallTemplate != null) { wallTemplate.name = "__WallTemplate"; wallTemplate.SetActive(false); }

        StripDemoScene(s);

        // ── Cascarón: la sala mide x ∈ [-8, 8], y ∈ [-5, 5] ──
        Wall(wallTemplate, "Wall_Top",    new Vector2(0, 5),   new Vector2(16, 0.5f));
        Wall(wallTemplate, "Wall_Bottom", new Vector2(0, -5),  new Vector2(16, 0.5f));
        Wall(wallTemplate, "Wall_Left",   new Vector2(-8, 0),  new Vector2(0.5f, 10));
        Wall(wallTemplate, "Wall_Right",  new Vector2(8, 0),   new Vector2(0.5f, 10));

        // Bloque que obliga a rodear rozando: cubre y ∈ [-4.75, -1.25], sin hueco
        // por abajo, así que el jugador TIENE que subir bordeándolo.
        Wall(wallTemplate, "Wall_Slide",  new Vector2(-3.5f, -3f), new Vector2(0.6f, 3.5f));
        // Tapia que aísla la salida: cubre y ∈ [-3, 5]; el hueco de abajo lo tapa la puerta.
        Wall(wallTemplate, "Wall_Gate",   new Vector2(5f, 1f),     new Vector2(0.6f, 8f));

        MoveSpawnTo(new Vector2(-6.5f, -4f));

        // ── Puerta que cierra el hueco y ∈ [-5, -3] ──
        var door = Place(doorPrefab, "Door_Exit", new Vector2(5f, -4f));
        Door doorComp = null;
        if (door != null)
        {
            door.transform.localScale = new Vector3(0.5f, 0.67f, 1f);
            doorComp = door.GetComponent<Door>();
        }

        // ── Estación 2: dos piedras (ruido lejano y proyectil contra la puerta) ──
        Place(stonePrefab, "Stone_Ruido",  new Vector2(-6.8f, -2.6f));
        Place(stonePrefab, "Stone_Puerta", new Vector2(-5.6f, -2.6f));

        // ── Estación 3: tres trampas desfasadas un tercio de ciclo ──
        for (int i = 0; i < 3; i++)
        {
            var trap = Place(spikePrefab, $"SpikeTrap_{i + 1}", new Vector2(-1f + i, -4f));
            if (trap == null) continue;
            var so = new SerializedObject(trap.GetComponent<SpikeTrap>());
            so.FindProperty("startOffset").floatValue = i;   // periodo 3 s / 3 trampas
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ── Estación 4: placa y palanca sobre la MISMA puerta. Es el caso de los
        //    "dos dueños": la placa la suelta al salirse, así que puede cerrar lo que
        //    la palanca abrió. Aquí es deliberado —es el beat que se explica— y por
        //    eso en las salas 02-05 el constructor pone la placa en modo latching. ──
        var plate = Place(platePrefab, "PressurePlate", new Vector2(3f, -4f));
        if (plate != null && doorComp != null)
        {
            var so = new SerializedObject(plate.GetComponent<PressurePlate>());
            so.FindProperty("linkedDoor").objectReferenceValue = doorComp;
            so.FindProperty("latching").boolValue = false;   // aquí SÍ queremos ver que se suelta
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        var lever = Place(leverPrefab, "Lever_Rescate", new Vector2(3.6f, -2.6f));
        if (lever != null && doorComp != null)
        {
            var so = new SerializedObject(lever.GetComponent<Lever>());
            so.FindProperty("linkedDoor").objectReferenceValue = doorComp;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ── Estación 5: guardia estático de espaldas al pasillo ──
        var gs = Place(guardStaticPrefab, "Guard_Static", new Vector2(2.5f, -1.2f));
        if (gs != null) gs.transform.rotation = Quaternion.identity;   // 0° = mira al norte

        // ── Guardia de patrulla arriba: reacciona al ruido de la piedra ──
        var wpA = Waypoint("Waypoint_A", new Vector2(-2f, 3f));
        var wpB = Waypoint("Waypoint_B", new Vector2(3.5f, 3f));
        var gp = Place(guardPatrolPrefab, "Guard_Patrol", new Vector2(-2f, 3f));
        if (gp != null)
        {
            var so = new SerializedObject(gp.GetComponent<GuardPatrol>());
            var arr = so.FindProperty("waypoints");
            arr.arraySize = 2;
            arr.GetArrayElementAtIndex(0).objectReferenceValue = wpA.transform;
            arr.GetArrayElementAtIndex(1).objectReferenceValue = wpB.transform;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ── Salida: marcada como final, para que la demo termine en victoria ──
        DemoExit("ExitTrigger", new Vector2(6.8f, -4f), new Vector2(1.6f, 1.6f));

        if (wallTemplate != null) Object.DestroyImmediate(wallTemplate);
        EditorSceneManager.MarkSceneDirty(s);
        EditorSceneManager.SaveScene(s);
        AddDemoToBuildSettings();

        Debug.Log("[Semana04] Room_Demo construida. Ábrela y pulsa Play; F1 muestra el panel de estado.");
    }

    static void StripDemoScene(Scene s)
    {
        foreach (var go in s.GetRootGameObjects())
        {
            string n = go.name;
            if (n.StartsWith("Wall_") || n.StartsWith("Corner_") || n.StartsWith("Guard_") ||
                n.StartsWith("Waypoint_") || n.StartsWith("PointLight_") || n.StartsWith("SpikeTrap") ||
                n.StartsWith("Stone") || n.StartsWith("Lever") || n.StartsWith("Key") ||
                n.StartsWith("Door") || n == "PressurePlate" || n == "ExitTrigger")
            {
                Object.DestroyImmediate(go);
            }
        }
    }

    static GameObject Wall(GameObject template, string name, Vector2 pos, Vector2 size)
    {
        GameObject go;
        if (template != null)
        {
            go = Object.Instantiate(template);
            go.SetActive(true);
            go.hideFlags = HideFlags.None;
        }
        else
        {
            // Sin plantilla: muro mínimo pero funcional, en la capa correcta.
            go = new GameObject(name);
            go.AddComponent<BoxCollider2D>();
        }
        go.name = name;
        go.layer = CollisionLayers.Walls;
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        return go;
    }

    static GameObject Place(GameObject prefab, string name, Vector2 pos)
    {
        if (prefab == null) { Debug.LogWarning($"[Semana04] Falta el prefab para '{name}'."); return null; }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = name;
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        return go;
    }

    static GameObject Waypoint(string name, Vector2 pos)
    {
        var go = new GameObject(name);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        return go;
    }

    static void DemoExit(string name, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        var box = go.AddComponent<BoxCollider2D>();
        box.size = size;
        box.isTrigger = true;
        var trigger = go.AddComponent<ExitTrigger>();
        var so = new SerializedObject(trigger);
        so.FindProperty("isFinalExit").boolValue = true;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void MoveSpawnTo(Vector2 pos)
    {
        var spawn = Object.FindFirstObjectByType<SpawnPoint>();
        if (spawn != null) spawn.transform.position = new Vector3(pos.x, pos.y, 0f);
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) player.transform.position = new Vector3(pos.x, pos.y, 0f);
    }

    /// <summary>Añade Room_Demo al final de Build Settings sin tocar el orden de las salas.</summary>
    static void AddDemoToBuildSettings()
    {
        const string path = "Assets/Scenes/Room_Demo.unity";
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (scenes.Exists(sc => sc.path == path)) return;
        scenes.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
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
