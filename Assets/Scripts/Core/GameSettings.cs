using UnityEngine;

/// <summary>Player settings, saved with PlayerPrefs so they stick between launches.</summary>
public static class GameSettings
{
    const string VolumeKey = "Oddyseus.Volume";
    const string FullscreenKey = "Oddyseus.Fullscreen";
    const string ChatterKey = "Oddyseus.CompanionChatter";

    static int chatterCache = -1;

    /// <summary>0..1 master volume.</summary>
    public static float Volume
    {
        get { return PlayerPrefs.GetFloat(VolumeKey, 1f); }
        set
        {
            value = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(VolumeKey, value);
            AudioListener.volume = value;
        }
    }

    /// <summary>PC only. (Does nothing visible inside the Unity Editor.)</summary>
    public static bool Fullscreen
    {
        get { return Screen.fullScreen; }
        set
        {
            PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0);
            Screen.fullScreen = value;
        }
    }

    /// <summary>When on, the lobby companion talks by themselves every so often.</summary>
    public static bool CompanionChatter
    {
        get
        {
            if (chatterCache < 0) chatterCache = PlayerPrefs.GetInt(ChatterKey, 1);
            return chatterCache == 1;
        }
        set
        {
            chatterCache = value ? 1 : 0;
            PlayerPrefs.SetInt(ChatterKey, chatterCache);
        }
    }

    public static void Save() { PlayerPrefs.Save(); }

    // Applies the saved volume / fullscreen the moment the game starts.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Apply()
    {
        AudioListener.volume = Volume;
        if (!Application.isMobilePlatform && PlayerPrefs.HasKey(FullscreenKey))
            Screen.fullScreen = PlayerPrefs.GetInt(FullscreenKey) == 1;
    }
}
