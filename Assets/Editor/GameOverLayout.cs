#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Reordena la pantalla de Game Over con una rejilla clara: título, subtítulo y
/// estadísticas centrados arriba, y tres botones iguales apilados en el centro.
/// La escena original mezclaba anclajes (unos a la esquina superior izquierda,
/// otros al centro) y un canvas de píxeles fijos, por eso nada quedaba alineado.
///
/// Menú DungeonPuzzle → UI → Alinear Game Over, o <c>-executeMethod GameOverLayout.Fix</c>.
/// </summary>
public static class GameOverLayout
{
    const string ScenePath = "Assets/Scenes/GameOver.unity";

    [MenuItem("DungeonPuzzle/UI/Alinear Game Over")]
    public static void Fix()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        var canvas = GameObject.Find("GameOverCanvas");
        if (canvas == null) { Debug.LogError("[GameOverLayout] No existe GameOverCanvas"); return; }

        var scaler = canvas.GetComponent<CanvasScaler>() ?? canvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // Textos centrados, de arriba abajo.
        Center("MessageText", new Vector2(0f, 300f), new Vector2(1400f, 220f), 132f, TextAlignmentOptions.Center);
        Center("Subtitle",    new Vector2(0f, 170f), new Vector2(1400f, 50f),  28f,  TextAlignmentOptions.Center);
        Center("StatsText",   new Vector2(0f, 70f),  new Vector2(1200f, 130f), 30f,  TextAlignmentOptions.Center);
        Line("Divider", new Vector2(0f, -10f), 760f);

        // Botones iguales, apilados.
        Button("ReintentarButton", new Vector2(0f, -90f),  "REINTENTAR");
        Button("MainMenuButton",   new Vector2(0f, -190f), "MENÚ PRINCIPAL");
        Button("SalirButton",      new Vector2(0f, -290f), "SALIR");

        // Pie de página con las teclas.
        var footer = Find("Footer");
        if (footer != null)
        {
            var rt = (RectTransform)footer.transform;
            rt.anchorMin = new Vector2(0.5f, 0f); rt.anchorMax = new Vector2(0.5f, 0f); rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 44f); rt.sizeDelta = new Vector2(1200f, 30f);
            var t = footer.GetComponent<TextMeshProUGUI>();
            if (t != null) { t.text = "R   REINTENTAR          ESC   MENÚ"; t.alignment = TextAlignmentOptions.Center; t.fontSize = 18f; }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[GameOverLayout] GameOver alineada.");
    }

    static GameObject Find(string name)
    {
        var go = GameObject.Find(name);
        if (go == null) Debug.LogWarning($"[GameOverLayout] No existe '{name}'");
        return go;
    }

    static void Center(string name, Vector2 pos, Vector2 size, float fontSize, TextAlignmentOptions align)
    {
        var go = Find(name);
        if (go == null) return;
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var t = go.GetComponent<TextMeshProUGUI>();
        if (t != null)
        {
            t.fontSize = fontSize;
            t.alignment = align;
            t.enableAutoSizing = false;
            t.textWrappingMode = TextWrappingModes.Normal;
        }
    }

    static void Line(string name, Vector2 pos, float width)
    {
        var go = Find(name);
        if (go == null) return;
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(width, 2f);
    }

    static void Button(string name, Vector2 pos, string label)
    {
        var go = Find(name);
        if (go == null) return;
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(560f, 80f);

        var text = go.GetComponentInChildren<TextMeshProUGUI>(true);
        if (text != null)
        {
            var trt = (RectTransform)text.transform;
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
            text.text = label;
            text.fontSize = 34f;
            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = false;
        }
    }
}
#endif
