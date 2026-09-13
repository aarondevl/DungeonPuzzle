using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class PlayerAnimatorDirectionTests
{
    const string ControllerPath = "Assets/Animations/Player/TopDown/Layered/PlayerLayered.controller";
    const string IdleClipsFolder = "Assets/Animations/Player/TopDown/Layered";

    [TestCase("Idle", 0f, -1f, "Player_Idle_down")]
    [TestCase("Idle", 0f, 1f, "Player_Idle_up")]
    [TestCase("Idle", -1f, 0f, "Player_Idle_left")]
    [TestCase("Idle", 1f, 0f, "Player_Idle_right")]
    [TestCase("Walk", 0f, -1f, "Player_Walk_down")]
    [TestCase("Walk", 0f, 1f, "Player_Walk_up")]
    [TestCase("Walk", -1f, 0f, "Player_Walk_left")]
    [TestCase("Walk", 1f, 0f, "Player_Walk_right")]
    public void BlendTreeUsesTheClipThatMatchesItsCardinalDirection(
        string stateName,
        float moveX,
        float moveY,
        string expectedClipName)
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        Assert.That(controller, Is.Not.Null, $"No se encontro el controller en {ControllerPath}");

        var state = controller.layers[0].stateMachine.states
            .Select(child => child.state)
            .Single(candidate => candidate.name == stateName);
        var blendTree = state.motion as BlendTree;
        Assert.That(blendTree, Is.Not.Null, $"El estado {stateName} debe usar un BlendTree");

        var direction = new Vector2(moveX, moveY);
        var childMotion = blendTree.children.Single(child => child.position == direction).motion;

        Assert.That(childMotion.name, Is.EqualTo(expectedClipName),
            $"La direccion {direction} del estado {stateName} usa el clip opuesto");
    }

    [TestCase("Player_Idle_down", "Unarmed_Idle_full_0")]
    [TestCase("Player_Idle_left", "Unarmed_Idle_full_12")]
    [TestCase("Player_Idle_right", "Unarmed_Idle_full_24")]
    [TestCase("Player_Idle_up", "Unarmed_Idle_full_36")]
    public void IdleClipUsesTheFirstSpriteFromItsDirectionRow(
        string clipName,
        string expectedSpriteName)
    {
        var firstSprite = FirstSpriteOf(clipName);
        Assert.That(firstSprite.name, Is.EqualTo(expectedSpriteName),
            $"{clipName} toma un cuadro de otra direccion de la hoja Idle");
    }

    [TestCase("Player_Idle_down", 6)]
    [TestCase("Player_Idle_left", 6)]
    [TestCase("Player_Idle_right", 6)]
    [TestCase("Player_Idle_up", 4)]
    public void IdleClipIsAnAnimatedLoop(string clipName, int expectedFrames)
    {
        var clip = LoadClip(clipName);
        var binding = AnimationUtility.GetObjectReferenceCurveBindings(clip)
            .Single(b => b.propertyName == "m_Sprite");
        Assert.That(AnimationUtility.GetObjectReferenceCurve(clip, binding).Length, Is.EqualTo(expectedFrames));
        Assert.That(clip.isLooping, Is.True, $"{clipName} debe repetirse mientras el heroe esta quieto");
    }

    // La hoja hero_green_walk tiene 4 filas de 64 px. Medidas desde abajo (convencion
    // de Unity): y=192 es la fila superior y muestra al heroe DE FRENTE (camina hacia
    // abajo), y=0 es la fila inferior y muestra su ESPALDA (camina hacia arriba).
    [TestCase("Player_Walk_down", "down_0", 192)]
    [TestCase("Player_Walk_left", "left_0", 128)]
    [TestCase("Player_Walk_right", "right_0", 64)]
    [TestCase("Player_Walk_up", "up_0", 0)]
    public void WalkClipUsesTheSheetRowThatFacesItsDirection(
        string clipName,
        string expectedSpriteName,
        int expectedRowY)
    {
        var firstSprite = FirstSpriteOf(clipName);
        Assert.That(firstSprite.name, Is.EqualTo(expectedSpriteName),
            $"{clipName} toma un cuadro de otra direccion de la hoja Walk");
        // sprite.rect es la celda de 64x64 de la hoja (textureRect seria el recorte sin alfa).
        Assert.That((int)firstSprite.rect.y, Is.EqualTo(expectedRowY),
            $"{clipName} usa la fila {firstSprite.rect.y} de la hoja, que mira hacia otro lado");
    }

    static AnimationClip LoadClip(string clipName)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{IdleClipsFolder}/{clipName}.anim");
        Assert.That(clip, Is.Not.Null, $"No se encontro el clip {clipName}");
        return clip;
    }

    static Sprite FirstSpriteOf(string clipName)
    {
        var clip = LoadClip(clipName);
        var spriteBinding = AnimationUtility.GetObjectReferenceCurveBindings(clip)
            .Single(binding => binding.propertyName == "m_Sprite");
        return (Sprite)AnimationUtility.GetObjectReferenceCurve(clip, spriteBinding)[0].value;
    }
}
