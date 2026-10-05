using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Piloto automático para grabar la demo de la Semana 08 en <c>Room_Demo</c> sin
/// intervención humana. No modifica el gameplay: mueve al héroe fijando la velocidad
/// de su Rigidbody2D después de <see cref="PlayerMovement"/> y usa las mismas
/// acciones públicas que las teclas E y F (inventario, piedra, interactuables).
///
/// Recorrido (coordenadas de <c>Semana04Builder.BuildDemoRoom</c>):
///   spawn → piedra → lanzar al rincón lejano (el guardia patrulla investiga)
///   → rodear el bloque → pasar por la espalda del guardia fijo → palanca
///   → placa → puerta abierta → salida.
/// </summary>
[DefaultExecutionOrder(500)]
public class DemoAutopilot : MonoBehaviour
{
    const float Speed = 3.2f;
    const float ArriveRadius = 0.15f;

    Rigidbody2D _rb;
    Animator _animator;
    PlayerInteraction _interaction;
    PlayerInventory _inventory;
    Collider2D _ownCollider;
    Vector2 _desired;

    /// <summary>Se activa cuando el recorrido termina (lo lee el script de grabación).</summary>
    public static bool Finished { get; private set; }
    public static bool Started { get; private set; }

    /// <summary>
    /// Carpeta donde guardar un JPG por fotograma de la vista Game, o null para no
    /// grabar. Con <see cref="Time.captureFramerate"/> fijo, cada fotograma avanza
    /// exactamente 1/FPS s de juego: el video sale fluido aunque el editor vaya lento
    /// o esté en segundo plano, y nunca se captura nada fuera del juego.
    /// </summary>
    public static string RecordFolder;
    public const int RecordFps = 24;
    public static int FramesWritten { get; private set; }

    void Awake()
    {
        Application.runInBackground = true;
        FramesWritten = 0;
        if (string.IsNullOrEmpty(RecordFolder)) return;
        Directory.CreateDirectory(RecordFolder);
        Time.captureFramerate = RecordFps;
        StartCoroutine(Record());
    }

    IEnumerator Record()
    {
        var eof = new WaitForEndOfFrame();
        while (true)
        {
            yield return eof;
            // En el editor la vista Game a veces no entrega imagen (repintado de la UI):
            // ese fotograma se omite en vez de dejar que la excepción corte la grabación.
            Texture2D tex = null;
            try
            {
                tex = ScreenCapture.CaptureScreenshotAsTexture();
                if (tex == null || tex.width < 16) continue;
                File.WriteAllBytes(Path.Combine(RecordFolder, $"{FramesWritten:00000}.jpg"), tex.EncodeToJPG(88));
                FramesWritten++;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[DemoAutopilot] Fotograma omitido: {e.Message}");
            }
            finally
            {
                if (tex != null) Destroy(tex);
            }
        }
    }

    void OnDestroy() => Time.captureFramerate = 0;

    IEnumerator Start()
    {
        Finished = false;
        Started = false;
        GameObject player = null;
        while (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            yield return null;
        }
        _rb = player.GetComponent<Rigidbody2D>();
        _animator = player.GetComponentInChildren<Animator>();
        _interaction = player.GetComponent<PlayerInteraction>();
        _inventory = player.GetComponent<PlayerInventory>();
        foreach (var c in player.GetComponents<Collider2D>())
            if (!c.isTrigger) { _ownCollider = c; break; }

        // Esperar al cartel de entrada de la sala (tiempo de juego: con captureFramerate
        // el tiempo real no corresponde con lo que se ve en el video).
        Started = true;
        yield return new WaitForSeconds(2.5f);
        SetOverlay(true);
        yield return new WaitForSeconds(2.0f);

        // 1 · Recoger la piedra (E).
        yield return Go(-6.5f, -3.2f);
        yield return Go(-6.8f, -2.8f);
        yield return Pause(0.8f);
        Interact();
        yield return Pause(1.2f);

        // 2 · Lanzar la piedra (F) al rincón superior izquierdo: ruido lejano.
        Face(new Vector2(-0.3f, 1f));
        ThrowAt(new Vector2(-6.5f, 4f));
        yield return Pause(3.0f);

        // 3 · Rodear el bloque deslizante por arriba (vista del parallax en marcha).
        yield return Go(-5.2f, -0.8f);
        yield return Go(-2.6f, -0.8f);
        yield return Pause(0.6f);
        // Ida y vuelta en horizontal: las grietas se quedan atrás, la niebla cruza al revés.
        yield return Go(-4.8f, -0.8f);
        yield return Pause(0.5f);
        yield return Go(-2.2f, -0.8f);
        yield return Pause(0.8f);

        // 4 · Bajar y cruzar por la espalda del guardia fijo, lejos de los pinchos.
        yield return Go(-1.6f, -2.5f);
        yield return Go(3.4f, -2.6f);
        yield return Pause(0.5f);

        // 5 · Palanca (E): abre la puerta.
        Interact();
        yield return Pause(1.2f);

        // 6 · Puerta y salida. La placa de esta sala NO es de enclavamiento: si el héroe
        //     la pisa y sale, vuelve a cerrar la puerta que abrió la palanca. Se rodea.
        yield return Go(4.3f, -3.3f);
        yield return Go(4.6f, -4.0f);
        yield return Go(6.8f, -4.0f);
        yield return Pause(1.5f);

        _desired = Vector2.zero;
        Finished = true;
    }

    void FixedUpdate()
    {
        if (_rb == null || !Started) return;
        _rb.linearVelocity = _desired;
        if (_animator != null)
        {
            if (_desired != Vector2.zero)
            {
                Vector2 f = PlayerMovement.SnapFacing(_desired);
                _animator.SetFloat("MoveX", f.x);
                _animator.SetFloat("MoveY", f.y);
            }
            _animator.SetFloat("Speed", _desired.magnitude);
        }
    }

    IEnumerator Go(float x, float y)
    {
        var target = new Vector2(x, y);
        float timeout = Time.time + 6f;
        while (_rb != null && Time.time < timeout)
        {
            Vector2 delta = target - _rb.position;
            if (delta.magnitude <= ArriveRadius) break;
            _desired = delta.normalized * Mathf.Min(Speed, delta.magnitude / Time.fixedDeltaTime);
            yield return new WaitForFixedUpdate();
        }
        _desired = Vector2.zero;
    }

    IEnumerator Pause(float seconds)
    {
        _desired = Vector2.zero;
        yield return new WaitForSeconds(seconds);
    }

    void Face(Vector2 dir)
    {
        if (_animator == null) return;
        Vector2 f = PlayerMovement.SnapFacing(dir);
        _animator.SetFloat("MoveX", f.x);
        _animator.SetFloat("MoveY", f.y);
    }

    /// <summary>Lo mismo que pulsar E: acciona el objetivo que el sensor tiene elegido.</summary>
    void Interact()
    {
        if (_interaction == null) return;
        typeof(PlayerInteraction)
            .GetMethod("TryInteract", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.Invoke(_interaction, null);
    }

    /// <summary>Lo mismo que pulsar F con el cursor sobre <paramref name="target"/>.</summary>
    void ThrowAt(Vector2 target)
    {
        if (_inventory == null || !_inventory.HasItem<Stone>()) return;
        var stone = _inventory.TakeItem() as Stone;
        stone.Throw(_rb.position, target, _ownCollider);
    }

    static void SetOverlay(bool visible)
    {
        var overlay = FindAnyObjectByType<DemoOverlay>();
        if (overlay == null) return;
        typeof(DemoOverlay)
            .GetField("_visible", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(overlay, visible);
    }
}
