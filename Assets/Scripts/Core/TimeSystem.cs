using UnityEngine;

public class TimeSystem : MonoBehaviour
{
    public static TimeSystem Instance { get; private set; }

    public const float SecondsPerGameMinute = 1f;
    public const int   MinutesPerGameDay    = 480;

    public float GameTimeSeconds  { get; private set; }
    public int   CurrentGameMinute => Mathf.FloorToInt(GameTimeSeconds / SecondsPerGameMinute);

    private bool _running = false; // kampaniya başlayana qədər dayandırılmış

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        if (!_running) return;

        GameTimeSeconds += Time.deltaTime;

        if (CurrentGameMinute >= MinutesPerGameDay)
        {
            _running = false;
            EventBus.DayEnded(GameState.Instance.Day);
        }
    }

    public void StartDay()
    {
        GameTimeSeconds = 0f;
        _running = true;
    }

    public void Pause()  => _running = false;
    public void Resume() => _running = true;

    public float DayProgress => Mathf.Clamp01(GameTimeSeconds / (MinutesPerGameDay * SecondsPerGameMinute));
}
