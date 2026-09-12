using NUnit.Framework;
using UnityEngine;

public class GalleryRuntimeTests
{
    GameObject _player;

    [TearDown]
    public void TearDown()
    {
        if (_player != null) Object.DestroyImmediate(_player);
    }

    Collider2D NewPlayerCollider(bool trigger)
    {
        _player = new GameObject("GalleryTestPlayer") { tag = "Player" };
        var collider = _player.AddComponent<CircleCollider2D>();
        collider.isTrigger = trigger;
        return collider;
    }

    [Test]
    public void PhysicalPlayerRule_RejectsInteractionSensor()
    {
        Assert.That(GalleryPlayerContact.IsPhysicalPlayer(NewPlayerCollider(true)), Is.False);
    }

    [Test]
    public void PhysicalPlayerRule_AcceptsSolidPlayerBody()
    {
        Assert.That(GalleryPlayerContact.IsPhysicalPlayer(NewPlayerCollider(false)), Is.True);
    }

    [Test]
    public void CameraClamp_AccountsForOrthographicViewport()
    {
        var result = GalleryCameraFollow.ClampCenter(
            new Vector2(50f, -50f), new Rect(-32f, -20f, 64f, 42f), 6.5f, 16f / 9f);
        Assert.That(result.x, Is.EqualTo(32f - 6.5f * 16f / 9f).Within(0.001f));
        Assert.That(result.y, Is.EqualTo(-13.5f).Within(0.001f));
    }
}
