using UnityEngine;
using UnityEngine.UI;

public class DetectionFlash : MonoBehaviour
{
    public static DetectionFlash Instance { get; private set; }

    [SerializeField] Color flashColor = new Color(0.85f, 0.05f, 0.05f, 0.55f);
    [SerializeField] float decay = 1.6f;

    Image _image;
    float _alpha;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _image = GetComponent<Image>();
        if (_image != null) _image.color = new Color(flashColor.r, flashColor.g, flashColor.b, 0f);
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void Update()
    {
        if (_image == null || _alpha <= 0f) return;
        _alpha = Mathf.Max(0f, _alpha - Time.unscaledDeltaTime * decay);
        _image.color = new Color(flashColor.r, flashColor.g, flashColor.b, _alpha);
    }

    public static void Flash()
    {
        if (Instance == null || Instance._image == null) return;
        Instance._alpha = Instance.flashColor.a;
        Instance._image.color = new Color(Instance.flashColor.r, Instance.flashColor.g, Instance.flashColor.b, Instance._alpha);
    }
}
