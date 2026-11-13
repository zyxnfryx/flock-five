#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 116 TestFlight report, garden 51: "I push the button to watch the video a bunch of
    // times and it still doesn't give me a branch." Garden 51 loads ladder entry 13 (nectar-pinch,
    // 50 % 38 = 12). Once both bottom gifts are spent, the freeze card must not offer the
    // "Watch Ad to Continue + Bonus Branch" bar, because the grant has nothing left to open.
    static class Build116L51FreezeGiftTests
    {
        [MenuItem("Flock Five/Build 116 L51 Freeze Gift Tests")]
        public static void Run()
        {
            int pass = 0, fail = 0;
            void Check(string name, bool ok, string detail)
            {
                if (ok) pass++; else fail++;
                Debug.Log("[b116-l51] " + (ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
            }

            var b = LevelData.Open(50);
            Check("L51 loads nectar-pinch", LevelData.DisplayNumber == 51 && LevelData.Current.Id == "nectar-pinch",
                LevelData.Current.Id + " disp " + LevelData.DisplayNumber);
            Check("L51 has two locked gifts", BonusBranches.CountOn(b) == 2, "gifts " + BonusBranches.CountOn(b));
            Check("L51 winnable with no gift", GardenSolve.Look(b.Clone(), 200000) == GardenSolve.Outlook.Winnable, "");

            var claimed = new bool[2];
            Check("fresh board offers continue", BonusBranches.ContinueOffered(b, claimed), "");

            int n1 = RewardEffects.Branch(b, claimed, -1, out int i1);
            Check("first ad opens a branch", n1 == 1 && i1 >= 0 && !b.Branches[i1].AdLocked, "n " + n1 + " i " + i1);
            Check("one gift left still offers continue", BonusBranches.ContinueOffered(b, claimed), "");

            int n2 = RewardEffects.Branch(b, claimed, -1, out int i2);
            Check("second ad opens the other branch", n2 == 1 && i2 >= 0 && i2 != i1, "n " + n2 + " i " + i2);

            // The bug: this third grant used to be sold by the freeze card and paid nothing.
            int n3 = RewardEffects.Branch(b, claimed, -1, out int i3);
            Check("third ad has nothing to open", n3 == 0 && i3 < 0, "n " + n3 + " i " + i3);
            Check("both spent: continue not offered", !BonusBranches.ContinueOffered(b, claimed), "");

            // One claimed + one lightning-burned is the same dead end.
            var b2 = LevelData.Open(50);
            var c2 = new bool[2];
            RewardEffects.Branch(b2, c2, -1, out _);
            int burn = LightningStrike.Target(b2, c2, null);
            if (burn >= 0) LightningStrike.Burn(b2.Branches[burn]);
            Check("claimed + burned: continue not offered", burn >= 0 && !BonusBranches.ContinueOffered(b2, c2), "burn " + burn);

            // Retry keeps the earned gifts open (ApplyClaims), so a restart is a real way out.
            var r = LevelData.Open(50);
            BonusBranches.ApplyClaims(r, claimed);
            int open = 0;
            for (int i = 0; i < r.Branches.Count; i++)
                if (r.Branches[i].IsBonus && !r.Branches[i].AdLocked) open++;
            Check("retry keeps both earned branches", open == 2, "open " + open);

            Debug.Log("[b116-l51] " + pass + " passed, " + fail + " failed");
        }
    }
}
#endif
