using System.Collections;
using UnityEngine;

/// <summary>
/// RIGHT stop. The MAP on the table is the Dungeon Mode button. Put this on the map object.
/// Hover: a circle draws itself around an island (LineRenderer on 'ring').
/// Click: a knife is thrown into the circle (spinning), camera zooms and shakes, then Dungeon scene loads.
/// </summary>
public class DungeonTableHotspot : Hotspot
{
    public LobbyCameraRig rig;
    public LineRenderer ring;          // child of the map, centered on the island
    public Transform knife, knifeStart;
    public float ringRadius = 0.55f;

    public override string Label => "Dungeon Mode";

    const int Segments = 48;
    float progress;
    bool busy;

    void Awake()
    {
        ring.useWorldSpace = false;
        ring.loop = false;
        knife.gameObject.SetActive(false);
        SetProgress(0f);
    }

    void Update()
    {
        if (busy) return;
        progress = Mathf.MoveTowards(progress, IsHovered ? 1f : 0f, Time.deltaTime * 2.5f);
        SetProgress(progress);
    }

    void SetProgress(float p)
    {
        int n = Mathf.CeilToInt(Mathf.Clamp01(p) * Segments);
        if (n <= 0) { ring.positionCount = 0; return; }
        ring.positionCount = n + 1;
        for (int i = 0; i <= n; i++)
        {
            float a = i / (float)Segments * Mathf.PI * 2f;
            ring.SetPosition(i, new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * ringRadius);
        }
    }

    public override void Click()
    {
        if (busy) return;
        busy = true;
        GameState.LastLobbyStop = rig.Index;
        StartCoroutine(ThrowKnife());
    }

    IEnumerator ThrowKnife()
    {
        rig.Focus(ring.transform, 2.6f);
        progress = 1f;
        SetProgress(1f);

        Vector3 a = knifeStart.position, b = ring.transform.position;
        float aim = Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;
        knife.position = a;
        knife.gameObject.SetActive(true);

        const float flight = 0.5f;
        for (float t = 0; t < flight; t += Time.deltaTime)
        {
            float k = t / flight;
            knife.position = Vector3.Lerp(a, b, k);
            knife.rotation = Quaternion.Euler(0, 0, aim + (1f - k) * 720f);   // spins, ends pointing at the island
            yield return null;
        }
        knife.position = b;
        knife.rotation = Quaternion.Euler(0, 0, aim);

        // impact
        ring.widthMultiplier *= 2.5f;
        ring.startColor = ring.endColor = Color.white;
        rig.Shake(0.25f, 0.35f);
        yield return new WaitForSeconds(0.55f);

        SceneFader.LoadScene(GameScenes.DungeonHub, 0.5f);
    }
}
