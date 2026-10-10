using System;
using UnityEngine;

namespace FlockFive
{
    // One rewarded show. LevelPlay delivers OnAdRewarded and OnAdClosed in either
    // order, and a click-through can deliver the reward while the app is paused
    // or after the gift screen has already closed. The grant is the reward
    // callback, not the end of the wait. It is stored the moment it fires and
    // applied once, when the UI can take it.
    public static class RewardedGrant
    {
        public enum Place
        {
            None = 0,
            Branch = 1,
            Wheel = 2,
            Keep = 3
        }

        public struct Ticket
        {
            public int Token;
            public Place Place;
            public int Payload;
            public bool TutorialGoal;
            public int Level;
        }

        struct Pending
        {
            public int Token;
            public Place Place;
            public int Payload;
            public bool TutorialGoal;
            public int Level;
            public bool Open;
            public bool Shown;
            public bool Earned;
            public bool Applied;
            public bool Failed;
        }

        public const string Pref = "flockfive.reward.pending";

        // False keeps the earn queued. Tutorial and coach state are not this.
        public static bool UiBusy;

        public static bool TutorialUp { get; private set; }
        public static bool CoachGuard { get; private set; }
        public static bool GiftClosed { get; private set; }
        public static bool Paused { get; private set; }
        public static int ApplyCount { get; private set; }

        public static Func<Ticket, bool> TryApply;

        static Pending _cur;
        static Pending _owed;
        static bool _queued;

        // This show, or a latched show that has not been applied yet.
        // An already-applied latch must not make the next skip look earned.
        public static bool Earned => _cur.Earned || (_owed.Earned && !_owed.Applied);
        public static bool Shown => _cur.Shown || _owed.Shown;
        public static bool Closed { get; private set; }
        public static bool Failed => _cur.Failed && !_cur.Earned && !_owed.Earned;

        public static bool HasUnapplied =>
            (_cur.Earned && !_cur.Applied) || (_owed.Earned && !_owed.Applied);

        // An earned branch lesson stays up until the grant finishes it.
        public static bool ProtectsTutorial =>
            (_cur.TutorialGoal && _cur.Earned && !_cur.Applied)
            || (_owed.TutorialGoal && _owed.Earned && !_owed.Applied);

        public static int ActiveToken => _cur.Earned ? _cur.Token : (_owed.Earned ? _owed.Token : _cur.Token);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            TryApply = null;
            UiBusy = false;
            ClearMemory();
        }

        public static int Begin(Place place, int payload, bool tutorialGoal, int level)
        {
            if (HasUnapplied) Flush();
            // Keep an earn the UI could not take. A finished grant is not the next show.
            if (_cur.Earned && !_cur.Applied)
            {
                if (!_owed.Earned || _owed.Applied)
                    _owed = _cur;
            }
            else if (_cur.Open && _cur.Shown && !_cur.Earned && !_cur.Applied)
                _owed = _cur;
            else if (!_owed.Earned || _owed.Applied)
                _owed = default;

            int token = _cur.Token + 1;
            if (_owed.Token >= token) token = _owed.Token + 1;
            if (token < 1) token = 1;
            _cur = new Pending
            {
                Token = token,
                Place = place,
                Payload = payload,
                TutorialGoal = tutorialGoal,
                Level = level,
                Open = true
            };
            Closed = false;
            GiftClosed = false;
            _queued = false;
            return token;
        }

        public static void OnDisplayed()
        {
            if (!_cur.Open) return;
            _cur.Shown = true;
            // This show owns later callbacks. An earn that is still queued stays.
            if (!_owed.Earned || _owed.Applied)
                _owed = default;
        }

        // LevelPlay OnAdRewarded. Persists before anything else, including while paused.
        public static bool OnRewarded()
        {
            if (_cur.Open && !_cur.Applied && !_cur.Earned && (_cur.Shown || _owed.Token == 0) && !(_cur.Failed && !_cur.Shown))
                return Earn(ref _cur);
            if (_owed.Token != 0 && _owed.Shown && !_owed.Applied && !_owed.Earned && !_cur.Shown)
                return Earn(ref _owed);
            if (HasUnapplied) Flush();
            return false;
        }

        // LevelPlay OnAdClosed. A reward may still arrive after this.
        public static void OnClosed()
        {
            Closed = true;
        }

        // No-fill or a display error. A show that never appeared cannot be earned
        // by a later callback. A show that did appear still accepts one.
        public static void OnFailed()
        {
            if (!_cur.Open || _cur.Earned) return;
            _cur.Failed = true;
            if (!_cur.Shown) _cur.Open = false;
        }

        public static void OnPause(bool paused)
        {
            Paused = paused;
            if (!paused) Flush();
        }

        public static void OnFocus(bool focus)
        {
            if (!focus)
            {
                Paused = true;
                return;
            }
            Paused = false;
            Flush();
        }

        // Resume tore the gift (or daily) screen down. The earn still applies.
        public static void NoteUiClosed()
        {
            GiftClosed = true;
        }

        // Recorded so a test can prove a live lesson does not block the grant.
        public static void NoteCoach(bool tutorialUp, bool guardUp)
        {
            TutorialUp = tutorialUp;
            CoachGuard = guardUp;
        }

