using System;
using UnityEngine;

namespace FlockFive
{
    // Bird Care v1 on the home splash: the adopted bird has one stat (hunger), a nectar meter,
    // a free Feed button, a warm "snack time?" bubble, a nap pose when low, and a happy feeding
    // moment. Rules live in BirdCare (pure, unit-tested). Behind BirdCareFlag (remote, default off).
    public sealed partial class FlockFiveApp
    {
        const string CareHungerPref = "flockfive.care.hunger";
        const string CareLastPref = "flockfive.care.last";
        const string CareFirstPref = "flockfive.care.firstFed";
        const string CareFeedAtPref = "flockfive.care.feedAt";
        const string CareAdTakenPref = "flockfive.care.adTaken";
        const string CareNectarPref = "flockfive.care.nectar";
        const string CareTutorPref = "flockfive.care.tutor";
        const string CareBornPref = "flockfive.care.born";

        // Two lines max (global glove rule). The name is the player's own (12 chars max).
        public const string CareTutorLine2 = "Tap Feed to share nectar.";
        public static string CareTutorLine(string name) => "Snack time for " + (string.IsNullOrEmpty(name) ? "your bird" : name) + "!\n" + CareTutorLine2;
        public const string CareHungryBubble = "Snack time?";
        public const string CareYumLine = "Yum! Thank you!";

        BirdCare.State _care;
        bool _careLoaded;
        int _careNectar;
        float _careFeedT = -1f;
        BirdCare.FeedResult _careLastFeed;
        Rect _careFeedR;
        Rect _careMeterR;
        bool _careTutorLive;
        static GUIStyle _careWord;
        static Texture2D _careDot;

        // One iPhone point (390x844 logical). Care art is sized in points, so it reads the same on every phone.
        static float CarePt() => Mathf.Max(1f, Mathf.Min(Screen.width / 390f, Screen.height / 844f));

        static long CareNow() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // The whole feature is off unless the remote flag says otherwise.
        bool CareOn()
        {
            if (!BirdCareFlag.Enabled) return false;
            return AvatarAdopted();
        }

        bool CareHomeIdle()
        {
            if (!_splash || _home != HomeFace.Splash) return false;
            if (_adoptLive || _adoptStep != AdoptStep.Off || _avatarRename || _settingsOpen) return false;
            if (VipOffer.IsOpen || _dailyOpen || _dailyAskOpen || _welcomeOpen) return false;
            return OpenModal() == ModalBlock.Id.None;
        }

        void LoadCare()
        {
            if (_careLoaded) return;
            _careLoaded = true;
            DailyReminder.CareEntries = CareNotifyEntries;
            long now = CareNow();
            if (PlayerPrefs.GetInt(CareBornPref, 0) == 0)
            {
                // A new bird arrives a little peckish, so the first (free) feed means something.
                _care = BirdCare.State.NewBird(now);
                _care.Hunger = 40.0;
                _careNectar = 0;
                SaveCare();
                PlayerPrefs.SetInt(CareBornPref, 1);
                PlayerPrefs.Save();
                return;
            }
            _care.Hunger = PlayerPrefs.GetFloat(CareHungerPref, (float)BirdCare.Max);
            long.TryParse(PlayerPrefs.GetString(CareLastPref, "0"), out _care.LastUnix);
            long.TryParse(PlayerPrefs.GetString(CareFeedAtPref, "0"), out _care.LastFeedUnix);
            _care.FirstFeedUsed = PlayerPrefs.GetInt(CareFirstPref, 0) != 0;
            _care.AdBonusTaken = PlayerPrefs.GetInt(CareAdTakenPref, 1) != 0;
            _careNectar = Mathf.Clamp(PlayerPrefs.GetInt(CareNectarPref, 0), 0, BirdCare.NectarCap);
        }

