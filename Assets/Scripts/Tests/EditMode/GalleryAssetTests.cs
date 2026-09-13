using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class GalleryAssetTests
{
    static readonly string[] AnimatedPrefabs =
    {
        "Gallery_Knight", "Gallery_Rogue", "Gallery_Mage", "Gallery_Fire",
        "Gallery_EnergyBarrier", "Gallery_IceCrystal", "Gallery_PoisonCloud"
    };

    [Test]
    public void AnimatedGalleryPrefabs_HaveRendererAndFlipbook()
    {
        foreach (string name in AnimatedPrefabs)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"Assets/Prefabs/Gallery/{name}.prefab");
            Assert.That(prefab, Is.Not.Null, name);
            Assert.That(prefab.GetComponentInChildren<SpriteRenderer>(), Is.Not.Null, name);
            Assert.That(prefab.GetComponentInChildren<Flipbook>(), Is.Not.Null, name);
        }
    }

    [Test]
    public void RangerDisplay_IsPresentationOnly()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefabs/Gallery/Gallery_RangerDisplay.prefab");
        Assert.That(prefab, Is.Not.Null);
        Assert.That(prefab.GetComponentInChildren<SpriteRenderer>(), Is.Not.Null);
        Assert.That(prefab.GetComponentInChildren<Rigidbody2D>(), Is.Null);
    }
}
