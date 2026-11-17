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
/// Stops 0, 2 and 3 use the painted backgrounds: invisible hotspots sit on the painted window, map and door
/// (positions measured on the 1672x941 art), no coloured placeholder shapes.
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

    // same, for any stop (0 window, 1 lobby, 2 table, 3 door)
    static Vector2 PxAt(int stop, float u, float v)
    {
        return new Vector2(stop * 20f + (u - 836f) * WU, (470.5f - v) * WU);
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
        ConfigureSprite(ArtDir + "/character_polites.png", 100f, true);   // readable: the visible part is measured automatically

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
        Transform mouthT = null;
        if (polites != null)
        {
            // Any PNG size works: the transparent margin is trimmed, the visible figure is 480 background px tall,
            // and its feet stand at v=810 (the floor).
            AutoFit("Character Polites (replace sprite)", model, ArtDir + "/character_polites.png",
                    Px(900f, 810f), 480f, 5);
        }
        else
        {
            // No character_polites.png yet: simple placeholder (body, head, mouth). Re-run this menu when the PNG exists.
            Debug.LogWarning("Oddyseus: character_polites.png not found in " + ArtDir + ", using a placeholder Polites.");
            Spr("PLACEHOLDER_PolitesBody", model, new Vector2(0f, -0.9f), new Vector2(1.6f, 3.4f), new Color(.45f, .3f, .2f), 5);
            Spr("PLACEHOLDER_PolitesHead", model, new Vector2(0f, 1.5f), new Vector2(1.3f, 1.3f), new Color(.9f, .72f, .58f), 6, true);
            mouthT = Spr("PLACEHOLDER_PolitesMouth", model, new Vector2(0f, 1.25f), new Vector2(.45f, .08f), new Color(.55f, .1f, .1f), 7).transform;
        }
        Box(charRoot.gameObject, Vector2.zero, new Vector2(260f * WU, 470f * WU));
        CharacterTalker talker = charRoot.gameObject.AddComponent<CharacterTalker>();
        talker.mouth = mouthT;
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
        if (wheel != null)
            wh.glow = Cutout("Glow", wheelT, wheel, new RectInt(106, 58, 1059, 1074),
                             wheelCenter, new Vector2(0.2389f, 0.2682f), 10);
        else Debug.LogWarning("Oddyseus: wheel_glow.png not found, the Settings wheel has no hover glow yet.");

        // Scroll barrel = Almanac
        Vector2 barrelCenter = Px(397.5f, 568.5f);
        Transform barrelT = Empty("Scroll Barrel (Almanac hotspot)", hotspots, barrelCenter);
        Box(barrelT.gameObject, Vector2.zero, new Vector2(112f * WU, 205f * WU));
        MenuObjectHotspot bh = barrelT.gameObject.AddComponent<MenuObjectHotspot>();
        bh.action = MenuAction.Almanac;
        bh.label = "Almanac";
        bh.hoverPop = 0.03f;
        if (scrolls != null)
            bh.glow = Cutout("Glow", barrelT, scrolls, new RectInt(305, 66, 606, 1098),
                             barrelCenter, new Vector2(0.185f, 0.185f), 10);
        else Debug.LogWarning("Oddyseus: scrolls_glow.png not found, the Almanac barrel has no hover glow yet.");

        // ===== STOPS 0, 2, 3: painted backgrounds. Invisible hotspots sit on the painted objects. =====
        // Positions are background pixels of the 1672x941 art (u right, v down).

        // ---- STOP 0 (x=0): the boarded window = Story Mode
        var win = Empty("StoryWindow (stop 0)", hotspots, PxAt(0, 834f, 290f));
        Box(win.gameObject, Vector2.zero, new Vector2(520f * WU, 570f * WU));
        var story = win.gameObject.AddComponent<StoryWindowHotspot>();
        story.rig = rig;                                   // bird / lock are optional (painted window)
        win.gameObject.AddComponent<PaintedGlow>();

        // ---- STOP 2 (x=40): the map on the table = Dungeon Mode
        var mapRoot = Empty("Map (Dungeon button)", hotspots, PxAt(2, 845f, 398f));
        Box(mapRoot.gameObject, Vector2.zero, new Vector2(1195f * WU, 614f * WU));

        var ringGo = new GameObject("IslandCircle (LineRenderer, move onto any island)");
        ringGo.transform.SetParent(mapRoot, false);
        ringGo.transform.position = new Vector3(PxAt(2, 829f, 369f).x, PxAt(2, 829f, 369f).y, 0f);   // the middle island
        var ring = ringGo.AddComponent<LineRenderer>();
        ring.sharedMaterial = spriteMat;
        ring.startColor = ring.endColor = new Color(.85f, .1f, .1f);
        ring.widthMultiplier = .07f;
        ring.useWorldSpace = false;
        ring.numCapVertices = 4;
        ring.sortingOrder = 15;

        var dungeon = mapRoot.gameObject.AddComponent<DungeonTableHotspot>();
        dungeon.rig = rig; dungeon.ring = ring;
        dungeon.ringRadius = 105f * WU;                    // knife / knifeStart are optional (painted map)

        // ---- STOP 3 (x=60): the double door = Exit
        var door = Empty("ExitDoor (stop 3)", hotspots, PxAt(3, 834f, 341f));
        Box(door.gameObject, Vector2.zero, new Vector2(453f * WU, 565f * WU));
        var exit = door.gameObject.AddComponent<ExitDoorHotspot>();
        exit.rig = rig;                                    // doorPivot is optional (painted door)
        door.gameObject.AddComponent<PaintedGlow>();

        // ---- wire input
        LobbyHotspotInput input = cam.gameObject.AddComponent<LobbyHotspotInput>();
        input.rig = rig;
        input.ui = ui;

        Directory.CreateDirectory(ScenesDir);
        EditorSceneManager.SaveScene(scene, scenePath);
        EnsureInBuildSettings(scenePath);
        Debug.Log("Oddyseus: Lobby (remastered) built at " + scenePath + ". Press Play from the Title or Lobby scene.");
    }

    // Fits a PNG by its VISIBLE pixels: trims the transparent margin, scales the figure to heightBg background
    // pixels tall and stands its bottom centre on feetWorld. Works for any PNG size. Needs a readable texture.
    static GameObject AutoFit(string name, Transform parent, string path, Vector2 feetWorld, float heightBg, int order)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (sprite == null || tex == null) return null;
        int w = tex.width, h = tex.height;
        Color32[] px = tex.GetPixels32();
        int minX = w, minY = h, maxX = -1, maxY = -1;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                if (px[y * w + x].a > 8)
                {
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
        if (maxX < 0) { Debug.LogWarning("Oddyseus: " + path + " is fully transparent."); return null; }

        float s = heightBg * WU / (maxY - minY + 1);                  // world units per sprite pixel
        float cx = (minX + maxX + 1) * 0.5f;
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(feetWorld.x + (w * 0.5f - cx) * s, feetWorld.y + (h * 0.5f - minY) * s, 0f);
        go.transform.localScale = new Vector3(s * 100f, s * 100f, 1f);   // sprite PPU is 100
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sharedMaterial = spriteMat;
        sr.sortingOrder = order;
        return go;
    }

    // Puts a sprite on screen so that its non-transparent box is centred on targetWorld.
    // bgPxPerSpritePx = how many background pixels one sprite pixel covers (the scale).
    static GameObject Cutout(string name, Transform parent, Sprite sprite, RectInt box,
                             Vector2 targetWorld, Vector2 bgPxPerSpritePx, int order)
    {
        if (sprite == null) return null;
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

    static void ConfigureSprite(string path, float ppu, bool readable = false)
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
        ti.isReadable = readable;
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