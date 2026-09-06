using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class InteractionSensorTests
{
    [Test]
    public void EligeElCandidatoMasCercano()
    {
        var candidatos = new List<Vector2> { new(3f, 0f), new(0.5f, 0f), new(-2f, 1f) };
        Assert.AreEqual(1, InteractionSensor.ClosestIndex(Vector2.zero, candidatos));
    }

    [Test]
    public void SinCandidatosDevuelveMenosUno()
    {
        Assert.AreEqual(-1, InteractionSensor.ClosestIndex(Vector2.zero, new List<Vector2>()));
    }

    [Test]
    public void ConEmpateSeQuedaConElPrimero()
    {
        var candidatos = new List<Vector2> { new(1f, 0f), new(-1f, 0f) };
        Assert.AreEqual(0, InteractionSensor.ClosestIndex(Vector2.zero, candidatos));
    }
}
