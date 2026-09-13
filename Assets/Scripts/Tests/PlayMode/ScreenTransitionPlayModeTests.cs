using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// La capa de fundidos es la que encadena captura, salida y viaje entre salas: si
/// no llega exactamente a negro o a transparente, la sala siguiente entra a medias.
/// </summary>
public class ScreenTransitionPlayModeTests
{
    [UnityTest]
    public IEnumerator FadeOutAndIn_ReachesExactEndpoints()
    {
        if (ScreenTransition.Instance == null)
        {
            new GameObject("ScreenTransition_Test").AddComponent<ScreenTransition>();
            yield return null;
        }

        var transition = ScreenTransition.Instance;
        Assert.That(transition, Is.Not.Null, "ScreenTransition did not self-create");

        yield return ScreenTransition.FadeOut(0.01f);
        Assert.That(transition.IsDark, Is.True, "fade out should end fully dark");

        yield return ScreenTransition.FadeIn(0.01f);
        Assert.That(transition.IsDark, Is.False, "fade in should end fully transparent");

        ScreenTransition.SetDark();
        Assert.That(transition.IsDark, Is.True, "SetDark must be immediate");
        yield return ScreenTransition.FadeIn(0.01f);
        Assert.That(transition.IsDark, Is.False);
    }
}
