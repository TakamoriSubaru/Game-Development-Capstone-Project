using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Menu: Oddyseus > Rebuild Lobby (ALL in one)
/// Runs "Build Lobby (Remastered)" and then "Add Lobby Interactables" in one go, saves the scene and shows a
/// message box with what it found, so you can see at once whether the new Lobby was really built.
/// The old Lobby scene is backed up to Assets/_Project/Scenes/_Backups first (by Build Lobby).
/// </summary>
public static class LobbyRebuildAll
{
    [MenuItem("Oddyseus/Rebuild Lobby (ALL in one)")]
    public static void Run()
    {
        try
        {
            LobbyBuilder.Build();

            Scene scene = SceneManager.GetActiveScene();
            GameObject env = GameObject.Find("ENVIRONMENT");
            if (scene.name != GameScenes.Lobby || env == null)
            {
                EditorUtility.DisplayDialog("Rebuild Lobby",
                    "The Lobby was NOT rebuilt.\n\nMost likely you pressed Cancel in the 'save scene' question, or Console shows a red error. " +
                    "Open the Console, fix or copy the first red error, then run this menu again.", "OK");
                return;
            }

            LobbyPropsBuilder.Add();
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

            int backgrounds = 0, withArt = 0;
            foreach (Transform t in env.transform)
            {
                if (!t.name.StartsWith("Lobby Stop")) continue;
                backgrounds++;
                SpriteRenderer sr = t.GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite != null) withArt++;
            }
            GameObject props = GameObject.Find("PROPS (interactables, drag onto the painted objects)");
            int propCount = props != null ? props.transform.childCount : 0;

            EditorUtility.DisplayDialog("Rebuild Lobby",
                "Lobby rebuilt and saved.\n\n" +
                "Backgrounds with art: " + withArt + " of " + backgrounds + "\n" +
                "Interactable props: " + propCount + "\n\n" +
                (withArt < 4 ? "Some background has NO sprite: check Console for 'Lobby art missing'.\n" : "") +
                "Press Play from the Title (or Lobby) scene.", "OK");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            EditorUtility.DisplayDialog("Rebuild Lobby", "Failed: " + e.Message + "\n\nSee the Console for details.", "OK");
        }
    }
}