        void SaveCare()
        {
            PlayerPrefs.SetFloat(CareHungerPref, (float)_care.Hunger);
            PlayerPrefs.SetString(CareLastPref, _care.LastUnix.ToString());
            PlayerPrefs.SetString(CareFeedAtPref, _care.LastFeedUnix.ToString());
            PlayerPrefs.SetInt(CareFirstPref, _care.FirstFeedUsed ? 1 : 0);
            PlayerPrefs.SetInt(CareAdTakenPref, _care.AdBonusTaken ? 1 : 0);
            PlayerPrefs.SetInt(CareNectarPref, _careNectar);
            PlayerPrefs.Save();
        }

        void TickCare()
        {
            if (!CareOn()) return;
            LoadCare();
            var clock = BirdCare.Advance(ref _care, CareNow());
            if (clock == BirdCare.Clock.Vacation)
            {
                BirdCareAnalytics.Emit(BirdCareAnalytics.Vacation);
                _avatarHappy = 1f;
                SaveCare();
            }
        }

        // Called next to Purse.AwardClear. Silent when the flag is off.
        void CareEarnNectar()
        {
            if (!CareOn()) return;
            LoadCare();
            _careNectar = BirdCare.EarnNectar(_careNectar);
            SaveCare();
        }

        const string CareNotifyYesPref = "flockfive.care.notifyYes";
        const string CareParentOkPref = "flockfive.care.parentOk";
        const string CareIgnoredPref = "flockfive.care.notifyIgnored";

        // Read by DailyReminder on background/quit. Empty unless flag + opt-in + audience allow.
        System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<DateTime, string>> CareNotifyEntries()
        {
            var outList = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<DateTime, string>>();
            if (!CareOn() || !_careLoaded) return outList;
            var st = BirdCareNotify.Defaults(BirdCareFlag.ChildDirected);
            st.FeatureOn = BirdCareFlag.NotifyFeatureOn;
            st.OptedIn = PlayerPrefs.GetInt(CareNotifyYesPref, 0) != 0;
            st.ParentAllowed = PlayerPrefs.GetInt(CareParentOkPref, 0) != 0;
            st.Cadence = BirdCareFlag.Cadence;
            st.Ignored = PlayerPrefs.GetInt(CareIgnoredPref, 0);
            long now = CareNow();
            var when = BirdCareNotify.Plan(_care, now, now, st, u => DateTimeOffset.FromUnixTimeSeconds(u).LocalDateTime);
            string name = SavedAvatarName();
            for (int i = 0; i < when.Count; i++)
                outList.Add(new System.Collections.Generic.KeyValuePair<DateTime, string>(when[i], BirdCareNotify.Line(name, i + (int)(now / 86400))));
            if (when.Count > 0)
            {
                PlayerPrefs.SetInt(CareIgnoredPref, st.Ignored + 1);
                BirdCareAnalytics.Emit(BirdCareAnalytics.NotifyScheduled, new System.Collections.Generic.Dictionary<string, string> { { "cadence", st.Cadence.ToString() }, { "count", when.Count.ToString() } });
            }
            return outList;
        }

        void NoteCareBackground()
        {
            if (!_careLoaded) return;
            BirdCare.Advance(ref _care, CareNow());
            SaveCare();
        }

        BirdCare.Mood CareMood() => BirdCare.MoodOf(_care.Hunger);

        // Nap pose: a soft head-tuck tilt (no sick or sad art). Added to the preen angle.
        float CareNapTilt(bool onPerch)
        {
            if (!onPerch || !CareOn() || !_careLoaded || _careFeedT >= 0f) return 0f;
            if (CareMood() != BirdCare.Mood.Nap) return 0f;
            float breathe = Mathf.Sin(AdPause.Unscaled * 1.4f) * 1.5f;
            return (_avatarFaceLeft ? -9f : 9f) + breathe;
        }

        // Tutorial: first time the player owns a bird with the feature on, at home after a clear.
        bool CareTutorDue()
        {
            if (!CareOn() || PlayerPrefs.GetInt(CareTutorPref, 0) != 0) return false;
            if (LevelData.NextPlay < 1 || !CareHomeIdle()) return false;
            if (_hiveIntro || _pokerIntro || _dailyIntro || _hiveIntroLive || _pokerIntroLive || _dailyIntroLive) return false;
            if (_welcomeGlove || _adHand || _streakSlide >= 0f) return false;
            return BirdCare.CanFeed(_care, _careNectar);
        }

