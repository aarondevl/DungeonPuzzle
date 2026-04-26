using UnityEngine;
using UnityEngine.UI;

public class HUDManager : MonoBehaviour
{
    [SerializeField] Image[] heartIcons;
    [SerializeField] Image inventoryIcon;
    [SerializeField] Sprite heartFull;
    [SerializeField] Sprite heartEmpty;

    PlayerInventory _inventory;

    void Start()
    {
        var player = FindFirstObjectByType<PlayerInventory>();
        if (player != null) _inventory = player;
    }

    void Update()
    {
        UpdateHearts();
        UpdateInventory();
    }

    void UpdateHearts()
    {
        if (GameManager.Instance == null) return;
        int lives = GameManager.Instance.Lives;
        for (int i = 0; i < heartIcons.Length; i++)
            heartIcons[i].sprite = i < lives ? heartFull : heartEmpty;
    }

    void UpdateInventory()
    {
        if (_inventory == null) { inventoryIcon.enabled = false; return; }
        bool hasItem = _inventory.HeldItem != null;
        inventoryIcon.enabled = hasItem;
        if (hasItem && _inventory.HeldItem.icon != null)
            inventoryIcon.sprite = _inventory.HeldItem.icon;
    }
}
