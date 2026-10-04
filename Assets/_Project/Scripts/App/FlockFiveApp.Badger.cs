using System.Collections.Generic;
using UnityEngine;

namespace FlockFive
{
    // Honey badger contest, Phase 3: loadout and contest screen (IMGUI).
    // Reached only through OpenBadgerFight, which obeys the BadgerSchedule.Enabled kill
    // switch. Nothing in the real flower or flag flow calls it yet (Phases 4 and 5).
    // Mild and cartoonish: a knock is a honey splat and a comic bump, never a wound.
    //
    // Shared pieces: BadgerLoadout (deal and swap), BadgerFight (rules), BadgerSchedule
    // (targets, prices, boss mix), Hive.HoneyOfFinish (every honey number),
    // CardText.DrawHoneyDigit, AlbumWood/AlbumFace (the album plate colors), DrawBadgerTile
    // (the one honeycomb tile: grids, arena, picker), BadgerButton (FIGHT, power-ups,
    // picker controls, Continue), Purse.TrySpend (inside BadgerFight), Sfx.CardBump/Deny.
    public sealed partial class FlockFiveApp
    {
        enum BadgerStage { Loadout, Picker, BossWait, YourPick, Reveal, Verdict, Over }
        enum BadgerLook { Face, Down, Spent }

        const float BadgerBossWait = 0.75f;
        const float BadgerSlide = 0.35f;
        const float BadgerRevealHold = 0.80f;
        const float BadgerVerdictHold = 1.20f;
        const float BadgerDotStep = 0.11f;
        const float BadgerShake = 0.35f;
        const int BadgerPickPage = 12;
        const int BadgerPowerKey = 100;

        BadgerStage _bgStage;
        BadgerLoadout _bgLoadout;
        BadgerFight _bgFight;
        int _bgAppearance;
        int _bgSeed;
        float _bgT;
        float _bgDotT;
        float _bgBossAt;
        float _bgPlayerAt;
        int _bgBossIx = -1;
        int _bgPlayerIx = -1;
        BadgerPower _bgArmed;
        BadgerRound _bgRound;
        bool _bgResolved;
        int _bgShownPlayer;
        int _bgShownBoss;
        int _bgPickSlot = -1;
        int _bgPickPage;
        int _bgShakeKey = -1;
        float _bgShakeT;
        string _bgLine = "";
        string _bgNeedLine = "";
        readonly string[] _bgPrice = new string[5];
        readonly List<BadgerTile> _bgOptions = new List<BadgerTile>(64);
        static GUIStyle _bgStyle;
        static GUIStyle _bgWrap;
        static Texture2D _bgHex;
#if UNITY_EDITOR
        bool _bgSwitchWas;
        bool _bgSwitchHeld;
#endif

        static readonly Color BadgerWax = new Color(1f, 0.86f, 0.40f, 1f);
        static readonly Color BadgerHide = new Color(0.52f, 0.46f, 0.43f, 1f);
        static readonly Color BadgerFoilRim = new Color(0.15f, 0.48f, 0.92f, 1f);
        static readonly Color BadgerInverseRim = new Color(0.82f, 0.22f, 0.68f, 1f);
        static readonly Color BadgerCream = new Color(1f, 0.95f, 0.78f, 1f);
        static readonly Color BadgerGold = new Color(0.98f, 0.74f, 0.18f, 1f);

        // ---- entry and exit ----

        // False while the kill switch is off. Phase 4/5 will call this from the flower/flag flow.
        bool OpenBadgerFight(int cleared)
        {
            if (!BadgerSchedule.Enabled) return false;
            int n = BadgerSchedule.Appearance(cleared);
            if (n < 1) n = 1;
            _bgAppearance = n;
            _bgSeed = Random.Range(1, int.MaxValue);
            _bgLoadout = BadgerLoadout.Preload(BadgerSchedule.Tiles);
            _bgFight = null;
            _bgStage = BadgerStage.Loadout;
            _bgT = 0f;
            _bgDotT = 0f;
            _bgBossIx = -1;
            _bgPlayerIx = -1;
            _bgArmed = BadgerPower.None;
            _bgResolved = false;
            _bgShownPlayer = 0;
            _bgShownBoss = 0;
            _bgPickSlot = -1;
            _bgShakeKey = -1;
            _bgShakeT = 0f;
            _bgLine = "Tap a tile to swap its bee. Then hit FIGHT.";
            _bgNeedLine = "You need " + BadgerSchedule.PlayerTarget(n) + "    Badger needs " + BadgerSchedule.BadgerTarget(n);
            for (int p = 1; p < _bgPrice.Length; p++)
                _bgPrice[p] = Money.Format(BadgerSchedule.PowerUpPrice((BadgerPower)p, n));
            _splash = true;
            _home = HomeFace.Badger;
            return true;
        }

