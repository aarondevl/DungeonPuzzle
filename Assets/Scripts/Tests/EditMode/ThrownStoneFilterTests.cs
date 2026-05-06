using NUnit.Framework;

public class ThrownStoneFilterTests
{
    [Test]
    public void ShouldTriggerNoise_WhenLayerMatchesMask_ReturnsTrue()
    {
        int mask = 1 << 8;        // "Walls" layer
        int hitLayer = 8;
        Assert.IsTrue(ThrownStone.ShouldTriggerNoise(mask, hitLayer));
    }

    [Test]
    public void ShouldTriggerNoise_WhenLayerOutsideMask_ReturnsFalse()
    {
        int mask = 1 << 8;
        int hitLayer = 9;        // "Items"
        Assert.IsFalse(ThrownStone.ShouldTriggerNoise(mask, hitLayer));
    }

    [Test]
    public void ShouldTriggerNoise_WhenMaskIsEmpty_ReturnsFalse()
    {
        Assert.IsFalse(ThrownStone.ShouldTriggerNoise(0, 8));
    }
}
