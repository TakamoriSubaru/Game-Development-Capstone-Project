using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Menu: Oddyseus > Add Lobby Interactables
/// Run it with the Lobby scene OPEN, after Oddyseus > Build Lobby (Remastered) used the new lobby_main.png.
///  1. Adds 12 small clickable props (PropHotspot) under a "PROPS" group.
///  2. Places prop_fire, prop_dog, prop_horse and prop_lantern (Assets/_Project/Art/Lobby) on their props:
///     the fire flickers, the dog breathes, the horse and lantern wobble when clicked.
///  3. Moves the existing Polites, ship's wheel and scroll barrel onto the new painting and turns off the
///     old glow art (it was drawn for the previous background).
///  4. UseWheelSprite = true puts prop_wheel on the wheel so it spins before Settings opens. Only turn it on
///     when the painted wheel has been removed from lobby_main.png, otherwise you see two wheels.
/// All four stops use positions measured on the painted backgrounds in Art/Lobby. Positions are background pixels on a 1672x941 image (u right, v down).
/// Running the menu again replaces the PROPS group (positions you moved by hand are lost).
/// </summary>
public static class LobbyPropsBuilder
{
    const bool UseWheelSprite = false;

    const float WU = 20f / 1672f;   // world units per background pixel (same as LobbyBuilder)
    const string RootName = "PROPS (interactables, drag onto the painted objects)";
    const string ArtDir = "Assets/_Project/Art/Lobby";
    const string MatPath = "Assets/_Project/Art/Placeholders/Mat_SpriteUnlit.mat";

    class Prop
    {
        public int stop;
        public string name, label;
        public float u, v, w, h;                 // collider: centre and size
        public string[] lines;
        // optional own sprite (file in Art/Lobby, without ".png") fitted into this box
        public string sprite;
        public float su, sv, sw, sh;
        public PropHotspot.Reaction reaction = PropHotspot.Reaction.None;
        public float idleAmount, idleSpeed = 1.6f;
        public int order = 5;
        public bool topPivot;                    // swing from the top edge (hanging lantern)
        public string life;                      // "fire", "breathe", "rock" or "swing": adds the matching animator script
    }

    static Prop P(int stop, string name, string label, float u, float v, float w, float h, params string[] lines)
    {
        return new Prop { stop = stop, name = name, label = label, u = u, v = v, w = w, h = h, lines = lines };
    }

    static Prop WithSprite(this Prop p, string file, float u, float v, float w, float h,
                           PropHotspot.Reaction r, float idle = 0f, float speed = 1.6f)
    {
        p.sprite = file; p.su = u; p.sv = v; p.sw = w; p.sh = h;
        p.reaction = r; p.idleAmount = idle; p.idleSpeed = speed;
        return p;
    }

    static Prop TopPivot(this Prop p) { p.topPivot = true; return p; }

    static Prop Life(this Prop p, string kind) { p.life = kind; return p; }