        void FinishCareTutor()
        {
            _careTutorLive = false;
            PlayerPrefs.SetInt(CareTutorPref, 1);
            PlayerPrefs.Save();
            BirdCareAnalytics.Emit(BirdCareAnalytics.TutorialDone);
        }

        Rect CareFeedRect(float s)
        {
            float u = CarePt();
            float w = Mathf.Round(150f * u);
            float h = Mathf.Round(46f * u);
            var bird = _avatarDrawR;
            if (bird.width <= 2f) return default;
            // Opposite half from the bird, level with it, so the bird, its bubble and the plate stay clear.
            bool birdRight = bird.center.x >= Screen.width * 0.5f;
            float cx = birdRight ? Screen.width * 0.28f : Screen.width * 0.72f;
            var r = new Rect(cx - w * 0.5f, bird.center.y - h * 0.5f, w, h);
            float edge = 14f * u;
            r.x = Mathf.Clamp(r.x, edge, Screen.width - edge - w);
            var play = FlowerPlayRect();
            if (play.width > 2f && r.Overlaps(play)) r.y = play.y - h - 30f * u;
            return r;
        }

        // Runs with the other home hits, before the play flower, so a Feed tap never starts a level.
        void HitBirdCare(float s)
        {
            if (!CareOn() || !_careLoaded || !CareHomeIdle()) return;
            if (_careFeedR.width <= 2f) return;
            // Another home lesson owns the screen: the shared gate decides, we do not.
            if (!_careTutorLive && (HomeLessonUp() || ForcedLessonUp())) return;
            if (VeilBlocks(_careFeedR)) return;
            if (!HitHomeFirst(_careFeedR, out _)) return;
            CareFeed();
        }

        void CareFeed()
        {
            var r = BirdCare.Feed(ref _care, CareNow(), _careNectar);
            if (!r.Fed) return;
            _careNectar = Mathf.Max(0, _careNectar - r.NectarSpent);
            _careLastFeed = r;
            _careFeedT = 0f;
            _avatarHappy = 1f;
            Sfx.Chirp(_avatarCol);
            BirdCareAnalytics.LogFeed(r);
            // A feed means the player came back: reminders start fresh.
            PlayerPrefs.SetInt(CareIgnoredPref, 0);
            SaveCare();
            if (_careTutorLive)
            {
                NoteGloveTap();
                FinishCareTutor();
            }
        }

