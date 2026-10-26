using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Menu: Oddyseus > Build Lobby (Remastered)
/// Rebuilds ONLY Scenes/Lobby.unity with the remastered art from Assets/_Project/Art/Lobby.
/// It does not change any script. The HUD (compass, chevrons, ribbon, markers, dialogue) is your existing LobbyUI.
/// The old Lobby.unity is copied to Assets/_Project/Scenes/_Backups first.
///
/// Layout: each stop is one 1672x941 background, exactly 20 world units wide (= LobbyCameraRig.StopSpacing),
/// so the 4 backgrounds sit side by side and the camera (size 5.628) shows exactly one per stop.
/// Art slots are objects named "NAME (replace sprite)": swap the Sprite in the Inspector, or overwrite the PNG
/// in Art/Lobby with the same name.
/// NOTE: the older menu "Oddyseus/Build Placeholder Scenes" rebuilds the OLD placeholder Lobby. Do not run it after this.
/// </summary>
public static class LobbyBuilder
{
    const string ArtDir = "Assets/_Project/Art/Lobby";
    const string PlaceDir = "Assets/_Project/Art/Placeholders";
    const string ScenesDir = "Assets/_Project/Scenes";

    const float BgPpu = 1672f / 20f;     // pixels per unit that makes a background 20 units wide
    const float WU = 20f / 1672f;        // world units per background pixel
    const float ViewSize = 5.6281f;      // orthographic size: 941 px * WU / 2
    const float X0 = 0f, X1 = 20f, X2 = 40f, X3 = 60f;   // stops: window, lobby, table, door (same as SceneBuilder)

    static Sprite square, circle;
    static Material spriteMat;

    // background pixel (u right, v down) on stop 1 -> world position
    static Vector2 Px(float u, float v)
    {
        return new Vector2(X1 + (u - 836f) * WU, (470.5f - v) * WU);
    }

    [MenuItem("Oddyseus/Build Lobby (Remastered)")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        square = AssetDatabase.LoadAssetAtPath<Sprite>(PlaceDir + "/Square.png");
        circle = AssetDatabase.LoadAssetAtPath<Sprite>(PlaceDir + "/Circle.png");
        spriteMat = AssetDatabase.LoadAssetAtPath<Material>(PlaceDir + "/Mat_SpriteUnlit.mat");
        if (square == null || circle == null || spriteMat == null)
        {
            Debug.LogError("Oddyseus: placeholder art missing in " + PlaceDir + ". Run Oddyseus > Build Placeholder Scenes once, then try again.");
            return;
        }

        string[] bgs = { "lobby_story_placeholder.png", "lobby_main.png", "lobby_dungeon_placeholder.png", "lobby_exit_placeholder.png" };
        for (int i = 0; i < bgs.Length; i++) ConfigureSprite(ArtDir + "/" + bgs[i], BgPpu);
        ConfigureSprite(ArtDir + "/wheel_glow.png", 100f);
        ConfigureSprite(ArtDir + "/scrolls_glow.png", 100f);
        ConfigureSprite(ArtDir + "/character_polites.png", 100f);

        string scenePath = ScenesDir + "/" + GameScenes.Lobby + ".unity";
        BackupScene(scenePath);
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ---- camera + rig
        Camera cam = MakeCamera(X1);
        LobbyCameraRig rig = cam.gameObject.AddComponent<LobbyCameraRig>();
        rig.defaultSize = ViewSize;

        // ---- backgrounds, one per stop
        Transform env = new GameObject("ENVIRONMENT").transform;
        string[] stopNames = { "Story Window", "Lobby", "Dungeon Table", "Exit Door" };
        for (int i = 0; i < 4; i++)
        {
            GameObject g = new GameObject("Lobby Stop " + i + " " + stopNames[i] + " Background (replace sprite)");
            g.transform.SetParent(env, false);
            g.transform.position = new Vector3(i * 20f, 0f, 0f);
            SpriteRenderer sr = g.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtDir + "/" + bgs[i]);
            sr.sharedMaterial = spriteMat;
            sr.sortingOrder = -100;
        }

        Transform hotspots = new GameObject("HOTSPOTS (replace placeholder art, keep scripts)").transform;
        Color wood = new Color(.42f, .28f, .17f);

        // ---- UI: the existing LobbyUI builds the whole HUD from the UI Kit when the scene plays
        GameObject uiGo = new GameObject("Lobby UI");
        LobbyUI ui = uiGo.AddComponent<LobbyUI>();
        ui.rig = rig;

        // ===== STOP 1 (x=20): remastered lobby =====
        Sprite polites = AssetDatabase.LoadAssetAtPath<Sprite>(ArtDir + "/character_polites.png");
        Sprite wheel = AssetDatabase.LoadAssetAtPath<Sprite>(ArtDir + "/wheel_glow.png");
        Sprite scrolls = AssetDatabase.LoadAssetAtPath<Sprite>(ArtDir + "/scrolls_glow.png");

