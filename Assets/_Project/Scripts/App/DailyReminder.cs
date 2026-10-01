using System;
using System.Globalization;
using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR
using Unity.Notifications.iOS;
#endif
#if UNITY_ANDROID && !UNITY_EDITOR
using Unity.Notifications.Android;
#endif

namespace FlockFive
{
    // Local reminder for the daily bonus. com.unity.mobile.notifications 2.4.x.
    // Editor and desktop builds compile the same methods with no native calls.
    // iOS permission is requested only after the first claim, not at launch.
    public static class DailyReminder
    {
        const string PrefAsked = "flockfive.daily.asked";
        const string PrefNoteReady = "flockfive.daily.noteReady";
        const string PrefNoteKeep = "flockfive.daily.noteKeep";
        const string ReadyBody = "Your daily coins are ready.";
        const string IosReady = "flockfive.daily.ready";
        const string IosKeep = "flockfive.daily.keep";
        const string ChannelId = "flockfive_daily";

        static int _armStreak;
        static bool _armRisk;
        static bool _armClaimed;

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

        public static void Reschedule(int streak, bool claimedToday, bool atRisk)
        {
            if (streak < 0) streak = 0;
            _armStreak = streak;
            _armClaimed = claimedToday;
            _armRisk = atRisk && streak >= 2;
#if UNITY_IOS && !UNITY_EDITOR
            ScheduleIos();
#elif UNITY_ANDROID && !UNITY_EDITOR
            ScheduleAndroid();
#else
            // No notification assembly in the editor or on desktop. Read the armed
            // reminder so those builds still compile the same fields.
            Plan(out _, out _, out _);
#endif
        }

        // First successful claim only. Later opens reschedule without asking again.
        public static void AskAfterFirstClaim(int streak, bool claimedToday, bool atRisk)
        {
            if (streak < 0) streak = 0;
            _armStreak = streak;
            _armClaimed = claimedToday;
            _armRisk = atRisk && streak >= 2;
#if UNITY_IOS && !UNITY_EDITOR
            if (PlayerPrefs.GetInt(PrefAsked, 0) == 0)
            {
                PlayerPrefs.SetInt(PrefAsked, 1);
                PlayerPrefs.Save();
                try
                {
                    _iosAuth = new AuthorizationRequest(AuthorizationOption.Alert | AuthorizationOption.Sound, false);
                    return;
                }
                catch (Exception) { }
            }
#elif UNITY_ANDROID && !UNITY_EDITOR
            if (PlayerPrefs.GetInt(PrefAsked, 0) == 0)
            {
                PlayerPrefs.SetInt(PrefAsked, 1);
                PlayerPrefs.Save();
                try
                {
                    _androidAuth = new PermissionRequest();
                    return;
                }
                catch (Exception) { }
            }
#endif
            Reschedule(_armStreak, _armClaimed, _armRisk);
        }

        public static void Tick()
        {
#if UNITY_IOS && !UNITY_EDITOR
            if (_iosAuth != null && _iosAuth.IsFinished)
            {
                bool granted = _iosAuth.Granted;
                _iosAuth.Dispose();
                _iosAuth = null;
                if (granted) ScheduleIos();
            }
#elif UNITY_ANDROID && !UNITY_EDITOR
            if (_androidAuth != null && _androidAuth.Status != PermissionStatus.RequestPending)
            {
                bool granted = _androidAuth.Status == PermissionStatus.Allowed;
                _androidAuth = null;
                if (granted) ScheduleAndroid();
            }
#endif
        }

        // After today's claim, both alerts are tomorrow (6pm coins, 8pm streak).
        // If today's coins are still unclaimed, use the next 6pm that has not passed,
        // and only keep the streak line when that evening is still today.
        static void Plan(out DateTime coins, out DateTime keep, out bool doKeep)
        {
            if (_armClaimed)
            {
                coins = DayAt(1, 18);
                keep = DayAt(1, 20);
                doKeep = _armRisk;
                return;
            }
            coins = NextFuture(18);
            keep = NextFuture(20);
            var now = DateTime.Now;
            doKeep = _armRisk && keep.Year == now.Year && keep.Month == now.Month && keep.Day == now.Day;
        }

        static DateTime DayAt(int daysAhead, int hour)
        {
            var now = DateTime.Now;
            return new DateTime(now.Year, now.Month, now.Day, hour, 0, 0, DateTimeKind.Local).AddDays(daysAhead);
        }

        static DateTime NextFuture(int hour)
        {
            var slot = DayAt(0, hour);
            if (slot <= DateTime.Now) slot = slot.AddDays(1);
            return slot;
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
                Plan(out var coinsAt, out var keepAt, out bool doKeep);
                ScheduleIosAt(IosReady, "Daily bonus", ReadyBody, coinsAt);
                if (doKeep)
                {
                    string keep = "Keep your " + _armStreak.ToString(CultureInfo.InvariantCulture) + " day streak!";
                    ScheduleIosAt(IosKeep, "Daily bonus", keep, keepAt);
                }
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
            trigger.Minute = 0;
            trigger.Second = 0;
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
                Plan(out var coinsAt, out var keepAt, out bool doKeep);
                int ready = SendAndroid("Daily bonus", ReadyBody, coinsAt);
                PlayerPrefs.SetInt(PrefNoteReady, ready + 1);
                if (doKeep)
                {
                    string keep = "Keep your " + _armStreak.ToString(CultureInfo.InvariantCulture) + " day streak!";
                    int id = SendAndroid("Daily bonus", keep, keepAt);
                    PlayerPrefs.SetInt(PrefNoteKeep, id + 1);
                }
                else PlayerPrefs.SetInt(PrefNoteKeep, 0);
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
