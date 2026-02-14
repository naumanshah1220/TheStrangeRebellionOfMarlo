using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Collections.Generic;

public static class BuildSettingsHelper
{
    [MenuItem("Tools/Flow/Setup Build Settings")]
    public static void SetupBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>();

        string mainMenuPath = "Assets/Scenes/MainMenu.unity";
        string detectivePath = "Assets/Scenes/Detective 6.0.unity";

        scenes.Add(new EditorBuildSettingsScene(mainMenuPath, true));
        scenes.Add(new EditorBuildSettingsScene(detectivePath, true));

        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log($"[BuildSettingsHelper] Build Settings updated: {scenes.Count} scenes. MainMenu=0, Detective 6.0=1");
    }

    [MenuItem("Tools/Flow/Set Play Mode to MainMenu")]
    public static void SetPlayModeScene()
    {
        var mainMenuAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/MainMenu.unity");
        if (mainMenuAsset != null)
        {
            EditorSceneManager.playModeStartScene = mainMenuAsset;
            Debug.Log("[BuildSettingsHelper] Play Mode start scene set to MainMenu.");
        }
        else
        {
            Debug.LogError("[BuildSettingsHelper] MainMenu.unity not found at Assets/Scenes/MainMenu.unity!");
        }
    }

    [MenuItem("Tools/Flow/Clear Play Mode Scene (use open scene)")]
    public static void ClearPlayModeScene()
    {
        EditorSceneManager.playModeStartScene = null;
        Debug.Log("[BuildSettingsHelper] Play Mode start scene cleared — will use currently open scene.");
    }
}
