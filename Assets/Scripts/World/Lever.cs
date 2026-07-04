using UnityEngine;

public class Lever : MonoBehaviour, IInteractable
{
    [SerializeField] Door linkedDoor;

    public void Interact(PlayerInventory inventory)
    {
        SfxLibrary.Play("SFX/lever");
        Vfx.Spark(transform.position);
        linkedDoor.Toggle();
    }
}
