using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Panel de diagnóstico para la sustentación: <b>F1</b> lo muestra y lo oculta.
///
/// El problema de demostrar colisiones en vivo es que casi todo lo interesante es
/// invisible: en qué fase va cada trampa, cuántos cuerpos hay sobre una placa, si el
/// guardia está alerta o solo mirando, si el héroe está rozando un muro o parado.
/// Este panel lee ese estado y lo escribe en pantalla, sin tocar la lógica del juego:
/// solo consulta propiedades públicas.
///
/// Se autocrea con <c>[RuntimeInitializeOnLoadMethod]</c> igual que el GameManager,
/// así que funciona en cualquier sala sin añadirlo a ninguna escena. Arranca oculto:
/// nadie ve un HUD de depuración si no lo pide.
/// </summary>
public class DemoOverlay : MonoBehaviour
{
    const float RefreshSeconds = 0.1f;

    static DemoOverlay _instance;

    bool _visible;
    float _timer;
    string _text = "";
    GUIStyle _style;
    Texture2D _background;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCreate()
    {
        if (_instance != null) return;
        var go = new GameObject("DemoOverlay");
        _instance = go.AddComponent<DemoOverlay>();
        DontDestroyOnLoad(go);
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && kb.f1Key.wasPressedThisFrame) _visible = !_visible;
        if (!_visible) return;

        _timer -= Time.unscaledDeltaTime;
        if (_timer > 0f) return;
        _timer = RefreshSeconds;
        _text = Compose();
    }

    string Compose()
    {
        var sb = new StringBuilder();

        if (GameManager.Instance != null)
            sb.AppendLine($"SALA {GameManager.Instance.CurrentLevel}   vidas {GameManager.Instance.Lives}   " +
                          $"t {GameProgress.FormatTime(GameManager.Instance.RoomTime)}");

        var body = FindPlayerBody();
        if (body != null)
        {
            float v = body.linearVelocity.magnitude;
            sb.AppendLine($"heroe    v = {v:0.00} u/s   {(v < 0.05f ? "detenido" : "en movimiento")}");
        }

        var guards = Object.FindObjectsByType<GuardBase>(FindObjectsSortMode.None);
        for (int i = 0; i < guards.Length; i++)
        {
            var g = guards[i];
            string estado = g.IsSeeingPlayer ? "TE VE" : (g.IsAlerted ? "alerta" : "normal");
            sb.AppendLine($"guardia  {g.name,-18} {estado}");
        }

        var traps = Object.FindObjectsByType<SpikeTrap>(FindObjectsSortMode.None);
        for (int i = 0; i < traps.Length; i++)
        {
            var t = traps[i];
            sb.AppendLine($"trampa   {t.name,-18} {PhaseLabel(t.CurrentPhase)}");
        }

        var plates = Object.FindObjectsByType<PressurePlate>(FindObjectsSortMode.None);
        for (int i = 0; i < plates.Length; i++)
        {
            var pl = plates[i];
            sb.AppendLine($"placa    {pl.name,-18} {(pl.IsPressed ? "PISADA" : "libre")}   " +
                          $"colliders encima: {pl.OccupantCount}");
        }

        sb.AppendLine();
        sb.AppendLine("gravedad 2D " + Physics2D.gravity + "   ·   F1 cierra este panel");
        return sb.ToString();
    }

    /// <summary>Etiqueta legible de la fase; marca en mayúsculas la única que mata.</summary>
    public static string PhaseLabel(SpikeTrap.Phase phase) => phase switch
    {
        SpikeTrap.Phase.Hidden => "oculta",
        SpikeTrap.Phase.Rising => "subiendo",
        SpikeTrap.Phase.Extended => "CLAVADA (mata)",
        _ => "bajando"
    };

    static Rigidbody2D FindPlayerBody()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        return player != null ? player.GetComponent<Rigidbody2D>() : null;
    }

    void OnGUI()
    {
        if (!_visible) return;
        EnsureStyle();

        var size = _style.CalcSize(new GUIContent(_text));
        var rect = new Rect(12f, 12f, size.x + 24f, size.y + 20f);
        GUI.DrawTexture(rect, _background, ScaleMode.StretchToFill);
        GUI.Label(new Rect(rect.x + 12f, rect.y + 10f, rect.width, rect.height), _text, _style);
    }

    void EnsureStyle()
    {
        if (_style != null) return;
        _style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            richText = false,
            alignment = TextAnchor.UpperLeft
        };
        _style.normal.textColor = new Color(0.93f, 0.91f, 0.84f);

        _background = new Texture2D(1, 1);
        _background.SetPixel(0, 0, new Color(0.07f, 0.07f, 0.09f, 0.82f));
        _background.Apply();
    }

    void OnDestroy()
    {
        if (_background != null) Destroy(_background);
        if (_instance == this) _instance = null;
    }
}
