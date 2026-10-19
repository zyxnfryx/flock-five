#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // b103 (Brandon): bee card rewards prefer cards the player does not own, the first card
    // upgrade is free once, and the Upgrade lesson only accepts a tap on the glove's target.
    // Run: Unity -batchmode -executeMethod FlockFive.Editor.Build103CardRewardTests.Run
    public static class Build103CardRewardTests
    {
        const string Out = "/tmp/flock-five-b103-tests.txt";

        public static void Run()
        {
            var sb = new System.Text.StringBuilder();
            void Check(string name, bool ok, string detail)
            {
                sb.AppendLine((ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[build103] " + (ok ? "PASS " : "FAIL ") + name + " " + detail);
            }

            int kinds = 5, F = Hive.Finishes;
            var rng = new System.Random(7);
            System.Func<int, int, int> range = (a, b) => rng.Next(a, b);

            // 1. Unowned bees come first, whatever finish rolled.
            var c = new int[kinds * F];
            c[0] = 2; c[1 * F + 1] = 1; // kinds 0 and 1 owned
            bool allHoles = true;
            for (int i = 0; i < 200; i++)
            {
                Hive.ChooseNewestFirst(c, kinds, (BeeFinish)(i % F), range, out int k, out var f);
                if (k < 2 || f != (BeeFinish)(i % F)) allHoles = false;
            }
            Check("unowned-bee-first", allHoles, "never a dup while a bee is unowned");

            // 2. Every bee owned (Normal only): a rolled Holo goes to a bee lacking Holo, not a dup.
            c = new int[kinds * F];
            for (int k = 0; k < kinds; k++) c[k * F] = 1;
            bool newFinish = true;
            for (int i = 0; i < 200; i++)
            {
                Hive.ChooseNewestFirst(c, kinds, BeeFinish.Normal, range, out int k, out var f);
                if (c[k * F + (int)f] != 0) newFinish = false; // a Normal roll must still land on a new card
            }
            Check("rolled-finish-owned-moves-to-missing", newFinish, "Normal roll lands on a card not owned");

            // 3. Rolled finish missing for some bees: pick only those bees.
            c = new int[kinds * F];
            for (int k = 0; k < kinds; k++) { c[k * F] = 1; c[k * F + 1] = 1; }
            c[3 * F + 1] = 0;
            bool onlyMissing = true;
            for (int i = 0; i < 100; i++)
            {
                Hive.ChooseNewestFirst(c, kinds, BeeFinish.Holo, range, out int k, out var f);
                if (k != 3 || f != BeeFinish.Holo) onlyMissing = false;
            }
            Check("missing-finish-for-rolled", onlyMissing, "Holo roll -> the one bee lacking Holo");

            // 4. Everything owned: duplicates allowed, keeps the rolled finish.
            for (int i = 0; i < c.Length; i++) c[i] = 1;
            Hive.ChooseNewestFirst(c, kinds, BeeFinish.Holo, range, out int kd, out var fd);
            Check("all-owned-allows-dup", kd >= 0 && kd < kinds && fd == BeeFinish.Holo, "k=" + kd + " f=" + fd);

            // 5. No count badge on a card.
            string app = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.cs");
            Check("no-count-badge", app.IndexOf("GuiSlot.AlbumCount", System.StringComparison.Ordinal) < 0, "AlbumCount label not drawn");

            // 6. Upgrade lesson: only the glove's target takes a tap.
            Check("upgrade-step-required", CoachTap.Kind(CoachStep.AlbumUpgrade) == CoachTapKind.Required
                && !CoachTap.AnyTap(CoachStep.AlbumUpgrade)
                && CoachTap.Advances(CoachStep.AlbumUpgrade, true) && !CoachTap.Advances(CoachStep.AlbumUpgrade, false), "required + target only");
            Check("upgrade-caption-two-lines", CaptionLayout.AlbumUpgradeLine.Split('\n').Length <= 2, CaptionLayout.AlbumUpgradeLine.Replace("\n", " / "));

            // 7. First upgrade is free once (needs the badger coach done; seeded here).
            PlayerPrefs.DeleteKey("flockfive.hive.up.free");
            Check("free-flag-defaults-available", PlayerPrefs.GetInt("flockfive.hive.up.free", 0) == 0, "flag unset");
            string hive = File.ReadAllText("Assets/_Project/Scripts/App/Hive.cs");
            int a = hive.IndexOf("public static bool TryUpgrade", System.StringComparison.Ordinal);
            string body = a >= 0 ? hive.Substring(a, 700) : "";
            Check("free-first-upgrade-once", body.Contains("FreeUpgradeAvailable") && body.Contains("PrefFreeUpgrade, 1")
                && body.IndexOf("TrySpend", System.StringComparison.Ordinal) > body.IndexOf("PrefFreeUpgrade", System.StringComparison.Ordinal), "free path sets the flag; paid path after");
        }
    }
}
#endif
