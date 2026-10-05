#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Demo automática de la Semana 08 para grabar el gameplay.
///
/// Menú <b>DungeonPuzzle ▸ Semana 08 ▸ Demo automática</b>, o crear el archivo
/// <c>Temp/semana08_demo.trigger</c> desde fuera de Unity. En ambos casos:
///   1. ejecuta <see cref="Semana08Builder.BuildAll"/> (parallax en las salas),
///   2. abre <c>Room_Demo</c>, maximiza la vista Game y entra en Play,
///   3. añade <see cref="DemoAutopilot"/>, que recorre la sala solo,
///   4. escribe <c>Temp/semana08_demo.started</c> y <c>.done</c> (con la carpeta de
///      fotogramas) y sale de Play al terminar. La grabación la hace Unity sobre la
///      vista Game: no captura el escritorio.
/// </summary>
[InitializeOnLoad]
public static class Semana08Demo
{
    const string ScenePath = "Assets/Scenes/Room_Demo.unity";
    const string ActiveKey = "Semana08Demo.Active";
    const string RecordKey = "Semana08Demo.RecordFolder";
    static readonly string TempDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp"));
    static string TriggerFile => Path.Combine(TempDir, "semana08_demo.trigger");
    static string StartedFile => Path.Combine(TempDir, "semana08_demo.started");
    static string DoneFile => Path.Combine(TempDir, "semana08_demo.done");

    static double _finishedAt = -1;

    static Semana08Demo()
    {
        EditorApplication.update += Poll;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    [MenuItem("DungeonPuzzle/Semana 08/Demo automática (para grabar)")]
    public static void RunMenu() => Run(true);

    /// <param name="record">Graba la vista Game en Vídeos/DungeonPuzzle/frames_FECHA.</param>
    public static void Run(bool record)
    {
        string folder = record
            ? Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyVideos),
                "DungeonPuzzle", "frames_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss"))
            : "";
        SessionState.SetString(RecordKey, folder);
        File.Delete(StartedFile);
        File.Delete(DoneFile);
        Semana08Builder.BuildAll();
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        SessionState.SetBool(ActiveKey, true);
        MaximizeGameView();
        EditorApplication.EnterPlaymode();
    }

    static void Poll()
    {
        if (!EditorApplication.isPlaying && !EditorApplication.isCompiling && File.Exists(TriggerFile))
        {
            File.Delete(TriggerFile);
            Run(true);
            return;
        }

        if (!EditorApplication.isPlaying || !SessionState.GetBool(ActiveKey, false)) return;

        if (DemoAutopilot.Started && !File.Exists(StartedFile))
            File.WriteAllText(StartedFile, System.DateTime.Now.ToString("O"));

        if (DemoAutopilot.Finished && _finishedAt < 0)
            _finishedAt = EditorApplication.timeSinceStartup;

        // La salida final lleva a la pantalla de victoria: se deja verla 4 s.
        if (_finishedAt > 0 && EditorApplication.timeSinceStartup - _finishedAt > 4.0)
        {
            File.WriteAllText(DoneFile, SessionState.GetString(RecordKey, "") + "|" + DemoAutopilot.FramesWritten);
            _finishedAt = -1;
            SessionState.SetBool(ActiveKey, false);
            EditorApplication.ExitPlaymode();
        }
    }

    static void OnPlayModeChanged(PlayModeStateChange change)
    {
        if (change != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(ActiveKey, false)) return;
        string folder = SessionState.GetString(RecordKey, "");
        DemoAutopilot.RecordFolder = string.IsNullOrEmpty(folder) ? null : folder;
        var go = new GameObject("DemoAutopilot");
        Object.DontDestroyOnLoad(go);
        go.AddComponent<DemoAutopilot>();
    }

    static void MaximizeGameView()
    {
        var type = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        if (type == null) return;
        var view = EditorWindow.GetWindow(type);
        view.Focus();
        view.maximized = true;
    }
}
#endif
