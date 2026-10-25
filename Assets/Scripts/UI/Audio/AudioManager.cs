using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Audio stub. Created automatically before the first scene loads and kept between scenes.
/// Volumes come from GameSettings. If the AudioMixer asset exists
/// (Resources/UIKit/MainMixer with exposed MusicVolume and SFXVolume) it is used;
/// if not, the volume is set directly on the AudioSources (still works).
/// </summary>
public class AudioManager : MonoBehaviour
{
    public const string MusicParam = "MusicVolume";
    public const string SfxParam = "SFXVolume";
    const string MixerPath = "UIKit/MainMixer";

    static AudioManager instance;

    public static AudioManager Instance
    {
        get
        {
            if (instance == null) Create();
            return instance;
        }
    }

    AudioMixer mixer;
    AudioSource musicSource;
    AudioSource sfxSource;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()
    {
        if (instance == null) Create();
    }

    static void Create()
    {
        GameObject go = new GameObject("AudioManager");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<AudioManager>();
    }

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;

        mixer = Resources.Load<AudioMixer>(MixerPath);

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = true;
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.loop = false;

        if (mixer != null)
        {
            AudioMixerGroup[] m = mixer.FindMatchingGroups("Music");
            AudioMixerGroup[] s = mixer.FindMatchingGroups("SFX");
            if (m != null && m.Length > 0) musicSource.outputAudioMixerGroup = m[0];
            if (s != null && s.Length > 0) sfxSource.outputAudioMixerGroup = s[0];
        }

        GameSettings.Changed += ApplyVolumes;
        ApplyVolumes();
    }

    void OnDestroy()
    {
        GameSettings.Changed -= ApplyVolumes;
        if (instance == this) instance = null;
    }

    public void ApplyVolumes()
    {
        float m = GameSettings.MusicVolume;
        float s = GameSettings.SfxVolume;
        bool mixerOk = false;
        if (mixer != null)
        {
            bool a = mixer.SetFloat(MusicParam, ToDecibel(m));
            bool b = mixer.SetFloat(SfxParam, ToDecibel(s));
            mixerOk = a && b;
        }
        if (!mixerOk)
        {
            // Fallback when the mixer is missing or the parameters are not exposed.
            if (musicSource != null) musicSource.volume = m;
            if (sfxSource != null) sfxSource.volume = s;
        }
        else
        {
            if (musicSource != null) musicSource.volume = 1f;
            if (sfxSource != null) sfxSource.volume = 1f;
        }
    }

    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (clip == null || musicSource == null) return;
        if (musicSource.clip == clip && musicSource.isPlaying) return;
        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource != null) musicSource.Stop();
    }

    public void PlaySfx(AudioClip clip, float volume = 1f)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    static float ToDecibel(float linear)
    {
        if (linear <= 0.0001f) return -80f;
        return Mathf.Log10(linear) * 20f;
    }
}
