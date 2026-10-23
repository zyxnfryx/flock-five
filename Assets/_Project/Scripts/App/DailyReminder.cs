using System;
using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR
using Unity.Notifications.iOS;
#endif
#if UNITY_ANDROID && !UNITY_EDITOR
using Unity.Notifications.Android;
#endif

namespace FlockFive
{
    // Local reminder for the daily bonus. Nothing is pending while the app is open (cancelled on
    // launch/foreground). It is written on OnAppQuit (real terminate) or OnAppBackground (guess:
    // iOS/Android cannot tell us the app was swiped away), with a 2h grace on the background path.
    // See NotifyPlan for the limits. com.unity.mobile.notifications 2.4.x.
    // Editor and desktop builds compile the same methods with no native calls.
    // Nothing here requests permission. The OS prompt runs only from RequestOsNow,
    // after the player says yes on the in-game card (Accept) and the game is idle
    // after its reward audio. Reschedule is a no-op
    // until that permission is already granted.
    public static class DailyReminder
    {
        public const string AskedKey = "flockfive.daily.asked";
        public const string YesKey = "flockfive.daily.askYes";
        public const string AsksKey = "flockfive.daily.askN";
        // Build 52: "yes" on the in-game card no longer opens the OS dialog right away. It only
        // records that the dialog is owed. The game asks for it (RequestOsNow) once the
        // reward audio and animation are done, so the Apple/Android sheet cannot cut off the
        // welcome bonus coin sound. Stored in PlayerPrefs so a quit before then still asks later.
        public const string OsOwedKey = "flockfive.daily.osOwed";
        const string PrefNoteReady = "flockfive.daily.noteReady";
        const string PrefNoteKeep = "flockfive.daily.noteKeep";
        const string IosReady = "flockfive.daily.ready";
        const string IosKeep = "flockfive.daily.keep";
        const string ChannelId = "flockfive_daily";

        static int _armStreak;
        static bool _armRisk;
        static bool _armClaimed;
        static long _armLast;
        // The in-game card and the OS notification sheet share one session. Either one
        // already asked, so the card does not also appear later in this launch.
        static bool _permissionAskedThisSession;

#if UNITY_IOS && !UNITY_EDITOR
        static AuthorizationRequest _iosAuth;
#endif
#if UNITY_ANDROID && !UNITY_EDITOR
        static PermissionRequest _androidAuth;
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _armStreak = 0;
            _armRisk = false;
            _armClaimed = false;
            _armLast = 0;
            _permissionAskedThisSession = false;
#if UNITY_IOS && !UNITY_EDITOR
            if (_iosAuth != null)
            {
                _iosAuth.Dispose();
                _iosAuth = null;
            }
#endif
#if UNITY_ANDROID && !UNITY_EDITOR
            _androidAuth = null;
#endif
        }

        // The app is active from launch until OnAppBackground/OnAppQuit.
        static bool _active = true;
        static NotifyPlan.Signal _signal = NotifyPlan.Signal.Background;

        // Records the latest streak state. While the app is active nothing may be
        // pending, so this also clears any stale notification. The schedule itself is
        // written only by OnAppBackground / OnAppQuit.
        // lastClaimUnix is DailyBonus.LastClaimUnix (0 = never). With it the coins reminder is
        // lastClaim + 24h and the streak reminder trails the grace deadline (NotifyPlan.ComputeWindow).
        public static void Reschedule(int streak, bool claimedToday, bool atRisk, long lastClaimUnix = 0)
        {
            _armLast = lastClaimUnix;
            if (streak < 0) streak = 0;
            _armStreak = streak;
            _armClaimed = claimedToday;
            _armRisk = atRisk && streak >= 2;
            if (_active) CancelAll();
        }

        // Launch / foreground: cancel pending, clear delivered.
        public static void OnAppForeground()
        {
            _active = true;
            CancelAll();
        }

        // Background / pause: schedule for the correct future time.
        public static void OnAppBackground()
        {
            _active = false;
            _signal = NotifyPlan.Signal.Background;
            ScheduleNow(_signal);
        }

        // Quit: the one genuine terminate signal; uses the short lead.
        public static void OnAppQuit()
        {
            _active = false;
            _signal = NotifyPlan.Signal.Quit;
            ScheduleNow(_signal);
        }

