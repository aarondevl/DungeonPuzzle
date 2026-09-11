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

    [Test]
    public void CanStart_RejectsBlankSceneAndActiveTransition()
    {
        Assert.That(CastleTravelRules.CanStart(false, "", _ => true), Is.False);
        Assert.That(CastleTravelRules.CanStart(true, "Room_02", _ => true), Is.False);
    }

    [Test]
    public void CanStart_UsesSceneLoadabilityProbe()
    {
        Assert.That(CastleTravelRules.CanStart(false, "Room_02", s => s == "Room_02"), Is.True);
        Assert.That(CastleTravelRules.CanStart(false, "Missing", s => s == "Room_02"), Is.False);
    }

    [Test]
    public void NextScene_StopsAfterFinalRoom()
    {
        Assert.That(CastleTravelRules.NextScene(4, 5), Is.EqualTo("Room_05"));
        Assert.That(CastleTravelRules.NextScene(5, 5), Is.Null);
    }
}