        // TODO(Phase 5): a win clears BadgerSave, pays the cleared level's reward and one
        // Hive.TakeVisitor(); a loss keeps the flag. Phase 3 only returns to the splash.
        void LeaveBadger()
        {
            _home = HomeFace.Splash;
            _bgFight = null;
            _bgLoadout = null;
            _bgStage = BadgerStage.Loadout;
#if UNITY_EDITOR
            if (_bgSwitchHeld)
            {
                BadgerSchedule.Enabled = _bgSwitchWas;
                _bgSwitchHeld = false;
            }
#endif
        }

#if UNITY_EDITOR
        // Drop a file on /tmp/flock-five-badger-fight (optional cleared level inside, default 15).
        // The switch is held on only until the screen is left.
        void EditorOpenBadgerFight()
        {
            const string path = "/tmp/flock-five-badger-fight";
            int level = 15;
            try { int.TryParse(System.IO.File.ReadAllText(path).Trim(), out level); }
            catch { }
            try { System.IO.File.Delete(path); }
            catch { }
            if (level < BadgerSchedule.FirstAfter) level = BadgerSchedule.FirstAfter;
            if (!_bgSwitchHeld)
            {
                _bgSwitchWas = BadgerSchedule.Enabled;
                _bgSwitchHeld = true;
            }
            BadgerSchedule.Enabled = true;
            OpenBadgerFight(level);
        }
#endif

        // ---- round flow ----

        void BeginBadgerFight()
        {
            _bgFight = new BadgerFight(_bgAppearance, _bgSeed, _bgLoadout.Honeys(),
                BadgerSchedule.BossTileMix(_bgAppearance, _bgSeed));
            _bgShownPlayer = 0;
            _bgShownBoss = 0;
            NextBadgerRound();
            Sfx.CardBump();
        }

        void NextBadgerRound()
        {
            _bgBossIx = -1;
            _bgPlayerIx = -1;
            _bgArmed = BadgerPower.None;
            _bgResolved = false;
            _bgT = 0f;
            _bgStage = BadgerStage.BossWait;
            _bgLine = "Honey badger goes first.";
        }

        void TickBadger(float dt)
        {
            if (dt > 0.1f) dt = 0.1f;
            if (_bgShakeT > 0f) _bgShakeT = Mathf.Max(0f, _bgShakeT - dt);
            _bgT += dt;
            if (_bgFight == null) return;
            switch (_bgStage)
            {
                case BadgerStage.BossWait:
                    if (_bgT < BadgerBossWait) break;
                    int ix = _bgFight.BossPick();
                    if (ix < 0)
                    {
                        FinishBadger();
                        break;
                    }
                    _bgBossIx = ix;
                    _bgBossAt = Time.unscaledTime;
                    _bgT = 0f;
                    _bgStage = BadgerStage.YourPick;
                    _bgLine = "Your pick. Tap a power-up first if you want one.";
                    Sfx.CardBump();
                    break;
                case BadgerStage.Reveal:
                    if (_bgT < BadgerSlide + BadgerRevealHold) break;
                    if (!_bgFight.Resolve(out _bgRound))
                    {
                        NextBadgerRound();
                        break;
                    }
                    _bgResolved = true;
                    _bgT = 0f;
                    _bgDotT = 0f;
                    _bgStage = BadgerStage.Verdict;
                    _bgLine = BadgerVerdict(_bgRound);
                    if (_bgRound.PlayerGained > 0 || _bgRound.BossGained > 0 || BadgerFight.IsBlock(_bgRound.Power))
                        Sfx.CardBump();
                    break;
                case BadgerStage.Verdict:
                    StepBadgerDots(dt);
                    if (_bgT < BadgerVerdictHold || BadgerDotsBusy()) break;
                    if (_bgFight.Result != BadgerResult.Playing) FinishBadger();
                    else NextBadgerRound();
                    break;
            }
        }

        bool BadgerDotsBusy()
        {
            return _bgShownPlayer < Mathf.Min(_bgFight.PlayerScore, _bgFight.PlayerTarget)
                || _bgShownBoss < Mathf.Min(_bgFight.BossScore, _bgFight.BadgerTarget);
        }

        void StepBadgerDots(float dt)
        {
            if (!BadgerDotsBusy()) return;
            _bgDotT += dt;
            if (_bgDotT < BadgerDotStep) return;
            _bgDotT = 0f;
            if (_bgShownPlayer < Mathf.Min(_bgFight.PlayerScore, _bgFight.PlayerTarget)) _bgShownPlayer++;
            else _bgShownBoss++;
            SfxLibrary.Play("tick", 0.30f);
        }

        void FinishBadger()
        {
            bool won = _bgFight != null && _bgFight.Result == BadgerResult.PlayerWon;
            _bgStage = BadgerStage.Over;
            _bgT = 0f;
            _bgShownPlayer = _bgFight != null ? Mathf.Min(_bgFight.PlayerScore, _bgFight.PlayerTarget) : 0;
            _bgShownBoss = _bgFight != null ? Mathf.Min(_bgFight.BossScore, _bgFight.BadgerTarget) : 0;
            _bgLine = won ? "You win! The badger grumbles off." : "The badger wins this one. Try again.";
            if (won) SfxLibrary.Play("fanfare", 0.40f);
            else Sfx.Deny();
        }

