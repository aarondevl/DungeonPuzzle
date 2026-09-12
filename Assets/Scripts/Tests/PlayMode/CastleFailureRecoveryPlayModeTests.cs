using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Matriz de fallo y recuperación del castillo (Task 10, paso 3): en cada sala, las
/// dos primeras detecciones recargan esa misma sala en su spawn por defecto, la
/// tercera abre Game Over, y reintentar devuelve la sala con tres vidas sin perder
/// el progreso de castillo ya conseguido.
///
/// Los guardias y las trampas se desactivan al cargar cada sala: aquí se verifica la
/// reacción del <see cref="GameManager"/> ante una detección, no si un guardia es
/// capaz de detectar (eso lo cubren VisionConeRangeTests y los tests de guardia).
/// Sin desactivarlos, un guardia podría gastar vidas por su cuenta y el recuento
/// dejaría de ser determinista.
/// </summary>
public class CastleFailureRecoveryPlayModeTests
{
    static readonly string[] PrefKeys =
    {
        "DP.HighestUnlocked", "DP.TotalDeaths",
        "DP.Castle.Room.patio_hall.Complete", "DP.Castle.Room.guard_wing.Complete",
        "DP.Castle.Room.arcane_library.Complete", "DP.Castle.Room.dungeons.Complete",
        "DP.Castle.Room.warden_tower.Complete", "DP.Castle.Secret.room_04_passage"
    };

    readonly Dictionary<string, int> _savedInts = new Dictionary<string, int>();
    readonly List<string> _absentKeys = new List<string>();
    int _sceneLoads;

    [SetUp]
    public void SnapshotProgress()
    {
        _savedInts.Clear();
        _absentKeys.Clear();
        foreach (string key in PrefKeys)
        {
            if (PlayerPrefs.HasKey(key)) _savedInts[key] = PlayerPrefs.GetInt(key);
            else _absentKeys.Add(key);
        }
        SceneManager.sceneLoaded += CountScene;
    }

    [TearDown]
    public void RestoreProgress()
    {
        SceneManager.sceneLoaded -= CountScene;
        foreach (var pair in _savedInts) PlayerPrefs.SetInt(pair.Key, pair.Value);
        foreach (string key in _absentKeys) PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
    }

    void CountScene(Scene scene, LoadSceneMode mode) => _sceneLoads++;

    IEnumerator WaitForSceneLoad(string expectedName)
    {
        int before = _sceneLoads;
        float deadline = Time.realtimeSinceStartup + 15f;
        while (_sceneLoads == before && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(_sceneLoads, Is.GreaterThan(before), $"timed out waiting for '{expectedName}'");
        yield return null; // deja correr OnSceneLoaded y los Awake de la escena
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(expectedName));
        Neutralise();
    }

    /// <summary>Apaga guardias y trampas para que solo cuenten las detecciones del test.</summary>
    static void Neutralise()
    {
        foreach (var guard in Object.FindObjectsByType<GuardBase>(FindObjectsSortMode.None))
            guard.enabled = false;
        foreach (var trap in Object.FindObjectsByType<SpikeTrap>(FindObjectsSortMode.None))
            trap.enabled = false;
    }

