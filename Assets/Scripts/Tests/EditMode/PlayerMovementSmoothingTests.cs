using NUnit.Framework;
using UnityEngine;

public class PlayerMovementSmoothingTests
{
    [Test]
    public void StepVelocity_FromZeroTowardMax_RespectsAcceleration()
    {
        Vector2 current = Vector2.zero;
        Vector2 target  = new Vector2(4f, 0f);
        float accel = 20f;
        float dt    = 0.02f;

        Vector2 next = PlayerMovement.StepVelocity(current, target, accel, dt);
        Assert.AreEqual(0.4f, next.x, 0.0001f);
        Assert.AreEqual(0f,   next.y, 0.0001f);
    }

    [Test]
    public void StepVelocity_AlreadyAtTarget_DoesNotOvershoot()
    {
        Vector2 current = new Vector2(4f, 0f);
        Vector2 target  = new Vector2(4f, 0f);
        Vector2 next = PlayerMovement.StepVelocity(current, target, 20f, 0.02f);
        Assert.AreEqual(target, next);
    }

    [Test]
    public void StepVelocity_DecelerationCase_DoesNotOvershootZero()
    {
        Vector2 current = new Vector2(0.3f, 0f);
        Vector2 target  = Vector2.zero;
        Vector2 next = PlayerMovement.StepVelocity(current, target, 20f, 0.02f);
        Assert.AreEqual(0f, next.x, 0.0001f);
    }
}
