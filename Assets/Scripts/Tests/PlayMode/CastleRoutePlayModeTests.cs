using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Recorrido completo del castillo (Task 10, paso 2), en su parte automatizable:
/// desde una partida nueva, Patio lleva a Torre pasando por las cinco salas sin
/// volver al menú, cada sala queda marcada como completada y la salida final da la
/// victoria.
///
/// ALCANCE: esto verifica que la RUTA está cableada, no que los puzles se puedan
/// resolver. El jugador se coloca sobre cada salida en vez de jugar la sala, así que
/// no prueba palancas, llaves ni puertas, y el tiempo que tarda no dice nada sobre
/// los 20-30 minutos de primera partida que pide el plan. Eso sigue necesitando una
/// sesión humana.
/// </summary>
public class CastleRoutePlayModeTests
{
    static readonly string[] RoomIds =
    {
        "patio_hall", "guard_wing", "arcane_library", "dungeons", "warden_tower"
    };

    static readonly string[] PrefKeys =
    {
        "DP.HighestUnlocked", "DP.TotalDeaths",
        "DP.Castle.Room.patio_hall.Complete", "DP.Castle.Room.guard_wing.Complete",
        "DP.Castle.Room.arcane_library.Complete", "DP.Castle.Room.dungeons.Complete",
        "DP.Castle.Room.warden_tower.Complete", "DP.Castle.Secret.room_04_passage",
        "DP.BestTime.Room_01", "DP.BestTime.Room_02", "DP.BestTime.Room_03",
        "DP.BestTime.Room_04", "DP.BestTime.Room_05"
    };

    readonly Dictionary<string, int> _savedInts = new Dictionary<string, int>();
    readonly Dictionary<string, float> _savedFloats = new Dictionary<string, float>();
    readonly List<string> _absentKeys = new List<string>();

    [SetUp]
    public void SnapshotProgress()
    {
        _savedInts.Clear();
        _savedFloats.Clear();
        _absentKeys.Clear();
        foreach (string key in PrefKeys)
        {
            if (!PlayerPrefs.HasKey(key)) { _absentKeys.Add(key); continue; }
            if (key.StartsWith("DP.BestTime.")) _savedFloats[key] = PlayerPrefs.GetFloat(key);
            else _savedInts[key] = PlayerPrefs.GetInt(key);
        }
    }

    [TearDown]
    public void RestoreProgress()
    {
        foreach (var pair in _savedInts) PlayerPrefs.SetInt(pair.Key, pair.Value);
        foreach (var pair in _savedFloats) PlayerPrefs.SetFloat(pair.Key, pair.Value);
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
        foreach (var guard in Object.FindObjectsByType<GuardBase>(FindObjectsSortMode.None))
            guard.enabled = false;
        foreach (var trap in Object.FindObjectsByType<SpikeTrap>(FindObjectsSortMode.None))
            trap.enabled = false;
    }

    static IEnumerator StepOntoExit()
    {
        var exit = Object.FindFirstObjectByType<ExitTrigger>();
        Assert.That(exit, Is.Not.Null, "the room has no ExitTrigger");
        for (int frame = 0; frame < 6; frame++)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null || exit == null) yield break;
            player.transform.position = exit.transform.position;
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
            yield return new WaitForFixedUpdate();
        }
    }

    [UnityTest]
    public IEnumerator FreshSave_RunsPatioToTowerWithoutReturningToTheMenu()
    {
        foreach (string id in RoomIds) GameProgress.ClearRoomCompleted(id);
        GameProgress.ClearSecret("room_04_passage");
        PlayerPrefs.Save();

        var gm = GameManager.Instance;
        gm.StartLevel(1);
        yield return WaitForScene("Room_01");
        Assert.That(gm.Lives, Is.EqualTo(3));

        for (int level = 1; level <= 4; level++)
        {
            string current = $"Room_{level:00}";
            string next = $"Room_{level + 1:00}";
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(current));

            yield return StepOntoExit();
            yield return WaitForScene(next);

            Assert.That(GameProgress.IsRoomCompleted(RoomIds[level - 1]), Is.True,
                $"leaving {current} must mark '{RoomIds[level - 1]}' completed");
            Assert.That(gm.Lives, Is.EqualTo(3),
                $"travelling from {current} to {next} must not cost a life");
        }

        // La salida de la Torre es la final: da la victoria en lugar de viajar.
        yield return StepOntoExit();
        yield return WaitForScene("GameOver");

        Assert.That(gm.IsWin, Is.True, "the tower exit must produce a win");
        foreach (string id in RoomIds)
            Assert.That(GameProgress.IsRoomCompleted(id), Is.True,
                $"room '{id}' was not recorded as completed after the full run");
    }
}
