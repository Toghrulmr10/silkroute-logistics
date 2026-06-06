using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class SmokeTestMenu
{
    [MenuItem("SmokeTest/Start New Game")]
    static void StartNewGame()
    {
        if (!Application.isPlaying) { Debug.LogWarning("Play mode!"); return; }
        if (CampaignManager.Instance == null) { Debug.LogError("[SmokeTest] CampaignManager.Instance null — bootstrap tamamlanmayib!"); return; }
        ForceHideMenu();
        CampaignManager.Instance.StartNewGame();
        Debug.Log("[SmokeTest] StartNewGame cagrildi");
    }

    [MenuItem("SmokeTest/Force Hide Menu")]
    static void ForceHideMenuCmd()
    {
        if (!Application.isPlaying) return;
        ForceHideMenu();
    }

    static void ForceHideMenu()
    {
        // 1. Via Instance reference
        if (MainMenuPanel.Instance != null)
            MainMenuPanel.Instance.Hide();

        // 2. Backup: find by name and disable every Canvas with sortOrder>=50 named MainMenuPanel
        var all = GameObject.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var c in all)
        {
            if (c.gameObject.name == "MainMenuPanel")
            {
                c.gameObject.SetActive(false);
                Debug.Log($"[SmokeTest] Canvas '{c.gameObject.name}' force-gizlendi (sortOrder={c.sortingOrder})");
            }
        }

        Debug.Log("[SmokeTest] ForceHideMenu tamamlandi");
    }

    [MenuItem("SmokeTest/Print State")]
    static void PrintState()
    {
        if (!Application.isPlaying) return;
        var gs = GameState.Instance;
        var mm = MainMenuPanel.Instance;
        bool menuVisible = false;
        if (mm != null)
        {
            // Use reflection to check _root active state
            var field = typeof(MainMenuPanel).GetField("_root",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                var root = field.GetValue(mm) as GameObject;
                menuVisible = root != null && root.activeSelf;
            }
        }
        Debug.Log($"[SmokeTest STATE] Day={gs?.Day} Money=¥{gs?.Money:N0} Rep={gs?.Reputation} Energy={gs?.Energy} " +
                  $"GameActive={CampaignManager.Instance?.GameActive} MenuVisible={menuVisible} TimeScale={Time.timeScale}");
    }

    [MenuItem("SmokeTest/Set TimeScale 4")]
    static void SetTimeScale4() { Time.timeScale = 4f; Debug.Log("[SmokeTest] timeScale = 4"); }

    [MenuItem("SmokeTest/Set TimeScale 1")]
    static void SetTimeScale1() { Time.timeScale = 1f; Debug.Log("[SmokeTest] timeScale = 1"); }

    [MenuItem("SmokeTest/Proceed To Shop")]
    static void ProceedToShop()
    {
        if (!Application.isPlaying) return;
        CampaignManager.Instance?.ProceedToShop();
        Debug.Log("[SmokeTest] ProceedToShop");
    }

    [MenuItem("SmokeTest/Proceed To Next Day")]
    static void ProceedToNextDay()
    {
        if (!Application.isPlaying) return;
        CampaignManager.Instance?.ProceedToNextDay();
        Debug.Log("[SmokeTest] ProceedToNextDay");
    }
}
