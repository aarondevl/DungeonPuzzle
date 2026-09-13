#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Diseño de las 5 salas a partir de mapas ASCII (una celda = una unidad del mundo).
///
/// Referencias de diseño: Metal Gear (MSX) para los conos y las rutas de patrulla en
/// pasillos, Monaco para las salas encadenadas con varias rutas, y los "escape rooms"
/// clásicos para la estructura celda → pasillos → sala de guardia → patio → puerta final.
///
/// Leyenda del mapa:
///   #  muro            .  suelo           P  aparición del héroe     E  salida (trampilla)
///   A-D puertas (letras contiguas = una sola puerta más ancha)
///   a-d llave que abre la puerta A-D      1-4 palanca de la puerta A-D    5-8 placa de la puerta A-D
///   G  guardia fijo (mira hacia donde hay más espacio; luego se adapta solo a los muros)
///   m n q r  rutas de patrulla: cada letra es una ruta, sus celdas son los puntos (ordenados en círculo)
///   T  trampa de pinchos (desfasadas entre sí)      S  piedra      *  antorcha (luz puntual)
///
/// Cada sala se genera clonando Room_02 (para heredar cámara, HUD, luz global y post-proceso)
/// y reconstruyendo todo el contenido. Es reproducible: menú DungeonPuzzle → Niveles, o
/// <c>-executeMethod LevelDesigner.BuildAll</c> en batch.
/// </summary>
public static class LevelDesigner
{
    const string TemplateScene = "Assets/Scenes/Room_02.unity";

    // ------------------------------------------------------------------ mapas

    public static readonly string[][] Maps =
    {
        // Room_01 — LA CELDA. Solo sigilo: la reja de la celda está rota (abierta por
        // abajo), una patrulla da vueltas al bloque de arriba y un guardia fijo vigila
        // la salida. El corredor entre los dos bloques es la ruta segura.
        new[]
        {
            "##########################",
            "#P....#..................#",
            "#.....#....m.......m.....#",
            "#.....#....######........#",
            "#.....#....######.....*..#",
            "#....##....######........#",
            "#..........######.....E..#",
            "#..........m.......m.....#",
            "#...T....................#",
            "#..........######........#",
            "#...*......######...G....#",
            "#..........######........#",
            "#........................#",
            "##########################",
        },

        // Room_02 — LA LLAVE. La salida está tras la reja A, abajo a la derecha. La llave
        // está en la esquina superior derecha, dentro de la ronda de la patrulla, y un
        // guardia fijo cubre la mitad baja. Sin la llave no hay salida.
        new[]
        {
            "##########################",
            "#P.......#......m......m.#",
            "#........#...............#",
            "#........#..........a....#",
            "#........#....####.......#",
            "#....######...####.......#",
            "#.............####.......#",
            "#.............####.......#",
            "#........................#",
            "#...G...........m......m.#",
            "#........................#",
            "#...........##########A###",
            "#..*........#........E...#",
            "##########################",
        },

        // Room_03 — LOS PASILLOS. La reja doble AA de la salida la abre la palanca de la
        // esquina superior derecha, en plena ronda de la patrulla: la piedra sirve para
        // atraerla al lado contrario antes de entrar. El guardia del puesto central mira
        // por su ventana hacia el corredor. Pinchos en el tramo bajo.
        new[]
        {
            "##########################",
            "#P....#..........m.....m.#",
            "#..S..#..................#",
            "#.....#....#######.....1.#",
            "#.....#....#######.......#",
            "#.....#....##.G.##.......#",
            "#.....#....##...##.......#",
            "#.....#....###.###.......#",
            "#................m.....m.#",
            "#......#.................#",
            "#......#..T..........T...#",
            "#......#....###########AA#",
            "#..*...#....#........E...#",
            "##########################",
        },

        // Room_04 — LA ARMERÍA. La celda da al corredor de pinchos que patrulla un
        // guardia; la placa está en la esquina inferior derecha y abre la reja doble BB
        // de la sala de la salida, arriba a la derecha. Hay que cruzar el patio dos veces.
        new[]
        {
            "##########################",
            "#P...#.......#.......*...#",
            "#....#...q...#......E....#",
            "#....#.......#...........#",
            "#....#.......######BB#####",
            "#....#..T....#...........#",
            "#........q...#....n......#",
            "#....#....T..#...........#",
            "#....#.......#...........#",
            "######.....###....n......#",
            "#........................#",
            "#..*....G.............6..#",
            "#........................#",
            "##########################",
        },

        // Room_05 — EL PATIO. Final: la placa de abajo a la izquierda (vigilada) abre la
        // reja C del cuarto de la llave; la llave abre el portón doble BB de la salida.
        // Dos patrullas se cruzan en el patio y las piedras sirven para apartarlas.
        new[]
        {
            "##########################",
            "#P....#.....m.......m....#",
            "#..S..#..................#",
            "#..S..#......####........#",
            "#.....#......####.....G..#",
            "#.....#......####........#",
            "#....##......####....#####",
            "#.......n........n...C.b.#",
            "#.....................#.T#",
            "#.....G.....m.......m.#..#",
            "#......n.........n...#T..#",
            "#..7.........####BB#######",
            "#............#.......E...#",
            "##########################",
        },
    };

