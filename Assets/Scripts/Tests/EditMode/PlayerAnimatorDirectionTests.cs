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
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{IdleClipsFolder}/{clipName}.anim");
        Assert.That(clip, Is.Not.Null);

        var spriteBinding = AnimationUtility.GetObjectReferenceCurveBindings(clip)
            .Single(binding => binding.propertyName == "m_Sprite");
        var firstSprite = AnimationUtility.GetObjectReferenceCurve(clip, spriteBinding)[0].value;

        Assert.That(firstSprite.name, Is.EqualTo(expectedSpriteName),
            $"{clipName} toma un cuadro de otra direccion de la hoja Idle");
    }
}