        static string BadgerVerdict(BadgerRound r)
        {
            if (BadgerFight.IsBlock(r.Power))
            {
                string name = r.Power == BadgerPower.HotSauce ? "Hot sauce!" : "Pepper!";
                return name + " The badger gets nothing. You take " + r.PlayerGained + ".";
            }
            if (r.PlayerGained > 0)
                return r.Power == BadgerPower.None ? "You take " + r.PlayerGained + "." : "Boosted! You take " + r.PlayerGained + ".";
            if (r.BossGained > 0) return "Badger takes " + r.BossGained + ".";
            return "Tie. Nobody scores.";
        }

        static string BadgerPowerName(BadgerPower p)
        {
            if (p == BadgerPower.X2) return "x2";
            if (p == BadgerPower.X3) return "x3";
            if (p == BadgerPower.HotSauce) return "Hot Sauce";
            return "Pepper";
        }

        void BadgerShakeNow(int key)
        {
            _bgShakeKey = key;
            _bgShakeT = BadgerShake;
            Sfx.Deny();
        }

        float BadgerShakeX(int key, float s)
        {
            if (_bgShakeT <= 0f || _bgShakeKey != key) return 0f;
            return Mathf.Sin(_bgShakeT * 60f) * 8f * s * (_bgShakeT / BadgerShake);
        }

        void ClickBadgerPower(BadgerPower p)
        {
            if (_bgStage != BadgerStage.YourPick || _bgFight == null) return;
            if (_bgArmed == p)
            {
                _bgArmed = BadgerPower.None;
                _bgLine = "Your pick. Tap a power-up first if you want one.";
                return;
            }
            if (!_bgFight.PowerReady(p)) return;
            if (!_bgFight.CanAfford(p))
            {
                BadgerShakeNow(BadgerPowerKey + (int)p);
                _bgLine = "Not enough coins for that one.";
                return;
            }
            _bgArmed = p;
            Sfx.CardTap();
            _bgLine = BadgerFight.IsBlock(p)
                ? BadgerPowerName(p) + " is ready. Now pick a tile."
                : BadgerPowerName(p) + " is ready. Now pick the tile to boost.";
        }

        void ClickBadgerTile(int i)
        {
            if (_bgStage == BadgerStage.Loadout)
            {
                _bgPickSlot = i;
                _bgPickPage = 0;
                _bgLoadout.Options(i, _bgOptions);
                _bgStage = BadgerStage.Picker;
                Sfx.CardTap();
                return;
            }
            if (_bgStage != BadgerStage.YourPick || _bgFight == null) return;
            if (!_bgFight.PlayerOpen(i)) return;
            if (!_bgFight.PlayerPlay(i, _bgArmed))
            {
                BadgerShakeNow(i);
                _bgLine = "Not enough coins for that one.";
                return;
            }
            _bgPlayerIx = i;
            _bgPlayerAt = Time.unscaledTime;
            _bgT = 0f;
            _bgStage = BadgerStage.Reveal;
            _bgLine = "";
            if (_bgArmed != BadgerPower.None) Sfx.Clink();
            Sfx.CardBump();
        }

        // ---- layout ----

        struct BadgerRects
        {
            public float S;
            public Rect Back, Title, Need, Coin;
            public Rect BossGrid, Arena, PlayerGrid, Power, Caption;
            public Rect ColL, ColR;
            public float Unit;
        }

