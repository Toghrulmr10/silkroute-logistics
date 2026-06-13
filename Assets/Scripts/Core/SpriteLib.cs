using System.Collections.Generic;
using UnityEngine;

/// Resources/Sprites altındakı sprite-ları yükləyən sadə kitabxana.
/// Sprite-lar dilimlənmiş (spriteMode: Multiple) ola bilər — LoadAll ilə ilk kadrı götürürük.
/// Hər şey null-safe: sprite tapılmasa null qaytarır, çağıran tərəf rəngli qutu/mətnə geri düşür.
public static class SpriteLib
{
    private static readonly Dictionary<string, Sprite> _cache = new();

    /// path: "Sprites/drone_idle" və ya "Sprites/Icons/icon_coin" (uzantısız, Resources-a nisbi)
    public static Sprite Get(string path)
    {
        if (_cache.TryGetValue(path, out var cached)) return cached;

        Sprite result = null;

        // Dilimlənmiş sprite-lar üçün LoadAll → ilk kadr
        var all = Resources.LoadAll<Sprite>(path);
        if (all != null && all.Length > 0)
            result = all[0];

        // Tək sprite halı
        if (result == null)
            result = Resources.Load<Sprite>(path);

        if (result == null)
            Debug.LogWarning($"[SpriteLib] Sprite tapılmadı: {path} (rəngli fallback istifadə olunacaq)");

        _cache[path] = result;
        return result;
    }
}