        static void ScheduleNow(NotifyPlan.Signal signal)
        {
            _signal = signal;
            CancelAll();
#if UNITY_IOS && !UNITY_EDITOR
            if (!IosGranted()) return;
            ScheduleIos();
#elif UNITY_ANDROID && !UNITY_EDITOR
            if (!AndroidGranted()) return;
            ScheduleAndroid();
#else
            PlanNow();
#endif
        }

        static System.Collections.Generic.List<NotifyPlan.Entry> PlanNow()
        {
            return _armLast > 0
                ? NotifyPlan.ComputeWindow(DateTime.Now, _armLast, _armStreak, _armRisk, _signal)
                : NotifyPlan.Compute(DateTime.Now, _armStreak, _armClaimed, _armRisk, _signal);
        }

        static void CancelAll()
        {
#if UNITY_IOS && !UNITY_EDITOR
            try
            {
                iOSNotificationCenter.RemoveAllScheduledNotifications();
                iOSNotificationCenter.RemoveAllDeliveredNotifications();
                iOSNotificationCenter.ApplicationBadge = 0;
            }
            catch (Exception) { }
#elif UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                AndroidNotificationCenter.CancelAllScheduledNotifications();
                AndroidNotificationCenter.CancelAllDisplayedNotifications();
                PlayerPrefs.SetInt(PrefNoteReady, 0);
                PlayerPrefs.SetInt(PrefNoteKeep, 0);
            }
            catch (Exception) { }
#endif
        }

        // True after any notification-permission ask this launch: the streak card,
        // or the OS sheet from RequestOsNow. The card must not open again beside one.
        public static bool PermissionAskedThisSession => _permissionAskedThisSession;

        public static void NotePermissionAsked() => _permissionAskedThisSession = true;

        public static void ResetSessionForTest() => _permissionAskedThisSession = false;

        // In-game "want a reminder?" card. One ask, ever. Yes and Not now both stick.
        // Showing the card sticks too (MarkShown), so a quit before the tap still counts.
        // Yes is the only path that owes the OS dialog. That dialog is the only
        // notification permission request in the project (AuthorizationRequest /
        // PermissionRequest). Ad consent and ATT are a different prompt.
        public static bool PromptDue()
        {
            if (_permissionAskedThisSession) return false;
            if (OsGranted()) return false;
            if (PlayerPrefs.GetInt(YesKey, 0) != 0) return false;
            if (PlayerPrefs.GetInt(AskedKey, 0) != 0) return false;
            if (PlayerPrefs.GetInt(OsOwedKey, 0) != 0) return false;
            return PlayerPrefs.GetInt(AsksKey, 0) < 1;
        }

        // The card is on screen. Persist it now, before the player answers.
        public static void MarkShown()
        {
            StickAsked();
            PlayerPrefs.Save();
        }

        public static void Accept()
        {
            StickAsked();
            PlayerPrefs.SetInt(YesKey, 1);
            PlayerPrefs.SetInt(OsOwedKey, 1);
            PlayerPrefs.Save();
        }

        // True after the player said yes and before the OS dialog has been shown.
        public static bool OsRequestOwed => PlayerPrefs.GetInt(OsOwedKey, 0) != 0;

        // Shows the OS permission dialog now. The caller (FlockFiveApp.TickDailyOsRequest)
        // only calls this when the game is idle after a reward.
        public static void RequestOsNow()
        {
            if (PlayerPrefs.GetInt(OsOwedKey, 0) == 0) return;
            NotePermissionAsked();
            PlayerPrefs.SetInt(OsOwedKey, 0);
            PlayerPrefs.Save();
#if UNITY_IOS && !UNITY_EDITOR
            try
            {
                _iosAuth = new AuthorizationRequest(AuthorizationOption.Alert | AuthorizationOption.Sound, false);
                return;
            }
            catch (Exception) { }
#elif UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                _androidAuth = new PermissionRequest();
                return;
            }
            catch (Exception) { }
#endif
            // Foreground: nothing may be pending. The schedule is written on background.
        }

        public static void Decline()
        {
            StickAsked();
            PlayerPrefs.Save();
        }

        // Either answer, or the card being shown, is the one ask. A later claim
        // must not bring the card back. AsksKey stays at least 1 so a build that
        // already counted one "Not now" (and never set AskedKey) does not ask again.
        static void StickAsked()
        {
            NotePermissionAsked();
            if (PlayerPrefs.GetInt(AsksKey, 0) < 1)
                PlayerPrefs.SetInt(AsksKey, 1);
            PlayerPrefs.SetInt(AskedKey, 1);
        }

        static bool OsGranted()
        {
#if UNITY_IOS && !UNITY_EDITOR
            return IosGranted();
#elif UNITY_ANDROID && !UNITY_EDITOR
            return AndroidGranted();
#else
            return false;
#endif
        }

