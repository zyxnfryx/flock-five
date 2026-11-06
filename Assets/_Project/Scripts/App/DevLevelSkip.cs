namespace FlockFive
{
    // Temporary inspector hook. The boot that reads this and loads a garden is
    // compiled only for WebGL development builds (see MaybeDevLevelSkip). This
    // parser has no side effects. Release player builds never call it.
    public static class DevLevelSkip
    {
        public const string QueryKey = "fflevel";
        public const string OnboardingKey = "skipOnboarding";
        // 1-based. Garden 2 is the first board where restart and the hive can
        // both show. Garden 1 may hide the hive until collection progress.
        public const int OnboardingGarden = 2;

        public struct Choice
        {
            public int Index;
            public bool KeepBee;
            public bool KeepFire;
            public bool SkipOnboarding;
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

        // Index is the 0-based LevelData slot, or -1 when there is nothing to load.
        // Garden 3 keeps the lightning lesson owed. Garden 5 keeps the bee lesson owed.
        // ?skipOnboarding=1 with no fflevel loads garden 2 and asks the boot to
        // stamp the home cascade seen. A valid fflevel still picks the garden.
        public static Choice Plan(string url, int levelCount)
        {
            int display = DisplayFromUrl(url);
            bool skip = OnboardingFromUrl(url);
            if (display < 1 && skip) display = OnboardingGarden;
            if (display < 1 || levelCount < 1 || display > levelCount)
                return new Choice { Index = -1 };
            return new Choice
            {
                Index = display - 1,
                KeepBee = display == 5,
                KeepFire = display == 3,
                SkipOnboarding = skip,
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
