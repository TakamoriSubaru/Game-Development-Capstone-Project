using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Oddyseus.UI;

/// <summary>
/// Membuat scene DungeonHub (satu kamera + satu GameObject HubController; seluruh UI dibuat kode) dan mendaftarkannya
/// di Build Settings TANPA menghapus scene lain. Otomatis berjalan sekali kalau DungeonHub.unity belum ada.
/// Manual: menu Oddyseus > Build Hub Scene.
/// </summary>
[InitializeOnLoad]
public static class HubSceneBuilder
{
    const string ScenePath = "Assets/_Project/Scenes/DungeonHub.unity";
    const string TitlePath = "Assets/_Project/Scenes/Title.unity";

    static HubSceneBuilder()
    {
        EditorApplication.delayCall += TryBuild;
    }

    static void TryBuild()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
        if (File.Exists(ScenePath) || !File.Exists(TitlePath)) return;   // kalau Title belum ada, SceneBuilder.BuildAll yang membuat semuanya
        if (EditorSceneManager.GetActiveScene().isDirty) return;         // jangan buang perubahan yang belum disimpan
        BuildAndRegister();
    }

    [MenuItem("Oddyseus/Build Hub Scene")]
    public static void BuildAndRegister()
    {
        string previous = EditorSceneManager.GetActiveScene().path;
        BuildScene();
        Register();
        if (!string.IsNullOrEmpty(previous) && previous != ScenePath) EditorSceneManager.OpenScene(previous);
        Debug.Log("Oddyseus: scene DungeonHub dibuat dan didaftarkan di Build Settings.");
    }

    public static void BuildScene()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>();
        camGo.AddComponent<AudioListener>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Theme.Parchment;
        camGo.transform.position = new Vector3(0f, 0f, -10f);

        new GameObject("HubController").AddComponent<HubController>();
        EditorSceneManager.SaveScene(scene, ScenePath);
    }

    static void Register()
    {
        var list = EditorBuildSettings.scenes.ToList();
        if (list.Any(s => s.path == ScenePath)) return;
        // taruh tepat sebelum scene Dungeon kalau ada, supaya urutan Title, Lobby, Story, DungeonHub, Dungeon
        int at = list.FindIndex(s => s.path.EndsWith("/Dungeon.unity"));
        var entry = new EditorBuildSettingsScene(ScenePath, true);
        if (at >= 0) list.Insert(at, entry); else list.Add(entry);
        EditorBuildSettings.scenes = list.ToArray();
    }
}
