using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDManager : MonoBehaviour
{
    [SerializeField] Image[] heartIcons;
    [SerializeField] Sprite heartFullSprite;
    [SerializeField] Sprite heartEmptySprite;
    [SerializeField] Image inventoryIcon;
    [SerializeField] Image damageFlash;
    [SerializeField] TextMeshProUGUI levelLabel;
    [SerializeField] TextMeshProUGUI timerLabel;
    [SerializeField] TextMeshProUGUI bestTimeLabel;

    PlayerInventory _inventory;
    int _lastLives = -1;

    void Start()
    {
        var player = FindFirstObjectByType<PlayerInventory>();
        if (player != null) _inventory = player;
        if (damageFlash != null)
        {
            var c = damageFlash.color; c.a = 0; damageFlash.color = c;
        }
        UpdateLabels();
    }

    void Update()
    {
        UpdateHearts();
        UpdateInventory();
        UpdateTimer();
    }

    void UpdateLabels()
    {
        if (GameManager.Instance == null) return;
        if (levelLabel != null) levelLabel.text = $"ROOM {GameManager.Instance.CurrentLevel:00}";
        if (bestTimeLabel != null)
        {
            float best = GameProgress.GetBestTime(GameManager.Instance.CurrentLevel);
            bestTimeLabel.text = best > 0f ? $"BEST  {GameProgress.FormatTime(best)}" : "BEST  --:--";
        }
    }

    void UpdateTimer()
    {
        if (timerLabel == null || GameManager.Instance == null) return;
        timerLabel.text = GameProgress.FormatTime(GameManager.Instance.RoomTime);
    }

    void UpdateHearts()
    {
        if (GameManager.Instance == null) return;
        int lives = GameManager.Instance.Lives;
        for (int i = 0; i < heartIcons.Length; i++)
            heartIcons[i].sprite = i < lives ? heartFullSprite : heartEmptySprite;

        if (_lastLives > -1 && lives < _lastLives && damageFlash != null)
            StartCoroutine(Flash());
        _lastLives = lives;
    }

    IEnumerator Flash()
    {
        var c = damageFlash.color;
        c.a = 0.5f; damageFlash.color = c;
        float t = 0f;
        while (t < 0.4f)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(0.5f, 0f, t / 0.4f);
            damageFlash.color = c;
            yield return null;
        }
        c.a = 0; damageFlash.color = c;
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
