using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class GenerateArcaneAmbience
{
    const int Rate = 44100;
    const int Seconds = 12;
    const int Seed = 42817;

    enum Mood { Patio, Guard, Library, Dungeons, Tower }

    [MenuItem("DungeonPuzzle/Audio/Generate Arcane Ambience")]
    public static void Generate()
    {
        Write("Assets/Resources/Audio/Music/patio_hall_ambient.wav", Build(Mood.Patio, Seed));
        Write("Assets/Resources/Audio/Music/guard_wing_ambient.wav", Build(Mood.Guard, Seed + 1));
        Write("Assets/Resources/Audio/Music/library_ambient.wav", Build(Mood.Library, Seed + 2));
        Write("Assets/Resources/Audio/Music/dungeons_ambient.wav", Build(Mood.Dungeons, Seed + 3));
        Write("Assets/Resources/Audio/Music/tower_ambient.wav", Build(Mood.Tower, Seed + 4));
        AssetDatabase.Refresh();
        ConfigureImports();
    }

    static float[] Build(Mood mood, int seed)
    {
        var random = new System.Random(seed);
        var samples = new float[Rate * Seconds];
        float wind = 0f;
        for (int i = 0; i < samples.Length; i++)
        {
            float t = i / (float)Rate;
            wind = Mathf.Lerp(wind, (float)(random.NextDouble() * 2.0 - 1.0), 0.004f);
            float sample = mood switch
            {
                Mood.Patio => wind * 0.22f + Crackle(random),
                Mood.Guard => wind * 0.13f + Pulse(t, 2f, 55f, 0.16f),
                Mood.Library => Drone(t, 0.16f) + Glass(t, 3.7f, 0.08f),
                Mood.Dungeons => wind * 0.10f + Drop(t, 3.1f, 0.13f),
                Mood.Tower => Drone(t, 0.12f) + TowerPulse(t),
                _ => 0f
            };
            samples[i] = sample;
        }
        CrossfadeLoop(samples, Rate / 4);
        Normalize(samples, 0.45f);
        return samples;
    }

    static float Crackle(System.Random random) =>
        random.NextDouble() < 0.0009 ? (float)(random.NextDouble() * 0.18 - 0.09) : 0f;

    static float Pulse(float t, float period, float hz, float gain)
    {
        float phase = t % period;
        return phase < 0.45f
            ? Mathf.Sin(2f * Mathf.PI * hz * phase) * Mathf.Exp(-phase * 9f) * gain
            : 0f;
    }

    static float Drone(float t, float gain) => gain *
        (Mathf.Sin(2f * Mathf.PI * 110f * t) * 0.55f
        + Mathf.Sin(2f * Mathf.PI * 164.81f * t) * 0.35f);

    static float Glass(float t, float period, float gain)
    {
        float phase = t % period;
        return phase < 0.8f
            ? Mathf.Sin(2f * Mathf.PI * 1318.51f * phase) * Mathf.Exp(-phase * 5f) * gain
            : 0f;
    }

    static float Drop(float t, float period, float gain)
    {
        float phase = t % period;
        float frequency = Mathf.Lerp(900f, 420f, Mathf.Clamp01(phase / 0.35f));
        return phase < 0.35f
            ? Mathf.Sin(2f * Mathf.PI * frequency * phase) * Mathf.Exp(-phase * 11f) * gain
            : 0f;
    }

    static float TowerPulse(float t)
    {
        int note = Mathf.FloorToInt(t / 2f) % 3;
        float hz = note == 0 ? 55f : note == 1 ? 82.41f : 110f;
        return Pulse(t, 2f, hz, 0.18f);
    }

    static void CrossfadeLoop(float[] samples, int count)
    {
        int start = samples.Length - count;
        for (int i = 0; i < count; i++)
        {
            float t = (i + 1f) / count;
            samples[start + i] = Mathf.Lerp(samples[start + i], samples[i], t);
        }
    }

    static void Normalize(float[] samples, float peak)
    {
        float max = 0f;
        foreach (float sample in samples) max = Mathf.Max(max, Mathf.Abs(sample));
        if (max <= 0f) return;
        float scale = peak / max;
        for (int i = 0; i < samples.Length; i++) samples[i] *= scale;
    }

    static void Write(string path, float[] samples)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        using var writer = new BinaryWriter(File.Create(path));
        writer.Write(new[] { 'R', 'I', 'F', 'F' });
        writer.Write(36 + samples.Length * 2);
        writer.Write(new[] { 'W', 'A', 'V', 'E' });
        writer.Write(new[] { 'f', 'm', 't', ' ' });
        writer.Write(16); writer.Write((short)1); writer.Write((short)1);
        writer.Write(Rate); writer.Write(Rate * 2); writer.Write((short)2); writer.Write((short)16);
        writer.Write(new[] { 'd', 'a', 't', 'a' });
        writer.Write(samples.Length * 2);
        foreach (float sample in samples)
            writer.Write((short)(Mathf.Clamp(sample, -1f, 1f) * short.MaxValue));
    }

    static void ConfigureImports()
    {
        string[] paths = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Resources/Audio/Music" });
        foreach (string guid in paths)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.Contains("_ambient")) continue;
            var importer = (AudioImporter)AssetImporter.GetAtPath(path);
            importer.forceToMono = true;
            importer.defaultSampleSettings = new AudioImporterSampleSettings
            {
                loadType = AudioClipLoadType.Streaming,
                compressionFormat = AudioCompressionFormat.Vorbis,
                quality = 0.55f,
                preloadAudioData = false
            };
            importer.SaveAndReimport();
        }
    }
}
