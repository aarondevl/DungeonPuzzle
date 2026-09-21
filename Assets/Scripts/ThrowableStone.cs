using UnityEngine;

public class ThrowableStone : MonoBehaviour
{
    [Tooltip("El radio en el que los guardias pueden escuchar el golpe")]
    [SerializeField] float noiseRadius = 5f;
    
    [Tooltip("La capa (Layer) donde están asignados los guardias")]
    [SerializeField] LayerMask guardLayer;

    [Tooltip("La capa contra la que choca para hacer ruido (ej. Walls)")]
    [SerializeField] LayerMask noiseTriggerLayers;

    [Tooltip("Tiempo en segundos antes de que la piedra se destruya automáticamente")]
    [SerializeField] float tiempoAutodestruccion = 5f;

    private bool _ruidoGenerado = false;

    void Start()
    {
        // Se autodestruye tras X segundos para limpiar la memoria, aunque no choque
        Destroy(gameObject, tiempoAutodestruccion);
    }

    // Se activa cuando el Rigidbody2D de la piedra choca con el suelo o pared
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 1. Verificamos si ya hicimos ruido para evitar cálculos duplicados en el impacto
        if (_ruidoGenerado) return;

        // 2. Comprobamos si el objeto chocado pertenece a la capa "Walls"
        if ((noiseTriggerLayers.value & (1 << collision.gameObject.layer)) > 0)
        {
            GenerarRuido();
            _ruidoGenerado = true; // Bloquea futuros ruidos de esta misma piedra
            
            // Opcional: Destruir la piedra inmediatamente al chocar o dejarla en el piso.
            // Si la comentas, el Start() la destruirá a los 5 segundos de todas formas.
            //Destroy(gameObject, 0.2f);
        }
    }

    void GenerarRuido()
    {
        // Crea un círculo invisible que detecta todos los colliders en la capa de guardias
        Collider2D[] guardsInEarshot = Physics2D.OverlapCircleAll(transform.position, noiseRadius, guardLayer);
        
        foreach (Collider2D guardCollider in guardsInEarshot)
        {
            // Busca el script base del guardia
            GuardBase guardBase = guardCollider.GetComponent<GuardBase>();
            
            if (guardBase != null)
            {
                // Dispara el aviso al guardia
                guardBase.TriggerNoise(transform.position); 
            }
        }
    }

    // Dibuja el radio del sonido en el editor para que puedas ajustarlo visualmente
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, noiseRadius);
    }
}