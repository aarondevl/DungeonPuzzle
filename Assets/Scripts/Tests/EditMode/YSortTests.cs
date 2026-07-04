using NUnit.Framework;

public class YSortTests
{
    [Test]
    public void ObjetoMasAbajo_SeDibujaEncima()
    {
        Assert.Greater(YSort.ComputeOrder(-2f), YSort.ComputeOrder(1f));
    }

    [Test]
    public void Orden_EscalaPor100_YRedondea()
    {
        Assert.AreEqual(-150, YSort.ComputeOrder(1.5f));
        Assert.AreEqual(230, YSort.ComputeOrder(-2.3f));
        Assert.AreEqual(0, YSort.ComputeOrder(0f));
    }
}
