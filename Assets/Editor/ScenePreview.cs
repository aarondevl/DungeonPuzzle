#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Fotografía cada sala desde su cámara y guarda un PNG por escena. Sirve para revisar
/// el aspecto de los mapas sin abrir el editor a mano (también en batch:
/// <c>-executeMethod ScenePreview.RenderAll</c>; escribe en la carpeta indicada por
/// la variable de entorno DP_PREVIEW_DIR o, si no existe, en <c>Previews/</c>).
/// </summary>
public static class ScenePreview
{
    static readonly string[] Rooms = { "Room_01", "Room_02", "Room_03", "Room_04", "Room_05", "GameOver", "MainMenu" };

    [MenuItem("DungeonPuzzle/Niveles/Fotografiar salas")]
    public static void RenderAll()
    {
        string dir = System.Environment.GetEnvironmentVariable("DP_PREVIEW_DIR");
        if (string.IsNullOrEmpty(dir)) dir = Path.Combine(Directory.GetCurrentDirectory(), "Previews");
        Directory.CreateDirectory(dir);

        foreach (string room in Rooms)
        {
            EditorSceneManager.OpenScene($"Assets/Scenes/{room}.unity", OpenSceneMode.Single);
            var cam = Camera.main;
            if (cam == null) { Debug.LogWarning($"[ScenePreview] {room}: sin cámara"); continue; }

            const int w = 1280, h = 720;
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            var prev = cam.targetTexture;
            cam.targetTexture = rt;
            // En batch la primera pasada puede salir con texturas sin subir (cuadros
            // grises o sprites cruzados): se renderiza varias veces y se guarda la última.
            for (int pass = 0; pass < 4; pass++) cam.Render();
            cam.targetTexture = prev;

            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            var active = RenderTexture.active;
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = active;

            string path = Path.Combine(dir, room + ".png");
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            rt.Release();
            Object.DestroyImmediate(rt);
            Debug.Log($"[ScenePreview] {room} -> {path}");
        }
    }
}
#endif
