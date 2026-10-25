using System;
using UnityEngine;

/// <summary>
/// Player settings saved in PlayerPrefs.
/// Music and SFX are 0..1 (default 0.7 = 70%).
/// CompanionChatter has no screen in the Settings panel (hidden), default ON.
/// There is no fullscreen option anymore.
/// </summary>
public static class GameSettings
{
    const string KeyMusic = "Oddyseus.MusicVolume";
    const string KeySfx = "Oddyseus.SfxVolume";
    const string KeyChatter = "Oddyseus.CompanionChatter";

    public const float DefaultVolume = 0.7f;

    static bool loaded;
    static float music = DefaultVolume;
    static float sfx = DefaultVolume;
    static bool chatter = true;

    // Raised whenever a value changes (AudioManager listens to this).
    public static event Action Changed;

    public static float MusicVolume
    {
        get { EnsureLoaded(); return music; }
        set
        {
            EnsureLoaded();
            value = Mathf.Clamp01(value);
            if (Mathf.Approximately(music, value)) return;
            music = value;
            PlayerPrefs.SetFloat(KeyMusic, music);
            RaiseChanged();
        }
    }

    public static float SfxVolume
    {
        get { EnsureLoaded(); return sfx; }
        set
        {
            EnsureLoaded();
            value = Mathf.Clamp01(value);
            if (Mathf.Approximately(sfx, value)) return;
            sfx = value;
            PlayerPrefs.SetFloat(KeySfx, sfx);
            RaiseChanged();
        }
    }

    // Hidden setting (no UI). Default ON.
    public static bool CompanionChatter
    {
        get { EnsureLoaded(); return chatter; }
        set
        {
            EnsureLoaded();
            if (chatter == value) return;
            chatter = value;
            PlayerPrefs.SetInt(KeyChatter, chatter ? 1 : 0);
            PlayerPrefs.Save();
            RaiseChanged();
        }
    }

    public static void Load()
    {
        music = Mathf.Clamp01(PlayerPrefs.GetFloat(KeyMusic, DefaultVolume));
        sfx = Mathf.Clamp01(PlayerPrefs.GetFloat(KeySfx, DefaultVolume));
        chatter = PlayerPrefs.GetInt(KeyChatter, 1) == 1;
        loaded = true;
        RaiseChanged();
    }

    public static void Save()
    {
        EnsureLoaded();
        PlayerPrefs.SetFloat(KeyMusic, music);
        PlayerPrefs.SetFloat(KeySfx, sfx);
        PlayerPrefs.SetInt(KeyChatter, chatter ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static void ResetToDefaults()
    {
        music = DefaultVolume;
        sfx = DefaultVolume;
        chatter = true;
        loaded = true;
        Save();
        RaiseChanged();
    }

    static void EnsureLoaded()
    {
        if (loaded) return;
        music = Mathf.Clamp01(PlayerPrefs.GetFloat(KeyMusic, DefaultVolume));
        sfx = Mathf.Clamp01(PlayerPrefs.GetFloat(KeySfx, DefaultVolume));
        chatter = PlayerPrefs.GetInt(KeyChatter, 1) == 1;
        loaded = true;
    }

    static void RaiseChanged()
    {
        if (Changed != null) Changed();
    }
}
