using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

// Passo 3: cria os prefabs, a cena do menu e a cena do jogo (arena, inimigos, HUD e menus).
public static class SceneBuilder
{
    const string PrefabFolder = "Assets/Prefabs";
    const string SceneFolder = "Assets/Scenes";
    const string MenuScenePath = SceneFolder + "/MainMenu.unity";
    const string GameScenePath = SceneFolder + "/Game.unity";

    static readonly Color WallCyan = new Color(0.35f, 0.9f, 1f);
    static readonly Color WallPink = new Color(1f, 0.4f, 0.85f);
    static readonly Color WallFill = new Color(0.09f, 0.07f, 0.2f);
    static readonly Color ButtonColor = new Color(0.22f, 0.42f, 0.85f);
    static readonly Color PanelColor = new Color(0.07f, 0.06f, 0.17f, 0.97f);

    static TMP_FontAsset font;
    static Material fontGlow;

    // ---------- Assets ----------

    static Sprite Sprite(string path)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null) Debug.LogError($"[Builder] Sprite não encontrado: {path}");
        return sprite;
    }

    static T Asset<T>(string path) where T : Object
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null) Debug.LogError($"[Builder] Asset não encontrado: {path}");
        return asset;
    }

    static Sprite Kenney(string file) => Sprite("Assets/Art/Kenney/" + file);
    static Sprite Basic(string file) => Sprite($"Assets/Art/Basicos/{file}.png");
    static AudioClip Sfx(string file) => Asset<AudioClip>($"Assets/Audio/SFX/{file}.ogg");

    [MenuItem("Poeira Estelar/3. Construir prefabs e cenas")]
    public static void BuildAll()
    {
        font = Asset<TMP_FontAsset>(ProjectSetup.FontPath);
        fontGlow = Asset<Material>(ProjectSetup.FontGlowPath);
        if (!AssetDatabase.IsValidFolder(PrefabFolder)) AssetDatabase.CreateFolder("Assets", "Prefabs");

        var prefabs = BuildPrefabs();
        BuildMenuScene(prefabs);
        BuildGameScene(prefabs);

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MenuScenePath, true),
            new EditorBuildSettingsScene(GameScenePath, true),
        };
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SceneFolder + "/SampleScene.unity") != null)
            AssetDatabase.DeleteAsset(SceneFolder + "/SampleScene.unity");

        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(MenuScenePath);
        Debug.Log("[Builder] Prefabs e cenas construídos.");
    }

    // ---------- Prefabs ----------

    class Prefabs
    {
        public GameObject player, star, oxygen, meteorBig, meteorMedium, drone, wall, portal, burst, music;
    }

    static GameObject SavePrefab(GameObject go, string name)
    {
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabFolder}/{name}.prefab");
        Object.DestroyImmediate(go);
        return prefab;
    }

    static SpriteRenderer SpriteChild(Transform parent, string name, Sprite sprite, int order, float scale = 1f, Color? color = null, Vector3? position = null)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position ?? Vector3.zero;
        go.transform.localScale = Vector3.one * scale;
        var r = go.AddComponent<SpriteRenderer>();
        r.sprite = sprite;
        r.sortingOrder = order;
        r.color = color ?? Color.white;
        return r;
    }

    static Prefabs BuildPrefabs()
    {
        var p = new Prefabs();
        p.burst = SavePrefab(BuildBurst(), "Explosao");
        p.player = SavePrefab(BuildPlayer(), "Player");
        p.star = SavePrefab(BuildStar(), "Estrela");
        p.oxygen = SavePrefab(BuildOxygen(), "CapsulaOxigenio");
        p.meteorBig = SavePrefab(BuildMeteor("MeteoroGrande", "Meteors/meteorBrown_big1.png", 1.15f, 0.48f), "MeteoroGrande");
        p.meteorMedium = SavePrefab(BuildMeteor("MeteoroMedio", "Meteors/meteorGrey_med2.png", 1.7f, 0.27f), "MeteoroMedio");
        p.drone = SavePrefab(BuildDrone(), "Drone");
        p.wall = SavePrefab(BuildWall(), "Parede");
        p.portal = SavePrefab(BuildPortal(), "Portal");
        p.music = SavePrefab(BuildMusicPlayer(), "MusicPlayer");
        return p;
    }

    static GameObject BuildBurst()
    {
        var go = new GameObject("Explosao");
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = 0.8f;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.75f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.stopAction = ParticleSystemStopAction.Destroy;
        main.useUnscaledTime = true;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 20) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.15f;

        var color = ps.colorOverLifetime;
        color.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        color.color = gradient;

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = Asset<Material>(ProjectSetup.MaterialsFolder + "/Particula.mat");
        renderer.sortingOrder = 30;
        return go;
    }

    static GameObject BuildPlayer()
    {
        var go = new GameObject("Player") { tag = "Player" };
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        go.AddComponent<CircleCollider2D>().radius = 0.3f;

        var audio = go.AddComponent<AudioSource>();
        audio.clip = Sfx("estrela");
        audio.playOnAwake = false;
        audio.loop = false;

        var glow = SpriteChild(go.transform, "Brilho", Basic("Brilho"), 8, 2.3f, new Color(0.4f, 0.85f, 1f, 0.3f));
        var visual = new GameObject("Visual");
        visual.transform.SetParent(go.transform, false);
        var flame = SpriteChild(visual.transform, "Chama", Kenney("Effects/fire11.png"), 9, 1.2f, Color.white, new Vector3(0f, -0.5f, 0f));
        var ship = SpriteChild(visual.transform, "Nave", Kenney("Ships/playerShip1_blue.png"), 10, 0.72f);

        var light = new GameObject("Luz").AddComponent<Light2D>();
        light.transform.SetParent(go.transform, false);
        light.lightType = Light2D.LightType.Point;
        light.color = new Color(0.5f, 0.85f, 1f);
        light.intensity = 0.8f;
        light.pointLightOuterRadius = 3f;
        light.pointLightInnerRadius = 0.2f;

        var trail = go.AddComponent<TrailRenderer>();
        trail.material = Asset<Material>(ProjectSetup.MaterialsFolder + "/Rastro.mat");
        trail.time = 0.3f;
        trail.minVertexDistance = 0.05f;
        trail.widthCurve = AnimationCurve.Linear(0f, 0.28f, 1f, 0f);
        var trailGradient = new Gradient();
        trailGradient.SetKeys(
            new[] { new GradientColorKey(new Color(0.5f, 0.9f, 1f), 0f), new GradientColorKey(new Color(0.6f, 0.4f, 1f), 1f) },
            new[] { new GradientAlphaKey(0.55f, 0f), new GradientAlphaKey(0f, 1f) });
        trail.colorGradient = trailGradient;
        trail.sortingOrder = 7;

        var movement = go.AddComponent<PlayerMovement>();
        movement.audioSource = audio;
        movement.oxygenClip = Sfx("oxigenio");
        movement.dashClip = Sfx("dash");
        movement.visual = visual.transform;
        movement.flame = flame.transform;
        movement.trail = trail;

        var health = go.AddComponent<PlayerHealth>();
        health.audioSource = audio;
        health.hurtClip = Sfx("dano");
        health.visual = visual;
        health.blinkRenderers = new[] { ship, flame, glow };
        return go;
    }

    static GameObject BuildStar()
    {
        var go = new GameObject("Estrela") { tag = "Coletavel" };
        go.transform.localScale = Vector3.one * 1.5f;
        var glow = SpriteChild(go.transform, "Brilho", Basic("Brilho"), 4, 0.9f, new Color(1f, 0.85f, 0.3f, 0.45f));
        var r = go.AddComponent<SpriteRenderer>();
        r.sprite = Kenney("Pickups/star_gold.png");
        r.sortingOrder = 5;
        var collider = go.AddComponent<PolygonCollider2D>();
        collider.isTrigger = true;
        var floater = go.AddComponent<Floater>();
        floater.bobAmplitude = 0.06f;
        floater.pulseAmount = 0.08f;
        glow.transform.localPosition = Vector3.zero;
        return go;
    }

    static GameObject BuildOxygen()
    {
        var go = new GameObject("CapsulaOxigenio") { tag = "Oxigenio" };
        var collider = go.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.36f;
        var visual = new GameObject("Visual");
        visual.transform.SetParent(go.transform, false);
        SpriteChild(visual.transform, "Brilho", Basic("Brilho"), 4, 1.6f, new Color(0.35f, 0.75f, 1f, 0.5f));
        SpriteChild(visual.transform, "Capsula", Kenney("Pickups/pill_blue.png"), 5, 2.3f);
        var floater = visual.AddComponent<Floater>();
        floater.spinSpeed = 45f;
        floater.pulseAmount = 0.1f;
        var capsule = go.AddComponent<OxygenCapsule>();
        capsule.visual = visual;
        return go;
    }

    static GameObject BuildMeteor(string name, string sprite, float scale, float radius)
    {
        var go = new GameObject(name) { tag = "Inimigo" };
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        var collider = go.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = radius;
        var visual = SpriteChild(go.transform, "Visual", Kenney(sprite), 7, scale);
        var patrol = go.AddComponent<EnemyPatrol>();
        patrol.visual = visual.transform;
        patrol.waypoints = new Vector2[0];
        return go;
    }

    static GameObject BuildDrone()
    {
        var go = new GameObject("Drone") { tag = "Inimigo" };
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.mass = 0.5f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        go.AddComponent<CircleCollider2D>().radius = 0.34f;

        var ring = SpriteChild(go.transform, "RaioDeVisao", Basic("Anel"), -5, 1f, new Color(1f, 0.4f, 0.4f, 0.08f));
        var visual = new GameObject("Visual");
        visual.transform.SetParent(go.transform, false);
        var body = SpriteChild(visual.transform, "Corpo", Kenney("Ships/enemyRed1.png"), 8, 0.68f);

        var chaser = go.AddComponent<EnemyChaser>();
        chaser.visual = visual.transform;
        chaser.body = body;
        chaser.radiusRing = ring;
        chaser.wallMask = 1 << ProjectSetup.WallLayer;
        chaser.patrolPoints = new Vector2[0];
        return go;
    }

    static GameObject BuildWall()
    {
        var go = new GameObject("Parede") { layer = ProjectSetup.WallLayer };
        var glow = SpriteChild(go.transform, "Brilho", Basic("Quadrado"), -2, 1f, new Color(WallCyan.r, WallCyan.g, WallCyan.b, 0.18f));
        var outline = SpriteChild(go.transform, "Contorno", Basic("Quadrado"), -1, 1f, WallCyan);
        var fill = go.AddComponent<SpriteRenderer>();
        fill.sprite = Basic("Quadrado");
        fill.color = WallFill;
        foreach (var r in new[] { glow, outline, fill }) r.drawMode = SpriteDrawMode.Sliced;
        go.AddComponent<BoxCollider2D>();
        return go;
    }

    static GameObject BuildPortal()
    {
        var go = new GameObject("Portal");
        var glow = SpriteChild(go.transform, "Brilho", Basic("Brilho"), 1, 3.4f);
        var ring = SpriteChild(go.transform, "Vortice", Basic("Vortice"), 2, 1.9f);
        var inner = SpriteChild(go.transform, "Anel", Basic("Anel"), 3, 1.1f);
        var collider = go.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.55f;
        var portal = go.AddComponent<Portal>();
        portal.ring = ring;
        portal.innerRing = inner;
        portal.glow = glow;
        portal.entrance = collider;
        ring.color = inner.color = portal.closedColor;
        glow.color = new Color(1f, 1f, 1f, 0f);
        return go;
    }

    static GameObject BuildMusicPlayer()
    {
        var go = new GameObject("MusicPlayer");
        var music = go.AddComponent<AudioSource>();
        music.clip = Asset<AudioClip>("Assets/Audio/Music/trilha_poeira_estelar.wav");
        music.loop = true;
        music.playOnAwake = false;
        var ui = go.AddComponent<AudioSource>();
        ui.playOnAwake = false;
        var player = go.AddComponent<MusicPlayer>();
        player.music = music;
        player.ui = ui;
        return go;
    }

    // ---------- Cenas: base comum ----------

    static SceneParts NewScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        var camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 9f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.07f, 0.05f, 0.13f);
        cameraObject.AddComponent<AudioListener>();
        camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        cameraObject.AddComponent<GameCamera>();

        var lightObject = new GameObject("Global Light 2D");
        var light = lightObject.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.intensity = 0.95f;

        var volumeObject = new GameObject("Global Volume");
        var volume = volumeObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.sharedProfile = VolumeProfileAsset();

        var background = new GameObject("Fundo");
        var tiles = SpriteChild(background.transform, "Ceu", Kenney("Backgrounds/darkPurple.png"), -100);
        tiles.drawMode = SpriteDrawMode.Tiled;
        tiles.size = new Vector2(40f, 26f);
        tiles.color = new Color(0.75f, 0.75f, 0.9f);

        var starfield = new GameObject("Campo de estrelas").AddComponent<Starfield>();
        starfield.transform.SetParent(background.transform, false);
        starfield.sprites = new[] { Kenney("Effects/star1.png"), Kenney("Effects/star2.png"), Kenney("Effects/star3.png") };

        return new SceneParts(scene, background.transform);
    }

    readonly struct SceneParts
    {
        public readonly UnityEngine.SceneManagement.Scene unityScene;
        public readonly Transform background;
        public SceneParts(UnityEngine.SceneManagement.Scene s, Transform b) { unityScene = s; background = b; }
    }

    static VolumeProfile VolumeProfileAsset()
    {
        const string path = "Assets/Settings/PoeiraEstelarVolume.asset";
        var existing = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if (existing != null) return existing;

        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        var bloom = profile.Add<Bloom>(true);
        bloom.threshold.Override(0.8f);
        bloom.intensity.Override(1.1f);
        bloom.scatter.Override(0.7f);
        var vignette = profile.Add<Vignette>(true);
        vignette.intensity.Override(0.32f);
        vignette.smoothness.Override(0.5f);
        AssetDatabase.CreateAsset(profile, path);
        foreach (var component in profile.components) AssetDatabase.AddObjectToAsset(component, profile);
        AssetDatabase.SaveAssets();
        return profile;
    }

    static void Planet(Transform parent, string file, Vector2 position, float scale, float alpha)
    {
        var r = SpriteChild(parent, "Planeta", Kenney("Planets/" + file), -90, scale, new Color(1f, 1f, 1f, alpha), position);
        r.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
    }

    static GameObject Instance(GameObject prefab, Transform parent, Vector2 position)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.transform.position = position;
        return go;
    }

    static void EventSystemObject()
    {
        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        var module = go.AddComponent<InputSystemUIInputModule>();
        module.AssignDefaultActions();
    }

    // ---------- UI ----------

    static RectTransform Rect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        return rt;
    }

    static RectTransform Centered(string name, Transform parent, Vector2 position, Vector2 size) =>
        Rect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);

    static RectTransform Stretch(string name, Transform parent)
    {
        var rt = Rect(name, parent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        return rt;
    }

    static Image ImageOn(RectTransform rt, Sprite sprite, Color color, bool sliced = false)
    {
        var image = rt.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        if (sliced) image.type = Image.Type.Sliced;
        return image;
    }

    static TextMeshProUGUI Text(RectTransform rt, string content, float size, TextAlignmentOptions alignment, Color? color = null, bool glow = false)
    {
        var text = rt.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        if (glow) text.fontSharedMaterial = fontGlow;
        text.text = content;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = color ?? Color.white;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        text.richText = true;
        return text;
    }

    static Canvas CanvasObject(string name)
    {
        var go = new GameObject(name);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(960f, 600f);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    static Button ButtonObject(Transform parent, string label, Vector2 position, Vector2 size)
    {
        var rt = Centered("Botao " + label, parent, position, size);
        var image = ImageOn(rt, Kenney("UI/button_rectangle_depth.png"), Color.white, true);
        var button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.normalColor = ButtonColor;
        colors.highlightedColor = new Color(0.35f, 0.78f, 1f);
        colors.selectedColor = new Color(0.35f, 0.78f, 1f);
        colors.pressedColor = new Color(0.16f, 0.3f, 0.62f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        var labelRect = Stretch("Texto", rt);
        labelRect.offsetMin = new Vector2(8f, 6f);
        labelRect.offsetMax = new Vector2(-8f, 0f);
        Text(labelRect, label, size.y * 0.38f, TextAlignmentOptions.Center);

        var fx = rt.gameObject.AddComponent<ButtonFx>();
        fx.hoverClip = Sfx("ui_hover");
        fx.clickClip = Sfx("ui_click");
        return button;
    }

    static RectTransform Window(Transform parent, string name, Vector2 position, Vector2 size)
    {
        // Contorno neon (como as paredes) com fundo escuro por dentro.
        var panel = Centered(name, parent, position, size);
        ImageOn(panel, null, new Color(WallCyan.r, WallCyan.g, WallCyan.b, 0.85f));
        var fill = Stretch("Fundo", panel);
        fill.offsetMin = new Vector2(3f, 3f);
        fill.offsetMax = new Vector2(-3f, -3f);
        ImageOn(fill, null, PanelColor);
        return panel;
    }

    static RectTransform Panel(Transform parent, string name, Vector2 size, bool dim)
    {
        Transform host = parent;
        if (dim)
        {
            var overlay = Stretch(name, parent);
            ImageOn(overlay, null, new Color(0.02f, 0.01f, 0.06f, 0.65f));
            host = overlay;
        }
        var panel = Window(host, dim ? "Painel" : name, Vector2.zero, size);
        return dim ? (RectTransform)host : panel;
    }

    // ---------- Menu inicial ----------

    static void BuildMenuScene(Prefabs prefabs)
    {
        var scene = NewScene();
        Random.InitState(3);
        Planet(scene.background, "planet05.png", new Vector2(-10.5f, -6.5f), 2.6f, 0.9f);
        Planet(scene.background, "planet08.png", new Vector2(11.5f, 6.2f), 0.9f, 0.75f);

        // Nave flutuando e meteoros passando ao fundo.
        var ship = new GameObject("Nave do menu");
        ship.transform.position = new Vector3(9.2f, -4.2f, 0f);
        ship.transform.rotation = Quaternion.Euler(0f, 0f, 28f);
        var shipFloat = ship.AddComponent<Floater>();
        shipFloat.bobAmplitude = 0.25f;
        shipFloat.bobSpeed = 1.3f;
        shipFloat.pulseAmount = 0f;
        SpriteChild(ship.transform, "Brilho", Basic("Brilho"), 8, 3f, new Color(0.4f, 0.85f, 1f, 0.35f));
        SpriteChild(ship.transform, "Chama", Kenney("Effects/fire11.png"), 9, 1.7f, Color.white, new Vector3(0f, -0.72f, 0f));
        SpriteChild(ship.transform, "Nave", Kenney("Ships/playerShip1_blue.png"), 10, 1f);

        var meteor = Instance(prefabs.meteorBig, null, new Vector2(-16f, 2f));
        meteor.GetComponent<EnemyPatrol>().waypoints = new[] { new Vector2(-17f, 1.5f), new Vector2(17f, -1.5f) };
        meteor.GetComponent<EnemyPatrol>().speed = 1.2f;
        meteor.GetComponent<Collider2D>().enabled = false;
        var meteor2 = Instance(prefabs.meteorMedium, null, new Vector2(16f, -6f));
        meteor2.GetComponent<EnemyPatrol>().waypoints = new[] { new Vector2(17f, -7f), new Vector2(-17f, -4.5f) };
        meteor2.GetComponent<EnemyPatrol>().speed = 1.8f;
        meteor2.GetComponent<Collider2D>().enabled = false;

        PrefabUtility.InstantiatePrefab(prefabs.music);
        EventSystemObject();

        var canvas = CanvasObject("Canvas");
        var root = canvas.transform;

        var title = Text(Centered("Titulo", root, new Vector2(0f, 190f), new Vector2(900f, 110f)), "POEIRA ESTELAR", 76f, TextAlignmentOptions.Center, new Color(0.85f, 0.95f, 1f), true);
        title.characterSpacing = 6f;
        var mainPanel = Centered("Menu principal", root, new Vector2(0f, -20f), new Vector2(320f, 260f));
        Text(Centered("Subtitulo", mainPanel, new Vector2(0f, 142f), new Vector2(940f, 30f)),
            "Colete as estrelas, escape pelo portal e não deixe o oxigênio acabar.", 14.5f, TextAlignmentOptions.Center, new Color(0.8f, 0.85f, 1f, 0.9f));

        var play = ButtonObject(mainPanel, "JOGAR", new Vector2(0f, 70f), new Vector2(280f, 60f));
        var howTo = ButtonObject(mainPanel, "COMO JOGAR", new Vector2(0f, 0f), new Vector2(280f, 60f));
        var quit = ButtonObject(mainPanel, "SAIR", new Vector2(0f, -70f), new Vector2(280f, 60f));
        var best = Text(Centered("Melhor tempo", mainPanel, new Vector2(0f, -150f), new Vector2(600f, 30f)), "", 16f, TextAlignmentOptions.Center, new Color(1f, 0.85f, 0.35f));

        var howToPanel = Window(root, "Como jogar", new Vector2(0f, -40f), new Vector2(760f, 320f));
        Text(Rect("Titulo", howToPanel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(0f, 40f)),
            "COMO JOGAR", 26f, TextAlignmentOptions.Center, new Color(0.45f, 0.9f, 1f));
        var body = Rect("Instrucoes", howToPanel, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        body.offsetMin = new Vector2(34f, 60f);
        body.offsetMax = new Vector2(-34f, -60f);
        var instructions = Text(body,
            "<color=#7FE3FF>Mover:</color> WASD ou setas  |  analógico ou direcional\n" +
            "<color=#7FE3FF>Impulso:</color> Espaço ou Shift  |  botão A ou RT (gasta energia)\n" +
            "<color=#7FE3FF>Pausar:</color> Esc ou P  |  Start\n\n" +
            "Colete todas as <color=#FFD54A>estrelas</color> para abrir o <color=#7FE3FF>portal</color> no centro e fuja por ele.\n" +
            "O oxigênio cai sem parar: pegue as <color=#7FB8FF>cápsulas azuis</color> (+10s).\n" +
            "Meteoros seguem rotas fixas. <color=#FF7A7A>Drones</color> perseguem quem entra no radar.\n" +
            "Você tem 3 vidas. Quanto mais rápido, melhor o recorde!",
            13.5f, TextAlignmentOptions.TopLeft, new Color(0.92f, 0.94f, 1f));
        instructions.lineSpacing = 10f;
        var back = ButtonObject(howToPanel, "VOLTAR", new Vector2(0f, -116f), new Vector2(200f, 50f));

        Text(Rect("Creditos", root, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(-40f, 40f)),
            "Feito por Miguel Fernandes Oliveira Damásio  ·  Jogos Digitais, Insper 2026\nArte e efeitos sonoros: Kenney (CC0)  ·  Música original gerada por código",
            11f, TextAlignmentOptions.Bottom, new Color(1f, 1f, 1f, 0.55f));

        var menu = canvas.gameObject.AddComponent<MainMenu>();
        menu.mainPanel = mainPanel.gameObject;
        menu.howToPanel = howToPanel.gameObject;
        menu.playButton = play;
        menu.howToButton = howTo;
        menu.quitButton = quit;
        menu.backButton = back;
        menu.bestTimeText = best;

        EditorSceneManager.SaveScene(scene.unityScene, MenuScenePath);
    }

    // ---------- Cena do jogo ----------

    static void BuildGameScene(Prefabs prefabs)
    {
        var scene = NewScene();
        Random.InitState(7);

        BuildArena(prefabs);

        var stars = new GameObject("Estrelas").transform;
        Vector2[] starPositions =
        {
            new Vector2(-12.7f, 5.9f), new Vector2(-9.2f, 4.1f), new Vector2(-12.7f, -7.7f), new Vector2(-10.6f, -1.2f),
            new Vector2(-5.4f, 0.4f), new Vector2(0f, 5.3f), new Vector2(5.4f, 2.6f), new Vector2(12.7f, 5.9f),
            new Vector2(12.6f, -1.6f), new Vector2(12.7f, -7.7f), new Vector2(3.6f, -7.6f), new Vector2(-1.4f, 0.6f),
        };
        foreach (var position in starPositions) Instance(prefabs.star, stars, position);

        var oxygen = new GameObject("Oxigenio").transform;
        foreach (var position in new[] { new Vector2(-5.6f, 6f), new Vector2(6.2f, -3.6f), new Vector2(-12.6f, 0.9f) })
            Instance(prefabs.oxygen, oxygen, position);

        var enemies = new GameObject("Inimigos").transform;
        Meteor(prefabs.meteorBig, enemies, 3f, new Vector2(-6.4f, 5.2f), new Vector2(6.4f, 5.2f));
        Meteor(prefabs.meteorMedium, enemies, 2.6f, new Vector2(-10.9f, -7.6f), new Vector2(-10.9f, 1.9f));
        Meteor(prefabs.meteorMedium, enemies, 2.6f, new Vector2(10.9f, 5.8f), new Vector2(10.9f, -3.4f));
        Meteor(prefabs.meteorBig, enemies, 2.2f, new Vector2(-4.2f, 3.2f), new Vector2(4.2f, 3.2f), new Vector2(4.2f, -5f), new Vector2(-4.2f, -5f));
        Meteor(prefabs.meteorMedium, enemies, 3.4f, new Vector2(8.6f, -7.2f), new Vector2(13.1f, -7.2f));
        Drone(prefabs.drone, enemies, new Vector2(-12.4f, 4.8f), new Vector2(-9.2f, 4.8f));
        Drone(prefabs.drone, enemies, new Vector2(9.6f, -1.9f), new Vector2(12.9f, -1.9f));

        Instance(prefabs.portal, null, new Vector2(0f, -0.9f));
        var player = Instance(prefabs.player, null, new Vector2(0f, -6.6f));

        var managerObject = new GameObject("GameManager");
        var sfx = managerObject.AddComponent<AudioSource>();
        sfx.playOnAwake = false;
        var manager = managerObject.AddComponent<GameManager>();
        manager.sfx = sfx;
        manager.winClip = Sfx("vitoria");
        manager.loseClip = Sfx("derrota");
        manager.portalOpenClip = Sfx("portal_abriu");
        manager.lowOxygenClip = Sfx("alerta_oxigenio");
        manager.burstPrefab = prefabs.burst.GetComponent<ParticleSystem>();

        PrefabUtility.InstantiatePrefab(prefabs.music);
        EventSystemObject();
        BuildGameCanvas(player);

        EditorSceneManager.SaveScene(scene.unityScene, GameScenePath);
    }

    static void BuildArena(Prefabs prefabs)
    {
        var arena = new GameObject("Arena").transform;
        const float left = -13.8f, right = 13.8f, bottom = -8.6f, top = 6.8f, t = 0.4f;

        // Bordas.
        Wall(prefabs, arena, new Vector2(0f, top + t / 2f), new Vector2(right - left + 2 * t, t), WallCyan);
        Wall(prefabs, arena, new Vector2(0f, bottom - t / 2f), new Vector2(right - left + 2 * t, t), WallCyan);
        Wall(prefabs, arena, new Vector2(left - t / 2f, (top + bottom) / 2f), new Vector2(t, top - bottom), WallCyan);
        Wall(prefabs, arena, new Vector2(right + t / 2f, (top + bottom) / 2f), new Vector2(t, top - bottom), WallCyan);

        // Divisórias que criam salas e corredores.
        Wall(prefabs, arena, new Vector2(-8f, (bottom - 2.6f) / 2f), new Vector2(t, -2.6f - bottom), WallPink);
        Wall(prefabs, arena, new Vector2(8f, (top + 0.4f) / 2f), new Vector2(t, top - 0.4f), WallPink);
        Wall(prefabs, arena, new Vector2((left - 10.4f) / 2f, 2.9f), new Vector2(-10.4f - left, t), WallPink);
        Wall(prefabs, arena, new Vector2((right + 10.4f) / 2f, -4.5f), new Vector2(right - 10.4f, t), WallPink);
        Wall(prefabs, arena, new Vector2(-4.9f, -7.3f), new Vector2(1.2f, 1.2f), WallPink);
        Wall(prefabs, arena, new Vector2(5.4f, -5.9f), new Vector2(1.2f, 1.2f), WallPink);

        // Câmara central do portal: quatro cantos em "L" com aberturas no meio de cada lado.
        Vector2 c = new Vector2(0f, -0.9f);
        const float half = 2.5f, arm = 1.6f;
        foreach (var sx in new[] { -1f, 1f })
        foreach (var sy in new[] { -1f, 1f })
        {
            var corner = c + new Vector2(sx * half, sy * half);
            Wall(prefabs, arena, corner + new Vector2(-sx * (arm / 2f - t / 2f), 0f), new Vector2(arm, t), WallCyan);
            Wall(prefabs, arena, corner + new Vector2(0f, -sy * (arm / 2f - t / 2f)), new Vector2(t, arm), WallCyan);
        }
    }

    static void Wall(Prefabs prefabs, Transform parent, Vector2 center, Vector2 size, Color color)
    {
        var go = Instance(prefabs.wall, parent, center);
        go.GetComponent<SpriteRenderer>().size = size;
        go.GetComponent<BoxCollider2D>().size = size;
        var outline = go.transform.Find("Contorno").GetComponent<SpriteRenderer>();
        outline.size = size + Vector2.one * 0.12f;
        outline.color = color;
        var glow = go.transform.Find("Brilho").GetComponent<SpriteRenderer>();
        glow.size = size + Vector2.one * 0.45f;
        glow.color = new Color(color.r, color.g, color.b, 0.18f);
    }

    static void Meteor(GameObject prefab, Transform parent, float speed, params Vector2[] waypoints)
    {
        var go = Instance(prefab, parent, waypoints[0]);
        var patrol = go.GetComponent<EnemyPatrol>();
        patrol.waypoints = waypoints;
        patrol.speed = speed;
        patrol.spin = Random.Range(-60f, 60f);
    }

    static void Drone(GameObject prefab, Transform parent, params Vector2[] patrol)
    {
        var go = Instance(prefab, parent, patrol[0]);
        go.GetComponent<EnemyChaser>().patrolPoints = patrol;
    }

    static void BuildGameCanvas(GameObject player)
    {
        var canvas = CanvasObject("Canvas");
        var root = canvas.transform;

        // Barra superior.
        var bar = Rect("Barra superior", root, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 58f));
        ImageOn(bar, null, new Color(0.04f, 0.03f, 0.1f, 0.82f));
        var line = Rect("Linha", bar, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 2f));
        ImageOn(line, null, new Color(WallCyan.r, WallCyan.g, WallCyan.b, 0.6f));

        var hearts = new List<Image>();
        for (int i = 0; i < 3; i++)
        {
            var heart = Rect("Vida " + (i + 1), bar, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f + i * 38f, -8f), new Vector2(33f, 26f));
            hearts.Add(ImageOn(heart, Kenney("UI/playerLife1_blue.png"), Color.white));
        }
        var staminaBack = Rect("Energia", bar, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -40f), new Vector2(110f, 10f));
        ImageOn(staminaBack, null, new Color(1f, 1f, 1f, 0.12f));
        var staminaFill = Stretch("Preenchimento", staminaBack);
        var staminaImage = ImageOn(staminaFill, Basic("Quadrado"), Color.yellow);
        staminaImage.type = Image.Type.Filled;
        staminaImage.fillMethod = Image.FillMethod.Horizontal;
        Text(Rect("Rotulo energia", bar, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(134f, -35f), new Vector2(90f, 20f)), "ENERGIA", 11f, TextAlignmentOptions.Left, new Color(1f, 1f, 1f, 0.7f));

        var score = Text(Rect("Pontos", bar, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-215f, -16f), new Vector2(200f, 28f)), "PONTOS 0", 17f, TextAlignmentOptions.Center);
        var starIcon = Rect("Icone estrela", bar, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-40f, -13f), new Vector2(31f, 30f));
        ImageOn(starIcon, Kenney("Pickups/star_gold.png"), Color.white);
        var starsText = Text(Rect("Estrelas", bar, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(70f, -13f), new Vector2(170f, 34f)), "0 de 12", 24f, TextAlignmentOptions.Left, new Color(1f, 0.88f, 0.4f));
        var time = Text(Rect("Tempo", bar, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(205f, -16f), new Vector2(220f, 28f)), "TEMPO 00:00.0", 17f, TextAlignmentOptions.Center);

        var oxygenText = Text(Rect("Oxigenio", bar, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-18f, -8f), new Vector2(200f, 26f)), "OXIGÊNIO 45", 16f, TextAlignmentOptions.Right);
        var oxygenBack = Rect("Barra oxigenio", bar, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-18f, -40f), new Vector2(170f, 10f));
        ImageOn(oxygenBack, null, new Color(1f, 1f, 1f, 0.12f));
        var oxygenFill = ImageOn(Stretch("Preenchimento", oxygenBack), Basic("Quadrado"), Color.cyan);
        oxygenFill.type = Image.Type.Filled;
        oxygenFill.fillMethod = Image.FillMethod.Horizontal;

        var message = Text(Rect("Mensagem", root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -78f), new Vector2(900f, 40f)), "", 22f, TextAlignmentOptions.Center, Color.white, true);

        var hud = canvas.gameObject.AddComponent<HUD>();
        hud.starsText = starsText;
        hud.scoreText = score;
        hud.oxygenText = oxygenText;
        hud.timeText = time;
        hud.messageText = message;
        hud.oxygenFill = oxygenFill;
        hud.staminaFill = staminaImage;
        hud.hearts = hearts.ToArray();
        hud.playerHealth = player.GetComponent<PlayerHealth>();
        hud.playerMovement = player.GetComponent<PlayerMovement>();

        // Pausa.
        var pause = Panel(root, "Pausa", new Vector2(420f, 360f), true);
        var pausePanel = pause.Find("Painel");
        Text(Centered("Titulo", pausePanel, new Vector2(0f, 128f), new Vector2(380f, 56f)), "PAUSADO", 40f, TextAlignmentOptions.Center, new Color(0.45f, 0.9f, 1f));
        var resume = ButtonObject(pausePanel, "CONTINUAR", new Vector2(0f, 45f), new Vector2(280f, 58f));
        var restart = ButtonObject(pausePanel, "REINICIAR", new Vector2(0f, -22f), new Vector2(280f, 58f));
        var pauseMenu = ButtonObject(pausePanel, "MENU", new Vector2(0f, -89f), new Vector2(280f, 58f));
        Text(Centered("Dica", pausePanel, new Vector2(0f, -148f), new Vector2(380f, 24f)), "Esc / P / Start para continuar", 12f, TextAlignmentOptions.Center, new Color(1f, 1f, 1f, 0.6f));

        // Fim de jogo.
        var end = Panel(root, "Fim de jogo", new Vector2(560f, 420f), true);
        var endPanel = end.Find("Painel");
        var endTitle = Text(Centered("Titulo", endPanel, new Vector2(0f, 160f), new Vector2(520f, 60f)), "FIM DE JOGO", 42f, TextAlignmentOptions.Center, Color.white, true);
        var endReason = Text(Centered("Motivo", endPanel, new Vector2(0f, 112f), new Vector2(520f, 30f)), "", 18f, TextAlignmentOptions.Center, new Color(0.85f, 0.88f, 1f));
        var endStats = Text(Centered("Resultado", endPanel, new Vector2(0f, 8f), new Vector2(480f, 190f)), "", 18f, TextAlignmentOptions.Center);
        endStats.lineSpacing = 10f;
        var playAgain = ButtonObject(endPanel, "JOGAR DE NOVO", new Vector2(-125f, -150f), new Vector2(230f, 58f));
        var endMenu = ButtonObject(endPanel, "MENU", new Vector2(125f, -150f), new Vector2(230f, 58f));

        var ui = canvas.gameObject.AddComponent<GameUI>();
        ui.pausePanel = pause.gameObject;
        ui.resumeButton = resume;
        ui.pauseRestartButton = restart;
        ui.pauseMenuButton = pauseMenu;
        ui.endPanel = end.gameObject;
        ui.endTitle = endTitle;
        ui.endReason = endReason;
        ui.endStats = endStats;
        ui.playAgainButton = playAgain;
        ui.endMenuButton = endMenu;
    }
}
