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
        const string PrefAsked = "flockfive.daily.asked";
        const string PrefYes = "flockfive.daily.askYes";
        const string PrefAsks = "flockfive.daily.askN";
        // Build 52: "yes" on the in-game card no longer opens the OS dialog right away. It only
        // records that the dialog is owed. The game asks for it (RequestOsNow) once the
        // reward audio and animation are done, so the Apple/Android sheet cannot cut off the
        // welcome bonus coin sound. Stored in PlayerPrefs so a quit before then still asks later.
        const string PrefOsOwed = "flockfive.daily.osOwed";
        const string PrefNoteReady = "flockfive.daily.noteReady";
        const string PrefNoteKeep = "flockfive.daily.noteKeep";
        const string IosReady = "flockfive.daily.ready";
        const string IosKeep = "flockfive.daily.keep";
        const string ChannelId = "flockfive_daily";

        // Bird Care v1: extra "feed me" entries (local time, body), written with the daily ones on
        // background/quit. Null or empty unless the remote flag, the opt-in and the audience rules
        // all allow it (BirdCareNotify.Allowed). Cleared with everything else on foreground.
        public static Func<System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<DateTime, string>>> CareEntries;
        const string IosCare = "flockfive.care.feed";

        static System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<DateTime, string>> CareNow()
        {
            try { return CareEntries != null ? CareEntries() : null; }
            catch (Exception) { return null; }
        }

        static int _armStreak;
        static bool _armRisk;
        static bool _armClaimed;
        static long _armLast;

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

        // In-game "want a reminder?" card. Two answers total. Yes is the only
        // path that constructs an authorization request. Not now spends one ask.
        public static bool PromptDue()
        {
            if (OsGranted()) return false;
            if (PlayerPrefs.GetInt(PrefYes, 0) != 0) return false;
            if (PlayerPrefs.GetInt(PrefAsked, 0) != 0) return false;
            return PlayerPrefs.GetInt(PrefAsks, 0) < 2;
        }

        public static void Accept()
        {
            if (PlayerPrefs.GetInt(PrefAsks, 0) < 2)
                PlayerPrefs.SetInt(PrefAsks, PlayerPrefs.GetInt(PrefAsks, 0) + 1);
            PlayerPrefs.SetInt(PrefYes, 1);
            PlayerPrefs.SetInt(PrefAsked, 1);
            PlayerPrefs.SetInt(PrefOsOwed, 1);
            PlayerPrefs.Save();
        }

        // True after the player said yes and before the OS dialog has been shown.
        public static bool OsRequestOwed => PlayerPrefs.GetInt(PrefOsOwed, 0) != 0;

        // Shows the OS permission dialog now. The caller (FlockFiveApp.TickDailyOsRequest)
        // only calls this when the game is idle after a reward.
        public static void RequestOsNow()
        {
            if (PlayerPrefs.GetInt(PrefOsOwed, 0) == 0) return;
            PlayerPrefs.SetInt(PrefOsOwed, 0);
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
            int n = PlayerPrefs.GetInt(PrefAsks, 0);
            if (n < 2) PlayerPrefs.SetInt(PrefAsks, n + 1);
            PlayerPrefs.Save();
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
                var care = CareNow();
                if (care != null)
                    for (int i = 0; i < care.Count; i++)
                        ScheduleIosAt(IosCare + "." + i, "Flock Five", care[i].Value, care[i].Key);
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
                var care = CareNow();
                if (care != null)
                    for (int i = 0; i < care.Count; i++)
                        SendAndroid("Flock Five", care[i].Value, care[i].Key);
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
