using UnityEngine;

public class Lever : MonoBehaviour, IInteractable
{
    [SerializeField] Door linkedDoor;

    public void Interact(PlayerInventory inventory) => linkedDoor.Toggle();
}