        BadgerRects BadgerLayout()
        {
            var L = new BadgerRects();
            float s = Mathf.Max(Screen.height / 720f, 1f);
            L.S = s;
            float top = TopHud();
            L.Back = BackMedalRect(s, top);
            var safe = Screen.safeArea;
            bool haveSafe = safe.width >= 2f && safe.height >= 2f;
            float xL = Mathf.Max(8f * s, haveSafe ? safe.xMin + 6f : 0f);
            float xR = Mathf.Min(Screen.width - 8f * s, haveSafe ? safe.xMax - 6f : Screen.width);
            float colW = Mathf.Max(14f, 15f * s);
            float icon = 40f * Mathf.Clamp(s, 1f, 1.35f);
            L.Coin = new Rect(xR - icon, top + 4f * s, icon, icon);
            float tx = L.Back.xMax + 8f * s;
            float tw = Mathf.Max(80f, Screen.width * 0.62f - tx);
            L.Title = new Rect(tx, top, tw, L.Back.height * 0.52f);
            L.Need = new Rect(tx, L.Title.yMax, tw, L.Back.height * 0.34f);

            float y0 = L.Back.yMax + 4f * s;
            float yEnd = Screen.height - Mathf.Max(8f * s, haveSafe ? safe.yMin + 4f : 0f);
            float gap = 6f * s;
            float room = Mathf.Max(100f, yEnd - y0 - gap * 4f);
            float capH = room * 0.14f;
            float powH = room * 0.11f;
            float arenaH = room * 0.16f;
            float gridH = (room - capH - powH - arenaH) * 0.5f;
            float gx = xL + colW + 10f * s;
            float gw = Mathf.Max(60f, xR - colW - 10f * s - gx);
            float y = y0;
            L.BossGrid = new Rect(gx, y, gw, gridH);
            y += gridH + gap;
            L.Arena = new Rect(gx, y, gw, arenaH);
            y += arenaH + gap;
            L.PlayerGrid = new Rect(gx, y, gw, gridH);
            y += gridH + gap;
            L.Power = new Rect(xL, y, xR - xL, powH);
            y += powH + gap;
            L.Caption = new Rect(xL, y, xR - xL, capH);
            float colH = L.PlayerGrid.yMax - L.BossGrid.y;
            L.ColL = new Rect(xL, L.BossGrid.y, colW, colH);
            L.ColR = new Rect(xR - colW, L.BossGrid.y, colW, colH);
            L.Unit = colH / BadgerSchedule.BadgerTargetFixed;
            return L;
        }

        // Pointy-top comb, odd rows shifted half a cell. Hit boxes tile with no overlap.
        struct BadgerHex
        {
            public float Cw, Ch, X0, Y0;
            public int Cols;

            public static BadgerHex Fit(Rect area, int cols, int rows)
            {
                float stack = 1f + (rows - 1) * 0.75f;
                float cw = area.width / (cols + 0.5f);
                float ch = cw * 1.1547f;
                if (ch * stack > area.height) ch = area.height / stack;
                cw = Mathf.Min(cw, ch * 0.95f);
                var hex = new BadgerHex { Cw = cw, Ch = ch, Cols = cols };
                hex.X0 = area.x + (area.width - (cols * cw + cw * 0.5f)) * 0.5f;
                hex.Y0 = area.y + (area.height - ch * stack) * 0.5f;
                return hex;
            }

            public Vector2 Center(int i)
            {
                int r = i / Cols;
                int c = i % Cols;
                float off = (r & 1) == 1 ? Cw * 0.5f : 0f;
                return new Vector2(X0 + off + Cw * (c + 0.5f), Y0 + Ch * 0.5f + r * Ch * 0.75f);
            }

            public Rect Draw(int i)
            {
                var c = Center(i);
                float w = Cw * 0.94f;
                float h = Ch * 0.94f;
                return new Rect(c.x - w * 0.5f, c.y - h * 0.5f, w, h);
            }

            public Rect Hit(int i)
            {
                var c = Center(i);
                return new Rect(c.x - Cw * 0.5f, c.y - Ch * 0.375f, Cw, Ch * 0.75f);
            }
        }

        // ---- shared drawing ----

        static GUIStyle BadgerStyle()
        {
            if (_bgStyle != null) return _bgStyle;
            _bgStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false,
                clipping = TextClipping.Clip
            };
            return _bgStyle;
        }

