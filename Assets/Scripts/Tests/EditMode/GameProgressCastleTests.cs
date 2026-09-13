using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public class GameProgressCastleTests
{
    const string Room = "guard_wing_test";
    const string Secret = "room_04_passage_test";
    static readonly string[] ApprovedCastleRooms =
    {
        "patio_hall",
        "guard_wing",
        "arcane_library",
        "dungeons",
        "warden_tower"
    };

    [TearDown]
    public void Cleanup()
    {
        GameProgress.ClearRoomCompleted(Room);
        GameProgress.ClearSecret(Secret);

        foreach (var manager in Object.FindObjectsByType<GameManager>())
            Object.DestroyImmediate(manager.gameObject);
        foreach (var identity in Object.FindObjectsByType<RoomIdentity>())
            Object.DestroyImmediate(identity.gameObject);
        foreach (var spark in GameObject.FindGameObjectsWithTag("Untagged"))
            if (spark.name == "Spark(Clone)") Object.DestroyImmediate(spark);

        SetStaticPropertyBackingField(typeof(GameManager), "Instance", null);
        SetStaticPropertyBackingField(typeof(RoomIdentity), "Current", null);
    }

    [Test]
    public void CompletedRoom_RoundTripsThroughPlayerPrefs()
    {
        Assert.That(GameProgress.IsRoomCompleted(Room), Is.False);
        GameProgress.MarkRoomCompleted(Room);
        Assert.That(GameProgress.IsRoomCompleted(Room), Is.True);
    }

    [Test]
    public void Secret_RoundTripsThroughPlayerPrefs()
    {
        Assert.That(GameProgress.IsSecretDiscovered(Secret), Is.False);
        GameProgress.DiscoverSecret(Secret);
        Assert.That(GameProgress.IsSecretDiscovered(Secret), Is.True);
    }

    [Test]
    public void ResetAll_ClearsApprovedCastleProgress()
    {
        var intKeys = new List<string>
        {
            "DP.HighestUnlocked",
            "DP.TotalDeaths",
            "DP.Castle.Secret.room_04_passage"
        };
        foreach (string roomId in ApprovedCastleRooms)
            intKeys.Add($"DP.Castle.Room.{roomId}.Complete");

        var originalInts = CaptureInts(intKeys);
        var bestTimeKeys = new List<string>();
        for (int level = 1; level <= GameProgress.TotalLevels; level++)
            bestTimeKeys.Add($"DP.BestTime.Room_{level:00}");
        var originalFloats = CaptureFloats(bestTimeKeys);

        try
        {
            foreach (string roomId in ApprovedCastleRooms)
                GameProgress.MarkRoomCompleted(roomId);
            GameProgress.DiscoverSecret("room_04_passage");

            GameProgress.ResetAll();

            foreach (string roomId in ApprovedCastleRooms)
                Assert.That(GameProgress.IsRoomCompleted(roomId), Is.False, roomId);
            Assert.That(GameProgress.IsSecretDiscovered("room_04_passage"), Is.False);
        }
        finally
        {
            RestoreInts(originalInts);
            RestoreFloats(originalFloats);
            PlayerPrefs.Save();
        }
    }

    [Test]
    public void CastleProgress_DoesNotChangeExistingLevelOrTimePreferences()
    {
        var originalInts = CaptureInts(new[] { "DP.HighestUnlocked" });
        var originalFloats = CaptureFloats(new[] { "DP.BestTime.Room_02" });

        try
        {
            GameProgress.HighestUnlocked = 3;
            PlayerPrefs.SetFloat("DP.BestTime.Room_02", 12.5f);
            PlayerPrefs.Save();

            GameProgress.MarkRoomCompleted(Room);
            GameProgress.DiscoverSecret(Secret);

            Assert.That(GameProgress.HighestUnlocked, Is.EqualTo(3));
            Assert.That(GameProgress.GetBestTime(2), Is.EqualTo(12.5f));
        }
        finally
        {
            RestoreInts(originalInts);
            RestoreFloats(originalFloats);
            PlayerPrefs.Save();
        }
    }

    [Test]
    public void ExplicitTravel_OnlyCompletesRoomWhenDestinationIsValid()
    {
        var identity = NewIdentity(Room);
        var manager = NewManager();

        LogAssert.Expect(LogType.Error, "Castle travel rejected: 'Missing_Castle_Room'.");
        Assert.That(manager.TravelTo("Missing_Castle_Room", "Default"), Is.False);
        Assert.That(GameProgress.IsRoomCompleted(Room), Is.False);

        SetField(manager, "_canLoadScene", new Func<string, bool>(_ => true));
        SetField(manager, "_startTravel", new Action<string, string>((_, _) => { }));
        Assert.That(manager.TravelTo("Room_01", "Default"), Is.True);
        Assert.That(GameProgress.IsRoomCompleted(identity.RoomId), Is.True);
    }

    [Test]
    public void TerminalLinearDeparture_CompletesCurrentRoom()
    {
        var originalBestTime = CaptureFloats(new[] { "DP.BestTime.Room_05" });
        NewIdentity(Room);
        var manager = NewManager();
        SetField(manager, "<CurrentLevel>k__BackingField", GameProgress.TotalLevels);

        try
        {
            // La salida ahora arranca una secuencia (cartel + fundido) y carga la escena
            // después; en EditMode la corrutina se detiene en su primer yield, así que
            // lo verificable es que la sala queda completada ANTES de cualquier carga.
            manager.LoadNextRoom();
            Assert.That(GameProgress.IsRoomCompleted(Room), Is.True);
        }
        finally
        {
            RestoreFloats(originalBestTime);
            PlayerPrefs.Save();
        }
    }

    [Test]
    public void InvalidConfiguredExit_CanBeTriggeredAgain()
    {
        NewIdentity(Room);
        NewManager();

        var player = new GameObject("Player_Test");
        player.tag = "Player";
        var playerCollider = player.AddComponent<BoxCollider2D>();

        var exitObject = new GameObject("InvalidExit_Test");
        var exit = exitObject.AddComponent<ExitTrigger>();
        SetField(exit, "destinationScene", "Missing_Castle_Room");

        LogAssert.Expect(LogType.Error,
            "Castle travel rejected by door 'InvalidExit_Test': 'Missing_Castle_Room'.");
        InvokeTrigger(exit, playerCollider);
        LogAssert.Expect(LogType.Error,
            "Castle travel rejected by door 'InvalidExit_Test': 'Missing_Castle_Room'.");
        InvokeTrigger(exit, playerCollider);

        Object.DestroyImmediate(exitObject);
        Object.DestroyImmediate(player);
    }

    [Test]
    public void SecretDiscovery_FirstEntryPersistsAndRevealsFeedback()
    {
        var discoveryObject = new GameObject("SecretDiscovery_Test");
        discoveryObject.AddComponent<BoxCollider2D>();
        var discovery = discoveryObject.AddComponent<SecretDiscovery>();
        SetField(discovery, "secretId", Secret);
        var revealEffect = new GameObject("RevealEffect_Test");
        revealEffect.SetActive(false);
        SetField(discovery, "revealEffect", revealEffect);

        var player = new GameObject("Player_Test");
        player.tag = "Player";
        var playerCollider = player.AddComponent<BoxCollider2D>();

        InvokeTrigger(discovery, playerCollider);

        Assert.That(GameProgress.IsSecretDiscovered(Secret), Is.True);
        Assert.That(revealEffect.activeSelf, Is.True);

        Object.DestroyImmediate(discoveryObject);
        Object.DestroyImmediate(revealEffect);
        Object.DestroyImmediate(player);
    }

    static RoomIdentity NewIdentity(string roomId)
    {
        var identity = new GameObject("RoomIdentity_Test").AddComponent<RoomIdentity>();
        SetField(identity, "roomId", roomId);
        SetStaticPropertyBackingField(typeof(RoomIdentity), "Current", identity);
        return identity;
    }

    static GameManager NewManager()
    {
        var manager = new GameObject("GameManager_Test").AddComponent<GameManager>();
        SetField(manager, "_startTravel", new Action<string, string>((_, _) => { }));
        SetStaticPropertyBackingField(typeof(GameManager), "Instance", manager);
        return manager;
    }

    static void InvokeTrigger(ExitTrigger exit, Collider2D playerCollider)
    {
        typeof(ExitTrigger).GetMethod("OnTriggerEnter2D",
                BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(exit, new object[] { playerCollider });
    }

    static void InvokeTrigger(SecretDiscovery discovery, Collider2D playerCollider)
    {
        typeof(SecretDiscovery).GetMethod("OnTriggerEnter2D",
                BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(discovery, new object[] { playerCollider });
    }

    static void SetField(object target, string name, object value)
    {
        target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(target, value);
    }

    static void SetStaticPropertyBackingField(Type type, string propertyName, object value)
    {
        type.GetField($"<{propertyName}>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic)
            .SetValue(null, value);
    }

    static Dictionary<string, int?> CaptureInts(IEnumerable<string> keys)
    {
        var values = new Dictionary<string, int?>();
        foreach (string key in keys)
            values[key] = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key) : null;
        return values;
    }

    static Dictionary<string, float?> CaptureFloats(IEnumerable<string> keys)
    {
        var values = new Dictionary<string, float?>();
        foreach (string key in keys)
            values[key] = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetFloat(key) : null;
        return values;
    }

    static void RestoreInts(Dictionary<string, int?> values)
    {
        foreach (var pair in values)
            if (pair.Value.HasValue) PlayerPrefs.SetInt(pair.Key, pair.Value.Value);
            else PlayerPrefs.DeleteKey(pair.Key);
    }

    static void RestoreFloats(Dictionary<string, float?> values)
    {
        foreach (var pair in values)
            if (pair.Value.HasValue) PlayerPrefs.SetFloat(pair.Key, pair.Value.Value);
            else PlayerPrefs.DeleteKey(pair.Key);
    }
}
