using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Self-building fade overlay + scene loader. No setup needed in scenes.</summary>
public class SceneFader : MonoBehaviour
{
    static SceneFader instance;
    CanvasGroup group;
    bool loading;

    public static SceneFader Instance
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject("SceneFader", typeof(RectTransform));
                DontDestroyOnLoad(go);
                instance = go.AddComponent<SceneFader>();
                instance.Build();
            }
            return instance;
        }
    }

    void Build()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        gameObject.AddComponent<GraphicRaycaster>();
        group = gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;

        var img = new GameObject("Black", typeof(RectTransform)).AddComponent<Image>();
        img.transform.SetParent(transform, false);
        img.color = Color.black;
        var rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    public static void LoadScene(string scene, float fade = 0.6f)
    {
        if (Instance.loading) return;
        Instance.StartCoroutine(Instance.LoadCo(scene, fade));
    }

    public Coroutine Fade(float toAlpha, float duration) => StartCoroutine(FadeCo(toAlpha, duration));

    IEnumerator LoadCo(string scene, float fade)
    {
        loading = true;
        yield return StartCoroutine(FadeCo(1f, fade));
        var op = SceneManager.LoadSceneAsync(scene);
        while (!op.isDone) yield return null;
        yield return null;
        yield return StartCoroutine(FadeCo(0f, fade));
        loading = false;
    }

    IEnumerator FadeCo(float to, float duration)
    {
        group.blocksRaycasts = true;
        float from = group.alpha, t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        group.alpha = to;
        group.blocksRaycasts = to > 0.01f;
    }
}
