#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 82 pass b82f. One full-screen veil under every popup.
    static class Build82TestsModal
    {
        const float W = 1179f;
        const float H = 2556f;

        [MenuItem("Flock Five/Build 82 Modal Tests")]
        public static void Run()
        {
            int pass = 0;
            int fail = 0;
            void Check(string name, bool ok, string detail)
            {
                if (ok) pass++;
                else fail++;
                Debug.Log("[build82f-modal] " + (ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
            }

            Check("sort", ModalBlock.BlockerSort == ModalBlock.PopupSort - 1,
                "blocker=" + ModalBlock.BlockerSort + " popup=" + ModalBlock.PopupSort);

            var avatar = new Rect(480f, 980f, 160f, 180f);
            var level = new Rect(360f, 1980f, 460f, 220f);
            var rail = new Rect(28f, 640f, 140f, 140f);
            var gear = new Rect(1020f, 70f, 110f, 110f);
            var controls = new[] { avatar, level, rail, gear };
            var names = new[] { "avatar", "level", "rail", "gear" };
            var popups = ModalBlock.Popups();
            bool sawAtt = false;
            bool sawConsent = false;
            bool blocked = true;
            bool released = true;
            string blockDetail = "n=" + popups.Length;
            for (int p = 0; p < popups.Length; p++)
            {
                var id = popups[p];
                if (id == ModalBlock.Id.Att) sawAtt = true;
                if (id == ModalBlock.Id.Consent) sawConsent = true;
                if (id == ModalBlock.Id.Lesson || id == ModalBlock.Id.None)
                {
                    blocked = false;
                    blockDetail = id + " is not a popup";
                    break;
                }
                var veil = ModalBlock.Raise(id, W, H);
                if (!veil.Open || veil.Sort != ModalBlock.BlockerSort || veil.Owner != id)
                {
                    blocked = false;
                    blockDetail = id + " veil sort=" + veil.Sort;
                    break;
                }
                for (int c = 0; c < controls.Length; c++)
                {
                    if (ModalBlock.Tap(veil, controls[c]))
                    {
                        blocked = false;
                        blockDetail = id + " let " + names[c] + " through";
                        break;
                    }
                }
                if (!blocked) break;
                var down = ModalBlock.Dismiss(veil);
                for (int c = 0; c < controls.Length; c++)
                {
                    if (!ModalBlock.Tap(down, controls[c]))
                    {
                        released = false;
                        blockDetail = id + " still blocks " + names[c];
                        break;
                    }
                }
                if (!released) break;
            }
            Check("popups-block", blocked && released && sawAtt && sawConsent && popups.Length >= 16, blockDetail);

            string root = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Assets/_Project/Scripts");
            string modal = File.ReadAllText(Path.Combine(root, "App/FlockFiveApp.Modal.cs"));
            string app = File.ReadAllText(Path.Combine(root, "App/FlockFiveApp.cs"));
            string settings = File.ReadAllText(Path.Combine(root, "App/FlockFiveApp.Settings.cs"));
            string daily = File.ReadAllText(Path.Combine(root, "App/FlockFiveApp.DailyBonus.cs"));
            string vip = File.ReadAllText(Path.Combine(root, "App/FlockFiveApp.VipOffer.cs"));
            string clear = File.ReadAllText(Path.Combine(root, "App/FlockFiveApp.ClearAds.cs"));
            bool open = modal.Contains("ModalBlock.Id.Att")
                && modal.Contains("ModalBlock.Id.Consent")
                && modal.Contains("ModalBlock.Id.Settings")
                && modal.Contains("ModalBlock.Id.DailyAsk")
                && modal.Contains("ModalBlock.Id.Daily")
                && modal.Contains("ModalBlock.Id.Welcome")
                && modal.Contains("ModalBlock.Id.VipSoft")
                && modal.Contains("ModalBlock.Id.Vip")
                && modal.Contains("ModalBlock.Id.ClearDouble")
                && modal.Contains("ModalBlock.Id.Streak")
                && modal.Contains("ModalBlock.Id.AvatarRename")
                && modal.Contains("ModalBlock.Id.Gift")
                && modal.Contains("ModalBlock.Id.Restart")
                && modal.Contains("ModalBlock.Id.ClearReward")
                && modal.Contains("ModalBlock.Id.Adopt")
                && modal.Contains("ModalBlock.Id.Inspect")
                && modal.Contains("ModalBlock.Id.PokerPay")
                && modal.Contains("ActiveVeil");
            bool paint = settings.Contains("ModalBlock.Id.Att")
                && settings.Contains("ModalBlock.Paint(")
                && settings.Contains("ModalBlock.Id.Consent")
                && settings.Contains("ModalBlock.Paint(ModalBlock.Id.Settings")
                && settings.Contains("SwallowRest")
                && daily.Contains("ModalBlock.Paint(ModalBlock.Id.Daily,")
                && daily.Contains("ModalBlock.Paint(ModalBlock.Id.DailyAsk")
                && daily.Contains("ModalBlock.Paint(ModalBlock.Id.Welcome")
                && vip.Contains("ModalBlock.Paint(ModalBlock.Id.Vip,")
                && vip.Contains("ModalBlock.Paint(ModalBlock.Id.VipSoft")
                && clear.Contains("ModalBlock.Paint(ModalBlock.Id.ClearDouble")
                && app.Contains("ModalBlock.Paint(ModalBlock.Id.Restart")
                && app.Contains("ModalBlock.Paint(ModalBlock.Id.ClearReward")
                && app.Contains("ModalBlock.Paint(ModalBlock.Id.Gift")
                && app.Contains("ModalBlock.Paint(ModalBlock.Id.Inspect")
                && app.Contains("ModalBlock.Paint(ModalBlock.Id.PokerPay");
            bool wired = app.Contains("VeilBlocks(") && app.Contains("VeilDrops(")
                && settings.Contains("VeilBlocks(")
                && app.IndexOf("AdConsent.BlocksControl", System.StringComparison.Ordinal) < 0;
            Check("openers", open && paint && wired,
                "open=" + open + " paint=" + paint + " wired=" + wired);

            Debug.Log("[build82f-modal] " + (fail == 0 ? "BUILD82F_MODAL_OK " + pass : "BUILD82F_MODAL_FAIL " + fail));
            if (fail > 0) throw new System.Exception("BUILD82F_MODAL_FAIL " + fail);
        }
    }
}
#endif
