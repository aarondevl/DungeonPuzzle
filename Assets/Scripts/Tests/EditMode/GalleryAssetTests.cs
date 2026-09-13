using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    [Test]
    public void BiomeGallery_HasPlayerBiomesAndThreeReturnPortals()
    {
        const string scenePath = "Assets/Scenes/Prototypes/BiomeGallery_Demo.unity";
        Scene gallery = SceneManager.GetSceneByPath(scenePath);
        bool openedForTest = !gallery.IsValid() || !gallery.isLoaded;
        if (openedForTest)
            gallery = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

        try
        {
            GameObject[] roots = gallery.GetRootGameObjects();
            Assert.That(System.Array.Exists(roots, root => root.name == "CentralPlaza"), Is.True);
            Assert.That(System.Array.Exists(roots, root => root.name == "Biome_Forest"), Is.True);
            Assert.That(System.Array.Exists(roots, root => root.name == "Biome_ArcaneRuins"), Is.True);
            Assert.That(System.Array.Exists(roots, root => root.name == "Biome_AlchemyMarsh"), Is.True);

            GameObject systems = System.Array.Find(roots, root => root.name == "Systems");
            Assert.That(systems, Is.Not.Null);
            Assert.That(systems.transform.Find("Player"), Is.Not.Null);
            Assert.That(systems.GetComponentInChildren<SpawnPoint>(true), Is.Not.Null);

            int portalCount = 0;
            foreach (GameObject root in roots)
                portalCount += root.GetComponentsInChildren<GalleryReturnPortal>(true).Length;
            Assert.That(portalCount, Is.EqualTo(3));
        }
        finally
        {
            if (openedForTest)
                EditorSceneManager.CloseScene(gallery, true);
        }
    }
}
