using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>
/// Menu: Oddyseus > Build Placeholder Scenes
/// Generates Title, Lobby, Story, Dungeon scenes (2D, sprites + a placeholder panorama) and registers
/// them in Build Settings. Safe to re-run: it rebuilds the 4 scene files (your own art files are not touched).
/// </summary>
public static class SceneBuilder
{
    const string Root = "Assets/_Project";
    const string ScenesDir = Root + "/Scenes";
    const string PlaceDir = Root + "/Art/Placeholders";
    const string PanoDir = Root + "/Art/Panoramas";

    static Sprite square, circle, panorama;
    static Material spriteMat;

    // lobby layout (world units). Ground line is y = -3, camera sees y -5..5.
    const float X0 = 0f, X1 = 20f, X2 = 40f, X3 = 60f;   // stops: window, lobby, table, door

    [MenuItem("Oddyseus/Build Placeholder Scenes")]
    public static void BuildAll()
    {
        foreach (var f in new[] { ScenesDir, PlaceDir, PanoDir, Root + "/Art/Characters", Root + "/Art/Props", Root + "/Audio", Root + "/Animations" })
            Directory.CreateDirectory(f);
        AssetDatabase.Refresh();

        PreparePlaceholderArt();

        BuildTitle();
        BuildLobby();
        BuildSimple(GameScenes.Story, "STORY MODE", "Placeholder. Story Mode arrives in a later update.");
        BuildSimple(GameScenes.Dungeon, "DUNGEON MODE", "Placeholder. The endless dungeon / island map goes here.");

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenePath(GameScenes.Title), true),
            new EditorBuildSettingsScene(ScenePath(GameScenes.Lobby), true),
            new EditorBuildSettingsScene(ScenePath(GameScenes.Story), true),
            new EditorBuildSettingsScene(ScenePath(GameScenes.Dungeon), true),
        };
        EditorSceneManager.OpenScene(ScenePath(GameScenes.Title));
        Debug.Log("Oddyseus: placeholder scenes built. Press Play from the Title scene.");
    }

    static string ScenePath(string n) => $"{ScenesDir}/{n}.unity";

    // ------------------------------------------------------------ placeholder art generation

    static Sprite MakeSprite(string path, int w, int h, Func<int, int, Color32> pixel, float ppu, FilterMode filter, int maxSize)
    {
        if (!File.Exists(path))
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    px[y * w + x] = pixel(x, y);
            tex.SetPixels32(px);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);

            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = ppu;
            ti.mipmapEnabled = false;
            ti.filterMode = filter;
            ti.alphaIsTransparency = true;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.maxTextureSize = maxSize;
            ti.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static void PreparePlaceholderArt()
    {
        square = MakeSprite($"{PlaceDir}/Square.png", 4, 4, (x, y) => new Color32(255, 255, 255, 255), 4f, FilterMode.Point, 64);

        circle = MakeSprite($"{PlaceDir}/Circle.png", 128, 128, (x, y) =>
        {
            float dx = (x + .5f - 64f) / 64f, dy = (y + .5f - 64f) / 64f;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            byte a = (byte)(Mathf.Clamp01((1f - d) * 32f) * 255f);
            return new Color32(255, 255, 255, a);
        }, 128f, FilterMode.Bilinear, 128);

        // 4096x1024 px at 51.2 px/unit  =>  80 x 20 world units; each 1024px sector = one lobby stop
        Color[] sector =
        {
            new Color(.60f, .75f, .90f), new Color(.95f, .80f, .60f),
            new Color(.70f, .85f, .65f), new Color(.80f, .65f, .80f)
        };
        panorama = MakeSprite($"{PanoDir}/Lobby_Panorama_PLACEHOLDER.png", 4096, 1024, (x, y) =>
        {
            float v = y / 1023f;                       // 0 bottom .. 1 top
            Color c = sector[x / 1024];
            c = v < 0.35f ? c * 0.45f : c * (0.7f + 0.3f * v);
            if (x % 1024 < 4) c *= 0.5f;              // thin marks where each stop's region starts
            return new Color32((byte)(c.r * 255), (byte)(c.g * 255), (byte)(c.b * 255), 255);
        }, 51.2f, FilterMode.Bilinear, 4096);

        string matPath = $"{PlaceDir}/Mat_SpriteUnlit.mat";
        spriteMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (spriteMat == null)
        {
            spriteMat = new Material(Shader.Find("Sprites/Default"));
            AssetDatabase.CreateAsset(spriteMat, matPath);
        }
    }

    // ------------------------------------------------------------ builders

    static GameObject Spr(string name, Transform parent, Vector2 pos, Vector2 size, Color color, int order, bool round = false, Sprite custom = null)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = custom != null ? custom : (round ? circle : square);
        sr.sharedMaterial = spriteMat;
        sr.color = color;
        sr.sortingOrder = order;
        return go;
    }

    static Transform Empty(string name, Transform parent, Vector2 pos)
    {
        var g = new GameObject(name);
        g.transform.SetParent(parent, false);
        g.transform.localPosition = pos;
        return g.transform;
    }

    static BoxCollider2D Box(GameObject go, Vector2 offset, Vector2 size)
    {
        var b = go.AddComponent<BoxCollider2D>();
        b.offset = offset;
        b.size = size;
        return b;
    }

    static Camera MakeCamera(Color bg, float x)
    {
        var go = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = go.AddComponent<Camera>();
        go.AddComponent<AudioListener>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = bg;
        go.transform.position = new Vector3(x, 0f, -10f);
        return cam;
    }

    // ------------------------------------------------------------ TITLE

    static void BuildTitle()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        MakeCamera(new Color(0.07f, 0.1f, 0.18f), 0f);

        var canvas = UIFactory.CreateCanvas("TitleCanvas");
        var title = UIFactory.CreateText(canvas.transform, "Title", "ODDYSEUS", 140, TextAnchor.MiddleCenter, new Color(0.95f, 0.85f, 0.55f));
        UIFactory.Anchor(title.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, 130), new Vector2(1400, 180));
        var sub = UIFactory.CreateText(canvas.transform, "Subtitle", "JOURNEY'S END", 64, TextAnchor.MiddleCenter, Color.white);
        UIFactory.Anchor(sub.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, 20), new Vector2(1000, 90));
        var prompt = UIFactory.CreateText(canvas.transform, "PressAnyKey", "Press any key / tap to start", 40, TextAnchor.MiddleCenter, Color.white);
        UIFactory.Anchor(prompt.rectTransform, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 160), new Vector2(1000, 70));

        canvas.gameObject.AddComponent<TitleScreen>().prompt = prompt;
        EditorSceneManager.SaveScene(scene, ScenePath(GameScenes.Title));
    }

    // ------------------------------------------------------------ STORY / DUNGEON placeholders

    static void BuildSimple(string sceneName, string title, string note)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        MakeCamera(new Color(0.12f, 0.1f, 0.14f), 0f);
        var canvas = UIFactory.CreateCanvas("Canvas");
        var t = UIFactory.CreateText(canvas.transform, "Title", title, 100, TextAnchor.MiddleCenter, Color.white);
        UIFactory.Anchor(t.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, 80), new Vector2(1400, 140));
        var n = UIFactory.CreateText(canvas.transform, "Note", note, 36, TextAnchor.MiddleCenter, new Color(1, 1, 1, .8f));
        UIFactory.Anchor(n.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, -20), new Vector2(1400, 80));
        var back = UIFactory.CreateButton(canvas.transform, "BackButton", "Back to Lobby", 36);
        UIFactory.Anchor((RectTransform)back.transform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, -150), new Vector2(420, 90));
        canvas.gameObject.AddComponent<ScenePlaceholder>().backButton = back;
        EditorSceneManager.SaveScene(scene, ScenePath(sceneName));
    }

    // ------------------------------------------------------------ LOBBY

    static void BuildLobby()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var cam = MakeCamera(Color.black, X1);
        var rig = cam.gameObject.AddComponent<LobbyCameraRig>();

        // ---- panorama slot (one wide image, 80 x 20 units, centered between stop 0 and stop 3)
        var env = new GameObject("ENVIRONMENT").transform;
        Spr("Panorama_Background (replace sprite)", env, new Vector2((X0 + X3) * 0.5f, 0), Vector2.one, Color.white, -100, false, panorama);

        var hotspots = new GameObject("HOTSPOTS (replace placeholder art, keep scripts)").transform;
        Color wood = new Color(.42f, .28f, .17f);

        // ===== STOP 1 (x=20): character =====
        var charRoot = Empty("Character_Lobby (stop 1)", hotspots, new Vector2(X1, 0));
        var model = Empty("Model (put animated character here)", charRoot, Vector2.zero);
        Spr("PLACEHOLDER_Body", model, new Vector2(0, -1.5f), new Vector2(1.8f, 3f), new Color(.45f, .3f, .2f), 5);
        Spr("PLACEHOLDER_Head", model, new Vector2(0, .9f), new Vector2(1.3f, 1.3f), new Color(.9f, .72f, .58f), 6, true);
        Spr("PLACEHOLDER_EyeL", model, new Vector2(-.25f, 1.05f), new Vector2(.15f, .15f), new Color(.1f, .1f, .1f), 7, true);
        Spr("PLACEHOLDER_EyeR", model, new Vector2(.25f, 1.05f), new Vector2(.15f, .15f), new Color(.1f, .1f, .1f), 7, true);
        var mouth = Spr("PLACEHOLDER_Mouth", model, new Vector2(0, .7f), new Vector2(.4f, .12f), new Color(.5f, .1f, .1f), 7);
        Box(charRoot.gameObject, new Vector2(0, -.7f), new Vector2(2f, 4.6f));

        // ===== STOP 0 (x=0): story window =====
        var win = Empty("StoryWindow (stop 0)", hotspots, new Vector2(X0, 0));
        Spr("PLACEHOLDER_Frame", win, new Vector2(0, 1), new Vector2(4.4f, 5.4f), wood, 1);
        Spr("PLACEHOLDER_Sky", win, new Vector2(0, 1), new Vector2(3.8f, 4.8f), new Color(.55f, .78f, .95f), 2);
        Spr("PLACEHOLDER_CloudA", win, new Vector2(-.8f, 2.4f), new Vector2(1.2f, .5f), new Color(1, 1, 1, .9f), 3, true);
        Spr("PLACEHOLDER_BarV", win, new Vector2(0, 1), new Vector2(.15f, 4.8f), wood, 4);
        Spr("PLACEHOLDER_BarH", win, new Vector2(0, 1), new Vector2(3.8f, .15f), wood, 4);
        Spr("PLACEHOLDER_Sill", win, new Vector2(0, -1.75f), new Vector2(5f, .4f), wood * 1.2f, 4);

        var lockIcon = Empty("LockIcon (hidden when unlocked)", win, new Vector2(0, 1));
        Spr("PLACEHOLDER_Shackle", lockIcon, new Vector2(0, .5f), new Vector2(.7f, .7f), new Color(.55f, .45f, .1f), 9, true);
        Spr("PLACEHOLDER_LockBody", lockIcon, Vector2.zero, new Vector2(1f, .75f), new Color(.85f, .68f, .15f), 10);

        var bird = Empty("Bird (idle on sill)", win, new Vector2(1.2f, -1.25f));
        Color birdC = new Color(.25f, .45f, .75f);
        Spr("PLACEHOLDER_BirdBody", bird, Vector2.zero, new Vector2(.8f, .6f), birdC, 7, true);
        Spr("PLACEHOLDER_BirdHead", bird, new Vector2(.4f, .25f), new Vector2(.4f, .4f), birdC, 8, true);
        Spr("PLACEHOLDER_Beak", bird, new Vector2(.65f, .25f), new Vector2(.25f, .1f), new Color(.95f, .6f, .1f), 8);
        var wingR = Empty("WingBack", bird, new Vector2(0, .1f));
        Spr("PLACEHOLDER_WingBackMesh", wingR, new Vector2(-.3f, .1f), new Vector2(.6f, .25f), birdC * .7f, 6, true);
        var wingL = Empty("WingFront", bird, new Vector2(0, .1f));
        Spr("PLACEHOLDER_WingFrontMesh", wingL, new Vector2(-.3f, .1f), new Vector2(.6f, .25f), birdC * .85f, 9, true);
        var flyTarget = Empty("FlyTarget (where the bird disappears to)", win, new Vector2(-.4f, 3f));

        Box(win.gameObject, new Vector2(0, .6f), new Vector2(5f, 6.6f));
        var story = win.gameObject.AddComponent<StoryWindowHotspot>();
        story.rig = rig; story.bird = bird; story.flyTarget = flyTarget;
        story.wingL = wingL; story.wingR = wingR; story.lockIcon = lockIcon.gameObject;

        // ===== STOP 2 (x=40): table + map =====
        var table = Empty("DungeonTable_Props (stop 2)", hotspots, Vector2.zero);
        Spr("PLACEHOLDER_TableTop", table, new Vector2(X2, -1.5f), new Vector2(7f, .4f), wood, 8);
        Spr("PLACEHOLDER_LegL", table, new Vector2(X2 - 2.8f, -2.4f), new Vector2(.3f, 1.4f), wood * .8f, 8);
        Spr("PLACEHOLDER_LegR", table, new Vector2(X2 + 2.8f, -2.4f), new Vector2(.3f, 1.4f), wood * .8f, 8);
        Color gold = new Color(1f, .8f, .1f);
        Spr("PLACEHOLDER_Gold1", table, new Vector2(X2 - 2.4f, -1.15f), new Vector2(.5f, .25f), gold, 9, true);
        Spr("PLACEHOLDER_Gold2", table, new Vector2(X2 - 2.0f, -1.15f), new Vector2(.5f, .25f), gold, 9, true);
        Spr("PLACEHOLDER_Gold3", table, new Vector2(X2 - 2.2f, -.95f), new Vector2(.5f, .25f), gold, 9, true);
        Spr("PLACEHOLDER_ChestBody", table, new Vector2(X2 + 2.3f, -.95f), new Vector2(1.1f, .7f), new Color(.5f, .3f, .1f), 9);
        Spr("PLACEHOLDER_ChestLid", table, new Vector2(X2 + 2.3f, -.5f), new Vector2(1.1f, .25f), new Color(.7f, .5f, .15f), 9);

        float[] px = { X2 - 4.7f, X2 + 4.7f, X2 + 6.3f };
        for (int i = 0; i < px.Length; i++)
        {
            var p = Empty("Person_" + (i + 1), table, new Vector2(px[i], 0));
            Spr("PLACEHOLDER_Body", p, new Vector2(0, -1.2f), new Vector2(1.3f, 3.6f), new Color(.2f + i * .12f, .22f, .32f), 3);
            Spr("PLACEHOLDER_Head", p, new Vector2(0, 1.1f), new Vector2(1f, 1f), new Color(.85f, .68f, .55f), 4, true);
        }

        var knifeStart = Empty("KnifeStart (where the knife is thrown from)", table, new Vector2(X2 - 3.4f, -3.6f));
        var knife = Empty("Knife", table, knifeStart.localPosition);
        Spr("PLACEHOLDER_Blade", knife, new Vector2(.3f, 0), new Vector2(1f, .12f), new Color(.85f, .87f, .92f), 20);
        Spr("PLACEHOLDER_Handle", knife, new Vector2(-.35f, 0), new Vector2(.4f, .17f), new Color(.4f, .25f, .12f), 20);

        var mapRoot = Empty("Map (Dungeon button)", hotspots, new Vector2(X2, .3f));
        Spr("PLACEHOLDER_MapBorder", mapRoot, Vector2.zero, new Vector2(5.3f, 3.5f), new Color(.3f, .2f, .12f), 5);
        Spr("PLACEHOLDER_MapParchment", mapRoot, Vector2.zero, new Vector2(5f, 3.2f), new Color(.85f, .76f, .55f), 6);
        Vector2[] isl = { new Vector2(-1.6f, -.7f), new Vector2(.8f, .3f), new Vector2(-.6f, .9f), new Vector2(1.7f, -.8f) };
        for (int i = 0; i < isl.Length; i++)
            Spr("PLACEHOLDER_Island_" + (i + 1), mapRoot, isl[i], new Vector2(.9f, .65f), new Color(.3f, .55f, .3f), 7, true);

        var ringGo = new GameObject("IslandCircle (LineRenderer, move onto any island)");
        ringGo.transform.SetParent(mapRoot, false);
        ringGo.transform.localPosition = isl[1];
        var ring = ringGo.AddComponent<LineRenderer>();
        ring.sharedMaterial = spriteMat;
        ring.startColor = ring.endColor = new Color(.85f, .1f, .1f);
        ring.widthMultiplier = .07f;
        ring.useWorldSpace = false;
        ring.numCapVertices = 4;
        ring.sortingOrder = 15;

        Box(mapRoot.gameObject, Vector2.zero, new Vector2(5.3f, 3.5f));
        var dungeon = mapRoot.gameObject.AddComponent<DungeonTableHotspot>();
        dungeon.rig = rig; dungeon.ring = ring; dungeon.knife = knife; dungeon.knifeStart = knifeStart;

        // ===== STOP 3 (x=60): exit door =====
        var door = Empty("ExitDoor (stop 3)", hotspots, new Vector2(X3, 0));
        Spr("PLACEHOLDER_Frame", door, new Vector2(0, -.15f), new Vector2(3.6f, 5.7f), new Color(.3f, .2f, .13f), 3);
        Spr("PLACEHOLDER_OutsideLight", door, new Vector2(0, -.3f), new Vector2(3f, 5.4f), new Color(1f, .95f, .75f), 4);
        var pivot = Empty("DoorPivot (hinge)", door, new Vector2(-1.5f, 0));
        Spr("PLACEHOLDER_DoorLeaf", pivot, new Vector2(1.5f, -.3f), new Vector2(3f, 5.4f), new Color(.5f, .33f, .2f), 5);
        Spr("PLACEHOLDER_Knob", pivot, new Vector2(2.6f, -.4f), new Vector2(.25f, .25f), gold, 6, true);

        Box(door.gameObject, new Vector2(0, -.15f), new Vector2(3.6f, 5.7f));
        var exit = door.gameObject.AddComponent<ExitDoorHotspot>();
        exit.rig = rig; exit.doorPivot = pivot;

        // ===== UI =====
        var canvas = UIFactory.CreateCanvas("LobbyCanvas");
        var ui = canvas.gameObject.AddComponent<LobbyUI>();
        ui.rig = rig;

        ui.stopLabel = UIFactory.CreateText(canvas.transform, "StopLabel", "Lobby", 44, TextAnchor.MiddleCenter, Color.white);
        UIFactory.Anchor(ui.stopLabel.rectTransform, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -30), new Vector2(800, 70));

        ui.leftButton = UIFactory.CreateButton(canvas.transform, "LeftButton", "<", 64);
        UIFactory.Anchor((RectTransform)ui.leftButton.transform, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(30, 0), new Vector2(90, 150));
        ui.rightButton = UIFactory.CreateButton(canvas.transform, "RightButton", ">", 64);
        UIFactory.Anchor((RectTransform)ui.rightButton.transform, new Vector2(1, .5f), new Vector2(1, .5f), new Vector2(1, .5f), new Vector2(-30, 0), new Vector2(90, 150));

        ui.tooltip = UIFactory.CreateText(canvas.transform, "Tooltip", "", 34, TextAnchor.UpperLeft, new Color(1f, .95f, .7f));
        ui.tooltip.horizontalOverflow = HorizontalWrapMode.Overflow;
        UIFactory.Anchor(ui.tooltip.rectTransform, Vector2.zero, Vector2.zero, new Vector2(0, 1), Vector2.zero, new Vector2(700, 60));

        var panel = UIFactory.CreatePanel(canvas.transform, "SpeechPanel", new Color(0, 0, 0, .65f));
        UIFactory.Anchor(panel.rectTransform, new Vector2(.1f, 0), new Vector2(.9f, 0), new Vector2(.5f, 0), new Vector2(0, 30), new Vector2(0, 210));
        ui.speechPanel = panel.gameObject;
        ui.speakerText = UIFactory.CreateText(panel.transform, "Speaker", "Companion", 40, TextAnchor.UpperLeft, new Color(1f, .85f, .4f));
        UIFactory.Anchor(ui.speakerText.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(.5f, 1), new Vector2(0, -15), new Vector2(-60, 55));
        ui.lineText = UIFactory.CreateText(panel.transform, "Line", "", 36, TextAnchor.UpperLeft, Color.white);
        UIFactory.Anchor(ui.lineText.rectTransform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(.5f, .5f), new Vector2(0, -30), new Vector2(-60, -90));

        // ---- wire character + input
        var talker = charRoot.gameObject.AddComponent<CharacterTalker>();
        talker.rig = rig; talker.ui = ui; talker.model = model; talker.mouth = mouth.transform;

        var input = cam.gameObject.AddComponent<LobbyHotspotInput>();
        input.rig = rig; input.ui = ui;

        EditorSceneManager.SaveScene(scene, ScenePath(GameScenes.Lobby));
    }
}
