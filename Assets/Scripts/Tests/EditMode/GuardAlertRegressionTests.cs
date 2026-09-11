using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class GuardAlertRegressionTests
{
    [Test]
    public void VisualAlert_UsesLastSeenPlayerPositionAsPatrolTarget()
    {
        var guardObject = new GameObject("Guard");
        guardObject.AddComponent<Rigidbody2D>();
        var guard = guardObject.AddComponent<GuardPatrol>();
        var seenPlayerPosition = new Vector2(-5f, 0f);

        MethodInfo onVisionAlerted = typeof(GuardPatrol).GetMethod(
            "OnVisionAlerted",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(onVisionAlerted, Is.Not.Null,
            "GuardPatrol must receive the position that triggered a visual alert.");

        onVisionAlerted.Invoke(guard, new object[] { seenPlayerPosition });

        FieldInfo alertTarget = typeof(GuardPatrol).GetField(
            "_alertTarget",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That((Vector2)alertTarget.GetValue(guard), Is.EqualTo(seenPlayerPosition));
        Object.DestroyImmediate(guardObject);
    }

    [Test]
    public void ReturnScheduling_DoesNotStartAnotherTimerWhileOneIsPending()
    {
        MethodInfo shouldScheduleReturn = typeof(GuardBase).GetMethod(
            "ShouldScheduleReturn",
            BindingFlags.Static | BindingFlags.NonPublic);

        Assert.That(shouldScheduleReturn, Is.Not.Null,
            "GuardBase must guard against scheduling a return coroutine every frame.");

        bool firstSchedule = (bool)shouldScheduleReturn.Invoke(
            null, new object[] { 0f, true, false, false });
        bool duplicateSchedule = (bool)shouldScheduleReturn.Invoke(
            null, new object[] { 0f, true, false, true });

        Assert.That(firstSchedule, Is.True);
        Assert.That(duplicateSchedule, Is.False);
    }
}