        // Polites. RectInt = the non-transparent part of the PNG, in PNG pixels.
        Vector2 charCenter = Px(900f, 570f);                 // feet near v=810, height 480 background px
        Transform charRoot = Empty("Character_Lobby (stop 1)", hotspots, charCenter);
        Transform model = Empty("Model (put animated character here)", charRoot, Vector2.zero);
        Cutout("Character Polites (replace sprite)", model, polites, new RectInt(213, 44, 617, 1482),
               charCenter, new Vector2(0.3239f, 0.3239f), 5);
        Box(charRoot.gameObject, Vector2.zero, new Vector2(260f * WU, 470f * WU));
        CharacterTalker talker = charRoot.gameObject.AddComponent<CharacterTalker>();
        talker.rig = rig;
        talker.ui = ui;
        talker.model = model;
        talker.speakerName = "Polites";
        talker.lines = new string[]
        {
            "A quiet day by the fire. I could get used to this.",
            "Pick a direction. The window, the map, or the door.",
            "Ithaca is still far. But every journey starts at a table like this one.",
            "The crew is restless. Shall we look at the charts?"
        };

        // Ship's wheel = Settings (the glow sprite fades in over the wheel in the painting)
        Vector2 wheelCenter = Px(162f, 162f);
        Transform wheelT = Empty("Settings Wheel (Settings hotspot)", hotspots, wheelCenter);
        Box(wheelT.gameObject, Vector2.zero, new Vector2(230f * WU, 265f * WU));
        MenuObjectHotspot wh = wheelT.gameObject.AddComponent<MenuObjectHotspot>();
        wh.action = MenuAction.Settings;
        wh.label = "Settings";
        wh.hoverPop = 0.03f;
        wh.glow = Cutout("Glow", wheelT, wheel, new RectInt(106, 58, 1059, 1074),
                         wheelCenter, new Vector2(0.2389f, 0.2682f), 10);

        // Scroll barrel = Almanac
        Vector2 barrelCenter = Px(397.5f, 568.5f);
        Transform barrelT = Empty("Scroll Barrel (Almanac hotspot)", hotspots, barrelCenter);
        Box(barrelT.gameObject, Vector2.zero, new Vector2(112f * WU, 205f * WU));
        MenuObjectHotspot bh = barrelT.gameObject.AddComponent<MenuObjectHotspot>();
        bh.action = MenuAction.Almanac;
        bh.label = "Almanac";
        bh.hoverPop = 0.03f;
        bh.glow = Cutout("Glow", barrelT, scrolls, new RectInt(305, 66, 606, 1098),
                         barrelCenter, new Vector2(0.185f, 0.185f), 10);

        // ===== STOPS 0, 2, 3: exactly the placeholder props SceneBuilder builds today =====
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

        // ---- wire input
        LobbyHotspotInput input = cam.gameObject.AddComponent<LobbyHotspotInput>();
        input.rig = rig;
        input.ui = ui;

        Directory.CreateDirectory(ScenesDir);
        EditorSceneManager.SaveScene(scene, scenePath);
        EnsureInBuildSettings(scenePath);
        Debug.Log("Oddyseus: Lobby (remastered) built at " + scenePath + ". Press Play from the Title or Lobby scene.");
    }

    // Puts a sprite on screen so that its non-transparent box is centred on targetWorld.
    // bgPxPerSpritePx = how many background pixels one sprite pixel covers (the scale).
    static GameObject Cutout(string name, Transform parent, Sprite sprite, RectInt box,
                             Vector2 targetWorld, Vector2 bgPxPerSpritePx, int order)
    {
        float w = sprite.rect.width, h = sprite.rect.height;
        float cx = box.x + box.width * 0.5f, cy = box.y + box.height * 0.5f;
        float sx = bgPxPerSpritePx.x * WU, sy = bgPxPerSpritePx.y * WU;     // world units per sprite pixel
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(targetWorld.x + (w * 0.5f - cx) * sx, targetWorld.y + (cy - h * 0.5f) * sy, 0f);
        go.transform.localScale = new Vector3(sx * 100f, sy * 100f, 1f);   // sprite PPU is 100
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sharedMaterial = spriteMat;
        sr.sortingOrder = order;
        return go;
    }

    // ---- same helpers as SceneBuilder, so the placeholder code below is identical
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

    static Camera MakeCamera(float x)
    {
        var go = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = go.AddComponent<Camera>();
        go.AddComponent<AudioListener>();
        cam.orthographic = true;
        cam.orthographicSize = ViewSize;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        go.transform.position = new Vector3(x, 0f, -10f);
        return cam;
    }

    static void ConfigureSprite(string path, float ppu)
    {
        TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) { Debug.LogWarning("Lobby art missing: " + path); return; }
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = ppu;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.filterMode = FilterMode.Bilinear;
        ti.maxTextureSize = 2048;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.SaveAndReimport();
    }

    static void BackupScene(string scenePath)
    {
        if (!File.Exists(scenePath)) return;
        string dir = ScenesDir + "/_Backups";
        if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder(ScenesDir, "_Backups");
        string dest = dir + "/" + Path.GetFileNameWithoutExtension(scenePath) + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".unity";
        if (AssetDatabase.CopyAsset(scenePath, dest)) Debug.Log("Oddyseus: old scene backed up to " + dest);
        else Debug.LogWarning("Oddyseus: could not back up " + scenePath);
    }

    static void EnsureInBuildSettings(string scenePath)
    {
        EditorBuildSettingsScene[] cur = EditorBuildSettings.scenes;
        for (int i = 0; i < cur.Length; i++) if (cur[i].path == scenePath) return;
        EditorBuildSettingsScene[] next = new EditorBuildSettingsScene[cur.Length + 1];
        Array.Copy(cur, next, cur.Length);
        next[cur.Length] = new EditorBuildSettingsScene(scenePath, true);
        EditorBuildSettings.scenes = next;
    }
}
