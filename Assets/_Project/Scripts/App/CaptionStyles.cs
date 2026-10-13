// Build 86 (Brandon): one shared, hand-assigned caption style per spot. No runtime
// auto-switching: a caption keeps its style from show through press and fade-out.
using System.Collections.Generic;

public static class CaptionStyles
{
    public const float BannerAlpha = 0.86f; // near-opaque, close to the solid snug box

    public static readonly Dictionary<string, bool> Banner = new Dictionary<string, bool>
    {
        // spot id          banner?
        { "adopt.greet",    true  }, // 'Look, a bird followed you home!' (bird below the band)
        { "home.intro",     true  }, // other home narration lines
        { "coach.line",     true  }, // in-garden coach lines (level lessons)
        { "badger.caption", true  }, // badger fight narration
        { "home.daily",     false }, // 'Tap Daily for your bonus!' (rail button)
        { "daily.claim",    false }, // Daily card claim step (inside the card)
        { "bee.garden",     false }, // bee callout (icon)
        { "home.poker",     false }, // 'You earned coins! Tap poker to bet them.'
        { "home.anchored",  false }, // other captions anchored to a small home element
        { "badger.lesson",  false }, // pinned badger lesson rule
        { "poker.table",    false }, // poker table lessons (would cover the cards)
        { "album.coach",    false }, // album card (popup)
        { "offer.clearads", false }, // Clear Ads offer (popup)
        { "vip.welcome",    false }, // VIP welcome (popup)
        { "ad.hand",        false }, // ad hand caption (anchored to the ad button)
    };

    public static bool IsBanner(string spot) => spot != null && Banner.TryGetValue(spot, out var b) && b;
}
