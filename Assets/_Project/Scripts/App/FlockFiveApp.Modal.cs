using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace FlockFive
{
    public sealed partial class FlockFiveApp
    {
        // The sheet is only the card LayoutHome just placed on the splash.
        // A stale kind must not keep blocking the garden after the player leaves.
        bool HomeSheetUp()
        {
            return _splash && _home == HomeFace.Splash && !_settingsOpen;
        }

        // One open popup. The first match wins, so a card covers a lesson under it.
        ModalBlock.Id OpenModal()
        {
            if (_settingsOpen) return ModalBlock.Id.Settings;
            if (HomeSheetUp() && _homePrompt.Kind == AdConsent.Sheet.Att) return ModalBlock.Id.Att;
            if (HomeSheetUp() && _homePrompt.Kind == AdConsent.Sheet.Gdpr) return ModalBlock.Id.Consent;
            if (VipOffer.IsOpen) return ModalBlock.Id.Vip;
            if (VipOffer.SoftUp) return ModalBlock.Id.VipSoft;
            if (_clearOffer) return ModalBlock.Id.ClearDouble;
            if (_dailyAskOpen) return ModalBlock.Id.DailyAsk;
            if (_dailyOpen) return ModalBlock.Id.Daily;
            if (_welcomeOpen) return ModalBlock.Id.Welcome;
            if (_avatarRename) return ModalBlock.Id.AvatarRename;
            if (_streakSlide >= 0f) return ModalBlock.Id.Streak;
            if (_gift != GiftFace.None) return ModalBlock.Id.Gift;
            if (_restartAsk != RestartAsk.None) return ModalBlock.Id.Restart;
            if (_clearReward) return ModalBlock.Id.ClearReward;
            if (_hiveInspect >= 0) return ModalBlock.Id.Inspect;
            if (_pokerPayOpen) return ModalBlock.Id.PokerPay;
            if (_adoptLive && _adoptStep != AdoptStep.Look) return ModalBlock.Id.Adopt;
            return ModalBlock.Id.None;
        }

        // Required and claim-only steps. Informational lessons (sparrow, hawk, pest bee, leaf) stay open.
        int LiveCoachStep()
        {
            if (_hiveLevelLive) return (int)CoachStep.HiveGarden;
            if (_cueGift && (_cueForce || _cueHand)) return (int)CoachStep.GiftStuck;
            if (_coach && _cueForce && _cueHand)
                return (int)(_sel < 0 ? CoachStep.CoachPickup : CoachStep.CoachPlace);
            if (_hiveIntroLive) return (int)CoachStep.HiveHome;
            if (_pokerIntroLive) return (int)CoachStep.PokerIntro;
            if (_dailyIntroLive && !_dailyOpen && !_dailyAskOpen) return (int)CoachStep.DailyRail;
            if (_dailyIntroLive && _dailyOpen && !_dailyAskOpen) return (int)CoachStep.DailyClaim;
            if (_adoptLive && _adoptStep == AdoptStep.Look) return (int)CoachStep.AdoptLook;
            if (_welcomeGlove && !_welcomeOpen) return (int)CoachStep.VipPitch;
            if (_adHand) return (int)CoachStep.AdHand;
            if (PokerBackGateLive()) return (int)CoachStep.PokerBack;
            return -1;
        }

        bool ForcedLessonUp()
        {
            int step = LiveCoachStep();
            if (step < 0) return false;
            return CoachTap.Forced((CoachStep)step);
        }

        bool GloveTargetFresh()
        {
            if (_gloveTapFrame < 0) return false;
            int age = Time.frameCount - _gloveTapFrame;
            return age >= 0 && age <= 1;
        }

        // GUI rect from the last pose, or the world hit the glove armed (coach hops).
        bool LessonTargetHit(Vector2 screen, Vector2 gui)
        {
            if (!GloveTargetFresh()) return false;
            if (_gloveTapHit != null) return _gloveTapHit(screen);
            return _gloveTapRectOn && _gloveTapRect.Contains(gui);
        }

        ModalBlock.Veil ActiveVeil()
        {
            float w = Screen.width > 2f ? Screen.width : 2f;
            float h = Screen.height > 2f ? Screen.height : 2f;
            var popup = OpenModal();
            if (popup != ModalBlock.Id.None)
                return ModalBlock.Raise(popup, w, h);
            if (ForcedLessonUp())
            {
                Rect cut = _gloveTapRectOn && GloveTargetFresh() ? _gloveTapRect : default;
                return ModalBlock.RaiseLesson(w, h, cut);
            }
            return default;
        }

        bool VeilBlocks(Rect control)
        {
            return ModalBlock.BlocksControl(ActiveVeil(), control);
        }

        // Screen point (y up). True when the veil eats it. The glove hole and a
        // world hit-callback fall through so the target control still runs.
        bool VeilDrops(Vector2 screen)
        {
            var veil = ActiveVeil();
            if (!veil.Open) return false;
            var gui = new Vector2(screen.x, Screen.height - screen.y);
            if (veil.Owner == ModalBlock.Id.Lesson && LessonTargetHit(screen, gui))
                return false;
            return ModalBlock.BlocksPoint(veil, gui);
        }

        bool _shot82f;

        public IEnumerator ShotBuild82f()
        {
            if (_shot82f) yield break;
            _shot82f = true;
            MuteShotAudio();
#if UNITY_EDITOR
            EditorShotLive = true;
            UnityEditor.EditorApplication.isPaused = false;
            Application.runInBackground = true;
#endif
            const string dir = "/Users/zfxgames/wkspaces/wt-b82/out";
            Directory.CreateDirectory(dir);
            int nextKeep = PlayerPrefs.GetInt("flockfive.next", 0);
            bool hadNext = PlayerPrefs.HasKey("flockfive.next");
            int coachKeep = PlayerPrefs.GetInt(CoachKey, 0);
            bool hadCoach = PlayerPrefs.HasKey(CoachKey);
            int leafKeep = PlayerPrefs.GetInt(CoachLeafKey, 0);
            bool hadLeaf = PlayerPrefs.HasKey(CoachLeafKey);
            int hiveKeep = PlayerPrefs.GetInt(CoachHiveGardenKey, 0);
            bool hadHive = PlayerPrefs.HasKey(CoachHiveGardenKey);
            int sparKeep = PlayerPrefs.GetInt(CoachSparrowKey, 0);
            bool hadSpar = PlayerPrefs.HasKey(CoachSparrowKey);

            GamePause.Reset();
            AdConsent.DismissTransient();
            AdConsent.EditorHoldAtt = false;
            Screen.SetResolution(1179, 2556, false);
            MuteShotAudio();

            yield return WaitForHomePaint();
            for (int i = 0; i < 4; i++)
            {
                PoseBuild79(false, true);
                MuteShotAudio();
                yield return null;
            }
            PoseBuild79(false, true);
            yield return new WaitForEndOfFrame();
            MuteShotAudio();
            SaveFrame(dir + "/b82f-att-blocker.png");
            PoseBuild79(false, false);
            AdConsent.EditorHoldAtt = false;
            AdConsent.DismissTransient();

            PrepGardenShot(0);
            PlayerPrefs.SetInt(CoachSparrowKey, 0);
            PlayerPrefs.Save();
            GamePause.Reset();
            Time.timeScale = 1f;
            yield return new WaitForSecondsRealtime(1.15f);
            GamePause.Reset();
            Time.timeScale = 1f;
            // The shared sparrow lesson only paints while a sparrow is on a feeder.
            // Patrol waits many seconds; Visit is the same arrival the garden uses.
            if (_garden.Feeders != null && _garden.Root != null && SparrowView.Live == null)
                StartCoroutine(SparrowView.Visit(_garden.Feeders, _garden.Root));
            float arrive = 0f;
            while (arrive < 3.4f)
            {
                var live = SparrowView.Live;
                if (live != null && live.Settled && live.IsBlocking) break;
                arrive += Time.unscaledDeltaTime;
                yield return null;
            }
            var perched = SparrowView.Live;
            Debug.Log("b82f sparrow live=" + (perched != null)
                + " settled=" + (perched != null && perched.Settled)
                + " block=" + (perched != null && perched.IsBlocking)
                + " wait=" + arrive.ToString("0.00")
                + " solved=" + PestSchedule.SolvedNow);
            if (perched != null && _pestCue != PestCueSparrow)
                BeginPestCue(PestCueSparrow);
            _coachFade = 1f;
            float shown = 0f;
            while (shown < 0.55f)
            {
                if (SparrowView.Live != null && _pestCue != PestCueSparrow)
                    BeginPestCue(PestCueSparrow);
                _coachFade = 1f;
                shown += Time.unscaledDeltaTime;
                yield return null;
            }
            _coachFade = 1f;
            MuteShotAudio();
            yield return new WaitForEndOfFrame();
            MuteShotAudio();
            Debug.Log("b82f sparrow caption cue=" + _pestCue + " line=" + (_cueLine ?? ""));
            SaveFrame(dir + "/b82f-sparrow-caption.png");
            _pestCue = 0;
            _cueLine = null;
            _cueHand = false;
            _coachFade = 0f;
            _gloveVis = false;
            ReleaseTutorPause();
            GamePause.Reset();
            Time.timeScale = 1f;
            if (SparrowView.Live != null)
                Destroy(SparrowView.Live.gameObject);

            _shotBeeLesson = true;
            _shotCardLesson = false;
            yield return new WaitForSecondsRealtime(1.25f);
            MuteShotAudio();
            yield return new WaitForEndOfFrame();
            SaveFrame(dir + "/b82f-bee-lesson-gate.png");
            _shotBeeLesson = false;
            _shotGlovePin = false;
            _hiveLevelLive = false;
            _hiveLevelCue = false;
            _cueHand = false;
            _cueLine = null;
            _gloveVis = false;
            _coachFade = 0f;

            GamePause.Reset();
            Time.timeScale = 1f;
            PrepGardenShot(0);
            yield return new WaitForSecondsRealtime(1.15f);
            GamePause.Reset();
            Time.timeScale = 1f;
            MuteShotAudio();
            StartRedistributeShot();
            yield return RecordClip("/tmp/b82f-redist", dir + "/b82f-sparrow-redistribute.mp4", 2.2f, null);

            if (hadNext) PlayerPrefs.SetInt("flockfive.next", nextKeep);
            else PlayerPrefs.DeleteKey("flockfive.next");
            if (hadCoach) PlayerPrefs.SetInt(CoachKey, coachKeep);
            else PlayerPrefs.DeleteKey(CoachKey);
            if (hadLeaf) PlayerPrefs.SetInt(CoachLeafKey, leafKeep);
            else PlayerPrefs.DeleteKey(CoachLeafKey);
            if (hadHive) PlayerPrefs.SetInt(CoachHiveGardenKey, hiveKeep);
            else PlayerPrefs.DeleteKey(CoachHiveGardenKey);
            if (hadSpar) PlayerPrefs.SetInt(CoachSparrowKey, sparKeep);
            else PlayerPrefs.DeleteKey(CoachSparrowKey);
            PlayerPrefs.Save();
            Debug.Log("B82F_STILLS_DONE");
#if UNITY_EDITOR
            EditorShotLive = false;
            UnityEditor.EditorApplication.Exit(0);
#endif
        }

        void StartRedistributeShot()
        {
            if (_garden.Branches == null) return;
            var birds = new List<SpriteRenderer>(16);
            var homes = new List<PestPark.Home>(16);
            var branches = _garden.Branches;
            for (int b = 0; b < branches.Length; b++)
            {
                var br = branches[b];
                if (br == null) continue;
                for (int s = 0; s < BranchState.Cap; s++)
                {
                    var sr = br.Birds[s];
                    if (sr == null || !sr.enabled) continue;
                    birds.Add(sr);
                    homes.Add(new PestPark.Home { Branch = b, Seat = s });
                }
            }
            int n = birds.Count;
            if (n <= 0) return;
            for (int i = 0; i < n; i++)
            {
                var tr = birds[i].transform;
                tr.position += new Vector3((i - n * 0.5f) * 0.35f, 1.7f + (i % 4) * 0.22f, 0f);
            }
            var plan = new PestPark.Plan { Homes = homes.ToArray() };
            StartCoroutine(PerchOnRemain(birds.ToArray(), n, plan));
            Debug.Log("b82f redistribute birds=" + n);
        }
    }
}
