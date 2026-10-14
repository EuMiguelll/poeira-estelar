using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

// Passo 1 e 2 da montagem do projeto: recursos do TextMeshPro, importação de sprites/áudio,
// tags, camadas, fonte, materiais e configurações de build para WebGL.
public static class ProjectSetup
{
    public const string FontPath = "Assets/Fonts/KenneyFuture SDF.asset";
    public const string FontGlowPath = "Assets/Fonts/KenneyFuture SDF - Brilho.mat";
    public const string MaterialsFolder = "Assets/Art/Materiais";
    public const int WallLayer = 8;

    static readonly string[] Tags = { "Coletavel", "Oxigenio", "Inimigo" };

    [MenuItem("Poeira Estelar/1. Importar recursos do TextMeshPro")]
    public static void ImportTmpEssentials()
    {
        if (AssetDatabase.IsValidFolder("Assets/TextMesh Pro"))
        {
            Debug.Log("[Setup] TMP Essential Resources já importado.");
            return;
        }
        AssetDatabase.ImportPackage("Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage", false);
        Debug.Log("[Setup] Importando TMP Essential Resources...");
    }

    [MenuItem("Poeira Estelar/2. Configurar assets e projeto")]
    public static void ConfigureAll()
    {
        ConfigureTagsAndLayers();
        ConfigureSprites();
        ConfigureAudio();
        CreateFont();
        CreateMaterials();
        ConfigurePlayerSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Setup] Assets e projeto configurados.");
    }

    static void ConfigureTagsAndLayers()
    {
        var manager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var tags = manager.FindProperty("tags");
        foreach (var tag in Tags)
        {
            bool exists = false;
            for (int i = 0; i < tags.arraySize; i++)
                if (tags.GetArrayElementAtIndex(i).stringValue == tag) exists = true;
            if (exists) continue;
            tags.InsertArrayElementAtIndex(tags.arraySize);
            tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
        }

        var layers = manager.FindProperty("layers");
        layers.GetArrayElementAtIndex(WallLayer).stringValue = "Paredes";
        manager.ApplyModifiedProperties();
    }

    static void ConfigureSprites()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string file = Path.GetFileNameWithoutExtension(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMode = (int)SpriteImportMode.Single;
            settings.mipmapEnabled = false;
            settings.alphaIsTransparency = true;
            settings.filterMode = FilterMode.Bilinear;
            settings.wrapMode = TextureWrapMode.Clamp;
            settings.spritePixelsPerUnit = 100;
            settings.spriteMeshType = SpriteMeshType.Tight;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            settings.spriteBorder = Vector4.zero;

            if (path.Contains("/Basicos/"))
            {
                settings.spritePixelsPerUnit = file == "Quadrado" ? 64 : (file == "Vortice" ? 256 : 128);
                settings.spriteMeshType = SpriteMeshType.FullRect;
            }
            else if (path.Contains("/Backgrounds/"))
            {
                settings.wrapMode = TextureWrapMode.Repeat;
                settings.spriteMeshType = SpriteMeshType.FullRect;
            }
            else if (path.Contains("/Planets/"))
            {
                settings.spritePixelsPerUnit = 400;
                importer.maxTextureSize = 512;
                importer.textureCompression = TextureImporterCompression.Compressed;
            }
            else if (path.Contains("/UI/"))
            {
                settings.spriteMeshType = SpriteMeshType.FullRect;
                if (file.StartsWith("button_rectangle")) settings.spriteBorder = new Vector4(16, 20, 16, 16);
                else if (file.StartsWith("panel")) settings.spriteBorder = new Vector4(20, 20, 20, 20);
                else if (file.StartsWith("bar_")) settings.spriteBorder = new Vector4(12, 0, 12, 0);
            }

            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }
    }

    static void ConfigureAudio()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = (AudioImporter)AssetImporter.GetAtPath(path);
            var settings = importer.defaultSampleSettings;
            bool music = path.Contains("/Music/");
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = music ? 0.55f : 0.7f;
            settings.loadType = music ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
            importer.defaultSampleSettings = settings;
            importer.loadInBackground = music;
            importer.SaveAndReimport();
        }
    }

    static void CreateFont()
    {
        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath) != null) return;

        var source = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Kenney Future.ttf");
        var font = TMP_FontAsset.CreateFontAsset(source, 64, 7, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, false);
        font.name = "KenneyFuture SDF";
        AssetDatabase.CreateAsset(font, FontPath);
        font.atlasTexture.name = "KenneyFuture SDF Atlas";
        AssetDatabase.AddObjectToAsset(font.atlasTexture, font);
        font.material.name = "KenneyFuture SDF Material";
        AssetDatabase.AddObjectToAsset(font.material, font);

        var characters = new List<char>();
        for (char c = ' '; c <= '~'; c++) characters.Add(c);
        characters.AddRange("ÁÀÂÃÉÊÍÓÔÕÚÜÇáàâãéêíóôõúüç·•×º");
        font.TryAddCharacters(new string(characters.ToArray()), out string missing);
        if (!string.IsNullOrEmpty(missing)) Debug.LogWarning($"[Setup] Caracteres sem glifo na fonte: {missing}");

        // Atlas fixo: tudo que o jogo usa já está gerado (mais seguro no WebGL).
        font.atlasPopulationMode = AtlasPopulationMode.Static;
        EditorUtility.SetDirty(font.atlasTexture);
        var fallback = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        if (fallback != null) font.fallbackFontAssetTable = new List<TMP_FontAsset> { fallback };
        EditorUtility.SetDirty(font);

        var glow = new Material(font.material) { name = "KenneyFuture SDF - Brilho" };
        glow.EnableKeyword("UNDERLAY_ON");
        glow.SetColor("_UnderlayColor", new Color(0.3f, 0.8f, 1f, 0.75f));
        glow.SetFloat("_UnderlaySoftness", 0.8f);
        glow.SetFloat("_UnderlayDilate", 0.4f);
        AssetDatabase.CreateAsset(glow, FontGlowPath);
    }

    static void CreateMaterials()
    {
        if (!AssetDatabase.IsValidFolder(MaterialsFolder)) AssetDatabase.CreateFolder("Assets/Art", "Materiais");
        var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");

        var particle = new Material(shader) { name = "Particula" };
        particle.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Kenney/Effects/star1.png");
        CreateOrReplace(particle, MaterialsFolder + "/Particula.mat");

        var trail = new Material(shader) { name = "Rastro" };
        CreateOrReplace(trail, MaterialsFolder + "/Rastro.mat");
    }

    static void CreateOrReplace(Object asset, string path)
    {
        if (AssetDatabase.LoadAssetAtPath<Object>(path) != null) AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(asset, path);
    }

    static void ConfigurePlayerSettings()
    {
        PlayerSettings.productName = "Poeira Estelar";
        PlayerSettings.companyName = "Miguel Damasio";
        PlayerSettings.defaultWebScreenWidth = 960;
        PlayerSettings.defaultWebScreenHeight = 600;
        PlayerSettings.runInBackground = true;
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.template = "APPLICATION:Default";

        var icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Kenney/Ships/playerShip1_blue.png");
        PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
    }
}
