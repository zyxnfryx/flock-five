using UnityEngine;

namespace FlockFive
{
    // First-garden helpers: a pulsing ring over a good branch plus one hint line.
    // They step aside as soon as the player shows they get it (a move of their own
    // choosing, two guided moves, or a collect), and never come back.
    public sealed partial class FlockFiveApp
    {
        const string CoachKey = "flockfive.coach.done";
        const string CoachGiftKey = "flockfive.coach.gift";

        bool _coach;
        int _coachMoves;
        int _coachFrom = -1, _coachTo = -1;
        float _coachFade;
        float _coachGiftUntil = -1f;

        void CoachBegin(int index)
        {
            _coach = index == 0 && PlayerPrefs.GetInt(CoachKey, 0) == 0;
            _coachMoves = 0;
            _coachFade = 0f;
            _coachGiftUntil = -1f;
        }

        // Called after every successful move.
        void CoachMoved(int from, int to, int collects)
        {
            if (!_coach) return;
            bool ownIdea = _coachFrom >= 0 && (from != _coachFrom || to != _coachTo);
            _coachMoves++;
            if (ownIdea || collects > 0 || _coachMoves >= 2) CoachDone();
        }

        void CoachDone()
        {
            if (!_coach) return;
            _coach = false;
            PlayerPrefs.SetInt(CoachKey, 1);
            if (PlayerPrefs.GetInt(CoachGiftKey, 0) == 0 && GiftBranch() >= 0)
            {
                _coachGiftUntil = Time.unscaledTime + 4.5f;
                PlayerPrefs.SetInt(CoachGiftKey, 1);
            }
            PlayerPrefs.Save();
        }

        int GiftBranch()
        {
            if (_board == null) return -1;
            for (int i = 0; i < _board.Branches.Count; i++)
                if (_board.Branches[i].AdLocked) return i;
            return -1;
        }

        bool CoachPick(out int from, out int to)
        {
            from = -1; to = -1;
            int fallFrom = -1, fallTo = -1;
            for (int a = 0; a < _board.Branches.Count; a++)
            {
                if (!_board.CanPick(a) || Locked(a) || GiftLocked(a)) continue;
                for (int b = 0; b < _board.Branches.Count; b++)
                {
                    if (b == a || Locked(b) || GiftLocked(b)) continue;
                    if (!_board.CanMove(a, b, out _)) continue;
                    // Prefer joining a matching color over dropping on an empty branch.
                    if (!_board.Branches[b].Empty) { from = a; to = b; return true; }
                    if (fallFrom < 0) { fallFrom = a; fallTo = b; }
                }
            }
            from = fallFrom; to = fallTo;
            return from >= 0;
        }

        Vector2 BranchGui(int i)
        {
            var cam = _garden.Cam;
            var t = _garden.Branches[i].transform;
            if (cam == null || t == null) return new Vector2(-999f, -999f);
            var p = cam.WorldToScreenPoint(t.position);
            return new Vector2(p.x, Screen.height - p.y);
        }

        void DrawCoachRing(Vector2 c, float s)
        {
            var glow = GlowTex();
            float t = Time.unscaledTime;
            float pulse = 0.5f + 0.5f * Mathf.Sin(t * 5.2f);
            float r = (58f + 10f * pulse) * s;
            GUI.color = new Color(1f, 0.93f, 0.55f, (0.30f + 0.30f * pulse) * _coachFade);
            GUI.DrawTexture(new Rect(c.x - r, c.y - r, r * 2f, r * 2f), glow, ScaleMode.ScaleToFit, true);
            var sp = SpriteCatalog.Sparkle;
            if (sp != null && sp.texture != null)
            {
                float k = 26f * s;
                float bob = Mathf.Sin(t * 4f) * 8f * s;
                GUI.color = new Color(1f, 1f, 1f, 0.95f * _coachFade);
                GUI.DrawTexture(new Rect(c.x - k * 0.5f, c.y - r - k * 0.3f + bob, k, k), sp.texture, ScaleMode.ScaleToFit, true);
            }
            GUI.color = Color.white;
        }

        void DrawCoachLine(string text, float s, float top)
        {
            var st = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            float w = Screen.width * 0.86f;
            float h = 54f * s;
            var r = new Rect((Screen.width - w) * 0.5f, top + 6f * s, w, h);
            st.fontSize = FitFontWrapped(st, text, r.width, r.height, 14, Mathf.RoundToInt(22f * s));
            StampOutlined(r, text, st, new Color(1f, 0.96f, 0.82f, _coachFade), 2, 1);
        }

        void DrawCoach(float s, float top)
        {
            if (_board == null || _garden.Cam == null) return;
            if (Time.unscaledTime < _coachGiftUntil)
            {
                int g = GiftBranch();
                if (g >= 0)
                {
                    _coachFade = Mathf.Clamp01(Mathf.Min(1f, (_coachGiftUntil - Time.unscaledTime) / 0.5f));
                    DrawCoachRing(BranchGui(g), s);
                    DrawCoachLine("Stuck? Tap the gift branch for a bonus spot.", s, top);
                }
                return;
            }
            if (!_coach || _busy || _won || _gift != GiftFace.None || _locked.Count > 0)
            {
                _coachFade = 0f;
                return;
            }
            _coachFade = Mathf.Min(1f, _coachFade + Time.unscaledDeltaTime / 0.35f);

            if (_sel < 0)
            {
                if (!CoachPick(out _coachFrom, out _coachTo)) return;
                DrawCoachRing(BranchGui(_coachFrom), s);
                DrawCoachLine("Tap a branch to pick up its top birds.", s, top);
                return;
            }

            int to = -1;
            if (_sel == _coachFrom && _coachTo >= 0 && _board.CanMove(_sel, _coachTo, out _))
                to = _coachTo;
            else
                for (int b = 0; b < _board.Branches.Count && to < 0; b++)
                    if (b != _sel && !Locked(b) && !GiftLocked(b) && _board.CanMove(_sel, b, out _)) to = b;
            if (to < 0)
            {
                DrawCoachLine("Those birds have nowhere to go yet. Try another branch.", s, top);
                return;
            }
            if (_sel != _coachFrom) { _coachFrom = _sel; _coachTo = to; }
            DrawCoachRing(BranchGui(to), s);
            DrawCoachLine("Now tap a branch with the same color on top, or an empty one.", s, top);
        }
    }
}
