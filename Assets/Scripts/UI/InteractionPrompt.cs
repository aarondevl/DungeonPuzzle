using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Letrero flotante que dice qué tecla pulsar sobre el objeto que el héroe tiene a
/// mano ("E  RECOGER LLAVE", "E  PALANCA", "PISA LA PLACA") y, si lleva una piedra,
/// "F  LANZAR" junto al héroe. Se autocrea, sobrevive a los cambios de escena y no
/// necesita nada en el HUD de cada sala.
///
/// Coordenadas: la posición del objeto está en el MUNDO; se pasa a PANTALLA con
/// Camera.WorldToScreenPoint y de ahí al espacio del canvas escalado.
/// </summary>
public class InteractionPrompt : MonoBehaviour
{
    static InteractionPrompt _instance;

    static readonly Color Cream = new Color(0.95f, 0.92f, 0.82f, 1f);

    RectTransform _canvasRect;
    RectTransform _label;
    TextMeshProUGUI _text;
    Image _band;
    RectTransform _throwLabel;
    TextMeshProUGUI _throwText;
    PlayerInteraction _player;
    PlayerInventory _inventory;
    float _searchTimer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AutoCreate()
    {
        if (_instance != null) return;
        var go = new GameObject("InteractionPrompt");
        _instance = go.AddComponent<InteractionPrompt>();
        DontDestroyOnLoad(go);
    }

    void Awake()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        _canvasRect = (RectTransform)transform;

        _label = MakeLabel("Prompt", out _text, out _band);
        _throwLabel = MakeLabel("Throw", out _throwText, out _);
        _label.gameObject.SetActive(false);
        _throwLabel.gameObject.SetActive(false);
    }

    RectTransform MakeLabel(string name, out TextMeshProUGUI text, out Image band)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(transform, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(360f, 44f);

        band = go.AddComponent<Image>();
        band.color = new Color(0f, 0f, 0f, 0.6f);
        band.raycastTarget = false;

        var tgo = new GameObject("Text", typeof(RectTransform));
        tgo.transform.SetParent(go.transform, false);
        var trt = (RectTransform)tgo.transform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(12f, 0f); trt.offsetMax = new Vector2(-12f, 0f);
        text = tgo.AddComponent<TextMeshProUGUI>();
        text.fontSize = 26f;
        text.color = Cream;
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Bold;
        text.characterSpacing = 4f;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        return rt;
    }

    void Update()
    {
        if (_player == null)
        {
            _searchTimer -= Time.unscaledDeltaTime;
            if (_searchTimer > 0f) return;
            _searchTimer = 0.5f;
            _player = FindAnyObjectByType<PlayerInteraction>();
            _inventory = _player != null ? _player.GetComponent<PlayerInventory>() : null;
            if (_player == null) { Hide(); return; }
        }

        bool busy = GameManager.Instance != null && (GameManager.Instance.IsTransitioning || GameManager.Instance.IsPaused);
        var cam = Camera.main;
        if (busy || cam == null) { Hide(); return; }

        // Objeto a mano: lo que PlayerInteraction accionaría con E ahora mismo.
        Component target = _player.CurrentTarget;
        string prompt = null;
        Vector3 anchor = Vector3.zero;
        if (target != null)
        {
            prompt = PromptFor(target);
            anchor = target.transform.position;
        }
        else if (_player.Sensor != null)
        {
            var plate = _player.Sensor.Closest<PressurePlate>(CollisionLayers.InteractableMask);
            if (plate != null && !plate.IsPressed) { prompt = "PISA LA PLACA"; anchor = plate.transform.position; }
        }

        Show(_label, _text, prompt, cam, anchor + Vector3.up * 0.7f);

        // Piedra en mano: recordatorio de la tecla de lanzar junto al héroe.
        bool holdsStone = _inventory != null && _inventory.HasItem<Stone>();
        Show(_throwLabel, _throwText, holdsStone ? "F  LANZAR PIEDRA  ·  APUNTA CON EL RATÓN" : null,
            cam, _player.transform.position + Vector3.down * 0.9f);
    }

    /// <summary>Texto según el tipo de objeto. Función pura, cubierta por tests.</summary>
    public static string PromptFor(Component target) => target switch
    {
        Prisoner => "E  LIBERAR AL PRESO (SEÑUELO)",
        Key => "E  RECOGER LLAVE",
        Stone => "E  RECOGER PIEDRA",
        Lever => "E  ACCIONAR PALANCA",
        PickupItem => "E  RECOGER",
        IInteractable => "E  USAR",
        _ => null,
    };

    void Show(RectTransform label, TextMeshProUGUI text, string content, Camera cam, Vector3 worldPos)
    {
        if (string.IsNullOrEmpty(content)) { label.gameObject.SetActive(false); return; }
        text.text = content;
        // Mundo → pantalla → canvas.
        Vector2 screen = cam.WorldToScreenPoint(worldPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screen, null, out Vector2 local);
        label.anchoredPosition = local;
        label.sizeDelta = new Vector2(Mathf.Max(200f, text.preferredWidth + 40f), 44f);
        label.gameObject.SetActive(true);
    }

    void Hide()
    {
        if (_label != null) _label.gameObject.SetActive(false);
        if (_throwLabel != null) _throwLabel.gameObject.SetActive(false);
    }
}
