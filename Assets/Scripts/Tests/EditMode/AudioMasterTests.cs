using NUnit.Framework;
using UnityEngine;

public class AudioMasterTests
{
    [Test]
    public void ShouldSwitchAmbience_RejectsMissingAndAlreadyPlayingClip()
    {
        var current = AudioClip.Create("Current", 8, 1, 8000, false);
        Assert.That(AudioMaster.ShouldSwitchAmbience(current, null, true), Is.False);
        Assert.That(AudioMaster.ShouldSwitchAmbience(current, current, true), Is.False);
        Object.DestroyImmediate(current);
    }

    [Test]
    public void ShouldSwitchAmbience_AcceptsDifferentOrStoppedClip()
    {
        var current = AudioClip.Create("Current", 8, 1, 8000, false);
        var next = AudioClip.Create("Next", 8, 1, 8000, false);
        Assert.That(AudioMaster.ShouldSwitchAmbience(current, next, true), Is.True);
        Assert.That(AudioMaster.ShouldSwitchAmbience(current, current, false), Is.True);
        Object.DestroyImmediate(current);
        Object.DestroyImmediate(next);
    }
}
