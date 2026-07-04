using UnityEngine;

/// <summary>
/// Rotación de la llave sobre su propio eje mientras está en el mapa.
/// El juego es 2D top-down, así que la rotación es sobre el eje Z.
/// Transformación aplicada: ROTACIÓN (contraparte de la TRASLACIÓN del jugador).
/// </summary>
public class KeySpin : MonoBehaviour
{
    [Tooltip("Velocidad de giro en grados por segundo. Positivo = antihorario.")]
    [SerializeField] float degreesPerSecond = 90f;

    void Update()
    {
        // Rotación continua e independiente del framerate (Time.deltaTime).
        transform.Rotate(0f, 0f, degreesPerSecond * Time.deltaTime, Space.Self);
    }
}
