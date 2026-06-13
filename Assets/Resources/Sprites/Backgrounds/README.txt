Açılış (intro) kartlarının fon şəkilləri bura qoyulur.

Tələblər:
- Format: PNG, 1080x1920 (portret, 9:16)
- Unity-də: Texture Type = "Sprite (2D and UI)"
- Adlar (dəqiq belə olmalıdır):
    intro_1.png  — Köhnə Bao'an (深圳/ŞENJEN)
    intro_2.png  — Rəqiblər (Nanşan)
    intro_3.png  — ROUTE (AI)
    intro_4.png  — Keçid başlayır (neon Şenjen)

Qeyd: şəkil olmasa, həmin kart avtomatik düz rəngli fona keçir (graceful fallback) —
oyun qırılmaz. IntroPanel bu şəkilləri "Sprites/Backgrounds/intro_N" yolu ilə yükləyir
və üstünə oxunaqlıq üçün qaranlıq pərdə qoyur.