    static readonly string[] Titles = { "La celda", "La llave", "Los pasillos", "La armería", "El patio" };

    /// <summary>Aspecto por sala: tinte de muros, de suelo y color de antorchas, para que cada una se reconozca.</summary>
    struct Theme { public Color Wall, Floor, Torch; }
    static readonly Theme[] Themes =
    {
        // Muros claros y suelo oscuro: el contraste es lo que hace legible por dónde se pasa.
        new Theme { Wall = new Color(0.85f, 0.85f, 0.92f), Floor = new Color(0.34f, 0.36f, 0.44f), Torch = new Color(1f, 0.85f, 0.6f) },   // celda: piedra fría
        new Theme { Wall = new Color(0.95f, 0.82f, 0.62f), Floor = new Color(0.40f, 0.33f, 0.24f), Torch = new Color(1f, 0.8f, 0.5f) },    // llave: arenisca cálida
        new Theme { Wall = new Color(0.70f, 0.92f, 0.70f), Floor = new Color(0.26f, 0.36f, 0.26f), Torch = new Color(0.8f, 1f, 0.7f) },    // pasillos: musgo
        new Theme { Wall = new Color(0.98f, 0.72f, 0.62f), Floor = new Color(0.40f, 0.27f, 0.24f), Torch = new Color(1f, 0.6f, 0.4f) },    // armería: ladrillo rojizo
        new Theme { Wall = new Color(0.72f, 0.76f, 1.00f), Floor = new Color(0.22f, 0.26f, 0.42f), Torch = new Color(0.7f, 0.8f, 1f) },    // patio: noche azul
    };

    // ---------------------------------------------------------------- entrada

    [MenuItem("DungeonPuzzle/Niveles/Construir Room_01-05 desde los mapas")]
    public static void BuildAll()
    {
        string report = Validate();
        if (report.Contains("ERROR"))
        {
            Debug.LogError("[LevelDesigner] Mapas inválidos, no se construye nada:\n" + report);
            return;
        }
        Debug.Log(report);

        // Room_02 es la plantilla: se reconstruye la última para que las demás
        // puedan clonarla intacta. Tras reconstruirla conserva los mismos nombres
        // (Wall_*, FloorTiled, ExitTrigger), así que el proceso es repetible.
        int[] order = { 0, 2, 3, 4, 1 };
        foreach (int i in order) Build(i);
        Debug.Log("[LevelDesigner] 5 salas construidas.");
    }

    [MenuItem("DungeonPuzzle/Niveles/Validar mapas")]
    public static void ValidateMenu() => Debug.Log(Validate());

    // -------------------------------------------------------------- validación

