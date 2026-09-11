using UnityEngine;

namespace FlockFive
{
    public enum BeeFinish
    {
        Normal = 0,
        Holo = 1,
        InverseRainbow = 2,
    }

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
        public BeeFinish Finish;
        public bool Fresh;
        public int Count;
    }

    public static class Hive
    {
        const string PrefV1 = "flockfive.hive.v1";
        const string Pref = "flockfive.hive.v2";
        const char Pair = ';';
        const char Kv = ':';
        public const int Finishes = 3;

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
            // Batch A — Chief of Puns approved (12 Keep / 6 Punch up)
            new BeeKind("lavender", "Lavender Buzz",
                "Soft purple, hard sting.",
                "Essential oils, optional attitude. Smells like calm; acts like overtime.",
                new Color(0.62f, 0.48f, 0.86f)),
            new BeeKind("rose", "Rose Petal",
                "Looks delicate. Isn't.",
                "Thorn-adjacent and proud. Petals for days, patience for none.",
                new Color(0.88f, 0.32f, 0.42f)),
            new BeeKind("sunflower", "Sunflower Sid",
                "Faces the light. Judging you.",
                "Tall, bright, and emotionally available — to the sun only.",
                new Color(0.98f, 0.82f, 0.18f)),
            new BeeKind("lilac", "Lilac Lane",
                "Spring's soft launch.",
                "Seasonal drop. If you missed it, wait a year. Scalpers hate this bee.",
                new Color(0.70f, 0.55f, 0.82f)),
            new BeeKind("frost", "Frost Tip",
                "Cold open.",
                "Nectar on ice. Warmth not included.",
                new Color(0.78f, 0.90f, 0.96f)),
            new BeeKind("storm", "Storm Cell",
                "Pressure drop with wings.",
                "Forecast: 100% chance of buzz. Pack a jacket; the hive's already loud.",
                new Color(0.28f, 0.34f, 0.48f)),
            new BeeKind("haze", "Haze Drift",
                "Soft focus. Hard landing.",
                "Visibility low, attitude lower. Golden hour on; comments off.",
                new Color(0.72f, 0.68f, 0.58f)),
            new BeeKind("drizzle", "Drizzle Dot",
                "Light precipitation, heavy opinions.",
                "Not a downpour — a persistent drip. Mild annoyance, chance of sting.",
                new Color(0.55f, 0.70f, 0.88f)),
            new BeeKind("mint", "Mint Sting",
                "Fresh to death.",
                "After-dinner bee. Clears the palate and the personal space.",
                new Color(0.42f, 0.86f, 0.68f)),
            new BeeKind("berry", "Berry Bomb",
                "Sweet until it isn't.",
                "Antioxidant with an agenda. Pair with yogurt; dodge the welt.",
                new Color(0.72f, 0.18f, 0.38f)),
            new BeeKind("caramel", "Caramel Comb",
                "Sticky situation.",
                "Slow drip, fast regret. Dessert first; dignity second.",
                new Color(0.78f, 0.48f, 0.22f)),
            new BeeKind("cocoa", "Cocoa Nib",
                "Bitter-sweet buzz.",
                "70% dark, 30% drama. Melts in the hive, not in your hand.",
                new Color(0.42f, 0.26f, 0.18f)),
            new BeeKind("neon", "Neon Nectar",
                "Glow-in-the-dark grind.",
                "Night-shift nectar. Bright enough to pollinate after dark.",
                new Color(0.22f, 0.95f, 0.55f)),
            new BeeKind("velvet", "Velvet Hour",
                "After-hours elegance.",
                "Dress code: stripes. Playlist: buzz. Cover charge: one flower.",
                new Color(0.48f, 0.18f, 0.42f)),
            new BeeKind("metro", "Metro Swarm",
                "Rush-hour wings.",
                "Transfers at Clover. Standing room only in the comb.",
                new Color(0.35f, 0.38f, 0.42f)),
            new BeeKind("static", "Static Wing",
                "Charged personality.",
                "Shockingly social. Hair stands up; so does the hive.",
                new Color(0.88f, 0.90f, 0.55f)),
            new BeeKind("copper", "Copper Wing",
                "Conducts business.",
                "Good conductivity, better gossip. Wired for nectar futures.",
                new Color(0.78f, 0.42f, 0.22f)),
            new BeeKind("chrome", "Chrome Comb",
                "Reflects poorly on rivals.",
                "Polished finish, unfinished business. Mirror, mirror, who's the buzz?",
                new Color(0.82f, 0.86f, 0.90f)),
            // Batch B — CoP punch-ups (+24 → 54)
            new BeeKind("salt", "Salt Spray",
                "Seasoned wings.",
                "Briny and brief. Leaves a crunch; keeps the receipt.",
                new Color(0.72f, 0.84f, 0.86f)),
            new BeeKind("coral", "Coral Reef",
                "Builds the neighborhood.",
                "Builds slowly, stings suddenly. Underwater HOA president.",
                new Color(1.00f, 0.48f, 0.52f)),
            new BeeKind("tide", "Tide Pool",
                "Comes and goes.",
                "Schedule: moon. Attitude: wet. Lost flip-flops sold separately.",
                new Color(0.22f, 0.55f, 0.68f)),
            new BeeKind("pearl", "Pearl Drop",
                "Smooth operator.",
                "Irritation optional, glow included. Worth the dive.",
                new Color(0.92f, 0.94f, 0.98f)),
            new BeeKind("jade", "Jade Sting",
                "Lucky strike.",
                "Cool to the touch, warm to the welt. Fortune favors the bold.",
                new Color(0.28f, 0.62f, 0.42f)),
            new BeeKind("ruby", "Ruby Wing",
                "Hard shine.",
                "Cut above the rest. Clarity: perfect. Patience: zero.",
                new Color(0.82f, 0.12f, 0.22f)),
            new BeeKind("onyx", "Onyx Shade",
                "Dark mode bee.",
                "Absorbs light and small talk. Formal wear for the hive.",
                new Color(0.12f, 0.12f, 0.14f)),
            new BeeKind("topaz", "Topaz Gleam",
                "Golden glance.",
                "Warm stone, warmer sting. Jewelry with opinions.",
                new Color(0.95f, 0.72f, 0.22f)),
            new BeeKind("maple", "Maple Turn",
                "Leaves early.",
                "Turns early, sticks around. Syrup for the soul; leaf for the lawn.",
                new Color(0.78f, 0.32f, 0.12f)),
            new BeeKind("pine", "Pine Needle",
                "Evergreen attitude.",
                "Sharp year-round. Scent: forest. Handshake: poke.",
                new Color(0.28f, 0.48f, 0.32f)),
            new BeeKind("harvest", "Harvest Hum",
                "Peak season.",
                "Brings the basket. Shares the credit. Keeps the last apple.",
                new Color(0.88f, 0.62f, 0.18f)),
            new BeeKind("blizzard", "Blizzard Buzz",
                "White-out wings.",
                "Visibility: none. Vibes: many. Bundle up or buzz off.",
                new Color(0.88f, 0.94f, 1.00f)),
            new BeeKind("bass", "Bass Note",
                "Low frequency.",
                "Felt more than heard. Subwoofer of the garden.",
                new Color(0.22f, 0.22f, 0.38f)),
            new BeeKind("treble", "Treble Hook",
                "High and mighty.",
                "Hits the high notes and your ear. Sheet music optional.",
                new Color(0.95f, 0.90f, 0.55f)),
            new BeeKind("tempo", "Tempo Mark",
                "On the beat.",
                "Metronome with a mean streak. Don't rush the nectar.",
                new Color(0.55f, 0.78f, 0.92f)),
            new BeeKind("encore", "Encore Bee",
                "One more song.",
                "Crowd favorite. Always has another sting in the setlist.",
                new Color(0.72f, 0.28f, 0.48f)),
            new BeeKind("pretzel", "Pretzel Twist",
                "Salted and sorted.",
                "Tied in knots, still flying. Snack break, forever.",
                new Color(0.72f, 0.48f, 0.28f)),
            new BeeKind("pickle", "Pickle Jar",
                "Brined personality.",
                "Sour when it counts. Crunch included; lid optional.",
                new Color(0.55f, 0.72f, 0.28f)),
            new BeeKind("waffle", "Waffle Iron",
                "Grid locked.",
                "Breakfast architecture. Syrup optional; opinions required.",
                new Color(0.92f, 0.72f, 0.32f)),
            new BeeKind("chili", "Chili Flake",
                "Heat check.",
                "Scoville with wings. Glass of milk not included.",
                new Color(0.90f, 0.22f, 0.12f)),
            new BeeKind("fleece", "Fleece Cloud",
                "Looks laundry-soft.",
                "Looks hug-ready. Is not. Cozy until the sting.",
                new Color(0.90f, 0.90f, 0.94f)),
            new BeeKind("marsh", "Marshmallow",
                "Toasty temper.",
                "Puffs up under pressure. Campfire credentialed.",
                new Color(0.98f, 0.92f, 0.88f)),
            new BeeKind("cotton", "Cotton Candy",
                "Spun sugar.",
                "Melts in the rain; sticks in the memory. Fairground royalty.",
                new Color(0.98f, 0.62f, 0.78f)),
            new BeeKind("plush", "Plush Puff",
                "Soft spoken.",
                "Looks nap-ready. Answers in welts. Quiet hours optional.",
                new Color(0.78f, 0.72f, 0.88f)),
            new BeeKind("dialup", "Dial-Up Drone",
                "Connecting…",
                "Screeches in, stings late. Buffering is a lifestyle.",
                new Color(0.55f, 0.48f, 0.42f)),
            new BeeKind("popup", "Pop-Up Pest",
                "Unskippable.",
                "Closes one flower, opens three. Terms of service: pain.",
                new Color(0.92f, 0.55f, 0.18f)),
            new BeeKind("404", "Four-Oh-Four",
                "Bee not found.",
                "The comb you seek has moved. Try a different garden.",
                new Color(0.35f, 0.38f, 0.42f)),
            new BeeKind("leet", "Leet Mode",
                "1337 buzz.",
                "Speaks in numbers, stings in paragraphs. Elite optional.",
                new Color(0.22f, 0.95f, 0.42f)),
            new BeeKind("fine", "Still Fine",
                "Everything's great.",
                "Garden's on fire. Mood: unbothered. Sip continues.",
                new Color(0.88f, 0.62f, 0.28f)),
            new BeeKind("wow", "Much Buzz",
                "Very sting. Wow.",
                "Such pollen. Little patience. Amplify the garden.",
                new Color(0.92f, 0.82f, 0.42f)),
            new BeeKind("sus", "Kinda Sus",
                "Kinda sus.",
                "Wasn't me. Check the other feeder. Theories welcome — proof optional.",
                new Color(0.55f, 0.22f, 0.28f)),
            new BeeKind("distracted", "Distracted Wing",
                "Looking elsewhere.",
                "One flower on the arm. Better flower in frame. Classic.",
                new Color(0.72f, 0.48f, 0.62f)),
            new BeeKind("dramatic", "Side-Eye Snap",
                "Turns. Stares.",
                "Full side-eye in 0.2 seconds. Cue the strings.",
                new Color(0.62f, 0.48f, 0.28f)),
            new BeeKind("shadow", "Shadow Swarm",
                "Come to the shade.",
                "Big mood: mysterious. Bigger mood: sting first, monologue later.",
                new Color(0.18f, 0.12f, 0.22f)),
            new BeeKind("sorted", "Vibe Sort",
                "Where you belong.",
                "Labels optional; vibe's eternal. House rules: share nectar.",
                new Color(0.42f, 0.22f, 0.55f)),
            new BeeKind("spaghetti", "Spaghetti Nest",
                "All noodles, no chill.",
                "Tangled in the comb. Sauce on the stripes. Still flying.",
                new Color(0.88f, 0.78f, 0.42f)),
            new BeeKind("left", "Offbeat Shark",
                "Off-beat icon.",
                "Wrong choreography, right legend. Keeps dancing anyway.",
                new Color(0.55f, 0.72f, 0.82f)),
            new BeeKind("assist", "Hover Help",
                "Need a hand?",
                "Offers tips anyway. Nobody clicked yes. Still hovering.",
                new Color(0.82f, 0.82f, 0.78f)),
            new BeeKind("rick", "Link Trap",
                "You know the link.",
                "Promised a flower. Delivered a classic. Stings forever.",
                new Color(0.42f, 0.55f, 0.88f)),
            new BeeKind("phoenix", "Phoenix Pollen",
                "Burns once. Buzzes twice.",
                "Ash optional. Comeback mandatory. Hot take with wings.",
                new Color(0.95f, 0.35f, 0.12f)),
            new BeeKind("hydra", "Hydra Hum",
                "Cut one, get two.",
                "Problem multiplies. Patience does not. Bring a bigger jar.",
                new Color(0.28f, 0.55f, 0.32f)),
            new BeeKind("sphinx", "Sphinx Sting",
                "Riddle first.",
                "Answers cost nectar. Wrong answers cost welts.",
                new Color(0.72f, 0.58f, 0.28f)),
            new BeeKind("kraken", "Kraken Grip",
                "Deep issues.",
                "Tentatively friendly. Absolutely clingy. Surface with snacks.",
                new Color(0.18f, 0.28f, 0.42f)),
            new BeeKind("goal", "Goal Post",
                "Posts up.",
                "Moves the goal when you're close. Still counts it as a win.",
                new Color(0.92f, 0.92f, 0.95f)),
            new BeeKind("slam", "Slam Dunk",
                "From downtown.",
                "Hangs on the rim of the comb. Crowd goes mild.",
                new Color(0.88f, 0.42f, 0.12f)),
            new BeeKind("huddle", "Huddle Buzz",
                "Break on three.",
                "Strategy: sting. Backup strategy: also sting.",
                new Color(0.22f, 0.48f, 0.28f)),
            new BeeKind("relay", "Relay Wing",
                "Passes the baton.",
                "Drops it sometimes. Blames the flower. Keeps running.",
                new Color(0.55f, 0.72f, 0.95f)),
            new BeeKind("orbit", "Orbit Drop",
                "Circles back.",
                "Gravity optional. Attitude locked. Reentry is a vibe.",
                new Color(0.42f, 0.48f, 0.78f)),
            new BeeKind("comet", "Comet Trail",
                "Brief visit.",
                "Long tail, short patience. Wish upon a welt.",
                new Color(0.72f, 0.85f, 0.98f)),
            new BeeKind("nova", "Nova Flash",
                "Bright idea.",
                "Burns fast, remembered longer. Don't stare.",
                new Color(0.98f, 0.92f, 0.55f)),
            new BeeKind("lunar", "Lunar Latch",
                "Moonstruck.",
                "Tides the garden. Sleep schedule: decorative.",
                new Color(0.78f, 0.82f, 0.90f)),
            new BeeKind("toast", "Toast Point",
                "Golden both sides.",
                "Butter optional. Opinions required. Crunch is character.",
                new Color(0.82f, 0.62f, 0.35f)),
            new BeeKind("noodle", "Noodle Loop",
                "Loopy logic.",
                "Tangled plans, straight sting. Extra sauce.",
                new Color(0.95f, 0.82f, 0.48f)),
            new BeeKind("soda", "Soda Fizz",
                "Bubbly temper.",
                "Opens loud. Goes flat if ignored. Ice optional.",
                new Color(0.95f, 0.35f, 0.42f)),
            new BeeKind("salsa", "Salsa Step",
                "Hot move.",
                "Two-step then sting. Chips not included.",
                new Color(0.88f, 0.22f, 0.18f)),
            new BeeKind("thunder", "Thunder Clap",
                "Loud entrance.",
                "Announces itself. Leaves a mark. Umbrella useless.",
                new Color(0.35f, 0.35f, 0.48f)),
            new BeeKind("mirage", "Mirage Wing",
                "Almost there.",
                "Looks like nectar. Isn't. Heat shimmer with attitude.",
                new Color(0.95f, 0.78f, 0.55f)),
            new BeeKind("gust", "Gust Front",
                "Blows in.",
                "Rearranges the garden. Claims it was always like that.",
                new Color(0.62f, 0.78f, 0.72f)),
            new BeeKind("horn", "Harbor Horn",
                "Warning shot.",
                "Heard before seen. Subtlety not installed.",
                new Color(0.55f, 0.58f, 0.52f)),
            new BeeKind("moth", "Moth Guest",
                "Wrong party.",
                "Came for the light. Stayed for the drama.",
                new Color(0.72f, 0.68f, 0.55f)),
            new BeeKind("firefly", "Firefly Glow",
                "Bring your own glow.",
                "Night shift done right. No batteries required.",
                new Color(0.55f, 0.95f, 0.42f)),
            new BeeKind("ant", "Ant Line",
                "Single file.",
                "Union of tiny steps. Picnic security detail.",
                new Color(0.42f, 0.28f, 0.18f)),
            new BeeKind("snail", "Snail Mail",
                "Eventually.",
                "Delivery estimate: someday. Stamp optional; slime included.",
                new Color(0.72f, 0.78f, 0.48f)),
            new BeeKind("yarn", "Yarn Ball",
                "All tied up.",
                "Hobby that stings. Knit one, purl one, welt one.",
                new Color(0.92f, 0.48f, 0.62f)),
            new BeeKind("glue", "Glue Stick",
                "Sticks around.",
                "Permanent until washed. Craft hour's enforcer.",
                new Color(0.95f, 0.90f, 0.72f)),
            new BeeKind("pixel", "Pixel Bead",
                "Low-res legend.",
                "Eight-bit heart, full-res sting. Zoom in for regret.",
                new Color(0.42f, 0.85f, 0.55f)),
            new BeeKind("stencil", "Stencil Bee",
                "Paint inside the lines.",
                "Then ignores the lines. Art is pain; pain is art.",
                new Color(0.55f, 0.55f, 0.62f)),
            new BeeKind("alley", "Alley Scout",
                "Shortcut specialist.",
                "Knows every dumpster flower. Tips in mystery.",
                new Color(0.38f, 0.38f, 0.42f)),
            new BeeKind("rooftop", "Rooftop Perch",
                "Better view.",
                "Elevator optional. Attitude penthouse. Pigeons jealous.",
                new Color(0.48f, 0.55f, 0.72f)),
        };

        static int[] _counts;
        static bool _ready;

        public static int Kinds => Roster.Length;
        public static int AlbumSlots => Kinds * Finishes;
        public static int Found => CountFound();
        public static int Visitors => CountAll();

        public static int SlotOf(int kind, BeeFinish finish) => kind * Finishes + (int)finish;
        public static int KindOfSlot(int slot) => slot / Finishes;
        public static BeeFinish FinishOfSlot(int slot) => (BeeFinish)(slot % Finishes);

        /// <summary>Total owned copies of a kind across all finishes.</summary>
        public static int CountOf(int kind)
        {
            Warm();
            if ((uint)kind >= (uint)Kinds) return 0;
            int n = 0;
            int baseIx = kind * Finishes;
            for (int f = 0; f < Finishes; f++)
                n += _counts[baseIx + f];
            return n;
        }

        public static int CountOf(int kind, BeeFinish finish)
        {
            Warm();
            int ix = SlotOf(kind, finish);
            if ((uint)ix >= (uint)_counts.Length) return 0;
            return _counts[ix];
        }

        public static int CountOfSlot(int slot)
        {
            Warm();
            if ((uint)slot >= (uint)_counts.Length) return 0;
            return _counts[slot];
        }

        public static BeeVisit TakeVisitor()
        {
            Warm();
            int kind = PickKind();
            BeeFinish finish = PickFinish();
            int ix = SlotOf(kind, finish);
            bool fresh = _counts[ix] == 0;
            _counts[ix]++;
            Save();
            return new BeeVisit
            {
                Kind = Roster[kind],
                Finish = finish,
                Fresh = fresh,
                Count = _counts[ix],
            };
        }

        static BeeFinish PickFinish()
        {
            // Mostly Normal, uncommon Holo, rare InverseRainbow.
            float roll = Random.value;
            if (roll < 0.78f) return BeeFinish.Normal;
            if (roll < 0.95f) return BeeFinish.Holo;
            return BeeFinish.InverseRainbow;
        }

        static int PickKind()
        {
            // Prefer kinds with no copies of any finish (album holes).
            int holes = 0;
            for (int k = 0; k < Kinds; k++)
                if (CountOf(k) == 0) holes++;
            if (holes > 0 && Random.value < 0.74f)
            {
                int skip = Random.Range(0, holes);
                for (int k = 0; k < Kinds; k++)
                {
                    if (CountOf(k) != 0) continue;
                    if (skip == 0) return k;
                    skip--;
                }
            }
            return Random.Range(0, Kinds);
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
            if (_ready && _counts != null && _counts.Length == AlbumSlots) return;
            _counts = new int[AlbumSlots];
            _ready = true;

            var raw = PlayerPrefs.GetString(Pref, "");
            if (!string.IsNullOrEmpty(raw))
            {
                LoadFlat(raw);
                return;
            }

            // Migrate v1 kind-only counts into Normal finish slots.
            var v1 = PlayerPrefs.GetString(PrefV1, "");
            if (string.IsNullOrEmpty(v1)) return;
            var parts = v1.Split(Pair);
            for (int p = 0; p < parts.Length; p++)
            {
                var bit = parts[p];
                int cut = bit.IndexOf(Kv);
                if (cut <= 0) continue;
                var id = bit.Substring(0, cut);
                if (!int.TryParse(bit.Substring(cut + 1), out int n) || n <= 0) continue;
                int kind = IndexOf(id);
                if (kind >= 0) _counts[SlotOf(kind, BeeFinish.Normal)] = n;
            }
            Save();
        }

        // v2 format: id|finish:count  (finish is 0/1/2); also accepts legacy id:count as Normal.
        static void LoadFlat(string raw)
        {
            var parts = raw.Split(Pair);
            for (int p = 0; p < parts.Length; p++)
            {
                var bit = parts[p];
                int cut = bit.IndexOf(Kv);
                if (cut <= 0) continue;
                var key = bit.Substring(0, cut);
                if (!int.TryParse(bit.Substring(cut + 1), out int n) || n <= 0) continue;

                int kind;
                BeeFinish finish = BeeFinish.Normal;
                int bar = key.IndexOf('|');
                if (bar > 0)
                {
                    kind = IndexOf(key.Substring(0, bar));
                    if (int.TryParse(key.Substring(bar + 1), out int f) && f >= 0 && f < Finishes)
                        finish = (BeeFinish)f;
                }
                else
                {
                    kind = IndexOf(key);
                }
                if (kind >= 0) _counts[SlotOf(kind, finish)] = n;
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
            for (int kind = 0; kind < Kinds; kind++)
            {
                for (int f = 0; f < Finishes; f++)
                {
                    int n = _counts[SlotOf(kind, (BeeFinish)f)];
                    if (n <= 0) continue;
                    if (sb.Length > 0) sb.Append(Pair);
                    sb.Append(Roster[kind].Id).Append('|').Append(f).Append(Kv).Append(n);
                }
            }
            PlayerPrefs.SetString(Pref, sb.ToString());
            PlayerPrefs.Save();
        }
    }
}