        public static bool Flush()
        {
            bool did = false;
            if (_owed.Earned && !_owed.Applied)
                did |= Apply(ref _owed);
            if (_cur.Earned && !_cur.Applied)
                did |= Apply(ref _cur);
            return did;
        }

        // Next launch. Memory is empty; an earned pref is queued again.
        public static void Boot()
        {
            if (_cur.Open || _owed.Token != 0) return;
            if (!Read(out var p)) return;
            if (!p.Earned || p.Applied)
            {
                ClearPersisted();
                return;
            }
            _cur = p;
            _cur.Open = true;
            _queued = true;
            Paused = false;
        }

        // Process kill. Prefs stay. The applier stays, so the next Boot can grant.
        public static void ForgetSession()
        {
            var apply = TryApply;
            bool busy = UiBusy;
            ClearMemory();
            TryApply = apply;
            UiBusy = busy;
        }

        public static void ResetEmpty()
        {
            var apply = TryApply;
            ClearMemory();
            TryApply = apply;
            ClearPersisted();
        }

        static bool Earn(ref Pending p)
        {
            if (p.Applied) return false;
            if (p.Earned)
            {
                Flush();
                return false;
            }
            p.Earned = true;
            p.Failed = false;
            Persist(p);
            _queued = true;
            AdLog.Add("reward earned " + p.Place + " token " + p.Token
                + (Paused ? " (paused)" : "")
                + (UiBusy ? " (ui busy)" : ""));
            if (CanApply()) Apply(ref p);
            return true;
        }

        static bool CanApply() => !Paused && !UiBusy && TryApply != null;

        static bool Apply(ref Pending p)
        {
            if (!p.Earned || p.Applied || !CanApply()) return false;
            bool ok = false;
            try
            {
                ok = TryApply(ToTicket(p));
            }
            catch (Exception e)
            {
                Debug.LogWarning("reward apply skipped: " + e.Message);
            }
            if (!ok)
            {
                _queued = true;
                return false;
            }
            p.Applied = true;
            _queued = false;
            ApplyCount++;
            if (PrefToken() == p.Token) ClearPersisted();
            AdLog.Add("reward applied " + p.Place + " token " + p.Token);
            return true;
        }

        static Ticket ToTicket(Pending p) => new Ticket
        {
            Token = p.Token,
            Place = p.Place,
            Payload = p.Payload,
            TutorialGoal = p.TutorialGoal,
            Level = p.Level
        };

        static void Persist(Pending p)
        {
            if (!p.Earned || p.Applied) return;
            if (Read(out var existing) && existing.Earned && !existing.Applied && existing.Token != p.Token)
                return;
            string body = p.Token
                + "|" + (int)p.Place
                + "|" + p.Payload
                + "|" + (p.TutorialGoal ? 1 : 0)
                + "|" + p.Level
                + "|1|0";
            PrefGuard.SetString(Pref, body);
            PlayerPrefs.Save();
        }

        static bool Read(out Pending p)
        {
            p = default;
            string raw = PrefGuard.GetString(Pref, "");
            if (string.IsNullOrEmpty(raw)) return false;
            var part = raw.Split('|');
            if (part.Length != 7) return false;
            if (!int.TryParse(part[0], out int token) || token < 1) return false;
            if (!int.TryParse(part[1], out int place)) return false;
            if (place < (int)Place.Branch || place > (int)Place.Keep) return false;
            if (!int.TryParse(part[2], out int payload)) return false;
            if (!int.TryParse(part[3], out int tutorial)) return false;
            if (!int.TryParse(part[4], out int level)) return false;
            if (part[5] != "1" || part[6] != "0") return false;
            p.Token = token;
            p.Place = (Place)place;
            p.Payload = payload;
            p.TutorialGoal = tutorial != 0;
            p.Level = level;
            p.Earned = true;
            p.Shown = true;
            return true;
        }

        static int PrefToken()
        {
            return Read(out var p) ? p.Token : 0;
        }

        static void ClearPersisted()
        {
            PlayerPrefs.DeleteKey(Pref);
            PlayerPrefs.DeleteKey(Pref + ".x");
            PlayerPrefs.Save();
        }

        static void ClearMemory()
        {
            _cur = default;
            _owed = default;
            TutorialUp = false;
            CoachGuard = false;
            GiftClosed = false;
            Paused = false;
            Closed = false;
            ApplyCount = 0;
            _queued = false;
            UiBusy = false;
        }
    }

    // The placement effects the grant applies once. Callers share these so a
    // queued, paused, or relaunched reward pays the same way as an immediate one.
    public static class RewardEffects
    {
        // 1 when a locked gift opened, 0 when that gift was already open, -1 when
        // the board cannot take it yet. index is -1 when nothing is left to open.
        public static int Branch(Board board, bool[] claimed, int preferred, out int index)
        {
            index = -1;
            if (board == null || claimed == null) return -1;
            index = BonusBranches.ContinueIndex(board, claimed, preferred);
            if (index < 0) return 0;
            return BonusBranches.Claim(board, claimed, index);
        }

        // 2x of a landed spin. A spin already paid at 1x is topped up. A second
        // call after 2x pays nothing more.
        public static bool Wheel(int baseCoins)
        {
            return DailyBonus.PayStoredDouble(baseCoins);
        }

        public static bool Keep(int mul)
        {
            if (mul < 2) return false;
            Purse.SetStreak(mul);
            return true;
        }
    }
}
