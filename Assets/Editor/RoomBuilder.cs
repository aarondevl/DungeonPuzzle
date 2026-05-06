#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public static class RoomBuilder
{
    const int LayerWalls = 8;
    const int LayerInteractable = 10;

    static GameObject _guardStatic, _guardPatrol, _key, _stone, _door, _lever;
    static GameObject _wallTemplate;

    [MenuItem("DungeonPuzzle/Build Levels 3-5")]
    public static void Build()
    {
        _guardStatic = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Guard_Static.prefab");
        _guardPatrol = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Guard_Patrol.prefab");
        _key         = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Key.prefab");
        _stone       = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Stone.prefab");
        _door        = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Door.prefab");
        _lever       = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Lever.prefab");

        BuildOne("Room_03", BuildRoom03);
        BuildOne("Room_04", BuildRoom04);
        BuildOne("Room_05", BuildRoom05);

        UpdateBuildSettings();
        Debug.Log("[RoomBuilder] Done.");
    }

    static void BuildOne(string sceneName, System.Action<UnityEngine.SceneManagement.Scene> build)
    {
        // Always re-clone Room_02 so stripping is clean.
        EditorSceneManager.OpenScene("Assets/Scenes/Room_02.unity", OpenSceneMode.Single);
        var src = EditorSceneManager.GetActiveScene();
        string dst = $"Assets/Scenes/{sceneName}.unity";
        EditorSceneManager.SaveScene(src, dst, false);
        var s = EditorSceneManager.OpenScene(dst, OpenSceneMode.Single);

        // Capture a wall template before stripping; rename so Strip skips it.
        var topWall = GameObject.Find("Wall_Top");
        _wallTemplate = topWall != null ? Object.Instantiate(topWall) : null;
        if (_wallTemplate != null) {
            _wallTemplate.name = "__WallTemplate";
            _wallTemplate.SetActive(false);
        }

        Strip(s);
        build(s);
        if (_wallTemplate != null) Object.DestroyImmediate(_wallTemplate);

        EditorSceneManager.MarkSceneDirty(s);
        EditorSceneManager.SaveScene(s);
    }

    static void Strip(UnityEngine.SceneManagement.Scene s)
    {
        var roots = new List<GameObject>(s.GetRootGameObjects());
        foreach (var go in roots)
        {
            string n = go.name;
            if (n.StartsWith("Wall_") || n.StartsWith("Guard_") || n.StartsWith("Waypoint_") ||
                n == "Stone" || n == "Key" || n == "Door" || n == "Lever" || n == "ExitTrigger" ||
                n.StartsWith("PointLight_"))
            {
                Object.DestroyImmediate(go);
            }
        }
    }

    static GameObject MakeWall(string name, Vector2 pos, Vector2 size)
    {
        var go = Object.Instantiate(_wallTemplate);
        go.SetActive(true);
        go.hideFlags = HideFlags.None;
        go.name = name;
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        return go;
    }

    static GameObject Spawn(GameObject prefab, string name, Vector2 pos)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = name;
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        return go;
    }

    static GameObject MakeWaypoint(string name, Vector2 pos)
    {
        var go = new GameObject(name);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        return go;
    }

    static GameObject MakePointLight(string name, Vector2 pos, Color c)
    {
        var go = new GameObject(name);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        var l = go.AddComponent<Light2D>();
        l.lightType = Light2D.LightType.Point;
        l.color = c;
        l.intensity = 1.4f;
        l.pointLightOuterRadius = 4.5f;
        l.pointLightInnerRadius = 0.5f;
        l.falloffIntensity = 0.5f;
        l.shadowsEnabled = true;
        return go;
    }

    static GameObject MakeExit(string name, Vector2 pos, Vector2 size, bool isFinal = false)
    {
        var go = new GameObject(name);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        var box = go.AddComponent<BoxCollider2D>();
        box.size = size;
        box.isTrigger = true;
        var trig = go.AddComponent<ExitTrigger>();
        var so = new SerializedObject(trig);
        var field = so.FindProperty("isFinalExit");
        if (field != null) field.boolValue = isFinal;
        so.ApplyModifiedProperties();
        return go;
    }

    static void LinkSerialized(Component target, string fieldName, Object value)
    {
        var so = new SerializedObject(target);
        var p = so.FindProperty(fieldName);
        if (p != null)
        {
            p.objectReferenceValue = value;
            so.ApplyModifiedProperties();
        }
    }

    static void LinkArray(Component target, string fieldName, Object[] values)
    {
        var so = new SerializedObject(target);
        var p = so.FindProperty(fieldName);
        if (p == null || !p.isArray) return;
        p.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        so.ApplyModifiedProperties();
    }

    static void MoveSpawn(Vector2 pos)
    {
        var sp = GameObject.Find("SpawnPoint");
        if (sp != null) sp.transform.position = new Vector3(pos.x, pos.y, 0f);
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) p.transform.position = new Vector3(pos.x, pos.y, 0f);
    }

    static void OuterWalls()
    {
        // Room shell matching Room_02 dimensions.
        MakeWall("Wall_Top",    new Vector2(0, 5),  new Vector2(16, 0.5f));
        MakeWall("Wall_Bottom", new Vector2(0, -5), new Vector2(16, 0.5f));
        MakeWall("Wall_Left",   new Vector2(-8, 0), new Vector2(0.5f, 10));
        MakeWall("Wall_Right",  new Vector2(8, 0),  new Vector2(0.5f, 10));
    }

    // ---------------- Room_03 — Sala Vigilada ----------------
    static void BuildRoom03(UnityEngine.SceneManagement.Scene _)
    {
        OuterWalls();
        // Vertical divider with bottom gap.
        MakeWall("Wall_Divider", new Vector2(0, 2), new Vector2(0.5f, 6));

        MoveSpawn(new Vector2(-6, -3));

        var door = Spawn(_door, "Door", new Vector2(5, -3));
        door.transform.localScale = new Vector3(0.5f, 2f, 1f);

        var key = Spawn(_key, "Key", new Vector2(5, 3));
        var keyComp = key.GetComponent<Key>();
        LinkSerialized(keyComp, "linkedDoor", door.GetComponent<Door>());

        var guard = Spawn(_guardStatic, "Guard_Static", new Vector2(3, 2));
        guard.transform.rotation = Quaternion.Euler(0, 0, 180); // face south

        MakeExit("ExitTrigger", new Vector2(7, -3), new Vector2(1.5f, 1.5f));

        MakePointLight("PointLight_NW", new Vector2(-5, 3), new Color(1f, 0.85f, 0.55f, 1f));
        MakePointLight("PointLight_SE", new Vector2(5, -3), new Color(1f, 0.85f, 0.55f, 1f));
    }

    // ---------------- Room_04 — Patrulla Cruzada ----------------
    static void BuildRoom04(UnityEngine.SceneManagement.Scene _)
    {
        OuterWalls();
        // Two interior walls forming an L.
        MakeWall("Wall_Mid_H", new Vector2(2, 1.5f),  new Vector2(8, 0.5f));
        MakeWall("Wall_Mid_V", new Vector2(2, -1.5f), new Vector2(0.5f, 4));

        MoveSpawn(new Vector2(-6, -3));

        var door = Spawn(_door, "Door", new Vector2(-5, 4));
        door.transform.rotation = Quaternion.Euler(0, 0, 90);
        door.transform.localScale = new Vector3(0.5f, 2f, 1f);

        var lever = Spawn(_lever, "Lever", new Vector2(6, -4));
        LinkSerialized(lever.GetComponent<Lever>(), "linkedDoor", door.GetComponent<Door>());

        Spawn(_stone, "Stone", new Vector2(-6, -1));

        // Two patrols on perpendicular routes.
        var wpA1 = MakeWaypoint("Waypoint_1A", new Vector2(-5, 3));
        var wpA2 = MakeWaypoint("Waypoint_1B", new Vector2(5, 3));
        var wpB1 = MakeWaypoint("Waypoint_2A", new Vector2(-5, -3));
        var wpB2 = MakeWaypoint("Waypoint_2B", new Vector2(0, -3));
        var g1 = Spawn(_guardPatrol, "Guard_Patrol_A", new Vector2(-5, 3));
        LinkArray(g1.GetComponent<GuardPatrol>(), "waypoints", new Object[] { wpA1.transform, wpA2.transform });
        var g2 = Spawn(_guardPatrol, "Guard_Patrol_B", new Vector2(-5, -3));
        LinkArray(g2.GetComponent<GuardPatrol>(), "waypoints", new Object[] { wpB1.transform, wpB2.transform });

        MakeExit("ExitTrigger", new Vector2(-7, 4), new Vector2(1.5f, 1.5f));

        MakePointLight("PointLight_NW", new Vector2(-5, 3),  new Color(0.95f, 0.7f, 0.5f, 1f));
        MakePointLight("PointLight_NE", new Vector2(5, 3),   new Color(0.95f, 0.7f, 0.5f, 1f));
        MakePointLight("PointLight_SW", new Vector2(-5, -3), new Color(0.95f, 0.7f, 0.5f, 1f));
        MakePointLight("PointLight_SE", new Vector2(5, -3),  new Color(0.95f, 0.7f, 0.5f, 1f));
    }

    // ---------------- Room_05 — Cripta Final ----------------
    static void BuildRoom05(UnityEngine.SceneManagement.Scene _)
    {
        OuterWalls();
        MakeWall("Wall_Pillar_NW", new Vector2(-3, 2),  new Vector2(2, 2));
        MakeWall("Wall_Pillar_NE", new Vector2(3, 2),   new Vector2(2, 2));
        MakeWall("Wall_Pillar_SW", new Vector2(-3, -2), new Vector2(2, 2));
        MakeWall("Wall_Pillar_SE", new Vector2(3, -2),  new Vector2(2, 2));

        MoveSpawn(new Vector2(-6, -4));

        var innerDoor = Spawn(_door, "Door", new Vector2(0, 0));
        innerDoor.transform.localScale = new Vector3(0.5f, 1.5f, 1f);

        var key = Spawn(_key, "Key", new Vector2(-6, 4));
        LinkSerialized(key.GetComponent<Key>(), "linkedDoor", innerDoor.GetComponent<Door>());

        var lever = Spawn(_lever, "Lever", new Vector2(6, 4));
        // Lever opens the inner door so player can re-pass if blocked. For final progression, ExitTrigger marks final exit and triggers WinGame.
        LinkSerialized(lever.GetComponent<Lever>(), "linkedDoor", innerDoor.GetComponent<Door>());

        Spawn(_stone, "Stone", new Vector2(-7, -3));
        var stone2 = Spawn(_stone, "Stone_2", new Vector2(-7, -2));

        var guardStatic = Spawn(_guardStatic, "Guard_Static", new Vector2(0, 4));
        guardStatic.transform.rotation = Quaternion.Euler(0, 0, 180);

        var wpTopA = MakeWaypoint("Waypoint_Top_A", new Vector2(-5, 0.5f));
        var wpTopB = MakeWaypoint("Waypoint_Top_B", new Vector2(5, 0.5f));
        var gTop = Spawn(_guardPatrol, "Guard_Patrol_Top", new Vector2(-5, 0.5f));
        LinkArray(gTop.GetComponent<GuardPatrol>(), "waypoints", new Object[] { wpTopA.transform, wpTopB.transform });

        var wpBotA = MakeWaypoint("Waypoint_Bot_A", new Vector2(-5, -4));
        var wpBotB = MakeWaypoint("Waypoint_Bot_B", new Vector2(5, -4));
        var gBot = Spawn(_guardPatrol, "Guard_Patrol_Bot", new Vector2(-5, -4));
        LinkArray(gBot.GetComponent<GuardPatrol>(), "waypoints", new Object[] { wpBotA.transform, wpBotB.transform });

        MakeExit("ExitTrigger", new Vector2(7, 0), new Vector2(1.5f, 1.5f), isFinal: true);

        MakePointLight("PointLight_NW", new Vector2(-5, 3),  new Color(0.95f, 0.7f, 0.5f, 1f));
        MakePointLight("PointLight_NE", new Vector2(5, 3),   new Color(0.95f, 0.7f, 0.5f, 1f));
        MakePointLight("PointLight_S",  new Vector2(0, -4),  new Color(0.85f, 0.6f, 0.45f, 1f));
    }

    static void UpdateBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Room_01.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Room_02.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Room_03.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Room_04.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Room_05.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/GameOver.unity", true),
        };
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
#endif
