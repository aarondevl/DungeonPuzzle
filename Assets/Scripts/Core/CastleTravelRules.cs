using System;

public static class CastleTravelRules
{
    public static bool CanStart(bool isTransitioning, string sceneName, Func<string, bool> canLoad)
    {
        return !isTransitioning
            && !string.IsNullOrWhiteSpace(sceneName)
            && canLoad != null
            && canLoad(sceneName);
    }

    public static string NextScene(int currentLevel, int totalLevels)
    {
        int next = currentLevel + 1;
        return next <= totalLevels ? $"Room_{next:00}" : null;
    }
}
