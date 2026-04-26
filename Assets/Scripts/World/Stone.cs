using UnityEngine;

public class Stone : PickupItem
{
    [SerializeField] GameObject thrownStonePrefab;

    public void Throw(Vector2 origin, Vector2 target)
    {
        GameObject thrown = Instantiate(thrownStonePrefab, origin, Quaternion.identity);
        thrown.GetComponent<ThrownStone>().Launch(target - origin);
        Destroy(gameObject);
    }
}
