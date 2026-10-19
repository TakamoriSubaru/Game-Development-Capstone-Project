using System.Collections;
using UnityEngine;

/// <summary>
/// Dark people-shapes that now and then walk across the Dungeon Table screen (like shadows of the crew
/// passing by the table). It clones 'template' for every walker, so:
///   ARTIST: replace the art inside the child "SilhouetteTemplate" with your person silhouette
///   (drawn facing RIGHT). Keep that object inactive. Walkers going left are flipped automatically.
/// Walkers stay inside the table area: they fade in / out at the ends of their walk, and fade away
/// completely when the camera is not at the table, so they never show up on the other stops.
/// </summary>
public class TableSilhouettes : MonoBehaviour
{
    public LobbyCameraRig rig;
    public Transform template;

    public float minInterval = 2.5f, maxInterval = 6f;   // seconds between walkers
    public float minSpeed = 2f, maxSpeed = 3.4f;         // world units per second
    public float walkHalfWidth = 11f;                    // walkers go from -this to +this around the table centre
    public float edgeFade = 3f;                          // distance over which a walker fades in / out at each end
    public float viewRange = 8f;                         // camera within this many units of the table = fully visible
    public float viewFade = 4f;                          // ...and fades to nothing over this many more units
    public float laneRange = 3.6f;                       // how far up/down from the middle they can walk
    public int maxAtOnce = 2;

    float timer;
    int active;

    void Start() => timer = Random.Range(0.5f, 2f);

    void Update()
    {
        if (template == null) return;
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = Random.Range(minInterval, maxInterval);

        if (active >= maxAtOnce) return;
        if (CameraFade() <= 0f) return;          // nobody is looking at the table
        StartCoroutine(Walk());
    }

    // 1 when the camera is at the table, fading to 0 as it moves away (so other stops never see them)
    float CameraFade()
    {
        if (rig == null) return 1f;
        float d = Mathf.Abs(rig.transform.position.x - transform.position.x);
        return Mathf.Clamp01((viewRange + viewFade - d) / Mathf.Max(0.01f, viewFade));
    }

    IEnumerator Walk()
    {
        active++;
        bool right = Random.value > 0.5f;
        float dir = right ? 1f : -1f;
        float y = transform.position.y + Random.Range(-laneRange, laneRange);
        float startX = transform.position.x - dir * walkHalfWidth;
        float endX = transform.position.x + dir * walkHalfWidth;
        float x = startX;
        float speed = Random.Range(minSpeed, maxSpeed);
        float size = Random.Range(0.9f, 1.3f);
        float phase = Random.value * 10f;

        var go = Instantiate(template.gameObject, transform);
        go.name = "Silhouette";
        go.SetActive(true);
        var t = go.transform;
        var s = template.localScale * size;
        t.localScale = new Vector3(s.x * dir, s.y, s.z);
        t.position = new Vector3(x, y, 0f);

        var rends = go.GetComponentsInChildren<SpriteRenderer>(true);
        var baseAlpha = new float[rends.Length];
        for (int i = 0; i < rends.Length; i++) baseAlpha[i] = rends[i].color.a;
        SetAlpha(rends, baseAlpha, 0f);          // start invisible (no one-frame flash)

        float total = Mathf.Abs(endX - startX);
        while ((right && x < endX) || (!right && x > endX))
        {
            x += dir * speed * Time.deltaTime;
            t.position = new Vector3(x, y + Mathf.Sin(Time.time * 7f + phase) * 0.06f, 0f);   // little walking bob

            float walked = Mathf.Abs(x - startX);
            float edge = Mathf.Clamp01(Mathf.Min(walked, total - walked) / Mathf.Max(0.01f, edgeFade));
            SetAlpha(rends, baseAlpha, edge * CameraFade());
            yield return null;
        }

        Destroy(go);
        active--;
    }

    static void SetAlpha(SpriteRenderer[] rends, float[] baseAlpha, float k)
    {
        for (int i = 0; i < rends.Length; i++)
        {
            var c = rends[i].color;
            c.a = baseAlpha[i] * k;
            rends[i].color = c;
        }
    }
}