using UnityEngine;

public static class GameProgress
{
    public const int TotalLevels = 5;
    const string KeyHighest = "DP.HighestUnlocked";
    const string KeyDeaths = "DP.TotalDeaths";
    const string KeyBestTimeFmt = "DP.BestTime.Room_{0:00}";
    const string KeyVolMaster = "DP.Vol.Master";
    const string KeyVolMusic = "DP.Vol.Music";
    const string KeyVolSfx = "DP.Vol.Sfx";
    const string KeyFullscreen = "DP.Display.Fullscreen";

    public static int HighestUnlocked
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt(KeyHighest, 1), 1, TotalLevels);
        set { PlayerPrefs.SetInt(KeyHighest, Mathf.Clamp(value, 1, TotalLevels)); PlayerPrefs.Save(); }
    }

    public static int TotalDeaths
    {
        get => PlayerPrefs.GetInt(KeyDeaths, 0);
        set { PlayerPrefs.SetInt(KeyDeaths, Mathf.Max(0, value)); PlayerPrefs.Save(); }
    }

    public static float GetBestTime(int level)
    {
        return PlayerPrefs.GetFloat(string.Format(KeyBestTimeFmt, level), 0f);
    }

    public static bool TrySetBestTime(int level, float time)
    {
        float current = GetBestTime(level);
        if (current > 0f && current <= time) return false;
        PlayerPrefs.SetFloat(string.Format(KeyBestTimeFmt, level), time);
        PlayerPrefs.Save();
        return true;
    }

    public static void RegisterDeath()
    {
        TotalDeaths = TotalDeaths + 1;
    }

    public static void Unlock(int level)
    {
        if (level > HighestUnlocked) HighestUnlocked = level;
    }

    public static bool IsUnlocked(int level) => level <= HighestUnlocked;

    public static float MasterVolume
    {
        get => PlayerPrefs.GetFloat(KeyVolMaster, 1f);
        set { PlayerPrefs.SetFloat(KeyVolMaster, Mathf.Clamp01(value)); PlayerPrefs.Save(); }
    }

    public static float MusicVolume
    {
        get => PlayerPrefs.GetFloat(KeyVolMusic, 0.8f);
        set { PlayerPrefs.SetFloat(KeyVolMusic, Mathf.Clamp01(value)); PlayerPrefs.Save(); }
    }

    public static float SfxVolume
    {
        get => PlayerPrefs.GetFloat(KeyVolSfx, 1f);
        set { PlayerPrefs.SetFloat(KeyVolSfx, Mathf.Clamp01(value)); PlayerPrefs.Save(); }
    }

    public static bool Fullscreen
    {
        get => PlayerPrefs.GetInt(KeyFullscreen, 1) == 1;
        set { PlayerPrefs.SetInt(KeyFullscreen, value ? 1 : 0); PlayerPrefs.Save(); }
    }

    public static void ResetAll()
    {
        PlayerPrefs.DeleteKey(KeyHighest);
        PlayerPrefs.DeleteKey(KeyDeaths);
        for (int i = 1; i <= TotalLevels; i++)
            PlayerPrefs.DeleteKey(string.Format(KeyBestTimeFmt, i));
        PlayerPrefs.Save();
    }

    public static string FormatTime(float seconds)
    {
        if (seconds <= 0f) return "--:--";
        int m = (int)(seconds / 60f);
        float s = seconds - m * 60f;
        return $"{m:00}:{s:00.00}";
    }
}
