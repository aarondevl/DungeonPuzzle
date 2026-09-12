using System.Reflection;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// El cono se dibuja en espacio local pero se consulta con física en espacio de
/// mundo. Como el guardia viene escalado (0.7 en los prefabs), ambos alcances solo
/// coinciden si el componente convierte <c>distance</c> antes de preguntar a la
/// física. Sin esa conversión el guardia reconocía al jugador bastante más lejos
/// de donde termina el cono visible.
/// </summary>
public class VisionConeRangeTests
{
    const BindingFlags Inst = BindingFlags.Instance | BindingFlags.NonPublic;
    const float Scale = 0.7f;
    const float Distance = 5f;
    const float NominalWorldReach = Distance * Scale; // 3.5

    static VisionCone NewCone(out GameObject root)
    {
        root = new GameObject("GuardRoot");
        root.transform.localScale = new Vector3(Scale, Scale, 1f);
        var coneGo = new GameObject("VisionCone");
        coneGo.transform.SetParent(root.transform, false);
        coneGo.AddComponent<MeshFilter>();
        coneGo.AddComponent<MeshRenderer>();
        var cone = coneGo.AddComponent<VisionCone>();
        cone.angle = 50f;
        cone.distance = Distance;
        typeof(VisionCone).GetMethod("Awake", Inst).Invoke(cone, null);
        return cone;
    }

    static void Rebuild(VisionCone cone) =>
        typeof(VisionCone).GetMethod("BuildMesh", Inst).Invoke(cone, null);

    /// <summary>Distancia en mundo, sobre el eje frontal, hasta donde el cono reconoce.</summary>
    static float DetectionReach(VisionCone cone)
    {
        var inside = typeof(VisionCone).GetMethod("PointInsideCone", Inst);
        Vector3 origin = cone.transform.position;
        Vector3 forward = cone.transform.up;
        float reach = 0f;
        for (float d = 0.05f; d <= 12f; d += 0.05f)
        {
            var local = cone.transform.InverseTransformPoint(origin + forward * d);
            if ((bool)inside.Invoke(cone, new object[] { local })) reach = d;
        }
        return reach;
    }

    static GameObject NewWall(VisionCone cone, float worldDistance, float thickness)
    {
        var wall = new GameObject("Wall") { layer = CollisionLayers.Walls };
        wall.transform.position =
            cone.transform.position + cone.transform.up * (worldDistance + thickness / 2f);
        wall.AddComponent<BoxCollider2D>().size = new Vector2(6f, thickness);
        Physics2D.SyncTransforms();
        return wall;
    }

    [Test]
    public void Detection_StopsAtConeReach_WhenNothingBlocksTheView()
    {
        var cone = NewCone(out var root);
        Rebuild(cone);
        Assert.That(DetectionReach(cone), Is.EqualTo(NominalWorldReach).Within(0.1f));
        Object.DestroyImmediate(root);
    }

    [Test]
    public void Detection_DoesNotStretchToWallsBeyondConeReach()
    {
        var cone = NewCone(out var root);
        // Muro a 4.2: fuera del cono visible (3.5) pero dentro del radio en bruto (5).
        var wall = NewWall(cone, 4.2f, 0.8f);
        Rebuild(cone);

        float reach = DetectionReach(cone);

        Object.DestroyImmediate(wall);
        Object.DestroyImmediate(root);

        Assert.That(reach, Is.EqualTo(NominalWorldReach).Within(0.1f),
            $"detection stretched to {reach:0.###} world units; the visible cone ends at {NominalWorldReach}");
    }

    [Test]
    public void Detection_StillStopsEarly_WhenWallIsInsideConeReach()
    {
        var cone = NewCone(out var root);
        var wall = NewWall(cone, 2f, 0.8f);
        Rebuild(cone);

        float reach = DetectionReach(cone);

        Object.DestroyImmediate(wall);
        Object.DestroyImmediate(root);

        Assert.That(reach, Is.EqualTo(2f).Within(0.15f),
            $"a wall at 2 world units must occlude the cone, but detection reached {reach:0.###}");
    }
}