        static GUIStyle BadgerWrapStyle()
        {
            if (_bgWrap != null) return _bgWrap;
            _bgWrap = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                clipping = TextClipping.Clip
            };
            return _bgWrap;
        }

        // White pointy-top hexagon with a soft edge. Tinted by GUI.color, stretched to any cell.
        static Texture2D HoneycombTex()
        {
            if (_bgHex != null) return _bgHex;
            const int W = 96;
            const int H = 111;
            var px = new Color32[W * H];
            float a = W * 0.5f;
            float r = H * 0.5f;
            float k = r / (2f * a);
            float norm = Mathf.Sqrt(1f + k * k);
            for (int iy = 0; iy < H; iy++)
            {
                for (int ix = 0; ix < W; ix++)
                {
                    float x = Mathf.Abs(ix + 0.5f - a);
                    float y = Mathf.Abs(iy + 0.5f - r);
                    float d = Mathf.Min(a - x, (r - k * x - y) / norm);
                    float al = Mathf.Clamp01(d + 0.5f);
                    px[iy * W + ix] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(al * 255f));
                }
            }
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.SetPixels32(px);
            tex.Apply(false, true);
            _bgHex = tex;
            return tex;
        }

        // The one honeycomb tile: grids, arena, and picker. Plate colors are the album's.
        // No pizza art, no foil sheen, no swarm. A foil or inverse bee gets a colored rim.
        static void DrawBadgerTile(Rect hex, BadgerLook look, int honey, Color tint, BeeFinish finish, float alpha, string label)
        {
            if (!GuiPaint() || alpha < 0.02f || hex.width < 4f) return;
            var tex = HoneycombTex();
            if (look == BadgerLook.Spent)
            {
                GUI.color = new Color(0.10f, 0.07f, 0.04f, 0.28f * alpha);
                GUI.DrawTexture(hex, tex, ScaleMode.StretchToFill, true);
                GUI.color = Color.white;
                return;
            }
            float lift = hex.height * 0.045f;
            GUI.color = new Color(0f, 0f, 0f, 0.35f * alpha);
            GUI.DrawTexture(new Rect(hex.x + lift * 0.4f, hex.y + lift, hex.width, hex.height), tex, ScaleMode.StretchToFill, true);
            Color rim = finish == BeeFinish.Holo ? BadgerFoilRim
                : finish == BeeFinish.InverseRainbow ? BadgerInverseRim
                : AlbumWood(tint, true);
            rim.a = alpha;
            GUI.color = rim;
            GUI.DrawTexture(hex, tex, ScaleMode.StretchToFill, true);
            var inner = Inset(hex, 0.07f);
            Color face = look == BadgerLook.Down ? new Color(0.36f, 0.25f, 0.10f, 1f) : AlbumFace(tint, true);
            face.a = alpha;
            GUI.color = face;
            GUI.DrawTexture(inner, tex, ScaleMode.StretchToFill, true);
            GUI.color = Color.white;
            bool named = !string.IsNullOrEmpty(label);
            var digit = named
                ? new Rect(inner.x, inner.y + inner.height * 0.08f, inner.width, inner.height * 0.56f)
                : new Rect(inner.x + inner.width * 0.10f, inner.y + inner.height * 0.12f, inner.width * 0.80f, inner.height * 0.76f);
            if (look == BadgerLook.Down) CardText.DrawHoneyDigit(digit, "?", alpha * 0.55f);
            else CardText.DrawHoneyDigit(digit, honey, alpha);
            if (!named) return;
            var st = BadgerStyle();
            var lr = new Rect(inner.x + inner.width * 0.12f, inner.y + inner.height * 0.66f, inner.width * 0.76f, inner.height * 0.20f);
            st.fontSize = FitFont(st, label, lr.width, lr.height, 8, 30);
            Paint(st, new Color(0.20f, 0.10f, 0.04f, alpha));
            GUI.Label(lr, label, st);
        }

        // The one button: FIGHT, Continue, power-ups, picker controls. Hit pad is the whole rect;
        // `live` false skips the hit entirely so a modal in front owns the finger.
        bool BadgerButton(Rect hit, string title, string sub, bool gold, bool dim, bool lit, bool live, Color ink, float shakeX)
        {
            hit.x += shakeX;
            bool held = false;
            bool fire = live && HitPad(hit, out held);
            if (!GuiPaint()) return fire;
            if (lit)
            {
                float pad = hit.height * 0.18f;
                GUI.color = new Color(1f, 0.88f, 0.36f, 0.75f);
                GUI.DrawTexture(new Rect(hit.x - pad, hit.y - pad, hit.width + pad * 2f, hit.height + pad * 2f), GlowTex(), ScaleMode.StretchToFill, true);
                GUI.color = Color.white;
            }
            var lab = DrawRoundedFace(hit, held, gold && !dim, true);
            var st = BadgerStyle();
            float a = dim ? 0.55f : 1f;
            bool two = !string.IsNullOrEmpty(sub);
            var tr = two ? new Rect(lab.x, lab.y, lab.width, lab.height * 0.58f) : lab;
            st.fontSize = FitFont(st, title, tr.width * 0.92f, tr.height * 0.92f, 10, 72);
            StampLight(tr, title, st, new Color(ink.r, ink.g, ink.b, a));
            if (two)
            {
                var sr = new Rect(lab.x, lab.y + lab.height * 0.52f, lab.width, lab.height * 0.46f);
                st.fontSize = FitFont(st, sub, sr.width * 0.92f, sr.height * 0.92f, 9, 56);
                StampLight(sr, sub, st, new Color(BadgerCream.r, BadgerCream.g, BadgerCream.b, a));
            }
            return fire;
        }

        // A score column: one segment per point still needed, filled from the bottom in honey gold.
        // Both columns share one segment height, so the shorter target is a shorter column.
        static void DrawBadgerColumn(Rect col, int target, int filled, float unit)
        {
            if (!GuiPaint() || target < 1) return;
            float gap = Mathf.Max(1f, unit * 0.10f);
            for (int i = 0; i < target; i++)
            {
                var seg = new Rect(col.x, col.yMax - (i + 1) * unit + gap * 0.5f, col.width, Mathf.Max(2f, unit - gap));
                GUI.color = i < filled ? BadgerGold : new Color(0.10f, 0.07f, 0.04f, 0.55f);
                GUI.DrawTexture(seg, Texture2D.whiteTexture);
                if (i >= filled)
                {
                    GUI.color = new Color(0.62f, 0.46f, 0.22f, 0.65f);
                    GUI.DrawTexture(new Rect(seg.x, seg.y, seg.width, 1.5f), Texture2D.whiteTexture);
                }
            }
            GUI.color = Color.white;
        }

        static Color BadgerTint(BadgerTile t)
        {
            if (t.Yard || (uint)t.Kind >= (uint)Hive.Kinds) return BadgerWax;
            return Hive.Roster[t.Kind].Tint;
        }

        // ---- the page ----

        void DrawBadgerPage()
        {
            if (_bgLoadout == null)
            {
                _home = HomeFace.Splash;
                return;
            }
            if (Event.current.type == EventType.Layout) TickBadger(Time.unscaledDeltaTime);
            var L = BadgerLayout();
            float s = L.S;
            DrawHomeWash(0.58f);

            bool picker = _bgStage == BadgerStage.Picker;
            if (picker) PaintBackMedal(L.Back, false);
            else if (DrawBackMedal(L.Back))
            {
                LeaveBadger();
                return;
            }

            if (GuiPaint())
            {
                var st = BadgerStyle();
                st.fontSize = FitFont(st, "HONEY BADGER", L.Title.width, L.Title.height, 12, 80);
                StampOutlined(L.Title, "HONEY BADGER", st, BadgerGold, 0, 2);
                st.fontSize = FitFont(st, _bgNeedLine, L.Need.width, L.Need.height, 10, 40);
                StampLight(L.Need, _bgNeedLine, st, BadgerCream);
            }
            DrawCoinCluster(L.Coin, s, 0.86f);

            DrawBadgerColumn(L.ColL, BadgerSchedule.PlayerTarget(_bgAppearance), _bgShownPlayer, L.Unit);
            DrawBadgerColumn(L.ColR, BadgerSchedule.BadgerTarget(_bgAppearance), _bgShownBoss, L.Unit);

            var bossHex = BadgerHex.Fit(L.BossGrid, 4, 4);
            var youHex = BadgerHex.Fit(L.PlayerGrid, 4, 4);
            bool tilesLive = !picker && (_bgStage == BadgerStage.Loadout || _bgStage == BadgerStage.YourPick);

            for (int i = 0; i < BadgerSchedule.Tiles; i++)
            {
                bool open = _bgFight == null || _bgFight.BossOpen(i);
                DrawBadgerTile(bossHex.Draw(i), open ? BadgerLook.Down : BadgerLook.Spent, 0, BadgerHide, BeeFinish.Normal, 1f, null);
            }

            int tapped = -1;
            for (int i = 0; i < BadgerSchedule.Tiles; i++)
            {
                var t = _bgLoadout[i];
                bool open = _bgFight == null || _bgFight.PlayerOpen(i);
                var draw = youHex.Draw(i);
                bool held = false;
                if (tilesLive && open && HitPad(youHex.Hit(i), out held)) tapped = i;
                if (held) draw = Inset(draw, 0.03f);
                draw.x += BadgerShakeX(i, s);
                DrawBadgerTile(draw, open ? BadgerLook.Face : BadgerLook.Spent, t.Honey, BadgerTint(t), t.Finish, 1f, null);
            }

            DrawBadgerArena(L, bossHex, youHex);
            DrawBadgerPowers(L, !picker);
            DrawBadgerCaption(L);
            if (tapped >= 0) ClickBadgerTile(tapped);

            if (picker) DrawBadgerPicker(L);
        }

        void DrawBadgerArena(BadgerRects L, BadgerHex bossHex, BadgerHex youHex)
        {
            float s = L.S;
            bool picker = _bgStage == BadgerStage.Picker;
            float size = Mathf.Min(L.Arena.height * 0.98f, L.Arena.width * 0.30f);
            float w = size * 0.87f;
            var youSlot = new Rect(L.Arena.center.x - size * 0.95f - w * 0.5f, L.Arena.center.y - size * 0.5f, w, size);
            var bossSlot = new Rect(L.Arena.center.x + size * 0.95f - w * 0.5f, L.Arena.center.y - size * 0.5f, w, size);

            if (GuiPaint())
            {
                var st = BadgerStyle();
                string mid = _bgResolved && _bgRound.PlayerGained == 0 && _bgRound.BossGained == 0 ? "TIE" : "VS";
                var mr = new Rect(L.Arena.center.x - size * 0.5f, L.Arena.center.y - size * 0.3f, size, size * 0.6f);
                st.fontSize = FitFont(st, mid, mr.width, mr.height, 10, 64);
                StampLight(mr, mid, st, BadgerCream);
            }

            if (_bgFight != null && _bgBossIx >= 0)
            {
                float u = Mathf.SmoothStep(0f, 1f, (Time.unscaledTime - _bgBossAt) / BadgerSlide);
                bool block = _bgResolved && BadgerFight.IsBlock(_bgRound.Power);
                int honey = _bgResolved ? _bgRound.BossFinal : _bgFight.BossHoney(_bgBossIx);
                var r = LerpRect(bossHex.Draw(_bgBossIx), bossSlot, u);
                if (block) r.x += Mathf.Sin(Time.unscaledTime * 38f) * 5f * s * Mathf.Clamp01(1.2f - _bgT);
                bool win = _bgResolved && _bgRound.BossGained > 0;
                if (win) DrawBadgerWinGlow(r);
                DrawBadgerTile(r, BadgerLook.Face, honey, BadgerHide, BeeFinish.Normal, block ? 0.55f : 1f, null);
                if (block) DrawBadgerSplat(r);
            }
            if (_bgFight != null && _bgPlayerIx >= 0)
            {
                float u = Mathf.SmoothStep(0f, 1f, (Time.unscaledTime - _bgPlayerAt) / BadgerSlide);
                var t = _bgLoadout[_bgPlayerIx];
                int honey = _bgResolved ? _bgRound.PlayerFinal : _bgFight.PlayerHoney(_bgPlayerIx);
                var r = LerpRect(youHex.Draw(_bgPlayerIx), youSlot, u);
                if (_bgResolved && _bgRound.PlayerGained > 0) DrawBadgerWinGlow(r);
                DrawBadgerTile(r, BadgerLook.Face, honey, BadgerTint(t), t.Finish, 1f, null);
                if (_bgArmed != BadgerPower.None && GuiPaint())
                {
                    var badge = new Rect(r.center.x - r.width * 0.45f, r.y - r.height * 0.10f, r.width * 0.9f, r.height * 0.26f);
                    DrawSolidRound(badge, badge.height * 0.5f, 0, 0.95f);
                    var st = BadgerStyle();
                    string name = BadgerPowerName(_bgArmed);
                    st.fontSize = FitFont(st, name, badge.width * 0.86f, badge.height * 0.80f, 8, 40);
                    StampLight(badge, name, st, BadgerGold);
                }
            }

            bool big = _bgStage == BadgerStage.Loadout || _bgStage == BadgerStage.Over;
            if (!big) return;
            var btn = new Rect(L.Arena.center.x - Mathf.Min(L.Arena.width * 0.34f, 300f * s),
                L.Arena.y + L.Arena.height * 0.08f,
                Mathf.Min(L.Arena.width * 0.68f, 600f * s), L.Arena.height * 0.84f);
            if (_bgStage == BadgerStage.Over)
            {
                bool fire = BadgerButton(btn, "Continue", null, true, false, false, !picker, Color.white, 0f);
                if (fire)
                {
                    // TODO(Phase 5): win -> BadgerSave.Clear(), pay the cleared level's reward,
                    // Hive.TakeVisitor(). Loss -> flag stays. Phase 3 just goes home.
                    LeaveBadger();
                }
                return;
            }
            if (BadgerButton(btn, "FIGHT!", null, true, false, false, !picker, Color.white, 0f))
                BeginBadgerFight();
        }

        void DrawBadgerWinGlow(Rect r)
        {
            if (!GuiPaint()) return;
            float pad = r.width * 0.28f;
            GUI.color = new Color(1f, 0.86f, 0.30f, 0.65f);
            GUI.DrawTexture(new Rect(r.x - pad, r.y - pad, r.width + pad * 2f, r.height + pad * 2f), GlowTex(), ScaleMode.StretchToFill, true);
            GUI.color = Color.white;
        }

        // Honey splat over a knocked tile: a gold blob and three drips. Comic, not injury.
        static void DrawBadgerSplat(Rect r)
        {
            if (!GuiPaint()) return;
            var glow = GlowTex();
            GUI.color = new Color(1f, 0.74f, 0.10f, 0.92f);
            float big = r.width * 0.95f;
            GUI.DrawTexture(new Rect(r.center.x - big * 0.5f, r.center.y - big * 0.5f, big, big), glow, ScaleMode.StretchToFill, true);
            for (int i = 0; i < 3; i++)
            {
                float d = r.width * (0.26f - i * 0.04f);
                float dx = (i - 1) * r.width * 0.30f;
                float dy = r.height * (0.34f + 0.06f * i);
                GUI.DrawTexture(new Rect(r.center.x + dx - d * 0.5f, r.center.y + dy - d * 0.5f, d, d), glow, ScaleMode.StretchToFill, true);
            }
            GUI.color = Color.white;
        }

        void DrawBadgerPowers(BadgerRects L, bool live)
        {
            float s = L.S;
            float gap = 8f * s;
            float w = (L.Power.width - gap * 3f) / 4f;
            for (int k = 0; k < 4; k++)
            {
                var p = (BadgerPower)(k + 1);
                var r = new Rect(L.Power.x + k * (w + gap), L.Power.y, w, L.Power.height);
                bool used = _bgFight != null && !_bgFight.PowerReady(p);
                bool inPick = _bgStage == BadgerStage.YourPick;
                bool lit = _bgArmed == p && (inPick || _bgStage == BadgerStage.Reveal || _bgStage == BadgerStage.Verdict);
                string sub = used ? "USED" : _bgPrice[(int)p];
                Color ink = p == BadgerPower.HotSauce ? new Color(1f, 0.55f, 0.35f)
                    : p == BadgerPower.Pepper ? new Color(0.70f, 0.95f, 0.45f)
                    : Color.white;
                bool fire = BadgerButton(r, BadgerPowerName(p), sub, k < 2, used || !inPick, lit,
                    live && inPick && !used, ink, BadgerShakeX(BadgerPowerKey + (int)p, s));
                if (fire) ClickBadgerPower(p);
            }
        }

        void DrawBadgerCaption(BadgerRects L)
        {
            if (!GuiPaint() || string.IsNullOrEmpty(_bgLine)) return;
            var panel = Inset(L.Caption, 0.03f);
            DrawSolidRound(panel, 18f, 0, 0.92f);
            var st = BadgerWrapStyle();
            var tr = new Rect(panel.x + panel.width * 0.04f, panel.y + panel.height * 0.08f, panel.width * 0.92f, panel.height * 0.84f);
            st.fontSize = FitFontWrapped(st, _bgLine, tr.width, tr.height, 10, 64);
            StampOutlined(tr, _bgLine, st, BadgerCream, 0, 2);
        }

        void DrawBadgerPicker(BadgerRects L)
        {
            float s = L.S;
            float pad = 10f * s;
            var panel = new Rect(L.Power.x, L.Arena.y, L.Power.width, L.Caption.yMax - L.Arena.y);
            if (GuiPaint())
            {
                DrawSolidRound(panel, 22f, 0, 0.97f);
                var st = BadgerStyle();
                var head = new Rect(panel.x, panel.y + 4f * s, panel.width, panel.height * 0.08f);
                string title = _bgOptions.Count > 0 ? "Swap in a bee" : "No bees yet. Yard honey is ready.";
                st.fontSize = FitFont(st, title, head.width * 0.9f, head.height, 10, 60);
                StampOutlined(head, title, st, BadgerCream, 0, 2);
            }
            float footH = panel.height * 0.14f;
            var foot = new Rect(panel.x + pad, panel.yMax - footH - pad, panel.width - pad * 2f, footH);
            var area = new Rect(panel.x + pad, panel.y + panel.height * 0.09f, panel.width - pad * 2f, foot.y - pad - (panel.y + panel.height * 0.09f));
            var hex = BadgerHex.Fit(area, 4, 3);
            int pages = Mathf.Max(1, (_bgOptions.Count + BadgerPickPage - 1) / BadgerPickPage);
            _bgPickPage = Mathf.Clamp(_bgPickPage, 0, pages - 1);
            var current = _bgLoadout[_bgPickSlot];
            int chosen = -1;
            for (int j = 0; j < BadgerPickPage; j++)
            {
                int idx = _bgPickPage * BadgerPickPage + j;
                if (idx >= _bgOptions.Count) break;
                var tile = _bgOptions[idx];
                var draw = hex.Draw(j);
                if (tile.Same(current)) DrawBadgerWinGlow(draw);
                bool held;
                if (HitPad(hex.Hit(j), out held)) chosen = idx;
                if (held) draw = Inset(draw, 0.03f);
                DrawBadgerTile(draw, BadgerLook.Face, tile.Honey, BadgerTint(tile), tile.Finish, 1f, Hive.Roster[tile.Kind].Name);
            }

            float gap = 8f * s;
            float bw = (foot.width - gap * 3f) / 4f;
            bool prev = BadgerButton(new Rect(foot.x, foot.y, bw, foot.height), "<", null, false, _bgPickPage <= 0, false, true, Color.white, 0f);
            bool yard = BadgerButton(new Rect(foot.x + (bw + gap), foot.y, bw, foot.height), "Yard", "honey 1", false, false, false, true, BadgerWax, 0f);
            bool next = BadgerButton(new Rect(foot.x + (bw + gap) * 2f, foot.y, bw, foot.height), ">", null, false, _bgPickPage >= pages - 1, false, true, Color.white, 0f);
            bool close = BadgerButton(new Rect(foot.x + (bw + gap) * 3f, foot.y, bw, foot.height), "Close", null, true, false, false, true, Color.white, 0f);

            if (chosen >= 0)
            {
                _bgLoadout.Set(_bgPickSlot, _bgOptions[chosen]);
                Sfx.CardBump();
                _bgStage = BadgerStage.Loadout;
            }
            else if (yard)
            {
                _bgLoadout.Set(_bgPickSlot, BadgerTile.YardTile());
                Sfx.CardBump();
                _bgStage = BadgerStage.Loadout;
            }
            else if (close)
            {
                _bgStage = BadgerStage.Loadout;
            }
            else if (prev && _bgPickPage > 0) _bgPickPage--;
            else if (next && _bgPickPage < pages - 1) _bgPickPage++;
        }
    }
}
