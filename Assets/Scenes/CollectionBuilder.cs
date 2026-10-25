using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// AI-4 scene builder for the collection screens. Menu: Oddyseus/Build Legends, Build Character Detail,
/// Build Armory, Build Pack Compendium, Build Achievements, Build All Collection Screens.
/// Each menu item creates Assets/_Project/Scenes/&lt;Scene&gt;.unity. If that scene already exists it is copied to
/// Assets/_Project/Scenes/_Backups first. The scene is added to Build Settings (existing entries are kept).
/// Needs the UI Kit sprites: run Oddyseus/Build UI Kit Gallery once before the first build.
/// </summary>
public static class CollectionBuilder
{
    const string SceneDir = "Assets/_Project/Scenes";
    const string BackupDir = SceneDir + "/_Backups";

    [MenuItem("Oddyseus/Build Legends")]
    static void MenuLegends() { Run(false, Step<LegendsScreen>(GameScenes.Legends)); }

    [MenuItem("Oddyseus/Build Character Detail")]
    static void MenuDetail() { Run(false, Step<CharacterDetailScreen>(CollectionNav.CharacterDetailScene)); }

    [MenuItem("Oddyseus/Build Armory")]
    static void MenuArmory() { Run(false, Step<ArmoryScreen>(GameScenes.Armory)); }

    [MenuItem("Oddyseus/Build Pack Compendium")]
    static void MenuPacks() { Run(false, Step<PackCompendiumScreen>(GameScenes.PackCompendium)); }

    [MenuItem("Oddyseus/Build Achievements")]
    static void MenuAchievements() { Run(false, Step<AchievementsScreen>(GameScenes.Achievements)); }

    [MenuItem("Oddyseus/Build All Collection Screens")]
    static void MenuAll()
    {
        Run(true,
            Step<LegendsScreen>(GameScenes.Legends),
            Step<CharacterDetailScreen>(CollectionNav.CharacterDetailScene),
            Step<ArmoryScreen>(GameScenes.Armory),
            Step<PackCompendiumScreen>(GameScenes.PackCompendium),
            Step<AchievementsScreen>(GameScenes.Achievements));
    }

    // ------------------------------------------------------------------
    class BuildStep
    {
        public string sceneName;
        public Action<Scene> build;
    }

    static BuildStep Step<T>(string sceneName) where T : CollectionScreenBase
    {
        BuildStep s = new BuildStep();
        s.sceneName = sceneName;
        s.build = delegate (Scene scene)
        {
            GameObject go = new GameObject(typeof(T).Name);
            T screen = go.AddComponent<T>();
            screen.BuildUI();
        };
        return s;
    }

    static void Run(bool all, params BuildStep[] steps)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Collection Builder", "Stop Play mode first.", "OK");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        UIArt.ClearCache();
        UITheme.ReloadFonts();
        Sprite panel = UIArt.Get("panel");
        if (panel == null || string.IsNullOrEmpty(AssetDatabase.GetAssetPath(panel)))
        {
            EditorUtility.DisplayDialog("Collection Builder",
                "The UI Kit sprites are not on disk yet (Resources/UIKit/panel.png is missing).\n\n" +
                "Run the menu  Oddyseus > Build UI Kit Gallery  once, then run this menu again.", "OK");
            return;
        }

        Directory.CreateDirectory(SceneDir);
        string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        List<string> done = new List<string>();
        List<string> backedUp = new List<string>();

        try
        {
            foreach (BuildStep step in steps)
            {
                string path = SceneDir + "/" + step.sceneName + ".unity";
                if (File.Exists(path))
                {
                    Directory.CreateDirectory(BackupDir);
                    string backup = BackupDir + "/" + step.sceneName + "_" + stamp + ".unity";
                    File.Copy(path, backup, true);
                    backedUp.Add(backup);
                }

                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                MakeCamera();
                step.build(scene);
                EditorSceneManager.SaveScene(scene, path);
                AddToBuildSettings(path);
                done.Add(path);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[CollectionBuilder] Failed: " + e);
            EditorUtility.DisplayDialog("Collection Builder",
                "Something went wrong. Copy the red error from the Console and send it back.\n\n" + e.Message, "OK");
            return;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string msg = "Built " + done.Count + " scene(s):\n" + string.Join("\n", done.ToArray());
        if (backedUp.Count > 0) msg += "\n\nOld versions were copied to " + BackupDir + ".";
        if (!UITheme.FontsInstalled)
            msg += "\n\nNOTE: Cinzel / Noto Serif font assets were not found, so the TMP default font was used. " +
                   "Run Oddyseus > Create UI Fonts, then build again.";
        EditorUtility.DisplayDialog("Collection Builder", msg, "OK");
    }

    static void MakeCamera()
    {
        GameObject cam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cam.tag = "MainCamera";
        Camera c = cam.GetComponent<Camera>();
        c.clearFlags = CameraClearFlags.SolidColor;
        c.backgroundColor = UITheme.Navy;
        c.orthographic = true;
        c.cullingMask = 0;
    }

    static void AddToBuildSettings(string path)
    {
        List<EditorBuildSettingsScene> list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        bool found = false;
        foreach (EditorBuildSettingsScene s in list)
        {
            if (s.path == path) { s.enabled = true; found = true; }
        }
        if (!found) list.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = list.ToArray();
    }
}
