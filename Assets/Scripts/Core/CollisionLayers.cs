using UnityEngine;

/// <summary>
/// Única fuente de verdad de las capas de física 2D del proyecto.
///
/// Antes cada componente declaraba su propio <c>LayerMask</c> en el Inspector, de
/// modo que un prefab mal configurado rompía la detección en silencio. Centralizar
/// los índices y las máscaras aquí permite:
///   * que los scripts declaren su intención ("me interesan los muros") sin depender
///     de que alguien recuerde marcar la casilla correcta;
///   * documentar en un solo sitio la matriz de colisiones de Project Settings;
///   * validar en tiempo de ejecución que el proyecto tiene las capas esperadas.
///
/// Los índices deben coincidir con ProjectSettings/TagManager.asset.
/// </summary>
public static class CollisionLayers
{
    public const int Default = 0;
    public const int Player = 6;
    public const int Guard = 7;
    public const int Walls = 8;
    public const int Items = 9;
    public const int Interactable = 10;
    public const int Projectile = 11;
    public const int Hazard = 12;

    public static readonly int PlayerMask = 1 << Player;
    public static readonly int GuardMask = 1 << Guard;
    public static readonly int WallsMask = 1 << Walls;
    public static readonly int ItemsMask = 1 << Items;
    public static readonly int InteractableMask = 1 << Interactable;
    public static readonly int ProjectileMask = 1 << Projectile;
    public static readonly int HazardMask = 1 << Hazard;

    /// <summary>Superficies contra las que una piedra lanzada hace ruido al impactar.</summary>
    public static readonly int NoiseSurfacesMask = WallsMask | InteractableMask;

    /// <summary>Todo lo que el jugador puede accionar o recoger.</summary>
    public static readonly int UsableMask = ItemsMask | InteractableMask;

    /// <summary>Máscara efectiva: si el Inspector no define ninguna capa, usa el valor por defecto.</summary>
    public static int Resolve(LayerMask configured, int fallback) =>
        configured.value != 0 ? configured.value : fallback;

    /// <summary>true si <paramref name="layer"/> está incluida en <paramref name="mask"/>.</summary>
    public static bool Contains(int mask, int layer) => (mask & (1 << layer)) != 0;

    /// <summary>Nombre legible de la capa, para mensajes de diagnóstico.</summary>
    public static string NameOf(int layer) => LayerMask.LayerToName(layer);
}
