using System.Collections;
using TMPro;
using UnityEngine;

public sealed class SceneTransition : MonoBehaviour
{
    public static SceneTransition Instance { get; private set; }

    [SerializeField] CanvasGroup canvasGroup;
    [SerializeField] TMP_Text roomTitle;
    [SerializeField] float fadeDuration = 0.22f;
    [SerializeField] float titleHoldDuration = 0.35f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AutoCreate()
    {
        if (Instance != null) return;
        var prefab = Resources.Load<SceneTransition>("UI/SceneTransition");
        if (prefab != null) Object.Instantiate(prefab);
        else Debug.LogWarning("Missing Resources/UI/SceneTransition prefab; travel will cut.");
    }

    public IEnumerator FadeOut()
    {
        yield return Fade(0f, 1f);
        canvasGroup.alpha = 1f;
    }

    public IEnumerator FadeIn(string title, Color accent)
    {
        roomTitle.text = title;
        roomTitle.color = accent;
        roomTitle.gameObject.SetActive(true);
        canvasGroup.alpha = 1f;
        if (titleHoldDuration > 0f)
            yield return new WaitForSecondsRealtime(titleHoldDuration);
        yield return Fade(1f, 0f);
        canvasGroup.alpha = 0f;
        roomTitle.gameObject.SetActive(false);
    }

    IEnumerator Fade(float from, float to)
    {
        float elapsed = 0f;
        canvasGroup.blocksRaycasts = true;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / fadeDuration));
            yield return null;
        }
        canvasGroup.blocksRaycasts = to > 0.5f;
    }
}