    static Vector3 DefaultSpawnPosition()
    {
        var spawn = SpawnPoint.Resolve(
            Object.FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None), null);
        Assert.That(spawn, Is.Not.Null, "the room has no SpawnPoint");
        return spawn.transform.position;
    }

    static GameObject Player() => GameObject.FindGameObjectWithTag("Player");

    [UnityTest]
    public IEnumerator SpikeInteractionSensorOutsidePhysicalBody_DoesNotCostLifeWhenTrapExtends()
    {
        var gm = GameManager.Instance;
        Assert.That(gm, Is.Not.Null, "GameManager.Instance missing");

        gm.StartLevel(2);
        yield return WaitForSceneLoad("Room_02");
        Assert.That(gm.Lives, Is.EqualTo(3));

        var trap = Object.FindFirstObjectByType<SpikeTrap>(FindObjectsInactive.Include);
        var player = Player();
        Assert.That(trap, Is.Not.Null, "Room_02 has no SpikeTrap");
        Assert.That(player, Is.Not.Null, "Room_02 has no Player");

        var physicalBody = System.Array.Find(player.GetComponents<Collider2D>(), c => !c.isTrigger);
        var interactionSensor = System.Array.Find(player.GetComponents<Collider2D>(), c => c.isTrigger);
        Assert.That(physicalBody, Is.Not.Null, "Player has no physical collider");
        Assert.That(interactionSensor, Is.Not.Null, "Player has no interaction trigger");

        // A 0.8-unit separation leaves the physical body clear of the 0.4-wide
        // spikes, while the larger interaction sensor still overlaps them.
        player.transform.position = trap.transform.position + Vector3.right * 0.8f;
        var trapCollider = trap.GetComponent<Collider2D>();
        trapCollider.enabled = true;
        Physics2D.SyncTransforms();
        Assert.That(physicalBody.Distance(trapCollider).isOverlapped, Is.False,
            "test setup error: the player's physical body still overlaps the spikes");
        Assert.That(interactionSensor.Distance(trapCollider).isOverlapped, Is.True,
            "test setup error: the interaction sensor does not overlap the spikes");
        trapCollider.enabled = false;

        MethodInfo apply = typeof(SpikeTrap).GetMethod("Apply", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(apply, Is.Not.Null);
        apply.Invoke(trap, new object[] { SpikeTrap.Phase.Hidden });
        apply.Invoke(trap, new object[] { SpikeTrap.Phase.Extended });

        Assert.That(gm.Lives, Is.EqualTo(3),
            "spikes must ignore the interaction trigger when the physical body is outside");

        MethodInfo onTriggerEnter = typeof(SpikeTrap).GetMethod(
            "OnTriggerEnter2D", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(onTriggerEnter, Is.Not.Null);
        onTriggerEnter.Invoke(trap, new object[] { interactionSensor });

        Assert.That(gm.Lives, Is.EqualTo(3),
            "walking an interaction trigger into extended spikes must not cost a life");
    }

    [UnityTest]
    public IEnumerator ThreeDetections_ReloadTwiceThenGameOver_AndRetryKeepsProgress(
        [Values(1, 2, 3, 4, 5)] int level)
    {
        // Progreso previo que debe sobrevivir a las tres muertes y al reintento.
        GameProgress.MarkRoomCompleted("patio_hall");
        GameProgress.DiscoverSecret("room_04_passage");

        string room = $"Room_{level:00}";
        var gm = GameManager.Instance;
        Assert.That(gm, Is.Not.Null, "GameManager.Instance missing");

        gm.StartLevel(level);
        yield return WaitForSceneLoad(room);
        Assert.That(gm.Lives, Is.EqualTo(3), "a fresh room must start with three lives");

        Vector3 spawn = DefaultSpawnPosition();

        for (int detection = 1; detection <= 2; detection++)
        {
            var player = Player();
            Assert.That(player, Is.Not.Null, "no Player in " + room);
            player.transform.position = spawn + new Vector3(1.5f, 0f, 0f);

            gm.PlayerDetected();
            yield return WaitForSceneLoad(room);

            Assert.That(gm.Lives, Is.EqualTo(3 - detection),
                $"detection {detection} in {room} should leave {3 - detection} lives");
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(room),
                $"detection {detection} must reload {room}, not change room");
            Assert.That(Vector3.Distance(Player().transform.position, spawn), Is.LessThan(0.05f),
                $"detection {detection} must respawn at the default SpawnPoint of {room}");
        }

        gm.PlayerDetected();
        yield return WaitForSceneLoad("GameOver");
        Assert.That(gm.Lives, Is.EqualTo(0), "the third detection must consume the last life");
        Assert.That(gm.IsWin, Is.False, "Game Over must not be flagged as a win");

        gm.RestartCurrentRoom();
        yield return WaitForSceneLoad(room);
        Assert.That(gm.Lives, Is.EqualTo(3), "retry must restore three lives");

        Assert.That(GameProgress.IsRoomCompleted("patio_hall"), Is.True,
            "castle completion was lost across the failure cycle");
        Assert.That(GameProgress.IsSecretDiscovered("room_04_passage"), Is.True,
            "secret discovery was lost across the failure cycle");
    }
}