    static Prop[] Props()
    {
        return new[]
        {
            // ---- stop 1: lobby (measured on the new painting)
            P(1, "Fireplace", "Fireplace", 1290, 425, 440, 320,
                "The embers glow. I could sit here until morning.",
                "Keep the fire fed and the crew keeps its spirits.",
                "A hearth like this makes Ithaca feel a little closer.")
                .WithSprite("prop_fire", 1295, 470, 190, 190, PropHotspot.Reaction.None).Life("fire"),
            P(1, "Sleeping Dog", "Sleeping Dog", 1285, 618, 250, 130,
                "He has slept since the day we docked. Lucky dog.",
                "Best not to wake him. He snores like a storm.",
                "His paws twitch. Chasing gulls, I would say.")
                .WithSprite("prop_dog_fit", 1285, 618, 235, 122, PropHotspot.Reaction.None).Life("breathe"),
            P(1, "Bookshelf", "Bookshelf", 402, 320, 330, 470,
                "Old sailing logs, mostly. Some of them are even true.",
                "Charts of every island we have yet to cross.",
                "Somebody keeps a tiny ship on the shelf. I wonder who."),
            P(1, "Toy Horse", "Toy Horse", 400, 640, 135, 130,
                "A little wooden horse. Someone's idea of a joke, I suspect.",
                "Never trust a horse that size. I speak from experience.")
                .WithSprite("prop_horse_fit", 400, 640, 120, 120, PropHotspot.Reaction.None).Life("rock"),
            P(1, "Ceiling Lantern", "Lantern", 792, 105, 80, 205,
                "The oil runs low. We should refill it before nightfall.")
                .WithSprite("prop_lantern_fit", 792, 103, 74, 205, PropHotspot.Reaction.None).TopPivot().Life("swing"),

            // ---- stop 0: story window (measured on lobby_story_placeholder.png)
            P(0, "Hanging Sign", "Hanging Sign", 1258, 246, 195, 135,
                "A blank sign. Somebody meant to paint a name on it.",
                "Maybe one day it will carry the name of our ship."),
            P(0, "Carpenter Tools", "Carpenter's Tools", 1480, 500, 380, 110,
                "Hammer, saw and nails. The ship will not mend itself."),
            P(0, "Dust Sheet", "Dust Sheet", 130, 520, 260, 330,
                "Something sleeps under that sheet. I have learned not to ask."),

            // ---- stop 2: dungeon table (measured on lobby_dungeon_placeholder.png)
            P(2, "Candelabra", "Candelabra", 1505, 150, 200, 270,
                "Three candles keep the whole table awake.",
                "Wax drips across old routes. Every voyage leaves a mark."),

            // ---- stop 3: exit door (measured on lobby_exit_placeholder.png)
            P(3, "Oars", "Oars", 297, 376, 190, 531,
                "Oars enough for the whole crew, if every hand shows up."),
            P(3, "Coats", "Coats", 1430, 297, 260, 280,
                "Cloaks for the cold crossing. Mine is the damp one."),
            P(3, "Supply Barrels", "Supply Barrels", 1511, 532, 320, 294,
                "Salted fish and fresh water. Always count the barrels.")
        };
    }

    static Vector2 World(int stop, float u, float v)
    {
        return new Vector2(stop * 20f + (u - 836f) * WU, (470.5f - v) * WU);
    }

