using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 2D panoramic lobby camera (orthographic). One wide panorama is laid out left to right and the
/// camera slides between 4 stops, 20 world units apart:
///   0 Story Window (x=0)   1 Lobby / character (x=20)   2 Dungeon Table (x=40)   3 Exit Door (x=60)
/// Focus() zooms in and tracks a transform (bird / knife transitions). Shake() for impacts.
/// </summary>
[RequireComponent(typeof(Camera))]
public class LobbyCameraRig : MonoBehaviour
{
    public static readonly string[] StopNames = { "Story Window", "Lobby", "Dungeon Table", "Exit Door" };
    public const int StopCount = 4;
    public const int FrontStop = 1;
    public const float StopSpacing = 20f;

    public float panSmoothTime = 0.35f;
    public float defaultSize = 5f;

    public bool InputLocked { get; set; }
    public int Index { get; private set; }
    public event Action<int> StopChanged;

    Camera cam;
    float targetX, velX;
    Transform follow;
    float followSize;
    Vector3 followVel;
    Vector3 shakeOffset;

    void Awake() => cam = GetComponent<Camera>();

    void Start()
    {
        Index = Mathf.Clamp(GameState.LastLobbyStop, 0, StopCount - 1);
        targetX = Index * StopSpacing;
        transform.position = new Vector3(targetX, 0f, -10f);
        cam.orthographic = true;
        cam.orthographicSize = defaultSize;
        StopChanged?.Invoke(Index);
    }

    void Update()
    {
        if (InputLocked) return;
        if (GameInput.LeftDown) Step(-1);
        if (GameInput.RightDown) Step(1);
    }

    void LateUpdate()
    {
        Vector3 pos;
        if (follow != null)
        {
            var dest = new Vector3(follow.position.x, follow.position.y, -10f);
            pos = Vector3.SmoothDamp(transform.position - shakeOffset, dest, ref followVel, 0.35f);
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, followSize, 1f - Mathf.Exp(-3f * Time.deltaTime));
        }
        else
        {
            float x = Mathf.SmoothDamp(transform.position.x - shakeOffset.x, targetX, ref velX, panSmoothTime);
            pos = new Vector3(x, 0f, -10f);
        }
        transform.position = pos + shakeOffset;
    }

    /// <summary>dir = -1 go left, +1 go right. Stops at both ends (no wrap).</summary>
    public void Step(int dir)
    {
        if (InputLocked || follow != null) return;
        int n = Index + dir;
        if (n < 0 || n >= StopCount) return;
        Index = n;
        targetX = n * StopSpacing;
        StopChanged?.Invoke(Index);
    }

    /// <summary>Locks input, then tracks t and zooms to orthographic size 'size' (smaller = closer).</summary>
    public void Focus(Transform t, float size)
    {
        follow = t;
        followSize = size;
        InputLocked = true;
    }

    public void Shake(float amplitude, float duration) => StartCoroutine(ShakeRoutine(amplitude, duration));

    IEnumerator ShakeRoutine(float amp, float dur)
    {
        for (float t = 0; t < dur; t += Time.deltaTime)
        {
            float k = 1f - t / dur;
            shakeOffset = new Vector3(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f), 0f) * amp * k;
            yield return null;
        }
        shakeOffset = Vector3.zero;
    }
}
