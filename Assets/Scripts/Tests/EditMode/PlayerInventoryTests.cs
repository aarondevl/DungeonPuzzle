using NUnit.Framework;

public class PlayerInventoryTests
{
    [Test]
    public void ConLaManoLibreSePuedeRecogerCualquierCosa()
    {
        Assert.IsTrue(PlayerInventory.CanPickUp(consumable: false, handIsFull: false));
        Assert.IsTrue(PlayerInventory.CanPickUp(consumable: true, handIsFull: false));
    }

    [Test]
    public void ConLaManoOcupadaNoSePuedeGuardarOtroObjeto()
    {
        Assert.IsFalse(PlayerInventory.CanPickUp(consumable: false, handIsFull: true));
    }

    [Test]
    public void UnConsumibleSeRecogeAunqueLaManoEsteOcupada()
    {
        // La llave abre su puerta en el acto: si ocupara la ranura, el jugador se
        // quedaba sin poder recoger ni lanzar nada el resto de la sala (Room_05).
        Assert.IsTrue(PlayerInventory.CanPickUp(consumable: true, handIsFull: true));
    }
}
