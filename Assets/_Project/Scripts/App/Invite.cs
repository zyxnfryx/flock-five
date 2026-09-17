using System;
using System.Globalization;
using System.Runtime.InteropServices;
using UnityEngine;

namespace FlockFive
{
    // One-tap share. Native sheet carries a unique hive code so a friend
    // can be credited without typing. Referrer thank-you is local and daily-capped
    // until a real install attribution backend exists.
    public static class Invite
    {
        const string PrefCode = "flockfive.invite.me";
        const string PrefClaimed = "flockfive.invite.claimed";
        const string PrefShareDay = "flockfive.invite.share.day";
        const string PrefShareN = "flockfive.invite.share.n";
        public const int SharePay = 8;
        public const int WelcomePay = 24;
        public const int DailyShareCap = 3;
        const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

        public static string Code { get; private set; }

        public static void Warm()
        {
            if (string.IsNullOrEmpty(Code))
            {
                Code = PrefGuard.GetString(PrefCode, "");
                if (Code.Length != 6)
                {
                    Code = Mint();
                    PrefGuard.SetString(PrefCode, Code);
                    PlayerPrefs.Save();
                }
            }
            TryClaimClipboard();
        }

        public static string Blurb()
        {
            int n = Hive.Found;
            string hive = n > 0 ? n + " bees in my hive" : "a new garden";
            return "Come play Flock Five with me — " + hive + ". "
                 + "Open the game and use hive code " + Code + " and we both get coins. "
                 + "flockfive://r/" + Code;
        }

        public static void Share()
        {
            Warm();
            string text = Blurb();
#if UNITY_IOS && !UNITY_EDITOR
            FlockFive_Share(text);
#elif UNITY_EDITOR
            GUIUtility.systemCopyBuffer = text;
            OnShared();
#else
            GUIUtility.systemCopyBuffer = text;
            OnShared();
#endif
        }

        public static void OnShared()
        {
            string today = DateTime.UtcNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            string last = PrefGuard.GetString(PrefShareDay, "");
            int n = last == today ? PrefGuard.GetInt(PrefShareN, 0) : 0;
            if (n >= DailyShareCap) return;
            PrefGuard.SetString(PrefShareDay, today);
            PrefGuard.SetInt(PrefShareN, n + 1);
            Purse.Credit(SharePay);
            Sfx.Clink();
        }

        public static bool TryClaim(string raw)
        {
            Warm();
            string code = Normalize(raw);
            if (code.Length != 6) return false;
            if (code == Code) return false;
            if (PrefGuard.GetInt(PrefClaimed, 0) != 0) return false;
            PrefGuard.SetInt(PrefClaimed, 1);
            PrefGuard.SetString("flockfive.invite.from", code);
            Purse.Credit(WelcomePay);
            PlayerPrefs.Save();
            Sfx.Clink();
            return true;
        }

        static void TryClaimClipboard()
        {
            string clip = "";
            try { clip = GUIUtility.systemCopyBuffer; }
            catch (Exception) { return; }
            if (string.IsNullOrEmpty(clip)) return;
            string code = Extract(clip);
            if (code.Length == 6) TryClaim(code);
        }

        static string Extract(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            string u = text.ToUpperInvariant();
            int at = u.IndexOf("FLOCKFIVE://R/", StringComparison.Ordinal);
            if (at >= 0)
                return Normalize(u.Substring(at + "FLOCKFIVE://R/".Length, Math.Min(8, u.Length - at - "FLOCKFIVE://R/".Length)));
            at = u.IndexOf("HIVE CODE ", StringComparison.Ordinal);
            if (at >= 0)
                return Normalize(u.Substring(at + "HIVE CODE ".Length, Math.Min(8, u.Length - at - "HIVE CODE ".Length)));
            return "";
        }

        static string Normalize(string s)
        {
            if (s == null) return "";
            var buf = new char[6];
            int n = 0;
            for (int i = 0; i < s.Length && n < 6; i++)
            {
                char c = char.ToUpperInvariant(s[i]);
                if (c == '0') c = 'O';
                if (c == '1') c = 'I';
                if (Alphabet.IndexOf(c) < 0) continue;
                buf[n++] = c;
            }
            return n == 6 ? new string(buf) : "";
        }

        static string Mint()
        {
            var buf = new char[6];
            int seed = unchecked(Environment.TickCount * 1103515245 + UnityEngine.Random.Range(1, 99991));
            for (int i = 0; i < 6; i++)
            {
                seed = unchecked(seed * 1664525 + 1013904223);
                buf[i] = Alphabet[(seed >> 8) & 31];
            }
            return new string(buf);
        }

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        static extern void FlockFive_Share(string text);
#endif
    }
}

