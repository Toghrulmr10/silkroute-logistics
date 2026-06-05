using System.Collections;
using UnityEngine;

public class CampaignManager : MonoBehaviour
{
    public static CampaignManager Instance { get; private set; }

    private DayConfig[] _days;
    private DayConfig   _currentDay;

    private int   _deliveredToday;
    private int   _failedToday;
    private float _moneyAtDayStart;
    private int   _repAtDayStart;

    public bool GameActive { get; private set; }

    // ROUTE AI yalnız 4-cü gündən tam tövsiyə panelini açır (campaign_days.json: routeAiActive)
    public bool RouteAiActive => _currentDay != null && _currentDay.routeAiActive;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // Kampaniya konfiqurasiyasını dayanıqlı yüklə — xəta menyunu bloklamamalıdır
        try
        {
            var ta = Resources.Load<TextAsset>("Data/campaign_days");
            if (ta == null)
            {
                Debug.LogError("[Campaign] 'Resources/Data/campaign_days' tapılmadı!");
                _days = System.Array.Empty<DayConfig>();
            }
            else
            {
                var list = JsonUtility.FromJson<DayConfigList>(ta.text);
                _days = list?.days ?? System.Array.Empty<DayConfig>();
                Debug.Log($"[Campaign] {_days.Length} gün konfiqurasiyası yükləndi.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Campaign] Konfiqurasiya yükləmə xətası: {e}");
            _days = System.Array.Empty<DayConfig>();
        }

        EventBus.OnDayEnded       += OnDayEnded;
        EventBus.OnOrderDelivered += o => _deliveredToday++;
        EventBus.OnOrderFailed    += o => _failedToday++;

        StartCoroutine(ShowMainMenuNextFrame());
    }

    private IEnumerator ShowMainMenuNextFrame()
    {
        yield return null; // bütün Start() metodları çalışsın

        if (MainMenuPanel.Instance == null)
        {
            Debug.LogError("[Campaign] MainMenuPanel.Instance null — menyu göstərilə bilmir!");
            yield break;
        }

        bool hasSave = SaveSystem.Instance != null && SaveSystem.Instance.HasSave();
        MainMenuPanel.Instance.Show(hasSave);
        Debug.Log("[Campaign] Ana menyu göstərildi.");
    }

    // ── Oyun başlanğıcı ───────────────────────────────────────────────────────
    public void StartNewGame()
    {
        GameState.Instance.ResetForNewGame();
        SaveSystem.Instance.DeleteSave();
        GameActive = true;
        StartDay(0);
    }

    public void ContinueGame()
    {
        var save = SaveSystem.Instance.Load();
        if (save == null) { StartNewGame(); return; }
        GameState.Instance.LoadFromSave(save);
        GameActive = true;
        StartDay(save.campaignDayIndex);
    }

    // ── Gün axını ─────────────────────────────────────────────────────────────
    private void StartDay(int index)
    {
        if (index >= _days.Length) { WinGame(); return; }

        _currentDay      = _days[index];
        _deliveredToday  = 0;
        _failedToday     = 0;
        _moneyAtDayStart = GameState.Instance.Money;
        _repAtDayStart   = GameState.Instance.Reputation;

        GameState.Instance.SetCampaignDay(index);
        GameState.Instance.SetWeather(_currentDay.weather);

        if (_currentDay.unlockRobot) GameState.Instance.UnlockRobot();
        if (_currentDay.unlockDrone) GameState.Instance.UnlockDrone();

        OrderManager.Instance.StartDay(_currentDay.orderCount);
        TimeSystem.Instance.StartDay();

        EventBus.DayStarted(GameState.Instance.Day, _currentDay);
        Debug.Log($"[Campaign] Gün {GameState.Instance.Day} başladı: {_currentDay.title}");
    }

    private void OnDayEnded(int day)
    {
        OrderManager.Instance.StopDay();
        var summary = BuildSummary();
        SaveSystem.Instance.Save(GameState.Instance.ToSaveData());

        bool lost = GameState.Instance.Reputation <= 10 || GameState.Instance.Money <= 0;
        if (lost)
        {
            string reason = GameState.Instance.Reputation <= 10
                ? "Reputasiya çox aşağı düşdü!"
                : "Kredit tükəndi!";
            EventBus.GameLost(reason);
            return;
        }

        DayReportPanel.Instance.Show(summary);
    }

    public void ProceedToShop()    => ShopPanel.Instance.Show();
    public void ProceedToNextDay()
    {
        GameState.Instance.AdvanceDay();
        StartDay(GameState.Instance.Day - 1);
    }

    private void WinGame()
    {
        SaveSystem.Instance.DeleteSave();
        EventBus.GameWon();
    }

    // ── Gün xülasəsi ──────────────────────────────────────────────────────────
    private DaySummary BuildSummary()
    {
        int   total       = _deliveredToday + _failedToday;
        float successRate = total > 0 ? (float)_deliveredToday / total : 0f;

        bool goalMet = true;
        if (_currentDay.goalDeliveries  > 0 && _deliveredToday                       < _currentDay.goalDeliveries)  goalMet = false;
        if (_currentDay.goalReputation  > 0 && GameState.Instance.Reputation          < _currentDay.goalReputation)  goalMet = false;
        if (_currentDay.goalMoney       > 0 && GameState.Instance.Money               < _currentDay.goalMoney)       goalMet = false;
        if (_currentDay.goalSuccessRate > 0 && successRate * 100f                     < _currentDay.goalSuccessRate) goalMet = false;

        int stars = successRate >= 0.85f ? 3 : successRate >= 0.70f ? 2 : successRate >= 0.50f ? 1 : 0;

        return new DaySummary
        {
            Day             = GameState.Instance.Day,
            DayTitle        = _currentDay.title,
            GoalDescription = _currentDay.goalDescription,
            Delivered       = _deliveredToday,
            Failed          = _failedToday,
            Revenue         = GameState.Instance.Money - _moneyAtDayStart,
            RepChange       = GameState.Instance.Reputation - _repAtDayStart,
            GoalMet         = goalMet,
            Stars           = stars
        };
    }
}
