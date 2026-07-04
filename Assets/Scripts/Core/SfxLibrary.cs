using System.Collections.Generic;
using UnityEngine;

/// <summary>Carga perezosa de clips desde Resources/Audio y reproducción vía AudioMaster.</summary>
public static class SfxLibrary
{
    static readonly Dictionary<string, AudioClip> Cache = new();

    public static AudioClip Get(string path)
    {
        if (!Cache.TryGetValue(path, out var clip))
        {
            clip = Resources.Load<AudioClip>("Audio/" + path);
            if (clip == null) Debug.LogWarning($"SfxLibrary: falta Resources/Audio/{path}");
            Cache[path] = clip;
        }
        return clip;
    }

    public static void Play(string path, float volume = 1f)
    {
        if (AudioMaster.Instance != null)
            AudioMaster.Instance.PlaySFX(Get(path), volume);
    }
}
