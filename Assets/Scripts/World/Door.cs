using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public class Door : MonoBehaviour
{
    [SerializeField] Sprite closedSprite;
    [SerializeField] Sprite openSprite;

    SpriteRenderer _sr;
    Collider2D _col;
    bool _open;

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _col = GetComponent<Collider2D>();
    }

    public void Open()
    {
        if (_open) return;
        _open = true;
        _sr.sprite = openSprite;
        _col.enabled = false;
    }

    public void Toggle() { if (_open) Close(); else Open(); }

    void Close()
    {
        _open = false;
        _sr.sprite = closedSprite;
        _col.enabled = true;
    }
}