    [MenuItem("Oddyseus/Add Lobby Interactables")]
    public static void Add()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.name != GameScenes.Lobby)
        {
            EditorUtility.DisplayDialog("Add Lobby Interactables",
                "Open the Lobby scene first (Assets/_Project/Scenes/" + GameScenes.Lobby + ".unity), then run this menu again.", "OK");
            return;
        }

        GameObject old = GameObject.Find(RootName);
        if (old != null)
        {
            if (!EditorUtility.DisplayDialog("Add Lobby Interactables",
                    "The PROPS group already exists. Replace it? Positions you moved by hand will be lost.", "Replace", "Cancel"))
                return;
            Undo.DestroyObjectImmediate(old);
        }

        Material mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
        GameObject root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Add Lobby Interactables");

        Prop[] props = Props();
        int spriteCount = 0;
        for (int i = 0; i < props.Length; i++)
        {
            Prop p = props[i];
            GameObject go = new GameObject(p.name + " (stop " + p.stop + ")");
            go.transform.SetParent(root.transform, false);
            Vector2 pos = World(p.stop, p.u, p.v);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            BoxCollider2D box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(p.w * WU, p.h * WU);

            PropHotspot hs = go.AddComponent<PropHotspot>();
            hs.label = p.label;
            hs.speaker = "Polites";
            hs.lines = p.lines;

            if (!string.IsNullOrEmpty(p.sprite))
            {
                GameObject spr = MakeSprite(ArtDir + "/" + p.sprite + ".png", go.transform, mat,
                                            World(p.stop, p.su, p.sv), p.sw, p.sh, p.order);
                if (spr != null)
                {
                    spriteCount++;
                    Transform target = spr.transform;
                    if (p.topPivot)
                    {
                        // an empty pivot at the top edge, so the lantern swings from its chain
                        Bounds b = spr.GetComponent<SpriteRenderer>().bounds;
                        GameObject pivot = new GameObject(spr.name + " pivot");
                        pivot.transform.SetParent(go.transform, false);
                        pivot.transform.position = new Vector3(b.center.x, b.max.y, 0f);
                        spr.transform.SetParent(pivot.transform, true);
                        target = pivot.transform;
                    }
                    if (!string.IsNullOrEmpty(p.life)) AttachLife(p.life, spr, target);
                    else
                    {
                        hs.reactTarget = target;
                        hs.reaction = p.reaction;
                        hs.idleAmount = p.idleAmount;
                        hs.idleSpeed = p.idleSpeed;
                    }
                }
            }
        }

        int moved = AlignExisting(mat);

        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = root;
        Debug.Log("Oddyseus: added " + props.Length + " lobby interactables (" + spriteCount + " with their own sprite), aligned "
                  + moved + " existing objects. Save the scene (Ctrl+S) and press Play.");
    }

    // Adds the animator script that makes a prop move (fire, dog, horse, lantern).
    static void AttachLife(string kind, GameObject sprite, Transform target)
    {
        switch (kind)
        {
            case "fire":
                sprite.AddComponent<FireAnimator>();
                break;
            case "breathe":
                sprite.AddComponent<PropLife>().kind = PropLifeKind.Breathe;
                break;
            case "rock":
                sprite.AddComponent<PropLife>().kind = PropLifeKind.Rock;
                break;
            case "swing":
                target.gameObject.AddComponent<PropLife>().kind = PropLifeKind.Swing;
                break;
        }
    }

    // Moves Polites, the wheel and the barrel onto the new painting and turns off the old glow art.
    static int AlignExisting(Material mat)
    {
        int n = 0;
        n += Align("Character_Lobby (stop 1)", 800f, 570f, 0f, 0f, null);
        n += Align("Settings Wheel (Settings hotspot)", 97f, 175f, 200f, 300f, mat);
        n += Align("Scroll Barrel (Almanac hotspot)", 95f, 535f, 195f, 405f, null);
        return n;
    }

    static int Align(string objectName, float u, float v, float w, float h, Material wheelMat)
    {
        GameObject go = GameObject.Find(objectName);
        if (go == null) { Debug.LogWarning("Oddyseus: '" + objectName + "' not found, not moved."); return 0; }

        Undo.RecordObject(go.transform, "Align lobby object");
        Vector2 pos = World(1, u, v);
        go.transform.position = new Vector3(pos.x, pos.y, go.transform.position.z);

        if (w > 0f)
        {
            BoxCollider2D box = go.GetComponent<BoxCollider2D>();
            if (box != null)
            {
                Undo.RecordObject(box, "Align lobby object");
                box.offset = Vector2.zero;
                box.size = new Vector2(w * WU, h * WU);
            }
        }

        MenuObjectHotspot mh = go.GetComponent<MenuObjectHotspot>();
        if (mh != null)
        {
            Undo.RecordObject(mh, "Align lobby object");
            if (mh.glow != null)
            {
                mh.glow.SetActive(false);   // old glow art was drawn for the previous background
                mh.glow = null;
            }
            if (UseWheelSprite && wheelMat != null && mh.action == MenuAction.Settings)
            {
                GameObject spr = MakeSprite(ArtDir + "/prop_wheel.png", go.transform, wheelMat,
                                            World(1, 95f, 175f), 240f, 240f, 4);
                if (spr != null) mh.spinTarget = spr.transform;
            }
            EditorUtility.SetDirty(mh);
        }
        return 1;
    }

    // Trims the transparent margin of a PNG, fits the visible part into a box (background pixels) and
    // centres it on 'centre'. Returns null when the file does not exist yet.
    static GameObject MakeSprite(string path, Transform parent, Material mat, Vector2 centre, float boxW, float boxH, int order)
    {
        TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) { Debug.LogWarning("Oddyseus: " + path + " not found, skipped."); return null; }
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = 100f;
        ti.alphaIsTransparency = true;
        ti.isReadable = true;
        ti.mipmapEnabled = false;
        ti.filterMode = FilterMode.Bilinear;
        ti.maxTextureSize = 2048;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.SaveAndReimport();

        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (tex == null || sprite == null) return null;

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
        if (maxX < 0)
        {
            Debug.LogWarning("Oddyseus: " + path + " is fully transparent, skipped.");
            return null;
        }

        float bw = maxX - minX + 1, bh = maxY - minY + 1;
        float bgPxPerSpritePx = Mathf.Min(boxW / bw, boxH / bh);
        float s = bgPxPerSpritePx * WU;                       // world units per sprite pixel
        float cx = (minX + maxX + 1) * 0.5f, cy = (minY + maxY + 1) * 0.5f;   // visible centre, y up

        GameObject go = new GameObject(System.IO.Path.GetFileNameWithoutExtension(path));
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(centre.x + (w * 0.5f - cx) * s, centre.y + (h * 0.5f - cy) * s, 0f);
        go.transform.localScale = new Vector3(s * 100f, s * 100f, 1f);   // sprite PPU is 100
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        if (mat != null) sr.sharedMaterial = mat;
        sr.sortingOrder = order;
        return go;
    }
}