using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class NoiseSourceTests
{
    [Test]
    public void GuardsWithinRadius_AllAreSelected()
    {
        Vector2 origin = Vector2.zero;
        var positions = new List<Vector2> {
            new Vector2(1f, 0f),
            new Vector2(0f, 2f),
            new Vector2(10f, 10f)
        };
        var indices = NoiseSource.SelectGuardsInRadius(origin, positions, 5f);
        Assert.AreEqual(2, indices.Count);
        Assert.Contains(0, indices);
        Assert.Contains(1, indices);
    }

    [Test]
    public void NoGuardsWithinRadius_ReturnsEmpty()
    {
        var indices = NoiseSource.SelectGuardsInRadius(Vector2.zero,
            new List<Vector2> { new Vector2(100f, 0f) }, 5f);
        Assert.AreEqual(0, indices.Count);
    }
}
