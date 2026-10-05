#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Media;
using UnityEngine;

/// <summary>
/// Convierte la carpeta de fotogramas que graba <see cref="DemoAutopilot"/> en un MP4
/// H.264 con el codificador que trae el propio editor (UnityEditor.Media.MediaEncoder),
/// sin instalar nada.
///
/// Uso: menú <b>DungeonPuzzle ▸ Semana 08 ▸ Codificar última grabación a MP4</b>, o
/// escribir la ruta de la carpeta en <c>Temp/semana08_encode.trigger</c>. Al terminar
/// escribe la ruta del video en <c>Temp/semana08_encode.done</c>.
/// </summary>
[InitializeOnLoad]
public static class Semana08VideoEncoder
{
    const int OutputWidth = 1920;
    const int OutputHeight = 1080;

    static readonly string TempDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp"));
    static string TriggerFile => Path.Combine(TempDir, "semana08_encode.trigger");
    static string DoneFile => Path.Combine(TempDir, "semana08_encode.done");

    static Semana08VideoEncoder() => EditorApplication.update += Poll;

    static void Poll()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
        if (!File.Exists(TriggerFile)) return;
        string folder = File.ReadAllText(TriggerFile).Trim();
        File.Delete(TriggerFile);
        string video = Encode(folder);
        File.WriteAllText(DoneFile, video ?? "ERROR");
    }

    [MenuItem("DungeonPuzzle/Semana 08/Codificar última grabación a MP4")]
    public static void EncodeLatest()
    {
        string root = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyVideos), "DungeonPuzzle");
        var latest = Directory.Exists(root)
            ? new DirectoryInfo(root).GetDirectories("frames_*").OrderByDescending(d => d.Name).FirstOrDefault()
            : null;
        if (latest == null) { Debug.LogError("[Semana08] No hay grabaciones en " + root); return; }
        Encode(latest.FullName);
    }

    public static string Encode(string folder)
    {
        var frames = Directory.GetFiles(folder, "*.jpg").OrderBy(f => f).ToArray();
        if (frames.Length == 0) { Debug.LogError("[Semana08] Carpeta sin fotogramas: " + folder); return null; }

        string name = "DungeonPuzzle_Semana08_" + new DirectoryInfo(folder).Name.Replace("frames_", "") + ".mp4";
        string output = Path.Combine(Directory.GetParent(folder).FullName, name);

        var attrs = new VideoTrackEncoderAttributes(new H264EncoderAttributes
        {
            gopSize = 24,
            numConsecutiveBFrames = 2,
            profile = VideoEncodingProfile.H264High
        })
        {
            frameRate = new MediaRational(DemoAutopilot.RecordFps),
            width = OutputWidth,
            height = OutputHeight,
            targetBitRate = 8_000_000
        };

        var source = new Texture2D(2, 2, TextureFormat.RGB24, false);
        var target = new Texture2D(OutputWidth, OutputHeight, TextureFormat.RGBA32, false);
        var rt = RenderTexture.GetTemporary(OutputWidth, OutputHeight, 0, RenderTextureFormat.ARGB32);
        try
        {
            using (var encoder = new MediaEncoder(output, attrs))
            {
                for (int i = 0; i < frames.Length; i++)
                {
                    if (i % 48 == 0)
                        EditorUtility.DisplayProgressBar("Semana 08", $"Codificando video {i}/{frames.Length}", (float)i / frames.Length);
                    source.LoadImage(File.ReadAllBytes(frames[i]));
                    Graphics.Blit(source, rt);
                    var prev = RenderTexture.active;
                    RenderTexture.active = rt;
                    target.ReadPixels(new Rect(0, 0, OutputWidth, OutputHeight), 0, 0);
                    target.Apply();
                    RenderTexture.active = prev;
                    encoder.AddFrame(target);
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            RenderTexture.ReleaseTemporary(rt);
            Object.DestroyImmediate(source);
            Object.DestroyImmediate(target);
        }

        Debug.Log($"[Semana08] Video generado ({frames.Length} fotogramas a {DemoAutopilot.RecordFps} fps): {output}");
        return output;
    }
}
#endif
