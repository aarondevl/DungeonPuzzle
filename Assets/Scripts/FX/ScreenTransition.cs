using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Capa de pantalla completa para fundidos a negro y carteles ("SALA 02",
/// "¡TE ATRAPARON!"). Se autocrea al arrancar, sobrevive a los cambios de escena
/// y se dibuja por encima de cualquier HUD (sortingOrder 1000), así que ninguna
/// sala necesita traerla en su escena.
///
/// Todo usa tiempo NO escalado: los fundidos deben seguir corriendo aunque el
/// juego esté en cámara lenta o pausado.
/// </summary>
public class ScreenTransition : MonoBehaviour
{
    public static ScreenTransition Instance { get; private set; }

    static readonly Color Cream = new Color(0.910f, 0.875f, 0.784f, 1f);
    static readonly Color Crimson = new Color(0.722f, 0.137f, 0.173f, 1f);

    Image _black;
    CanvasGroup _banner;
    RectTransform _bannerRect;
    TextMeshProUGUI _title;
    TextMeshProUGUI _subtitle;
    Coroutine _bannerRoutine;

    /// <summary>true mientras la pantalla está total o parcialmente en negro.</summary>
    public bool IsDark => _black != null && _black.color.a > 0.01f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AutoCreate()
    {
        if (Instance != null) return;
        var go = new GameObject("ScreenTransition");
        go.AddComponent<ScreenTransition>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Build();
    }

    void Build()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        _black = NewStretched<Image>("Black", transform);
        _black.color = new Color(0f, 0f, 0f, 0f);
        _black.raycastTarget = false;

        var bannerGo = new GameObject("Banner", typeof(RectTransform));
        bannerGo.transform.SetParent(transform, false);
        _bannerRect = (RectTransform)bannerGo.transform;
        _bannerRect.anchorMin = new Vector2(0.5f, 0.5f);
        _bannerRect.anchorMax = new Vector2(0.5f, 0.5f);
        _bannerRect.sizeDelta = new Vector2(1400f, 300f);
        _banner = bannerGo.AddComponent<CanvasGroup>();
        _banner.alpha = 0f;
        _banner.blocksRaycasts = false;
        _banner.interactable = false;

        var band = NewStretched<Image>("Band", bannerGo.transform);
        band.color = new Color(0f, 0f, 0f, 0.55f);
        band.raycastTarget = false;

        _title = NewText("Title", bannerGo.transform, 88f, Cream, new Vector2(0f, 40f));
        _subtitle = NewText("Subtitle", bannerGo.transform, 34f, Cream, new Vector2(0f, -55f));
    }

    static T NewStretched<T>(string name, Transform parent) where T : Component
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return go.AddComponent<T>();
    }

    static TextMeshProUGUI NewText(string name, Transform parent, float size, Color color, Vector2 offset)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(1f, 0.5f);
        rt.sizeDelta = new Vector2(0f, 120f);
        rt.anchoredPosition = offset;
        var text = go.AddComponent<TextMeshProUGUI>();
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Bold;
        text.characterSpacing = 6f;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        return text;
    }

    // ---------- fundidos ----------

    /// <summary>Oscurece la pantalla. Se puede hacer <c>yield return</c> sobre el resultado.</summary>
    public static Coroutine FadeOut(float seconds) => Instance != null ? Instance.StartCoroutine(Instance.Fade(1f, seconds)) : null;

    /// <summary>Descubre la pantalla desde negro.</summary>
    public static Coroutine FadeIn(float seconds) => Instance != null ? Instance.StartCoroutine(Instance.Fade(0f, seconds)) : null;

    /// <summary>Pone la pantalla en negro al instante (para arrancar una sala fundiendo desde negro).</summary>
    public static void SetDark()
    {
        if (Instance != null) Instance._black.color = Color.black;
    }

    IEnumerator Fade(float targetAlpha, float seconds)
    {
        float start = _black.color.a;
        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(start, targetAlpha, Mathf.Clamp01(t / seconds));
            _black.color = new Color(0f, 0f, 0f, a);
            yield return null;
        }
        _black.color = new Color(0f, 0f, 0f, targetAlpha);
    }

    // ---------- final ----------

    /// <summary>
    /// Secuencia de escape: sobre negro, el héroe cruza la pantalla caminando mientras
    /// aparecen tres frases, y termina con "FIN" y el tiempo total. Los cuadros del
    /// héroe se pasan desde fuera (los saca PlayerFeedback de su animación de andar).
    /// </summary>
    public static Coroutine PlayEnding(Sprite[] walkFrames, string statsLine) =>
        Instance != null ? Instance.StartCoroutine(Instance.Ending(walkFrames, statsLine)) : null;

    IEnumerator Ending(Sprite[] frames, string statsLine)
    {
        _black.color = Color.black;
        HideBanner();

        // Héroe caminando (imagen en el canvas, animada a mano con los cuadros).
        var heroGo = new GameObject("EndingHero", typeof(RectTransform));
        heroGo.transform.SetParent(transform, false);
        var heroRt = (RectTransform)heroGo.transform;
        heroRt.anchorMin = heroRt.anchorMax = new Vector2(0.5f, 0.5f);
        heroRt.sizeDelta = new Vector2(220f, 220f);
        var hero = heroGo.AddComponent<Image>();
        hero.raycastTarget = false;
        hero.preserveAspect = true;
        bool hasFrames = frames != null && frames.Length > 0;
        hero.enabled = hasFrames;

        // Suelo: una línea tenue por la que camina.
        var ground = NewStretched<Image>("EndingGround", transform);
        var grt = (RectTransform)ground.transform;
        grt.anchorMin = new Vector2(0.15f, 0.5f); grt.anchorMax = new Vector2(0.85f, 0.5f);
        grt.offsetMin = new Vector2(0f, -120f); grt.offsetMax = new Vector2(0f, -118f);
        ground.color = new Color(1f, 1f, 1f, 0.15f);
        ground.raycastTarget = false;

        string[] lines =
        {
            "SALISTE DEL CALABOZO",
            "LA NOCHE CUBRE TU HUIDA",
            "VUELVES CON TU FAMILIA",
        };
        const float walkSeconds = 7.5f;
        float t = 0f;
        int line = -1;
        while (t < walkSeconds)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / walkSeconds);
            heroRt.anchoredPosition = new Vector2(Mathf.Lerp(-760f, 760f, u), -20f);
            if (hasFrames) hero.sprite = frames[Mathf.FloorToInt(t * 8f) % frames.Length];

            int wanted = Mathf.Min(lines.Length - 1, Mathf.FloorToInt(u * lines.Length));
            if (wanted != line)
            {
                line = wanted;
                ShowBanner(lines[line], "", walkSeconds / lines.Length - 0.7f);
            }
            yield return null;
        }

        hero.enabled = false;
        ShowBanner("FIN", statsLine, 2.4f);
        yield return new WaitForSecondsRealtime(3.2f);

        Destroy(heroGo);
        Destroy(ground.gameObject);
    }

    // ---------- carteles ----------

    /// <summary>Cartel centrado con título y subtítulo; entra con un golpe de escala y se desvanece solo.</summary>
    public static void ShowBanner(string title, string subtitle, float holdSeconds, bool alarm = false)
    {
        if (Instance == null) return;
        if (Instance._bannerRoutine != null) Instance.StopCoroutine(Instance._bannerRoutine);
        Instance._bannerRoutine = Instance.StartCoroutine(Instance.Banner(title, subtitle, holdSeconds, alarm));
    }

    public static void HideBanner()
    {
        if (Instance == null) return;
        if (Instance._bannerRoutine != null) Instance.StopCoroutine(Instance._bannerRoutine);
        Instance._banner.alpha = 0f;
    }

    IEnumerator Banner(string title, string subtitle, float holdSeconds, bool alarm)
    {
        _title.text = title;
        _title.color = alarm ? Crimson : Cream;
        _subtitle.text = subtitle ?? "";

        const float inSeconds = 0.22f, outSeconds = 0.35f;
        float t = 0f;
        while (t < inSeconds)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / inSeconds);
            _banner.alpha = u;
            // Golpe de escala: entra grande y se asienta (ease-out).
            float s = Mathf.Lerp(1.25f, 1f, 1f - (1f - u) * (1f - u));
            _bannerRect.localScale = Vector3.one * s;
            yield return null;
        }
        _banner.alpha = 1f;
        _bannerRect.localScale = Vector3.one;

        yield return new WaitForSecondsRealtime(holdSeconds);

        t = 0f;
        while (t < outSeconds)
        {
            t += Time.unscaledDeltaTime;
            _banner.alpha = 1f - Mathf.Clamp01(t / outSeconds);
            yield return null;
        }
        _banner.alpha = 0f;
        _bannerRoutine = null;
    }
}
