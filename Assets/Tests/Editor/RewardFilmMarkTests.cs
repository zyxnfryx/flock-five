#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // The stage-clear 2X button drops "WATCH AD" and draws a film-strip play mark.
    // 820 is the iPad the mark is drawn for first. 402 and 412 are the phones it still has to read on.
    public static class RewardFilmMarkTests
    {
        const string Out = "/tmp/flock-five-reward-film-mark-tests.txt";

        [MenuItem("Flock Five/Reward Film Mark Tests")]
        public static void Run()
        {
            var sb = new System.Text.StringBuilder();
            int fail = 0;
            void Check(string name, bool ok, string detail)
            {
                if (!ok) fail++;
                sb.AppendLine((ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[reward-film] " + (ok ? "PASS " : "FAIL ") + name + " " + detail);
            }

            Check("sprockets-top", FilmPlayMark.SprocketRuns(false) == FilmPlayMark.Sprockets,
                "runs " + FilmPlayMark.SprocketRuns(false));
            Check("sprockets-bottom", FilmPlayMark.SprocketRuns(true) == FilmPlayMark.Sprockets,
                "runs " + FilmPlayMark.SprocketRuns(true));
            Check("arrow-points-right", FilmPlayMark.ArrowPointsRight(), "");
            Check("tape-black", FilmPlayMark.TapeBlack(), "");
            Check("corner-clear", FilmPlayMark.CornerClear(), "");
            Check("holes-clear-of-arrow", FilmPlayMark.HolesClearOfArrow(), "");
            Check("reward-word", FlockFiveApp.RewardCoinsDoubleWord == "2X COINS"
                && FlockFiveApp.RewardDoubleWord == "2X"
                && FlockFiveApp.RewardCollectWord == "Collect", "");

            // Card is about 0.62 of the play column (the 820 still's reward board).
            // The face is the inset DrawPopupButton returns for a wide plate.
            var devs = new[] { (820, 1180), (402, 874), (412, 915) };
            float phoneFilm = 0f;
            float padFilm = 0f;
            foreach (var (sw, sh) in devs)
            {
                var face = RewardFace(sw, sh, 0.62f);
                var full = FlockFiveApp.LayoutFilmOffer(face, 0f);
                var snug = FlockFiveApp.LayoutFilmOffer(face, full.Label.width * 0.45f);
                string tag = sw + "x" + sh;
                float aspect = full.Icon.width / Mathf.Max(1f, full.Icon.height);
                bool inside = Contains(face, full.Icon) && Contains(face, full.Label) && Contains(face, snug.Icon);
                bool ordered = full.Icon.xMax < full.Label.x - 1f && snug.Icon.xMax < snug.Label.x - 1f;
                float innerH = face.height * (1f - 2f * FlockFiveApp.PopupWordPadY);
                bool oneLine = Mathf.Abs(full.Label.height - innerH) < 0.6f;
                float fullMid = (full.Icon.xMin + full.Label.xMax) * 0.5f;
                float snugMid = (snug.Icon.xMin + snug.Label.xMax) * 0.5f;
                bool centered = Mathf.Abs(fullMid - face.center.x) < 0.6f && Mathf.Abs(snugMid - face.center.x) < 0.6f;
                bool sameFilm = Mathf.Abs(snug.Icon.height - full.Icon.height) < 0.2f;
                // "2X COINS" is about 4.8 ems in a bold face. 16px is the floor that still reads on the glass.
                bool wordRoom = full.Label.width >= 16f * 4.8f;
                bool filmReads = full.Icon.height >= 34f;
                Check(tag + " film",
                    inside && ordered && oneLine && centered && sameFilm && wordRoom && filmReads
                    && Mathf.Abs(aspect - FilmPlayMark.Aspect) < 0.02f,
                    "icon " + full.Icon.width.ToString("0.0") + "x" + full.Icon.height.ToString("0.0")
                    + " label " + full.Label.width.ToString("0.0") + "x" + full.Label.height.ToString("0.0")
                    + " face " + face.width.ToString("0.0") + "x" + face.height.ToString("0.0"));
                if (sw == 820) padFilm = full.Icon.height;
                else phoneFilm = Mathf.Max(phoneFilm, full.Icon.height);
            }
            Check("ipad-mark-larger-than-phone", padFilm > phoneFilm + 8f,
                "820 " + padFilm.ToString("0.0") + " phone " + phoneFilm.ToString("0.0"));

            string root = Application.dataPath;
            string choice = File.ReadAllText(Path.Combine(root, "_Project/Scripts/App/FlockFiveApp.RewardChoice.cs"));
            string daily = File.ReadAllText(Path.Combine(root, "_Project/Scripts/App/FlockFiveApp.DailyBonus.cs"));
            string app = File.ReadAllText(Path.Combine(root, "_Project/Scripts/App/FlockFiveApp.cs"));
            Check("watch-ad-line-gone", choice.IndexOf("WATCH AD") < 0 && choice.IndexOf("WatchAdWord") < 0, "");
            Check("reward-draws-coins-word", choice.Contains("DrawOfferChoice(lay, hasAd, priHeld, secHeld, a, s, RewardCollectWord, RewardCoinsDoubleWord)"), "");
            Check("reward-draws-film", choice.Contains("DrawFilmPlayMark(mark.Icon)"), "");
            Check("daily-keeps-short-2x", daily.Contains("DrawOfferChoice(") && daily.IndexOf("RewardCoinsDoubleWord") < 0, "");
            Check("freeze-keeps-plain-play", app.Contains("DrawPlayAdIcon("), "");
            Check("no-ad-unavailable-copy", choice.IndexOf("Ad not available") < 0, "");

            Debug.Log(fail == 0
                ? "[reward-film] DONE"
                : "[reward-film] FAIL " + fail);
        }

        // Wide plate face: 5% side inset, 14% top and bottom, matching DrawPopupButton.
        static Rect RewardFace(int screenW, int screenH, float cardFrac)
        {
            bool pillar = (float)screenW / screenH > DeviceFit.PhoneAspect + 0.03f;
            float col = pillar ? screenH * DeviceFit.PhoneAspect : screenW;
            float s = Mathf.Max(screenH / 720f, 1f);
            float card = col * cardFrac;
            float w = Mathf.Min(card * 0.86f, col * 0.78f);
            var btn = FlockFiveApp.OfferButtons(col * 0.5f, 0f, w, s, true).Primary;
            float ix = btn.width * 0.05f;
            float iy = btn.height * 0.14f;
            return new Rect(btn.x + ix, btn.y + iy, Mathf.Max(4f, btn.width - 2f * ix), Mathf.Max(4f, btn.height - 2f * iy));
        }

        static bool Contains(Rect outer, Rect inner)
        {
            return inner.xMin >= outer.xMin - 0.2f
                && inner.yMin >= outer.yMin - 0.2f
                && inner.xMax <= outer.xMax + 0.2f
                && inner.yMax <= outer.yMax + 0.2f;
        }
    }
}
#endif
