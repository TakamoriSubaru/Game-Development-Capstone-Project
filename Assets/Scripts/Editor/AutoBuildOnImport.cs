using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds the placeholder scenes automatically the first time the scripts compile in a project
/// (when Scenes/Title.unity does not exist yet). Nothing to click.
/// To rebuild later: delete the Scenes folder, or use the menu Oddyseus > Build Placeholder Scenes.
/// </summary>
[InitializeOnLoad]
public static class AutoBuildOnImport
{
    const string TitleScene = "Assets/_Project/Scenes/Title.unity";

    static AutoBuildOnImport()
    {
        EditorApplication.delayCall += TryBuild;
    }

    static void TryBuild()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
        if (File.Exists(TitleScene)) return;

        Debug.Log("Oddyseus: no scenes found, building them automatically...");
        SceneBuilder.BuildAll();
    }
}
