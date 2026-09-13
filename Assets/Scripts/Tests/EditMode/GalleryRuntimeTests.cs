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

    [Test]
    public void ProximityActivator_ChangesAllConfiguredTargets()
    {
        var root = new GameObject("Activator");
        var a = new GameObject("A");
        var b = new GameObject("B");
        var activator = root.AddComponent<GalleryProximityActivator>();
        activator.Configure(new[] { a, b }, true);
        activator.SetInside(true);
        Assert.That(a.activeSelf && b.activeSelf, Is.True);
        activator.SetInside(false);
        Assert.That(a.activeSelf || b.activeSelf, Is.False);
        Object.DestroyImmediate(root);
        Object.DestroyImmediate(a);
        Object.DestroyImmediate(b);
    }

    [Test]
    public void ReturnPortal_MovesBodyAndClearsVelocity()
    {
        var collider = NewPlayerCollider(false);
        var body = _player.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.linearVelocity = new Vector2(3f, 2f);
        var portalObject = new GameObject("Portal");
        var destinationObject = new GameObject("Destination");
        destinationObject.transform.position = new Vector3(4f, -3f);
        var portal = portalObject.AddComponent<GalleryReturnPortal>();
        portal.Configure(destinationObject.transform, 0.35f);
        Assert.That(portal.TryTeleport(collider, 1f), Is.True);
        Assert.That(body.position, Is.EqualTo(new Vector2(4f, -3f)));
        Assert.That(body.linearVelocity, Is.EqualTo(Vector2.zero));
        Object.DestroyImmediate(portalObject);
        Object.DestroyImmediate(destinationObject);
    }

    [Test]
    public void OccluderAlpha_MovesTowardTargetWithoutOvershoot()
    {
        Assert.That(ForegroundOccluder.StepAlpha(1f, 0.3f, 4f, 0.1f),
            Is.EqualTo(0.6f).Within(0.001f));
        Assert.That(ForegroundOccluder.StepAlpha(0.35f, 0.3f, 4f, 0.1f),
            Is.EqualTo(0.3f).Within(0.001f));
    }
}
