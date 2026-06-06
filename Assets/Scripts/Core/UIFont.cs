using UnityEngine;

/// Bütün legacy UI Text-ləri üçün etibarlı şrift təminatçısı.
/// Unity 6-da builtin "LegacyRuntime.ttf" bəzən null qaytarır → o zaman
/// OS şriftindən dinamik şrift yaradılır. Beləcə mətnlər həmişə render olunur.
public static class UIFont
{
    private static Font _cached;

    public static Font Get()
    {
        if (_cached != null) return _cached;

        _cached = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (_cached == null) _cached = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (_cached == null)
            _cached = Font.CreateDynamicFontFromOSFont(
                new[] { "Arial", "Liberation Sans", "Segoe UI", "DejaVu Sans", "Roboto" }, 16);

        if (_cached == null)
            Debug.LogError("[UIFont] Heç bir şrift tapılmadı — mətnlər görünməyəcək!");

        return _cached;
    }
}
