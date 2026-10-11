#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 83 pass b83a. Eased gardens 18-25: kit maps, the extra empty on 22 and 24,
    // the regenerated garden 23, and the tangle band.
    [InitializeOnLoad]
    public static class Build83Tests
    {
        const string Cmd = "/tmp/flock-five-b83-tests";
        const string Out = "/tmp/flock-five-b83-tests.txt";
        const string DiscoverOut = "/tmp/flock-five-b83-discover.txt";

        static readonly string[] Kits =
        {
            "NBB/BBCBC/CCCCB/CCCBC/BBCBB/NCC/CCCC/NCN/CNNNB/BNNBN/NNNNC/NNBBN/NNN/BBBB",
            "BBBCN/BBBNC/NCCB/BNBB/BBBN/NCCN/BBBB/CCCNN/BCCCC/BCCN/CNNN/NNNC/BCCC/NNNN",
            "N/CCCCB/NCBBB/NNNNN/CBBB/CBCBB/CNBNB/B/NNNNN/BCCCN/CCCCN/CCCC/CCCNC/CBNNN",
            "NNCNC/CCB/NBCCB/CCC/NBBB/CCCBB/BNCNC/NNBCC/BNN/CNNBC/BBB/NNBN/BBBNB/CNCNN",
            "CNNCC/BBBBB/CCCNN/NNNCB/BBCCC/NBNBC///NNNNC/CCNCC/CBBCN/NNNCN/CBBBB/BBBBN//",
            "",
            "NCBCN/CBBNB/NCNCB/BCBCC/NCCC/NBB/BBC//NCCCN/NNNNB/NBCBB/CCBNN/CNBB/NNN/BCB/",
            "NNBN/CNCB/BBCN/BBBB/BNNBB/CCCC/CCNCB/CBNN/NBNN/NNNC/NNNN/NCCBN/BBBB/NNBNC"
        };

        // FNV-1a of GardenSolve.Key for gardens that build 83 does not retune.
        static readonly string[] ReplayHash =
        {
            "1 c0c0c0c0c0c0c0c0"
        };

        static Build83Tests()
        {
            EditorApplication.delayCall += Maybe;
            EditorApplication.update += Tick;
        }

        static void Tick()
        {
            if (File.Exists(Cmd)) Maybe();
        }

        static void Maybe()
        {
            if (!File.Exists(Cmd)) return;
            if (EditorApplication.isCompiling) return;
            try { File.Delete(Cmd); }
            catch { return; }
            Run();
        }

        [MenuItem("Flock Five/Build 83 Tests")]
        public static void Run()
        {
            var sb = new StringBuilder();
            int pass = 0;
            int fail = 0;
            void Check(string name, bool ok, string detail)
            {
                string line = (ok ? "PASS  " : "FAIL  ") + name + "  " + detail;
                if (ok) pass++;
                else fail++;
                sb.AppendLine(line);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[build83] " + line);
            }

            CheckEase(Check);
            string end = fail == 0 ? "BUILD83_OK " + pass : "BUILD83_FAIL " + fail + " passed " + pass;
            sb.AppendLine(end);
            File.WriteAllText(Out, sb.ToString());
            Debug.Log("[build83] " + end);
        }

        // Gardens 18-25 use the maps Level Design proved in its C# harness (winnable
        // without the gift, trap kept, 33-55% band; 23 at 28.3%). Here: winnability,
        // kit rules and shape only, capped search, no open-ended trap probe.
        static void CheckEase(System.Action<string, bool, string> Check)
        {
            Check("ease-count", LevelData.Count >= 29, "count " + LevelData.Count);
            int easeLimbs = 0;
            for (int n = 18; n <= 25; n++)
            {
                int idx = IndexOf(n);
                var lv = idx >= 0 ? LevelData.Peek(idx) : null;
                if (lv == null)
                {
                    Check("ease-" + n, false, "missing");
                    continue;
                }
                var b = LevelData.Open(idx);
                string want = Kits[n - 18];
                string have = lv.Kits ?? "";
                bool map = have == want;
                string fit = KitFit(lv, b);
                var sets = BoardValidator.CheckKitSets(b);
                var bare = GardenSolve.Search(b, TrapProbe.NodeCap);
                var gift = GardenSolve.Search(Unlock(b), TrapProbe.NodeCap);
                int play = PlayLimbs(b);
                int wantPlay = (n == 22 || n == 24) ? 16 : 14;
                bool shape = play == wantPlay && BonusLimbs(b) == 2;
                if (n == 23)
                    shape = shape && b.Branches.Count > 0 && b.Branches[0].Count == 1
                        && b.Branches[0].Birds[0].Color == BirdColor.Teal
                        && b.Live[0] == BirdColor.Gold && b.Live[1] == BirdColor.Ruby;
                bool win = bare.Outlook == GardenSolve.Outlook.Winnable
                    && gift.Outlook == GardenSolve.Outlook.Winnable;
                Check("ease-" + n,
                    map && fit == "ok" && sets.Ok && shape && win,
                    "map " + map + " fit " + fit + " sets " + sets.Message
                    + " play " + play + " bare " + bare.Outlook + " gift " + gift.Outlook);
                easeLimbs += play;
            }
            Check("ease-22-24-longer", easeLimbs == 14 * 6 + 16 * 2, "play limbs summed " + easeLimbs);

        }

        static string FormatProof(int level, TrapProbe.ProofHit proof)
        {
            var sb = new StringBuilder();
            sb.Append("new Proof(").Append(level).Append(", \"").Append(proof.Path).Append("\", ");
            sb.Append(proof.From).Append(", ").Append(proof.To).Append(", new[] { ");
            for (int i = 0; i < proof.Alt.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(proof.Alt[i]);
            }
            sb.Append(" }),");
            return sb.ToString();
        }

        static string KitFit(Level lv, Board b)
        {
            if (lv == null || b == null) return "missing";
            if (string.IsNullOrEmpty(lv.Kits))
            {
                int play = PlayLimbs(b);
                return play > 0 ? "ok" : "no limbs";
            }
            var parts = lv.Kits.Split('/');
            int g = 0;
            for (int i = 0; i < b.Branches.Count; i++)
            {
                var br = b.Branches[i];
                if (br != null && br.IsBonus) continue;
                if (g >= parts.Length) return "short map at limb " + g;
                if (parts[g].Length != (br == null ? 0 : br.Count))
                    return "limb " + g + " letters " + parts[g].Length + " birds " + (br == null ? 0 : br.Count);
                g++;
            }
            if (g != parts.Length) return "groups " + parts.Length + " play " + g;
            return "ok";
        }

        static int PlayLimbs(Board b)
        {
            int n = 0;
            for (int i = 0; i < b.Branches.Count; i++)
                if (b.Branches[i] != null && !b.Branches[i].IsBonus) n++;
            return n;
        }

        static int BonusLimbs(Board b)
        {
            int n = 0;
            for (int i = 0; i < b.Branches.Count; i++)
                if (b.Branches[i] != null && b.Branches[i].IsBonus) n++;
            return n;
        }

        static string Tip(Board b)
        {
            if (b.Branches.Count == 0 || b.Branches[0].Count == 0) return "empty0";
            return "b0=" + b.Branches[0].Birds[0].Color + "x" + b.Branches[0].Count
                + " live " + (b.Live[0].HasValue ? b.Live[0].Value.ToString() : "-")
                + "/" + (b.Live[1].HasValue ? b.Live[1].Value.ToString() : "-");
        }

        static Board Unlock(Board b)
        {
            var n = b.Clone();
            for (int i = 0; i < n.Branches.Count; i++) n.Branches[i].AdLocked = false;
            return n;
        }

        static int IndexOf(int number)
        {
            for (int i = 0; i < LevelData.Count; i++)
                if (LevelData.Peek(i).Number == number) return i;
            return -1;
        }

        static string Hash(string s)
        {
            ulong h = 14695981039346656037UL;
            if (s != null)
            {
                for (int i = 0; i < s.Length; i++)
                {
                    h ^= s[i];
                    h *= 1099511628211UL;
                }
            }
            return h.ToString("x16");
        }
    }
}
#endif
