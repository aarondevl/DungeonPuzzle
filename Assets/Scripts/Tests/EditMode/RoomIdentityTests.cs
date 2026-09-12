using NUnit.Framework;
using UnityEngine;

public class RoomIdentityTests
{
    [Test]
    public void ResolvedDisplayName_UsesSceneNameWhenDisplayNameIsBlank()
    {
        var go = new GameObject("Identity");
        var identity = go.AddComponent<RoomIdentity>();
        Assert.That(identity.ResolvedDisplayName("Room_03"), Is.EqualTo("Room_03"));
        Object.DestroyImmediate(go);
    }

    [Test]
    public void ResolvedAmbienceKey_UsesDungeonDefaultWhenBlank()
    {
        var go = new GameObject("Identity");
        var identity = go.AddComponent<RoomIdentity>();
        Assert.That(identity.ResolvedAmbienceKey, Is.EqualTo("Music/dungeon_ambient"));
        Object.DestroyImmediate(go);
    }
}
