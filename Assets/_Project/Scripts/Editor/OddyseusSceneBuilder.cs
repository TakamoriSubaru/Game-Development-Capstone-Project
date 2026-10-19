#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Oddyseus.UI;

namespace Oddyseus.EditorTools
{
    // Menu: Oddyseus > Build Scenes. Membuat scene MainMenu dan Dungeon, lalu mendaftarkannya di Build Settings.
    public static class OddyseusSceneBuilder
    {
        const string Folder = "Assets/_Project/Scenes";

        [MenuItem("Oddyseus/Build Scenes")]
        public static void BuildScenes()
        {
            if (!Directory.Exists(Folder)) Directory.CreateDirectory(Folder);

            string menuPath = Folder + "/" + SceneNames.MainMenu + ".unity";
            string dungeonPath = Folder + "/" + SceneNames.Dungeon + ".unity";

            MakeScene(menuPath, "MainMenuController", typeof(MainMenuController));
            MakeScene(dungeonPath, "DungeonController", typeof(DungeonController));

            var list = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(menuPath, true),
                new EditorBuildSettingsScene(dungeonPath, true)
            };
            EditorBuildSettings.scenes = list.ToArray();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorSceneManager.OpenScene(menuPath);
            Debug.Log("Oddyseus: scene MainMenu dan Dungeon dibuat dan didaftarkan di Build Settings. Tekan Play di scene MainMenu.");
        }

        static void MakeScene(string path, string objectName, System.Type controller)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var go = new GameObject(objectName);
            go.AddComponent(controller);

            var cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Theme.Bg;
            }
            EditorSceneManager.SaveScene(scene, path);
        }
    }
}
#endif
