using NUnit.Framework;
using UnityEngine;

public class ThrownStoneBounceTests
{
    [Test]
    public void ReboteInvierteLaComponenteNormal()
    {
        Vector2 v = new Vector2(4f, -3f);
        Vector2 outVel = ThrownStone.Bounce(v, Vector2.up, 1f);
        Assert.AreEqual(4f, outVel.x, 0.001f);
        Assert.AreEqual(3f, outVel.y, 0.001f);
    }

    [Test]
    public void ReboteDisipaEnergia()
    {
        Vector2 v = new Vector2(0f, -8f);
        Vector2 outVel = ThrownStone.Bounce(v, Vector2.up, 0.35f);
        Assert.AreEqual(8f * 0.35f, outVel.magnitude, 0.001f);
    }

    [Test]
    public void ReboteNuloDetieneLaPiedra()
    {
        Assert.AreEqual(0f, ThrownStone.Bounce(new Vector2(5f, 5f), Vector2.left, 0f).magnitude, 0.001f);
    }

    [Test]
    public void DireccionDeLanzamientoEsUnitaria()
    {
        Vector2 d = Stone.SpawnDirection(Vector2.zero, new Vector2(3f, 4f));
        Assert.AreEqual(1f, d.magnitude, 0.001f);
        Assert.AreEqual(0.6f, d.x, 0.001f);
    }

    [Test]
    public void LanzarSobreUnoMismoNoProduceDireccionCero()
    {
        Vector2 d = Stone.SpawnDirection(Vector2.one, Vector2.one);
        Assert.AreEqual(1f, d.magnitude, 0.001f);
    }
}
