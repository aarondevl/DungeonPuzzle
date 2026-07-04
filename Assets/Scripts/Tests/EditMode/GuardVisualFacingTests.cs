using NUnit.Framework;
using UnityEngine;

public class GuardVisualFacingTests
{
    [Test]
    public void RotacionCero_MiraArriba()
    {
        var d = GuardVisual.RotationToDirection(0f);
        Assert.AreEqual(0f, d.x, 1e-4f);
        Assert.AreEqual(1f, d.y, 1e-4f);
    }

    [Test]
    public void RotacionMenos90_MiraDerecha()
    {
        var d = GuardVisual.RotationToDirection(-90f);
        Assert.AreEqual(1f, d.x, 1e-4f);
        Assert.AreEqual(0f, d.y, 1e-4f);
    }

    [Test]
    public void EnMovimiento_FacingSigueVelocidad_CardinalDominante()
    {
        var f = GuardVisual.ComputeFacing(new Vector2(2f, 0.5f), 0f, 0.05f);
        Assert.AreEqual(new Vector2(1f, 0f), f);
    }

    [Test]
    public void Quieto_FacingSigueRotacionDelCuerpo()
    {
        var f = GuardVisual.ComputeFacing(Vector2.zero, 180f, 0.05f);
        Assert.AreEqual(new Vector2(0f, -1f), f);
    }
}
