using UnityEngine;

namespace FlockFive
{
    public struct BeeKind
    {
        public string Id;
        public string Name;
        public string Front;   // short face-side blurb
        public string Back;    // reverse-side groaner
        public Color Tint;
        public BeeKind(string id, string name, string front, string back, Color tint)
        {
            Id = id;
            Name = name;
            Front = front;
            Back = back;
            Tint = tint;
        }
    }

    public struct BeeVisit
    {
        public BeeKind Kind;
        public bool Fresh;
        public int Count;
    }

    public static class Hive
    {
        const string Pref = "flockfive.hive.v1";
        const char Pair = ';';
        const char Kv = ':';

        // Pun density: high. Emotional damage: intentional.
        public static readonly BeeKind[] Roster =
        {
            new BeeKind("honey", "Honey Gold",
                "Sweetest sting in the yard.",
                "The gold standard of buzzness. Diversified in nectar futures.",
                new Color(1.00f, 0.78f, 0.22f)),
            new BeeKind("ginger", "Garden Ginger",
                "Spicy little garden pest.",
                "Root of all buzzness. Leaves a warm aftertaste and a welt.",
                new Color(0.92f, 0.48f, 0.18f)),
            new BeeKind("dusk", "Dusk Stripe",
                "Works the late shift.",
                "Night owl with a day job: pollen. Clock out at moonrise.",
                new Color(0.42f, 0.26f, 0.12f)),
            new BeeKind("clover", "Clover Nap",
                "Lucky and sleepy.",
                "Four leaves, zero alarms. Dreams in soft landings.",
                new Color(0.48f, 0.72f, 0.24f)),
            new BeeKind("pollen", "Pollen Fog",
                "Hard to see, easy to sneeze.",
                "Allergy season's MVP. Comes with free congestion.",
                new Color(0.96f, 0.88f, 0.52f)),
            new BeeKind("moon", "Moon Dust",
                "Lunar powered.",
                "One small step for bee, one giant leap for buzzkind.",
                new Color(0.72f, 0.78f, 0.92f)),
            new BeeKind("rust", "Rust Belt",
                "Industrial strength.",
                "Still making honey in a tough hive market. Union dues: pollen.",
                new Color(0.74f, 0.30f, 0.14f)),
            new BeeKind("ink", "Ink Band",
                "Writes its own buzz.",
                "Black-and-yellow press. Extra! Extra! Read all about the comb!",
                new Color(0.20f, 0.16f, 0.14f)),
            new BeeKind("amber", "Amber Nap",
                "Preserved energy.",
                "Stuck in a good way. Fossilized FOMO optional.",
                new Color(0.95f, 0.62f, 0.16f)),
            new BeeKind("thistle", "Thistle",
                "Prickly but popular.",
                "Handles rejection well — and spines. Soft hearts, hard edges.",
                new Color(0.56f, 0.38f, 0.72f)),
            new BeeKind("dew", "Dew Cap",
                "Morning person.",
                "Keeps it fresh. Literally. Condensation is a lifestyle.",
                new Color(0.52f, 0.82f, 0.76f)),
            new BeeKind("coal", "Coal Stripe",
                "Old-school grind.",
                "Burns the midnight pollen. Ashtrays not included.",
                new Color(0.30f, 0.24f, 0.18f)),
        };

        static int[] _counts;
        static bool _ready;

        public static int Kinds => Roster.Length;
        public static int Found => CountFound();
        public static int Visitors => CountAll();

        public static int CountOf(int i)
        {
            Warm();
            if ((uint)i >= (uint)_counts.Length) return 0;
            return _counts[i];
        }

        public static BeeVisit TakeVisitor()
        {
            Warm();
            int pick = Pick();
            bool fresh = _counts[pick] == 0;
            _counts[pick]++;
            Save();
            return new BeeVisit { Kind = Roster[pick], Fresh = fresh, Count = _counts[pick] };
        }

        static int Pick()
        {
            int holes = 0;
            for (int i = 0; i < _counts.Length; i++)
                if (_counts[i] == 0) holes++;
            if (holes > 0 && Random.value < 0.74f)
            {
                int skip = Random.Range(0, holes);
                for (int i = 0; i < _counts.Length; i++)
                {
                    if (_counts[i] != 0) continue;
                    if (skip == 0) return i;
                    skip--;
                }
            }
            return Random.Range(0, _counts.Length);
        }

        static int CountFound()
        {
            Warm();
            int n = 0;
            for (int i = 0; i < _counts.Length; i++)
                if (_counts[i] > 0) n++;
            return n;
        }

        static int CountAll()
        {
            Warm();
            int n = 0;
            for (int i = 0; i < _counts.Length; i++) n += _counts[i];
            return n;
        }

        static void Warm()
        {
            if (_ready && _counts != null && _counts.Length == Roster.Length) return;
            _counts = new int[Roster.Length];
            _ready = true;
            var raw = PlayerPrefs.GetString(Pref, "");
            if (string.IsNullOrEmpty(raw)) return;
            var parts = raw.Split(Pair);
            for (int p = 0; p < parts.Length; p++)
            {
                var bit = parts[p];
                int cut = bit.IndexOf(Kv);
                if (cut <= 0) continue;
                var id = bit.Substring(0, cut);
                if (!int.TryParse(bit.Substring(cut + 1), out int n) || n <= 0) continue;
                int i = IndexOf(id);
                if (i >= 0) _counts[i] = n;
            }
        }

        static int IndexOf(string id)
        {
            for (int i = 0; i < Roster.Length; i++)
                if (Roster[i].Id == id) return i;
            return -1;
        }

        static void Save()
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < Roster.Length; i++)
            {
                if (_counts[i] <= 0) continue;
                if (sb.Length > 0) sb.Append(Pair);
                sb.Append(Roster[i].Id).Append(Kv).Append(_counts[i]);
            }
            PlayerPrefs.SetString(Pref, sb.ToString());
            PlayerPrefs.Save();
        }
    }
}
