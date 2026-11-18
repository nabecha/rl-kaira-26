using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Registers the three game scenes and opens the start menu.</summary>
public static class KaiRASoccerIntegratedMenu
{
    const string Integrated = "Assets/KaiRASoccerIntegrated/Scenes/";
    static readonly string[] GameScenes = { "SoccerStartMenu", "SoccerHumanVsAI", "SoccerResult" };

    [MenuItem("KaiRA/ゲームを開く（Build Settingsを設定）")]
    public static void OpenIntegrated()
    {
        Open(Integrated);
    }

    static void Open(string folder)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Stop Play mode before switching Soccer scenes.");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        foreach (var name in GameScenes)
        {
            if (!File.Exists(folder + name + ".unity"))
                throw new FileNotFoundException("Soccer scene not found", folder + name + ".unity");
        }

        var scenes = new EditorBuildSettingsScene[GameScenes.Length];
        for (var i = 0; i < GameScenes.Length; i++)
        {
            scenes[i] = new EditorBuildSettingsScene(folder + GameScenes[i] + ".unity", true);
        }
        EditorBuildSettings.scenes = scenes;
        EditorSceneManager.OpenScene(folder + "SoccerStartMenu.unity");
        Debug.Log("Soccer scene set selected: " + folder);
    }
}
