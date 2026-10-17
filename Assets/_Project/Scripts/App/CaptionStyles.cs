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
        { "bee.garden",     new Spot(true) },     // 'You found a bee!' — high under the title, no icon
        { "home.poker",     new Spot(true) },     // b94 (Brandon): 'You earned coins! Tap poker to bet them.' full-width banner over the Level box, off the avatar bird
        { "home.anchored",  new Spot(false) },    // other captions anchored to a small home element
        { "badger.lesson",  new Spot(false) },    // pinned badger lesson rule
        { "poker.back",     new Spot(true) },     // b124: first-hand 'Tap the back button' full-width banner over the bet controls' seat
        { "poker.table",    new Spot(false) },    // poker table lessons (would cover the cards)
        { "album.coach",    new Spot(false) },    // album card (popup)
        { "offer.clearads", new Spot(false) },    // Clear Ads offer (popup)
        { "vip.welcome",    new Spot(false) },    // VIP welcome (popup)
        { "ad.hand",        new Spot(true) },     // b94 (Brandon): Watch-ad "Tap to unlock your bonus!" is a full-width banner below the Watch button
    };

    public static bool IsBanner(string spot) => spot != null && Banner.TryGetValue(spot, out var b) && b.On;

    // b89: a spot the table marks snug (banner off) always paints the snug plate:
    // the text plus the shared even pad, never a seat-sized box.
    public static bool IsSnug(string spot) => spot != null && Banner.TryGetValue(spot, out var b) && !b.On;

    public static float Drop(string spot) => spot != null && Banner.TryGetValue(spot, out var b) ? b.Drop : 0f;

    // 0 in the table means the shared BannerAlpha. A positive ink is that spot only.
    public static float Ink(string spot)
    {
        if (spot != null && Banner.TryGetValue(spot, out var b) && b.Ink > 0f) return b.Ink;
        return BannerAlpha;
    }
}