    /// <summary>Filas de igual ancho, un P y un E, y camino P → E contando las puertas como abiertas.</summary>
    public static string Validate()
    {
        var sb = new System.Text.StringBuilder("[LevelDesigner] Validación de mapas\n");
        for (int i = 0; i < Maps.Length; i++)
        {
            string[] map = Maps[i];
            string name = $"Room_{i + 1:00} ({Titles[i]})";
            int w = map[0].Length;
            bool ok = true;
            for (int r = 0; r < map.Length; r++)
                if (map[r].Length != w) { sb.AppendLine($"  ERROR {name}: la fila {r} mide {map[r].Length}, se esperaba {w}"); ok = false; }
            if (!ok) continue;

            int ps = Count(map, 'P'), es = Count(map, 'E');
            if (ps != 1 || es != 1) { sb.AppendLine($"  ERROR {name}: necesita 1 P y 1 E (hay {ps} y {es})"); continue; }

            bool reachable = Reachable(map, 'P', 'E');
            sb.AppendLine(reachable ? $"  OK   {name}: {w}x{map.Length}, salida alcanzable" : $"  ERROR {name}: la salida no es alcanzable desde P");

            foreach (char door in "ABCD")
            {
                if (Count(map, door) == 0) continue;
                char key = char.ToLower(door);
                int lever = '1' + (door - 'A'), plate = '5' + (door - 'A');
                bool hasOpener = Count(map, key) > 0 || Count(map, (char)lever) > 0 || Count(map, (char)plate) > 0;
                if (!hasOpener) sb.AppendLine($"  ERROR {name}: la puerta {door} no tiene llave, palanca ni placa");
            }
        }
        return sb.ToString();
    }

    static int Count(string[] map, char c) => map.Sum(row => row.Count(ch => ch == c));

    static bool Reachable(string[] map, char from, char to)
    {
        int h = map.Length, w = map[0].Length;
        var seen = new bool[h, w];
        var q = new Queue<(int r, int c)>();
        for (int r = 0; r < h; r++) for (int c = 0; c < w; c++) if (map[r][c] == from) { q.Enqueue((r, c)); seen[r, c] = true; }
        int[] dr = { 1, -1, 0, 0 }, dc = { 0, 0, 1, -1 };
        while (q.Count > 0)
        {
            var (r, c) = q.Dequeue();
            if (map[r][c] == to) return true;
            for (int k = 0; k < 4; k++)
            {
                int nr = r + dr[k], nc = c + dc[k];
                if (nr < 0 || nc < 0 || nr >= h || nc >= w || seen[nr, nc] || map[nr][nc] == '#') continue;
                seen[nr, nc] = true;
                q.Enqueue((nr, nc));
            }
        }
        return false;
    }

    // ------------------------------------------------------------ construcción

    class Templates
    {
        public GameObject Wall, Floor, Exit;
    }

    static GameObject P(string path) => AssetDatabase.LoadAssetAtPath<GameObject>(path);

