using UnityEditor;
using UnityEngine;

public static class SmokeTestMenu
{
    [MenuItem("SmokeTest/Start New Game")]
    static void StartNewGame()
    {
        if (!Application.isPlaying) { Debug.LogWarning("Play mode!"); return; }
        if (MainMenuPanel.Instance != null) MainMenuPanel.Instance.Hide();
        CampaignManager.Instance.StartNewGame();
        Debug.Log("[SmokeTest] StartNewGame cagrildi");
    }

    [MenuItem("SmokeTest/Hide Menu")]
    static void HideMenu()
    {
        if (!Application.isPlaying) return;
        if (MainMenuPanel.Instance != null) MainMenuPanel.Instance.Hide();
        Debug.Log("[SmokeTest] MainMenu gizledildi");
    }

    [MenuItem("SmokeTest/Set TimeScale 4")]
    static void SetTimeScale4() { Time.timeScale = 4f; Debug.Log("[SmokeTest] timeScale = 4"); }

    [MenuItem("SmokeTest/Set TimeScale 1")]
    static void SetTimeScale1() { Time.timeScale = 1f; }
}
