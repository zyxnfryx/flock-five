namespace FlockFive
{
    // Temporary inspector hook. The boot that reads this and loads a garden is
    // compiled only for WebGL development builds (see MaybeDevLevelSkip). This
    // parser has no side effects. Release player builds never call it.
    public static class DevLevelSkip
    {
        public const string QueryKey = "fflevel";
        public const string OnboardingKey = "skipOnboarding";
        public const string KeepTipsKey = "keepTips";
        public const string ForceFeederKey = "forceFeeder";
        public const string ForceNapKey = "forceNap";
        public const string OpenPokerKey = "openPoker";
        // 1-based. Garden 2 is the first board where restart and the hive can
        // both show. Garden 1 may hide the hive until collection progress.
        // Bee Thicket opens on Gold and Teal feeders and still has full Ruby
        // and Violet flocks, so a finished flock sleeps until a feeder wants
        // that color. A sleeping-bird check does not need a later garden.
        // ?fflevel= still overrides this.
        public const int OnboardingGarden = 2;
        // Inspector stake. Daily's coin ceiling is $1,000 (DailyBonus.BaseCap)
        // and the poker floor is $5, so this covers a long table session.
        // Purse.Credit writes flockfive.coins and the checksum the purse reads.
        public const int OnboardingCoins = 1000;

        public struct Choice
        {
            public int Index;
            public bool KeepBee;
            public bool KeepFire;
            public bool SkipOnboarding;
            // True only when this load is the skip path and the page also said
            // keepTips=1. The boot then leaves the kit, feeder, and sleep rule
            // plates owed. keepTips without skipOnboarding does not set this.
            public bool KeepTips;
            // True only with skipOnboarding=1. The boot turns one feeder and
            // raises the feeder plate, stacks a napping flock and raises the
            // sleep plate through the live nap gate, or opens Bird Poker on a
            // drawn hand with the Back tip still owed.
            public bool ForceFeeder;
            public bool ForceNap;
            public bool OpenPoker;
        }

        // 1-based garden from ?fflevel=, or 0 when the page did not ask.
        public static int DisplayFromUrl(string url)
        {
            return QueryInt(url, QueryKey);
        }

        // True only for ?skipOnboarding=1. Any other value is not a request.
        public static bool OnboardingFromUrl(string url)
        {
            return QueryInt(url, OnboardingKey) == 1;
        }

        // True only for ?keepTips=1. Any other value is not a request.
        // Honored only together with skipOnboarding. See Plan.
        public static bool KeepTipsFromUrl(string url)
        {
            return QueryInt(url, KeepTipsKey) == 1;
        }

        // True only for ?forceFeeder=1. Any other value is not a request.
        public static bool ForceFeederFromUrl(string url)
        {
            return QueryInt(url, ForceFeederKey) == 1;
        }

        // True only for ?forceNap=1. Any other value is not a request.
        public static bool ForceNapFromUrl(string url)
        {
            return QueryInt(url, ForceNapKey) == 1;
        }

        // True only for ?openPoker=1. Any other value is not a request.
        public static bool OpenPokerFromUrl(string url)
        {
            return QueryInt(url, OpenPokerKey) == 1;
        }

        // How many coins to add so the purse reaches OnboardingCoins.
        // Already at or above that stake adds nothing.
        public static int CoinsToSeed(int have)
        {
            if (have < 0) have = 0;
            if (have >= OnboardingCoins) return 0;
            return OnboardingCoins - have;
        }

        // Index is the 0-based LevelData slot, or -1 when there is nothing to load.
        // Garden 3 keeps the lightning lesson owed. Garden 5 keeps the bee lesson owed.
        // ?skipOnboarding=1 with no fflevel loads garden 2 and asks the boot to
        // stamp the home cascade seen, seed the purse, and leave the nap tip
        // unseen. A valid fflevel still picks the garden.
        // ?keepTips=1 on that same skip also leaves the kit and feeder rule
        // plates owed. keepTips without skipOnboarding leaves KeepTips false,
        // so a plain ?fflevel= load still stamps those plates.
        // forceFeeder, forceNap, and openPoker are the same: only a
        // skipOnboarding=1 load sets them. Absent the flag, the plate still
        // waits for a real feeder turn, a real nap, or a real walk to poker.
        public static Choice Plan(string url, int levelCount)
        {
            int display = DisplayFromUrl(url);
            bool skip = OnboardingFromUrl(url);
            if (display < 1 && skip) display = OnboardingGarden;
            if (display < 1 || levelCount < 1 || display > levelCount)
                return new Choice { Index = -1, SkipOnboarding = skip, OpenPoker = skip && OpenPokerFromUrl(url) };
            return new Choice
            {
                Index = display - 1,
                KeepBee = display == 5,
                KeepFire = display == 3,
                SkipOnboarding = skip,
                KeepTips = skip && KeepTipsFromUrl(url),
                ForceFeeder = skip && ForceFeederFromUrl(url),
                ForceNap = skip && ForceNapFromUrl(url),
                OpenPoker = skip && OpenPokerFromUrl(url),
            };
        }

        static int QueryInt(string url, string key)
        {
            if (url == null || url.Length == 0 || key == null || key.Length == 0) return 0;
            int q = url.IndexOf('?');
            if (q < 0 || q + 1 >= url.Length) return 0;
            int end = url.IndexOf('#', q + 1);
            if (end < 0) end = url.Length;
            int i = q + 1;
            while (i < end)
            {
                int amp = url.IndexOf('&', i);
                if (amp < 0 || amp > end) amp = end;
                int eq = -1;
                for (int k = i; k < amp; k++)
                {
                    if (url[k] != '=') continue;
                    eq = k;
                    break;
                }
                int keyLen = eq - i;
                if (eq > i && keyLen == key.Length)
                {
                    bool match = true;
                    for (int k = 0; k < keyLen; k++)
                        if (url[i + k] != key[k]) { match = false; break; }
                    if (match) return Digits(url, eq + 1, amp);
                }
                i = amp + 1;
            }
            return 0;
        }

        static int Digits(string s, int from, int to)
        {
            if (from >= to) return 0;
            int n = 0;
            for (int i = from; i < to; i++)
            {
                char c = s[i];
                if (c < '0' || c > '9') return 0;
                n = n * 10 + (c - '0');
                if (n > 999) return 0;
            }
            return n;
        }
    }
}
