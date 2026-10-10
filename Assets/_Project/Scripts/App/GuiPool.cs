using UnityEngine;

namespace FlockFive
{
    // One pooled IMGUI style or text per draw site. OnGUI runs at least twice a frame
    // (Layout, Repaint), and a "new GUIStyle(GUI.skin.label) { ... }" there allocated a
    // managed object, a native peer and a finalizer on every pass. Each site now takes
    // its own slot, reset to the same proto it used to copy, then sets the same fields,
    // so the drawn result is the one the old copy produced.
    public enum GuiSlot
    {
        HiveTally,
        ShareButton,
        NoAdsVip,
        VipRibbon,
        StreakLabel,
        StreakHero,
        StreakPipsExtra,
        RemainingBirds,
        FlowerLevel,
        FlowerJoke,
        SplashWord,
        InkStamp,
        PokerStampTitle,
        PokerStampLife,
        PokerStampHint,
        AlbumTitle,
        AlbumBody,
        PageTab,
        PageOf,
        InspectX,
        FlipHintMeasure,
        FlipHint,
        AlbumMystery,
        AlbumCount,
        AlbumFoil,
        NewPill,
        LevelHiveTitle,
        LevelHiveHow,
        LevelHivePage,
        HiveColumns,
        GiftMovie,
        GiftThanks,
        SettingsEntry,
        SettingsTitle,
        SettingsLink,
        SettingsRow,
        Count
    }

    public enum GuiText
    {
        Measure,
        RemainingBirds,
        AlbumBody,
        FlipHint,
        NewPill,
        GiftHead,
        GiftWatch,
        FreezeRetry,
        FreezeContinue,
        FreezeBonus,
        WrapWords,
        Count
    }

    public static class GuiPool
    {
        static readonly GUIStyle[] _styles = new GUIStyle[(int)GuiSlot.Count];
        static readonly GUIContent[] _texts = new GUIContent[(int)GuiText.Count];
        static readonly string[] _numPrefix = new string[(int)GuiSlot.Count];
        static readonly int[] _numValue = new int[(int)GuiSlot.Count];
        static readonly string[] _numText = new string[(int)GuiSlot.Count];

        // The skin label, copied once for this slot and reset to it on every later use.
        public static GUIStyle Label(GuiSlot slot) => From(slot, GUI.skin.label);

        public static GUIStyle From(GuiSlot slot, GUIStyle proto)
        {
            int i = (int)slot;
            var st = _styles[i];
            if (st == null)
            {
                st = proto != null ? new GUIStyle(proto) : new GUIStyle();
                _styles[i] = st;
                return st;
            }
            if (proto != null) ResetTo(st, proto);
            return st;
        }

        // Every field a site may change, put back to the proto (what new GUIStyle(proto) copied).
        public static void ResetTo(GUIStyle st, GUIStyle p)
        {
            st.font = p.font;
            st.fontSize = p.fontSize;
            st.fontStyle = p.fontStyle;
            st.alignment = p.alignment;
            st.wordWrap = p.wordWrap;
            st.richText = p.richText;
            st.clipping = p.clipping;
            st.imagePosition = p.imagePosition;
            st.contentOffset = p.contentOffset;
            st.fixedWidth = p.fixedWidth;
            st.fixedHeight = p.fixedHeight;
            st.stretchWidth = p.stretchWidth;
            st.stretchHeight = p.stretchHeight;
            st.padding = p.padding;
            st.margin = p.margin;
            st.border = p.border;
            st.overflow = p.overflow;
            CopyState(st.normal, p.normal);
            CopyState(st.hover, p.hover);
            CopyState(st.active, p.active);
            CopyState(st.focused, p.focused);
            CopyState(st.onNormal, p.onNormal);
            CopyState(st.onHover, p.onHover);
            CopyState(st.onActive, p.onActive);
            CopyState(st.onFocused, p.onFocused);
        }

        static void CopyState(GUIStyleState to, GUIStyleState from)
        {
            to.background = from.background;
            to.textColor = from.textColor;
        }

        // A reusable GUIContent for one site's measure or draw (never held across sites).
        public static GUIContent Text(GuiText slot, string text)
        {
            int i = (int)slot;
            var c = _texts[i];
            if (c == null)
            {
                c = new GUIContent();
                _texts[i] = c;
            }
            c.text = text ?? "";
            return c;
        }

        // prefix + n, rebuilt only when n (or the prefix) changes. HUD counters and the
        // LEVEL caption used to concatenate a new string every pass.
        public static string Num(GuiSlot slot, string prefix, int n)
        {
            int i = (int)slot;
            var t = _numText[i];
            if (t != null && _numValue[i] == n && ReferenceEquals(_numPrefix[i], prefix)) return t;
            t = prefix + n;
            _numText[i] = t;
            _numValue[i] = n;
            _numPrefix[i] = prefix;
            return t;
        }
    }
}
