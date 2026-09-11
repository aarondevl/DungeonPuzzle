using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class CastleTravelTests
{
    static SpawnPoint NewSpawn(string name, string id, bool isDefault)
    {
        var go = new GameObject(name);
        var spawn = go.AddComponent<SpawnPoint>();
        typeof(SpawnPoint).GetField("spawnId", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(spawn, id);
        typeof(SpawnPoint).GetField("isDefault", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(spawn, isDefault);
        return spawn;
    }

    [Test]
    public void Resolve_PrefersRequestedId()
    {
        var fallback = NewSpawn("Fallback", "South", true);
        var north = NewSpawn("North", "North", false);
        Assert.That(SpawnPoint.Resolve(new[] { fallback, north }, "North"), Is.SameAs(north));
        Object.DestroyImmediate(fallback.gameObject);
        Object.DestroyImmediate(north.gameObject);
    }

    [Test]
    public void Resolve_UsesDefaultWhenRequestedIdIsMissing()
    {
        var fallback = NewSpawn("Fallback", "South", true);
        var north = NewSpawn("North", "North", false);
        Assert.That(SpawnPoint.Resolve(new[] { north, fallback }, "Secret"), Is.SameAs(fallback));
        Object.DestroyImmediate(fallback.gameObject);
        Object.DestroyImmediate(north.gameObject);
    }

    [Test]
    public void Resolve_ReturnsNullForEmptyInput()
    {
        Assert.That(SpawnPoint.Resolve(System.Array.Empty<SpawnPoint>(), "South"), Is.Null);
    }
}
