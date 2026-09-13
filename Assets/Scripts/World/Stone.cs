using UnityEngine;

public class Stone : PickupItem
{
    [SerializeField] GameObject thrownStonePrefab;
    [Tooltip("Distancia a la que aparece la piedra por delante del lanzador.")]
    [SerializeField] float spawnOffset = 0.55f;

    public override void OnPickedUp(PlayerInventory inventory)
    {
        SfxLibrary.Play("SFX/stone_pickup");
        // Se lanza ANTES de que el inventario desactive la piedra.
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) PickupPop.Spawn(sr, 1.5f, 0.25f);
        inventory.GetComponent<PlayerFeedback>()?.Punch(1.12f, 0.16f);
    }

    /// <param name="thrower">Collider del lanzador; se ignora para que la piedra no lo empuje.</param>
    public void Throw(Vector2 origin, Vector2 target, Collider2D thrower = null)
    {
        SfxLibrary.Play("SFX/stone_throw");
        Vector2 dir = SpawnDirection(origin, target);
        GameObject thrown = Instantiate(thrownStonePrefab, origin + dir * spawnOffset, Quaternion.identity);
        var stone = thrown.GetComponent<ThrownStone>();
        stone.IgnoreThrower(thrower);
        stone.Launch(dir);
        Destroy(gameObject);
    }

    /// <summary>Dirección normalizada del lanzamiento; hacia arriba si origen y destino coinciden.</summary>
    public static Vector2 SpawnDirection(Vector2 origin, Vector2 target)
    {
        Vector2 delta = target - origin;
        return delta.sqrMagnitude < 0.0001f ? Vector2.up : delta.normalized;
    }
}
