using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class SceneTransitionPlayModeTests
{
    [UnityTest]
    public IEnumerator FadeOutAndIn_ReachesExactEndpointAlphas()
    {
        if (SceneTransition.Instance != null)
        {
            Object.Destroy(SceneTransition.Instance.gameObject);
            yield return null;
        }

        var go = new GameObject("TransitionTest");
        var group = go.AddComponent<CanvasGroup>();
        var labelObject = new GameObject("Label");
        var label = labelObject.AddComponent(
            System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro", true));
        label.transform.SetParent(go.transform);
        var transition = go.AddComponent<SceneTransition>();

        void Set(string field, object value) => typeof(SceneTransition)
            .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(transition, value);
        Set("canvasGroup", group);
        Set("roomTitle", label);
        Set("fadeDuration", 0.01f);
        Set("titleHoldDuration", 0f);

        yield return transition.FadeOut();
        Assert.That(group.alpha, Is.EqualTo(1f));
        yield return transition.FadeIn("Biblioteca Arcana", Color.cyan);
        Assert.That(group.alpha, Is.EqualTo(0f));
        Object.Destroy(go);
    }
}