    static void Build(int index)
    {
        string[] map = Maps[index];
        int h = map.Length, w = map[0].Length;
        string sceneName = $"Room_{index + 1:00}";
        string dst = $"Assets/Scenes/{sceneName}.unity";

        EditorSceneManager.OpenScene(TemplateScene, OpenSceneMode.Single);
        if (dst != TemplateScene) EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), dst, false);
        Scene s = EditorSceneManager.OpenScene(dst, OpenSceneMode.Single);

        var t = new Templates
        {
            Wall = Detach(GameObject.Find("Wall_Top"), "__Wall"),
            Floor = Detach(GameObject.Find("FloorTiled"), "__Floor"),
            Exit = Detach(GameObject.Find("ExitTrigger"), "__Exit"),
        };
        Strip(s);

        Vector2 World(int r, int c) => new Vector2(c - w / 2f + 0.5f, h / 2f - r - 0.5f);

        // Suelo y cámara ajustados al tamaño del mapa.
        Theme theme = Themes[index];
        var floor = Revive(t.Floor, "FloorTiled");
        if (floor != null)
        {
            floor.transform.position = Vector3.zero;
            var fsr = floor.GetComponent<SpriteRenderer>();
            if (fsr != null)
            {
                fsr.sprite = Sprite("floor_stone") ?? fsr.sprite;
                fsr.color = theme.Floor;
                fsr.drawMode = SpriteDrawMode.Tiled;
                fsr.size = new Vector2(w, h);
            }
        }
        var cam = Camera.main;
        if (cam != null)
        {
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.orthographicSize = Mathf.Max(h / 2f, w / 2f / (16f / 9f)) + 0.25f;
        }

        BuildWalls(map, t.Wall, World, theme.Wall);

        // Entidades.
        var doors = new Dictionary<char, Door>();
        var doorCells = new Dictionary<char, List<(int r, int c)>>();
        var patrols = new Dictionary<char, List<Vector2>>();
        var spikes = new List<Vector2>();
        var statics = new List<(int r, int c)>();
        var openers = new List<(char kind, char door, Vector2 pos)>();
        Vector2 spawn = Vector2.zero, exit = Vector2.zero;

        for (int r = 0; r < h; r++)
            for (int c = 0; c < w; c++)
            {
                char ch = map[r][c];
                Vector2 pos = World(r, c);
                switch (ch)
                {
                    case 'P': spawn = pos; break;
                    case 'E': exit = pos; break;
                    case 'G': statics.Add((r, c)); break;
                    case 'T': spikes.Add(pos); break;
                    case 'S':
                        // La piedra mide media unidad: se agranda y brilla para que se vea desde lejos.
                        var stone = Place(P("Assets/Prefabs/Stone.prefab"), $"Stone_{r}_{c}", pos);
                        stone.transform.localScale = Vector3.one * 1.8f;
                        Highlight(stone);
                        break;
                    case '*': Torch($"PointLight_{r}_{c}", pos, theme.Torch); break;
                    case >= 'A' and <= 'D':
                        if (!doorCells.ContainsKey(ch)) doorCells[ch] = new List<(int, int)>();
                        doorCells[ch].Add((r, c));
                        break;
                    case >= 'a' and <= 'd': openers.Add(('k', char.ToUpper(ch), pos)); break;
                    case >= '1' and <= '4': openers.Add(('l', (char)('A' + (ch - '1')), pos)); break;
                    case >= '5' and <= '8': openers.Add(('p', (char)('A' + (ch - '5')), pos)); break;
                    case 'm': case 'n': case 'q': case 'r':
                        if (!patrols.ContainsKey(ch)) patrols[ch] = new List<Vector2>();
                        patrols[ch].Add(pos);
                        break;
                }
            }

        MoveSpawn(spawn);

        foreach (var kv in doorCells)
        {
            int minR = kv.Value.Min(x => x.r), maxR = kv.Value.Max(x => x.r);
            int minC = kv.Value.Min(x => x.c), maxC = kv.Value.Max(x => x.c);
            Vector2 center = (World(minR, minC) + World(maxR, maxC)) * 0.5f;
            Vector2 size = new Vector2(maxC - minC + 1, maxR - minR + 1);
            doors[kv.Key] = MakeDoor($"Door_{kv.Key}", center, size);
        }

        foreach (var (kind, door, pos) in openers)
        {
            doors.TryGetValue(door, out Door target);
            switch (kind)
            {
                case 'k':
                    var key = Place(P("Assets/Prefabs/Key.prefab"), $"Key_{door}", pos);
                    Link(key.GetComponent<Key>(), "linkedDoor", target);
                    break;
                case 'l':
                    var lever = Place(P("Assets/Prefabs/Lever.prefab"), $"Lever_{door}", pos);
                    Link(lever.GetComponent<Lever>(), "linkedDoor", target);
                    // El prefab trae un cuadrado blanco genérico: sprite de palanca real y brillo.
                    var lsr = lever.GetComponent<SpriteRenderer>();
                    if (lsr != null) { lsr.sprite = Sprite("lever") ?? lsr.sprite; lsr.color = Color.white; }
                    lever.transform.localScale = Vector3.one;
                    Highlight(lever);
                    break;
                case 'p':
                    var plate = Place(P("Assets/Prefabs/PressurePlate.prefab"), $"PressurePlate_{door}", pos);
                    var so = new SerializedObject(plate.GetComponent<PressurePlate>());
                    so.FindProperty("linkedDoor").objectReferenceValue = target;
                    so.FindProperty("latching").boolValue = true;      // pisarla una vez abre para siempre
                    so.ApplyModifiedPropertiesWithoutUndo();
                    plate.transform.localScale = Vector3.one * 1.6f;
                    Highlight(plate);
                    break;
            }
        }

        for (int i = 0; i < spikes.Count; i++)
        {
            var trap = Place(P("Assets/Prefabs/SpikeTrap.prefab"), $"SpikeTrap_{i + 1}", spikes[i]);
            trap.transform.localScale = Vector3.one * 1.5f;          // pinchos grandes: se ven venir
            var so = new SerializedObject(trap.GetComponent<SpikeTrap>());
            so.FindProperty("startOffset").floatValue = i * 1.0f;   // periodo 3 s: nunca todas fuera a la vez
            so.FindProperty("risingSeconds").floatValue = 0.45f;   // aviso más largo antes de clavarse
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        int gi = 0;
        foreach (var (r, c) in statics)
        {
            var g = Place(P("Assets/Prefabs/Guard_Static.prefab"), $"Guard_Static_{++gi}", World(r, c));
            Vector2 facing = OpenestDirection(map, r, c);
            g.transform.rotation = Quaternion.Euler(0f, 0f, VectorMath.DirectionToAngle(facing));
        }

        foreach (var kv in patrols)
        {
            var points = OrderAroundCentroid(kv.Value);
            var wps = new Object[points.Count];
            for (int i = 0; i < points.Count; i++)
            {
                var wp = new GameObject($"Waypoint_{kv.Key}_{i + 1}");
                wp.transform.position = points[i];
                wps[i] = wp.transform;
            }
            var g = Place(P("Assets/Prefabs/Guard_Patrol.prefab"), $"Guard_Patrol_{kv.Key}", points[0]);
            LinkArray(g.GetComponent<GuardPatrol>(), "waypoints", wps);
        }

        var exitGo = Revive(t.Exit, "ExitTrigger");
        if (exitGo == null)
        {
            exitGo = new GameObject("ExitTrigger");
            var box = exitGo.AddComponent<BoxCollider2D>(); box.isTrigger = true;
            exitGo.AddComponent<ExitTrigger>();
        }
        exitGo.transform.position = exit;
        var eso = new SerializedObject(exitGo.GetComponent<ExitTrigger>());
        eso.FindProperty("isFinalExit").boolValue = index == Maps.Length - 1;
        eso.ApplyModifiedPropertiesWithoutUndo();

        foreach (var tmp in new[] { t.Wall, t.Floor, t.Exit })
            if (tmp != null && tmp.name.StartsWith("__")) Object.DestroyImmediate(tmp);

        EditorSceneManager.MarkSceneDirty(s);
        EditorSceneManager.SaveScene(s);
        Debug.Log($"[LevelDesigner] {sceneName} — {Titles[index]} construida ({w}x{h}).");
    }

    /// <summary>Muros: tramos horizontales de '#' y, para los de una celda, tramos verticales.</summary>
    static void BuildWalls(string[] map, GameObject wallTemplate, System.Func<int, int, Vector2> world, Color tint)
    {
        int h = map.Length, w = map[0].Length;
        var used = new bool[h, w];
        int n = 0;

        for (int r = 0; r < h; r++)
        {
            int c = 0;
            while (c < w)
            {
                if (map[r][c] != '#') { c++; continue; }
                int start = c;
                while (c < w && map[r][c] == '#') c++;
                int len = c - start;
                if (len == 1) continue;                      // se resuelve en la pasada vertical
                for (int k = start; k < c; k++) used[r, k] = true;
                Vector2 center = (world(r, start) + world(r, c - 1)) * 0.5f;
                MakeWall(wallTemplate, $"Wall_{++n}", center, new Vector2(len, 1f), tint);
            }
        }
        for (int c = 0; c < w; c++)
        {
            int r = 0;
            while (r < h)
            {
                if (map[r][c] != '#' || used[r, c]) { r++; continue; }
                int start = r;
                while (r < h && map[r][c] == '#' && !used[r, c]) r++;
                int len = r - start;
                for (int k = start; k < r; k++) used[k, c] = true;
                Vector2 center = (world(start, c) + world(r - 1, c)) * 0.5f;
                MakeWall(wallTemplate, $"Wall_{++n}", center, new Vector2(1f, len), tint);
            }
        }
    }

    static Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Sprites/Game/{name}.png");

    static GameObject MakeWall(GameObject template, string name, Vector2 pos, Vector2 size, Color tint)
    {
        GameObject go;
        if (template != null) { go = Object.Instantiate(template); go.SetActive(true); go.hideFlags = HideFlags.None; }
        else go = new GameObject(name, typeof(BoxCollider2D));
        go.name = name;
        go.layer = CollisionLayers.Walls;
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        go.transform.localScale = Vector3.one;
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr == null) sr = go.AddComponent<SpriteRenderer>();
        // Ladrillo de 64 px a 64 px/unidad: un ladrillo por celda, sin oscurecer, para
        // que muro y suelo se distingan a simple vista.
        sr.sprite = Sprite("wall") ?? sr.sprite;
        sr.color = tint;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = size;
        var box = go.GetComponent<BoxCollider2D>();
        if (box == null) box = go.AddComponent<BoxCollider2D>();
        box.size = size; box.offset = Vector2.zero;
        return go;
    }

    /// <summary>
    /// Puerta ajustada a sus celdas. El sprite de la puerta mide 1x3 unidades y el
    /// collider del prefab es diminuto, así que la escala se calcula desde el sprite y
    /// el collider se fija después para cubrir exactamente el hueco. En un muro
    /// horizontal (varias celdas en fila) la puerta se tumba 90°.
    /// </summary>
    static Door MakeDoor(string name, Vector2 center, Vector2 cells)
    {
        var go = Place(P("Assets/Prefabs/Door.prefab"), name, center);
        var sr = go.GetComponent<SpriteRenderer>();
        Vector2 sprite = sr != null && sr.sprite != null ? (Vector2)sr.sprite.bounds.size : new Vector2(1f, 3f);
        if (sprite.x < 0.01f) sprite.x = 1f;
        if (sprite.y < 0.01f) sprite.y = 3f;

        bool horizontal = cells.x > cells.y;
        Vector2 scale;
        if (horizontal)
        {
            go.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            scale = new Vector2(cells.y / sprite.x, cells.x / sprite.y);
        }
        else
        {
            go.transform.rotation = Quaternion.identity;
            // Un poco más alta que el hueco para que parezca encajada en el muro.
            scale = new Vector2(cells.x / sprite.x, (cells.y + 0.8f) / sprite.y);
        }
        go.transform.localScale = new Vector3(scale.x, scale.y, 1f);

        var box = go.GetComponent<BoxCollider2D>();
        if (box != null)
        {
            // Tamaño local = celdas del hueco divididas por la escala (tras la rotación).
            box.size = horizontal ? new Vector2(cells.y / scale.x, cells.x / scale.y)
                                  : new Vector2(cells.x / scale.x, cells.y / scale.y);
            box.offset = Vector2.zero;
        }
        return go.GetComponent<Door>();
    }

    /// <summary>Dirección cardinal con más celdas de suelo seguidas desde la posición del guardia.</summary>
    static Vector2 OpenestDirection(string[] map, int r, int c)
    {
        (Vector2 dir, int dr, int dc)[] dirs = { (Vector2.up, -1, 0), (Vector2.down, 1, 0), (Vector2.left, 0, -1), (Vector2.right, 0, 1) };
        Vector2 best = Vector2.down; int bestLen = -1;
        foreach (var (dir, dr, dc) in dirs)
        {
            int len = 0, rr = r + dr, cc = c + dc;
            while (rr >= 0 && cc >= 0 && rr < map.Length && cc < map[0].Length && map[rr][cc] != '#') { len++; rr += dr; cc += dc; }
            if (len > bestLen) { bestLen = len; best = dir; }
        }
        return best;
    }

    /// <summary>Ordena los puntos de una ruta en sentido circular alrededor de su centro.</summary>
    static List<Vector2> OrderAroundCentroid(List<Vector2> points)
    {
        if (points.Count <= 2) return points;
        Vector2 centroid = Vector2.zero;
        foreach (var p in points) centroid += p;
        centroid /= points.Count;
        return points.OrderBy(p => Mathf.Atan2(p.y - centroid.y, p.x - centroid.x)).ToList();
    }

    /// <summary>Brillo pulsante y balanceo suave: marca "esto se puede usar".</summary>
    static void Highlight(GameObject go)
    {
        if (go.GetComponent<GlowPulse>() == null) go.AddComponent<GlowPulse>();
        if (go.GetComponent<FloatBob>() == null) go.AddComponent<FloatBob>();
    }

    static void Torch(string name, Vector2 pos, Color color)
    {
        var go = new GameObject(name);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        var l = go.AddComponent<Light2D>();
        l.lightType = Light2D.LightType.Point;
        l.color = color;
        l.intensity = 1.3f;
        l.pointLightOuterRadius = 4.5f;
        l.pointLightInnerRadius = 0.4f;
        l.falloffIntensity = 0.5f;
        l.shadowsEnabled = true;
    }

    // ----------------------------------------------------------------- utilidades

    static GameObject Detach(GameObject src, string tmpName)
    {
        if (src == null) return null;
        var copy = Object.Instantiate(src);
        copy.name = tmpName;
        copy.SetActive(false);
        return copy;
    }

    static GameObject Revive(GameObject tmp, string name)
    {
        if (tmp == null) return null;
        var go = Object.Instantiate(tmp);
        go.name = name;
        go.SetActive(true);
        go.hideFlags = HideFlags.None;
        return go;
    }

    static void Strip(Scene s)
    {
        foreach (var go in s.GetRootGameObjects())
        {
            string n = go.name;
            if (n.StartsWith("__")) continue;
            if (n.StartsWith("Wall_") || n.StartsWith("Corner_") || n.StartsWith("Guard_") || n.StartsWith("Waypoint_") ||
                n.StartsWith("PointLight_") || n.StartsWith("SpikeTrap") || n.StartsWith("Stone") || n.StartsWith("Lever") ||
                n.StartsWith("Key") || n.StartsWith("Door") || n.StartsWith("PressurePlate") || n == "ExitTrigger" || n == "FloorTiled")
                Object.DestroyImmediate(go);
        }
    }

    static GameObject Place(GameObject prefab, string name, Vector2 pos)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = name;
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        return go;
    }

    static void Link(Component target, string field, Object value)
    {
        if (target == null) return;
        var so = new SerializedObject(target);
        var p = so.FindProperty(field);
        if (p == null) return;
        p.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void LinkArray(Component target, string field, Object[] values)
    {
        var so = new SerializedObject(target);
        var p = so.FindProperty(field);
        if (p == null || !p.isArray) return;
        p.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void MoveSpawn(Vector2 pos)
    {
        var sp = Object.FindAnyObjectByType<SpawnPoint>();
        if (sp != null) sp.transform.position = new Vector3(pos.x, pos.y, 0f);
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) player.transform.position = new Vector3(pos.x, pos.y, 0f);
    }
}
#endif
