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

    static readonly string[] EnvironmentTextures =
    {
        "Assets/Sprites/Environment/ThirdParty/KnowledgeTemple/Tileset.png",
        "Assets/Sprites/Environment/ThirdParty/DeadSwamp/dead-swamp-v4.png",
        "Assets/Sprites/Environment/ThirdParty/SwampACTG/SwampACTG.png"
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

    [Test]
    public void BiomeGallery_UsesPixelArtFromEveryEnvironmentPack()
    {
        const string scenePath = "Assets/Scenes/Prototypes/BiomeGallery_Demo.unity";
        string[] dependencies = AssetDatabase.GetDependencies(scenePath, true);

        foreach (string texturePath in EnvironmentTextures)
        {
            Assert.That(dependencies, Does.Contain(texturePath), texturePath);

            var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            Assert.That(importer, Is.Not.Null, texturePath);
            Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point), texturePath);
            Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed), texturePath);
            Assert.That(importer.mipmapEnabled, Is.False, texturePath);
        }

        Assert.That(AssetDatabase.LoadAssetAtPath<TextAsset>(
            "Assets/Sprites/Environment/ThirdParty/THIRD-PARTY-NOTICES.txt"), Is.Not.Null);

        Scene gallery = SceneManager.GetSceneByPath(scenePath);
        bool openedForTest = !gallery.IsValid() || !gallery.isLoaded;
        if (openedForTest)
            gallery = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

        try
        {
            GameObject[] roots = gallery.GetRootGameObjects();
            AssertBiomeUsesPack(roots, "Biome_Forest", "/SwampACTG/");
            AssertBiomeUsesPack(roots, "Biome_ArcaneRuins", "/KnowledgeTemple/");
            AssertBiomeUsesPack(roots, "Biome_AlchemyMarsh", "/DeadSwamp/");

            var usedEnvironmentTextures = new System.Collections.Generic.HashSet<string>();
            foreach (GameObject root in roots)
            foreach (SpriteRenderer renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                string path = AssetDatabase.GetAssetPath(renderer.sprite);
                if (path.StartsWith("Assets/Sprites/Environment/ThirdParty/"))
                    usedEnvironmentTextures.Add(path);
            }

            Assert.That(usedEnvironmentTextures.Count, Is.GreaterThan(EnvironmentTextures.Length));
            foreach (string texturePath in usedEnvironmentTextures)
                AssertPixelArtImporter(texturePath);
        }
        finally
        {
            if (openedForTest)
                EditorSceneManager.CloseScene(gallery, true);
        }
    }

    static void AssertBiomeUsesPack(GameObject[] roots, string biomeName, string packFolder)
    {
        GameObject biome = System.Array.Find(roots, root => root.name == biomeName);
        Assert.That(biome, Is.Not.Null, biomeName);

        bool usesPack = System.Array.Exists(
            biome.GetComponentsInChildren<SpriteRenderer>(true),
            renderer => AssetDatabase.GetAssetPath(renderer.sprite).Contains(packFolder));
        Assert.That(usesPack, Is.True, $"{biomeName} must use sprites from {packFolder}");
    }

    static void AssertPixelArtImporter(string texturePath)
    {
        var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        Assert.That(importer, Is.Not.Null, texturePath);
        Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point), texturePath);
        Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed), texturePath);
        Assert.That(importer.mipmapEnabled, Is.False, texturePath);
    }
}
