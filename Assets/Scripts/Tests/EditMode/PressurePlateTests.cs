using NUnit.Framework;

public class PressurePlateTests
{
    [Test]
    public void SeAccionaMientrasHayaAlMenosUnOcupante()
    {
        Assert.IsTrue(PressurePlate.ShouldBePressed(1, false, false));
        Assert.IsTrue(PressurePlate.ShouldBePressed(2, false, false));
    }

    [Test]
    public void SeSueltaCuandoElUltimoOcupanteSeVa()
    {
        Assert.IsFalse(PressurePlate.ShouldBePressed(0, false, false));
    }

    [Test]
    public void ConLatchingSiguePulsadaAunqueSeQuedeVacia()
    {
        Assert.IsTrue(PressurePlate.ShouldBePressed(0, true, true));
    }

    [Test]
    public void ConLatchingNoSeActivaSolaSiNuncaLaPisaron()
    {
        Assert.IsFalse(PressurePlate.ShouldBePressed(0, true, false));
    }
}
