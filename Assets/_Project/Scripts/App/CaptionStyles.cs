// Build 86 (Brandon): one shared, hand-assigned caption style per spot. No runtime
// auto-switching: a caption keeps its style from show through press and fade-out.
using System.Collections.Generic;

public static class CaptionStyles
{
    public const float BannerAlpha = 0.86f; // near-opaque, close to the solid snug box

    // Drop is extra reach past the home VIP badge, in UI units (times DeviceFit.Ui).
    // 0 keeps the shared banner seat. Ink 0 uses BannerAlpha. Only the adopt greet
    // moves down over that badge and paints solid, so VIP / No Ads cannot read through.
    public readonly struct Spot
    {
        public readonly bool On;
        public readonly float Drop;
        public readonly float Ink;
        public Spot(bool on, float drop = 0f, float ink = 0f)
        {
            On = on;
            Drop = drop < 0f ? 0f : drop;
            Ink = ink < 0f ? 0f : (ink > 1f ? 1f : ink);
        }
    }

    public static readonly Dictionary<string, Spot> Banner = new Dictionary<string, Spot>
    {
        // spot id          banner   drop
        { "adopt.greet",    new Spot(true, 8f, 1f) }, // 'Look, a bird followed you home!' — solid, down over VIP / No Ads
        { "home.intro",     new Spot(true) },     // other home narration lines
        { "coach.line",     new Spot(true) },     // in-garden coach lines (level lessons)
        { "badger.caption", new Spot(true) },     // badger fight narration
        { "home.daily",     new Spot(false) },    // 'Tap Daily for your bonus!' (rail button)
        { "daily.claim",    new Spot(false) },    // Daily card claim step (inside the card)
        { "bee.garden",     new Spot(false) },    // bee callout (icon)
        { "home.poker",     new Spot(false) },    // 'You earned coins! Tap poker to bet them.'
        { "home.anchored",  new Spot(false) },    // other captions anchored to a small home element
        { "badger.lesson",  new Spot(false) },    // pinned badger lesson rule
        { "poker.table",    new Spot(false) },    // poker table lessons (would cover the cards)
        { "album.coach",    new Spot(false) },    // album card (popup)
        { "offer.clearads", new Spot(false) },    // Clear Ads offer (popup)
        { "vip.welcome",    new Spot(false) },    // VIP welcome (popup)
        { "ad.hand",        new Spot(false) },    // ad hand caption (anchored to the ad button)
    };

    public static bool IsBanner(string spot) => spot != null && Banner.TryGetValue(spot, out var b) && b.On;

    public static float Drop(string spot) => spot != null && Banner.TryGetValue(spot, out var b) ? b.Drop : 0f;

    // 0 in the table means the shared BannerAlpha. A positive ink is that spot only.
    public static float Ink(string spot)
    {
        if (spot != null && Banner.TryGetValue(spot, out var b) && b.Ink > 0f) return b.Ink;
        return BannerAlpha;
    }
}