        static void EnsureCareArt()
        {
            if (_careWord == null)
            {
                _careWord = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = false,
                    clipping = TextClipping.Overflow,
                };
            }
            if (_careDot == null)
            {
                const int n = 64;
                _careDot = new Texture2D(n, n, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp };
                var px = new Color32[n * n];
                float c = (n - 1) * 0.5f;
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                        byte a = (byte)(Mathf.Clamp01(c - d + 0.5f) * 255f);
                        px[y * n + x] = new Color32(255, 255, 255, a);
                    }
                _careDot.SetPixels32(px);
                _careDot.Apply();
            }
        }

        static void CareDot(Vector2 c, float r, Color col)
        {
            var prev = GUI.color;
            GUI.color = col;
            GUI.DrawTexture(new Rect(c.x - r, c.y - r, r * 2f, r * 2f), _careDot);
            GUI.color = prev;
        }

        static void CareText(Rect r, string text, int size, Color col, Color shade)
        {
            _careWord.fontSize = size;
            var prev = GUI.color;
            GUI.color = shade;
            GUI.Label(new Rect(r.x, r.y + Mathf.Max(1f, size * 0.07f), r.width, r.height), text, _careWord);
            GUI.color = col;
            GUI.Label(r, text, _careWord);
            GUI.color = prev;
        }

        // A nectar drop pip: round bottom, soft point on top.
        static void CareDrop(Vector2 c, float r, float fill, float alpha)
        {
            var rim = new Color(0.36f, 0.20f, 0.10f, 0.90f * alpha);
            var empty = new Color(1f, 0.95f, 0.86f, 0.85f * alpha);
            var full = new Color(1f, 0.55f, 0.66f, alpha);
            CareDot(c, r * 1.12f, rim);
            CareDot(c + new Vector2(0f, -r * 0.62f), r * 0.62f, rim);
            CareDot(c, r, empty);
            CareDot(c + new Vector2(0f, -r * 0.58f), r * 0.50f, empty);
            if (fill > 0.02f)
            {
                CareDot(c, r * Mathf.Lerp(0.35f, 1f, fill), full);
                if (fill > 0.6f) CareDot(c + new Vector2(0f, -r * 0.58f), r * 0.46f, full);
                CareDot(c + new Vector2(-r * 0.35f, -r * 0.30f), r * 0.22f, new Color(1f, 1f, 1f, 0.75f * alpha));
            }
        }

        // Drawn after DrawHomeAvatar.
        void DrawBirdCare(float s)
        {
            _careFeedR = default;
            _careMeterR = default;
            if (!CareOn())
            {
                if (_careTutorLive) _careTutorLive = false;
                return;
            }
            TickCare();
            if (_avatarDrawR.width <= 2f || !CareHomeIdle()) return;
            EnsureCareArt();
            var mood = CareMood();
            var bird = _avatarDrawR;
            float t = AdPause.Unscaled;

            // Nectar meter: five drops under the name plate. Full = happy, never a red bar.
            float u = CarePt();
            var feedAt = CareFeedRect(s);
            if (feedAt.width <= 2f) return;
            float dr = 7f * u;
            float gap = 22f * u;
            float mw = gap * 4f + dr * 2f;
            var meterC = new Vector2(feedAt.center.x, feedAt.y - 16f * u);
            _careMeterR = new Rect(meterC.x - mw * 0.5f, meterC.y - dr * 1.6f, mw, dr * 3f);
            double h = BirdCare.Clamp(_care.Hunger) / BirdCare.Max * 5.0;
            for (int i = 0; i < 5; i++)
            {
                float fill = Mathf.Clamp01((float)(h - i));
                CareDrop(new Vector2(meterC.x - gap * 2f + gap * i, meterC.y), dr, fill, 1f);
            }

            // Feed button: always free (first feed on the house, then nectar from play).
            _careFeedR = feedAt;
            bool can = BirdCare.CanFeed(_care, _careNectar);
            bool full = BirdCare.Clamp(_care.Hunger) >= BirdCare.Max - 0.5;
            if (_careFeedR.width > 2f)
            {
                bool held = can && GUIUtility.hotControl != 0 && _careFeedR.Contains(Event.current.mousePosition);
                var prev = GUI.color;
                bool happy = _careFeedT >= 0f;
                if (!can && !happy) GUI.color = new Color(1f, 1f, 1f, 0.55f);
                DrawPopupButton(_careFeedR, held, can || happy ? PopupTint.Green : PopupTint.Wood, 1f, true, false);
                GUI.color = prev;
                string word = _careFeedT >= 0f ? "Yum!" : full ? "Full!" : !can ? "Play for nectar" : !_care.FirstFeedUsed ? "Feed (free)" : "Feed  x" + _careNectar;
                EnsureOfferWord();
                CareText(_careFeedR, word, Mathf.RoundToInt((word.Length > 12 ? 17f : 21f) * u), new Color(1f, 0.98f, 0.92f), new Color(0.15f, 0.08f, 0.03f, 0.75f));
            }

            // Hungry: a warm speech bubble. Nap: little z's drifting up. Neither is a warning.
            if (_careFeedT < 0f && mood == BirdCare.Mood.Hungry)
            {
                float bob = Mathf.Sin(t * 2.2f) * 2f * u;
                int fs = Mathf.RoundToInt(16f * u);
                _careWord.fontSize = fs;
                var size = _careWord.CalcSize(new GUIContent(CareHungryBubble));
                bool left = _avatarFaceLeft;
                var b = new Rect(0f, bird.y - size.y - 4f * u + bob, size.x + 22f * u, size.y + 10f * u);
                b.x = left ? bird.x - b.width * 0.35f : bird.xMax - b.width * 0.65f;
                b.x = Mathf.Clamp(b.x, 8f * u, Screen.width - b.width - 8f * u);
                var cream = new Color(1f, 0.97f, 0.90f, 0.97f);
                var rim = new Color(0.36f, 0.20f, 0.10f, 0.9f);
                float rr = b.height * 0.5f;
                CareDot(new Vector2(b.x + rr, b.center.y), rr + 1.5f * u, rim);
                CareDot(new Vector2(b.xMax - rr, b.center.y), rr + 1.5f * u, rim);
                var prev = GUI.color;
                GUI.color = rim;
                GUI.DrawTexture(new Rect(b.x + rr, b.y - 1.5f * u, b.width - rr * 2f, b.height + 3f * u), Texture2D.whiteTexture);
                GUI.color = cream;
                GUI.DrawTexture(new Rect(b.x + rr, b.y, b.width - rr * 2f, b.height), Texture2D.whiteTexture);
                GUI.color = prev;
                CareDot(new Vector2(b.x + rr, b.center.y), rr, cream);
                CareDot(new Vector2(b.xMax - rr, b.center.y), rr, cream);
                var tail = new Vector2(Mathf.Clamp(bird.center.x + (left ? -bird.width * 0.1f : bird.width * 0.1f), b.x + rr, b.xMax - rr), b.yMax + 4f * u);
                CareDot(tail, 4.5f * u, rim);
                CareDot(tail, 3f * u, cream);
                CareText(b, CareHungryBubble, fs, new Color(0.42f, 0.22f, 0.12f), new Color(1f, 1f, 1f, 0f));
            }
            else if (_careFeedT < 0f && mood == BirdCare.Mood.Nap)
            {
                for (int i = 0; i < 3; i++)
                {
                    float v = Mathf.Repeat(t * 0.35f + i / 3f, 1f);
                    float a = Mathf.Sin(v * Mathf.PI);
                    float dx = (_avatarFaceLeft ? -1f : 1f) * (bird.width * 0.22f + v * 26f * u);
                    var c = new Vector2(bird.center.x + dx, bird.y + bird.height * 0.25f - v * 60f * u);
                    int fs = Mathf.RoundToInt((14f + 12f * v) * u);
                    CareText(new Rect(c.x - 20f * u, c.y - 15f * u, 40f * u, 30f * u), "z", fs, new Color(1f, 1f, 1f, a), new Color(0.25f, 0.15f, 0.35f, a * 0.6f));
                }
            }

            // The feeding moment: hearts and nectar sparkles, a hop and a chirp. Big and cheerful,
            // made to be shown to someone on the couch. No timer, no pressure.
            if (_careFeedT >= 0f)
            {
                if (Event.current.type == EventType.Repaint) _careFeedT += AdPause.UnscaledDelta;
                float dur = 2.4f;
                float k = Mathf.Clamp01(_careFeedT / dur);
                var c = bird.center;
                for (int i = 0; i < 14; i++)
                {
                    float seed = i * 0.6180339f;
                    float delay = (i % 7) * 0.08f;
                    float v = Mathf.Clamp01((_careFeedT - delay) / 1.6f);
                    if (v <= 0f || v >= 1f) continue;
                    float ang = (seed % 1f) * Mathf.PI * 2f;
                    float rad = bird.width * (0.35f + 0.55f * v);
                    var p = c + new Vector2(Mathf.Cos(ang) * rad, Mathf.Sin(ang) * rad * 0.6f - v * 70f * u);
                    float a = Mathf.Sin(v * Mathf.PI);
                    if (i % 2 == 0)
                    {
                        int fs = Mathf.RoundToInt((26f + 10f * (i % 3)) * u);
                        CareText(new Rect(p.x - 25f * u, p.y - 20f * u, 50f * u, 40f * u), "\u2665", fs, new Color(1f, 0.42f, 0.58f, a), new Color(0.45f, 0.10f, 0.20f, a * 0.5f));
                    }
                    else
                    {
                        CareDot(p, (4f + 2f * (i % 3)) * u, new Color(1f, 0.86f, 0.35f, a));
                        CareDot(p, (2f + 1f * (i % 3)) * u, new Color(1f, 1f, 0.9f, a));
                    }
                }
                float pop = k < 0.15f ? k / 0.15f : (k > 0.85f ? (1f - k) / 0.15f : 1f);
                int yfs = Mathf.RoundToInt(28f * u);
                var yum = new Rect(0f, bird.y - 46f * u, Screen.width, 40f * u);
                CareText(yum, CareYumLine, Mathf.RoundToInt(yfs * Mathf.Lerp(0.7f, 1f, pop)), new Color(1f, 0.97f, 0.88f, pop), new Color(0.40f, 0.18f, 0.08f, pop * 0.85f));
                if (_careFeedT >= dur) _careFeedT = -1f;
            }

            // Tutorial glove on Feed, through the shared gate (LiveCoachStep -> BirdFeed).
            if (!_careTutorLive && CareTutorDue()) _careTutorLive = true;
            if (_careTutorLive)
            {
                if (_careFeedR.width <= 2f || !CareHomeIdle()) return;
                // Same caption plate fade the other home intros use.
                if (Event.current.type == EventType.Repaint) _coachFade = Mathf.Min(1f, _coachFade + AdPause.UnscaledDelta / 0.30f);
                DrawAnchoredIntroLine(CareTutorLine(SavedAvatarName()), s, CaptionAnchor.CoverDiscLettering, true);
                DrawCareTutorGlove(_careFeedR, s);
            }
        }

        float _careTutorT;

        // Same shared pieces as the hive card glove: entry from the side opposite its point
        // (right half: comes from the left pointing right; left half: from the right pointing left),
        // ClearGloveOfCaption, TapArc, SeatGlove and DrawGloveAt. GloveArmTap makes the Feed button
        // the only control the lesson veil lets through.
        void DrawCareTutorGlove(Rect target, float s)
        {
            if (Event.current.type == EventType.Repaint) _careTutorT += AdPause.UnscaledDelta;
            GloveArmTap(target, null);
            var aim = GloveTarget(target);
            bool mirror = aim.x >= Screen.width * 0.5f;
            float ang = ClampUpright(mirror);
            var away = new Vector2(mirror ? -1f : 1f, 0f);
            var rest = aim + away * (GloveDh(s) * 0.85f) + new Vector2(0f, -GloveRise(s));
            ClearGloveOfCaption(aim, s, ref rest, ref away, ref mirror, ref ang, float.NaN);
            float travel = Mathf.Clamp01(_careTutorT / 0.48f);
            travel = travel * travel * (3f - 2f * travel);
            TapArc(rest, aim, away, s, travel, out var pivot, out _, mirror, _gloveApexMinY);
            float cyc = Mathf.Repeat(_careTutorT - 0.5f, 1.4f);
            float dip = _careTutorT > 0.5f && cyc < 0.22f ? Mathf.Sin(cyc / 0.22f * Mathf.PI) : 0f;
            if (travel < 0.84f)
                SeatGlove(ref pivot, ref ang, aim, s, ref mirror, dip);
            DrawGloveAt(pivot, ang, GloveDh(s), 1f, dip, mirror);
            _careGloveMirror = mirror;
            _careGlovePivot = pivot;
        }

        bool _careGloveMirror;
        Vector2 _careGlovePivot;

#if UNITY_EDITOR
        // Test/shot probes.
        public bool CareGloveMirrorProbe => _careGloveMirror;
        public Vector2 CareGlovePivotProbe => _careGlovePivot;
        public Rect CareFeedRectProbe => _careFeedR;
#endif
    }
}
