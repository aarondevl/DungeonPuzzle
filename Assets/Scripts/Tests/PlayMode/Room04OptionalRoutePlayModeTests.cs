using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Ruta opcional de los Calabozos (Task 10, paso 4). Room_04 tiene una sola salida
/// hacia Room_05 y un <see cref="SecretDiscovery"/> aparte, de modo que el pasaje es
/// opcional: la sala debe poder completarse con él y sin él, y el secreto solo debe
/// quedar marcado cuando el jugador realmente lo pisa.
///
/// El jugador se coloca directamente sobre cada trigger en lugar de recorrer la sala,
/// para que la ruta "sin pasaje" no lo toque por accidente al pasar. Los guardias y
/// las trampas se apagan por el mismo motivo que en la matriz de fallo.
/// </summary>
public class Room04OptionalRoutePlayModeTests
{
    const string RoomId = "dungeons";
    const string SecretId = "room_04_passage";

    static readonly string[] PrefKeys =
    {
        "DP.HighestUnlocked", "DP.TotalDeaths",
        "DP.Castle.Room.dungeons.Complete", "DP.Castle.Secret.room_04_passage"
    };

    readonly Dictionary<string, int> _savedInts = new Dictionary<string, int>();
    readonly List<string> _absentKeys = new List<string>();

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
    }

    [TearDown]
    public void RestoreProgress()
    {
        foreach (var pair in _savedInts) PlayerPrefs.SetInt(pair.Key, pair.Value);
        foreach (string key in _absentKeys) PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
    }

    static IEnumerator WaitForScene(string name, float seconds = 20f)
    {
        float deadline = Time.realtimeSinceStartup + seconds;
        while (SceneManager.GetActiveScene().name != name && Time.realtimeSinceStartup < deadline)
            yield return null;
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(name),
            $"timed out waiting for scene '{name}'");
        yield return null;
    }

    static void Neutralise()
    {
        foreach (var guard in Object.FindObjectsByType<GuardBase>(FindObjectsSortMode.None))
            guard.enabled = false;
        foreach (var trap in Object.FindObjectsByType<SpikeTrap>(FindObjectsSortMode.None))
            trap.enabled = false;
    }

    /// <summary>
    /// Empuja al jugador sobre un trigger y deja correr unos frames de física. Se
    /// vuelve a buscar al jugador en cada frame y se corta en cuanto desaparece:
    /// pisar la salida arranca el viaje de sala, que descarga la escena y destruye
    /// tanto al jugador como al propio trigger a mitad del bucle.
    /// </summary>
    static IEnumerator StepOnto(Component target)
    {
        Assert.That(GameObject.FindGameObjectWithTag("Player"), Is.Not.Null,
            "no Player in the scene");
        for (int frame = 0; frame < 6; frame++)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null || target == null) yield break;
            player.transform.position = target.transform.position;
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
            yield return new WaitForFixedUpdate();
        }
    }

    IEnumerator EnterRoom04()
    {
        GameProgress.ClearSecret(SecretId);
        GameProgress.ClearRoomCompleted(RoomId);
        PlayerPrefs.Save();

        GameManager.Instance.StartLevel(4);
        yield return WaitForScene("Room_04");
        Neutralise();

        Assert.That(GameProgress.IsSecretDiscovered(SecretId), Is.False,
            "the secret should start undiscovered");
        Assert.That(GameProgress.IsRoomCompleted(RoomId), Is.False,
            "the room should start uncompleted");
    }

    [UnityTest]
    public IEnumerator Room04_CompletesWithoutTheSecretPassage()
    {
        yield return EnterRoom04();

        var exit = Object.FindFirstObjectByType<ExitTrigger>();
        Assert.That(exit, Is.Not.Null, "Room_04 has no ExitTrigger");

        yield return StepOnto(exit);
        yield return WaitForScene("Room_05");

        Assert.That(GameProgress.IsRoomCompleted(RoomId), Is.True,
            "reaching the exit must complete the dungeons");
        Assert.That(GameProgress.IsSecretDiscovered(SecretId), Is.False,
            "the secret must stay undiscovered when the passage is skipped");
    }

    [UnityTest]
    public IEnumerator Room04_CompletesThroughTheSecretPassage()
    {
        yield return EnterRoom04();

        var secret = Object.FindFirstObjectByType<SecretDiscovery>();
        Assert.That(secret, Is.Not.Null, "Room_04 has no SecretDiscovery");

        yield return StepOnto(secret);
        Assert.That(GameProgress.IsSecretDiscovered(SecretId), Is.True,
            "stepping on the passage must record the secret");

        var exit = Object.FindFirstObjectByType<ExitTrigger>();
        yield return StepOnto(exit);
        yield return WaitForScene("Room_05");

        Assert.That(GameProgress.IsRoomCompleted(RoomId), Is.True,
            "the room must still complete when the passage was taken");
        Assert.That(GameProgress.IsSecretDiscovered(SecretId), Is.True,
            "the secret must survive the room transition");
    }
}
