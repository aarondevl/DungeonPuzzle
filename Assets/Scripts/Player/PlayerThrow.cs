using UnityEngine;
using UnityEngine.InputSystem; // Requerido por tu proyecto

public class PlayerThrow : MonoBehaviour
{
    [Tooltip("Arrastra aquí el Prefab de la piedra desde tu panel Project")]
    [SerializeField] GameObject stonePrefab; 
    [SerializeField] float throwForce = 8f;
    
    Camera _cam;

    void Awake()
    {
        _cam = Camera.main;
    }

    void Update()
    {
        // Detecta la tecla F usando el Nuevo Input System
        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            LanzarPiedra();
        }
    }

    void LanzarPiedra()
    {
        if (stonePrefab == null) return;

        // Obtiene la posición del ratón en la pantalla
        Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
        
        // La convierte a coordenadas del mundo 2D
        Vector3 mousePos = _cam.ScreenToWorldPoint(new Vector3(mouseScreenPosition.x, mouseScreenPosition.y, _cam.nearClipPlane));
        mousePos.z = 0;
        
        // Calcula la dirección del tiro
        Vector2 direccion = (mousePos - transform.position).normalized;

        // Calcula una posición de aparición separada del jugador para evitar colisión (margen de 0.8 unidades)
        Vector3 posicionSpawn = transform.position + (Vector3)direccion * 0.8f;

        // Crea la piedra en la nueva posición separada
        GameObject nuevaPiedra = Instantiate(stonePrefab, posicionSpawn, Quaternion.identity);
        Rigidbody2D rb = nuevaPiedra.GetComponent<Rigidbody2D>();
        
        if (rb != null)
        {
            rb.linearVelocity = direccion * throwForce;
        }
    }
}