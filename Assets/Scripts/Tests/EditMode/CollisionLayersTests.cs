using NUnit.Framework;

public class CollisionLayersTests
{
    [Test]
    public void Contains_DetectaLaCapaIncluidaEnLaMascara()
    {
        Assert.IsTrue(CollisionLayers.Contains(CollisionLayers.WallsMask, CollisionLayers.Walls));
        Assert.IsFalse(CollisionLayers.Contains(CollisionLayers.WallsMask, CollisionLayers.Player));
    }

    [Test]
    public void NoiseSurfaces_IncluyeMurosYAccionables()
    {
        Assert.IsTrue(CollisionLayers.Contains(CollisionLayers.NoiseSurfacesMask, CollisionLayers.Walls));
        Assert.IsTrue(CollisionLayers.Contains(CollisionLayers.NoiseSurfacesMask, CollisionLayers.Interactable));
        // El jugador nunca debe hacer que su propia piedra genere ruido.
        Assert.IsFalse(CollisionLayers.Contains(CollisionLayers.NoiseSurfacesMask, CollisionLayers.Player));
    }

    [Test]
    public void Resolve_UsaElValorPorDefectoCuandoElInspectorEstaVacio()
    {
        Assert.AreEqual(CollisionLayers.WallsMask, CollisionLayers.Resolve(0, CollisionLayers.WallsMask));
    }

    [Test]
    public void Resolve_RespetaLaMascaraConfiguradaEnElInspector()
    {
        Assert.AreEqual(CollisionLayers.ItemsMask,
            CollisionLayers.Resolve(CollisionLayers.ItemsMask, CollisionLayers.WallsMask));
    }
}
