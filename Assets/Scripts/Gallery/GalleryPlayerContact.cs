using UnityEngine;

public static class GalleryPlayerContact
{
    public static bool IsPhysicalPlayer(Collider2D collider) =>
        collider != null && !collider.isTrigger && collider.CompareTag("Player");
}
