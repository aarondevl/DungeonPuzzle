using UnityEngine;

public sealed class RoomIdentity : MonoBehaviour
{
    public static RoomIdentity Current { get; private set; }

    [SerializeField] string roomId;
    [SerializeField] string displayName;
    [SerializeField] Color accentColor = new(0.31f, 0.91f, 0.86f, 1f);
    [SerializeField] string ambienceKey = "Music/dungeon_ambient";

    public string RoomId => roomId;
    public string DisplayName => displayName;
    public Color AccentColor => accentColor;
    public string AmbienceKey => ambienceKey;
    public string ResolvedAmbienceKey => string.IsNullOrWhiteSpace(ambienceKey)
        ? "Music/dungeon_ambient" : ambienceKey;

    public string ResolvedDisplayName(string sceneName) =>
        string.IsNullOrWhiteSpace(displayName) ? sceneName : displayName;

    void Awake() => Current = this;
    void OnDestroy() { if (Current == this) Current = null; }
}
