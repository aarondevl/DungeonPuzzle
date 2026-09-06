using NUnit.Framework;
using UnityEngine;

public class PlayerFacingTests
{
    [Test]
    public void PredominaElEjeHorizontalEnDiagonalesAbiertas()
    {
        Assert.AreEqual(Vector2.right, PlayerMovement.SnapFacing(new Vector2(1f, 0.4f)));
        Assert.AreEqual(Vector2.left, PlayerMovement.SnapFacing(new Vector2(-1f, 0.4f)));
    }

    [Test]
    public void PredominaElEjeVerticalCuandoEsMayor()
    {
        Assert.AreEqual(Vector2.up, PlayerMovement.SnapFacing(new Vector2(0.3f, 1f)));
        Assert.AreEqual(Vector2.down, PlayerMovement.SnapFacing(new Vector2(0.3f, -1f)));
    }

    [Test]
    public void SinEntradaMiraHaciaAbajo()
    {
        Assert.AreEqual(Vector2.down, PlayerMovement.SnapFacing(Vector2.zero));
    }

    [Test]
    public void LaDiagonalExactaResuelveEnHorizontal()
    {
        Assert.AreEqual(Vector2.right, PlayerMovement.SnapFacing(new Vector2(1f, 1f)));
    }
}
