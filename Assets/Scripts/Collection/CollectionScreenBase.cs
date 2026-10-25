using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Base for the five collection screens (Legends, Character Detail, Armory, Pack Compendium, Achievements).
///
/// HOW IT WORKS
///  - The editor builder (Oddyseus/Build ...) calls BuildUI() and saves the scene, so the whole layout is
///    real objects in the scene that your artist can edit and re-skin ("... (replace sprite)").
///  - If a scene has no built UI (for example you added the script to an empty scene), Awake() builds it.
///  - Button clicks are wired in Start() (code listeners are not saved in scenes). Data is filled in Populate().
/// </summary>
public abstract class CollectionScreenBase : MonoBehaviour
{
    public bool built;
    public Canvas canvas;
    public ScreenHeader header;
    public RectTransform templates;   // hidden container that holds card templates

    protected abstract string ScreenTitle { get; }
    protected abstract void BuildContent(Transform canvasRoot);
    protected abstract void Wire();
    protected abstract void Populate();

    protected virtual void OnBack()
    {
        CollectionNav.Back();
    }

    // ---------- Build ----------
    public void BuildUI()
    {
        if (canvas != null)
        {
            if (Application.isPlaying) Destroy(canvas.gameObject);
            else DestroyImmediate(canvas.gameObject);
        }

        canvas = UIKit.CreateOverlayCanvas(ScreenTitle + " Canvas", 0);
        canvas.transform.SetParent(transform, false);
        Transform root = canvas.transform;

        Image bg = UIKit.NewImage("Background (replace sprite)", root, null, Image.Type.Simple, UITheme.Navy, true);
        UIKit.Stretch(bg.rectTransform);

        templates = UIKit.NewRect("Templates (hidden)", root);
        templates.gameObject.SetActive(false);

        BuildContent(root);

        header = UIKit.CreateScreenHeader(root);
        header.titleLabel.text = ScreenTitle;
        header.transform.SetAsLastSibling();

        built = true;
    }

    // ---------- Runtime ----------
    protected virtual void Awake()
    {
        if (!built || canvas == null) BuildUI();
        UIKit.EnsureEventSystem();
    }

    protected virtual void Start()
    {
        CollectionData.EnsureLoaded();
        if (header != null) header.Setup(ScreenTitle, OnBack);
        Wire();
        Populate();
    }

    protected virtual void Update()
    {
        if (!EscapePressed()) return;
        ConfirmPanel open = FindFirstObjectByType<ConfirmPanel>();
        if (open != null && open.IsOpen) return;
        OnBack();
    }

    static bool EscapePressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Escape);
#else
        return false;
#endif
    }

    // Clones a hidden template into a container and returns it (active).
    protected static RectTransform Spawn(RectTransform template, Transform parent)
    {
        GameObject go = Instantiate(template.gameObject, parent, false);
        go.name = template.name.Replace(" Template", "");
        go.SetActive(true);
        return (RectTransform)go.transform;
    }
}
