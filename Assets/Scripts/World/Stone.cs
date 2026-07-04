using UnityEngine;

public class Stone : PickupItem
{
    [SerializeField] GameObject thrownStonePrefab;

    public override void OnPickedUp(PlayerInventory inventory) =>
        SfxLibrary.Play("SFX/stone_pickup");

    public void Throw(Vector2 origin, Vector2 target)
    {
        SfxLibrary.Play("SFX/stone_throw");
        GameObject thrown = Instantiate(thrownStonePrefab, origin, Quaternion.identity);
        thrown.GetComponent<ThrownStone>().Launch(target - origin);
        Destroy(gameObject);
    }
}