#if UNITY_IOS && !UNITY_EDITOR
        static bool IosGranted()
        {
            try
            {
                var st = iOSNotificationCenter.GetNotificationSettings().AuthorizationStatus;
                return st == AuthorizationStatus.Authorized
                    || st == AuthorizationStatus.Provisional
                    || st == AuthorizationStatus.Ephemeral;
            }
            catch (Exception)
            {
                return false;
            }
        }
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
        static bool AndroidGranted()
        {
            try
            {
                return AndroidNotificationCenter.UserPermissionToPost == PermissionStatus.Allowed;
            }
            catch (Exception)
            {
                return false;
            }
        }
#endif

        public static void Tick()
        {
#if UNITY_IOS && !UNITY_EDITOR
            if (_iosAuth != null && _iosAuth.IsFinished)
            {
                bool granted = _iosAuth.Granted;
                _iosAuth.Dispose();
                _iosAuth = null;
                if (granted && !_active) ScheduleIos();
            }
#elif UNITY_ANDROID && !UNITY_EDITOR
            if (_androidAuth != null && _androidAuth.Status != PermissionStatus.RequestPending)
            {
                bool granted = _androidAuth.Status == PermissionStatus.Allowed;
                _androidAuth = null;
                if (granted && !_active) ScheduleAndroid();
            }
#endif
        }

#if UNITY_IOS && !UNITY_EDITOR
        static void ScheduleIos()
        {
            try
            {
                iOSNotificationCenter.RemoveScheduledNotification(IosReady);
                iOSNotificationCenter.RemoveDeliveredNotification(IosReady);
                iOSNotificationCenter.RemoveScheduledNotification(IosKeep);
                iOSNotificationCenter.RemoveDeliveredNotification(IosKeep);
                foreach (var e in PlanNow())
                    ScheduleIosAt(e.Kind == NotifyPlan.Kind.Ready ? IosReady : IosKeep, "Daily bonus", e.Body, e.When);
            }
            catch (Exception) { }
        }

        static void ScheduleIosAt(string id, string title, string body, DateTime when)
        {
            var trigger = new iOSNotificationCalendarTrigger();
            trigger.Year = when.Year;
            trigger.Month = when.Month;
            trigger.Day = when.Day;
            trigger.Hour = when.Hour;
            trigger.Minute = when.Minute;
            trigger.Second = when.Second;
            trigger.Repeats = false;
            var note = new iOSNotification(id);
            note.Title = title;
            note.Body = body;
            note.ShowInForeground = false;
            note.Badge = -1;
            note.ThreadIdentifier = "flockfive.daily";
            note.Trigger = trigger;
            iOSNotificationCenter.ScheduleNotification(note);
        }
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
        static void ScheduleAndroid()
        {
            try
            {
                var channel = new AndroidNotificationChannel(
                    ChannelId,
                    "Daily bonus",
                    "A reminder when the daily coins are ready.",
                    Importance.Default);
                AndroidNotificationCenter.RegisterNotificationChannel(channel);
                CancelAndroid(PrefNoteReady);
                CancelAndroid(PrefNoteKeep);
                foreach (var e in PlanNow())
                {
                    int id = SendAndroid("Daily bonus", e.Body, e.When);
                    PlayerPrefs.SetInt(e.Kind == NotifyPlan.Kind.Ready ? PrefNoteReady : PrefNoteKeep, id + 1);
                }
                PlayerPrefs.Save();
            }
            catch (Exception) { }
        }

        static int SendAndroid(string title, string body, DateTime when)
        {
            var note = new AndroidNotification(title, body, when);
            note.ShouldAutoCancel = true;
            note.ShowInForeground = false;
            return AndroidNotificationCenter.SendNotification(note, ChannelId);
        }

        static void CancelAndroid(string pref)
        {
            int stored = PlayerPrefs.GetInt(pref, 0);
            if (stored <= 0) return;
            AndroidNotificationCenter.CancelNotification(stored - 1);
            PlayerPrefs.SetInt(pref, 0);
        }
#endif
    }
}
