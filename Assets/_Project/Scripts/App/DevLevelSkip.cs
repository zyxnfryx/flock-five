namespace FlockFive
{
    // WebGL Development inspect hook only. Not a shipping-path change.
    // The boot that reads these flags is compiled only for WebGL development
    // builds (see MaybeDevLevelSkip). This parser has no side effects.
    // Device, iOS, Android, and release player builds never call it.
    public static class DevLevelSkip
    {
        public const string QueryKey = "fflevel";
        // WebGL Development inspect hook only. Not a shipping-path change.
        public const string OnboardingKey = "skipOnboarding";
        public const string KeepTipsKey = "keepTips";
        public const string ForceFeederKey = "forceFeeder";
        public const string ForceNapKey = "forceNap";
        public const string OpenPokerKey = "openPoker";
        // WebGL Development inspect hook only. Not a shipping-path change.
        public const string ForceClearKey = "forceClear";
        // WebGL Development inspect hook only. Not a shipping-path change.
        // Honored only with skipOnboarding=1, and it acts only once forceClear
        // has already waited out the wipe and any ostrich entrance.
        public const string ForceRewardsKey = "forceRewards";
        // WebGL Development inspect hook only. Not a shipping-path change.
        // ?forceMaxxed=1 draws the open album card as MAXXED so Inspector can
        // see "Tap outside to put back". It does not write the album.
        public const string ForceMaxxedKey = "forceMaxxed";
        public const string ForcePauseKey = "forcePause";
        // A garden that never shows the entrance still has to finish the star
        // wipe. An ostrich who stays idle without ever striding (the entrance
        // did not start) is given up after this, so Inspector is not stuck.
        public const float ForceClearIdleCap = 8f;
        // He is still in the entrance. Do not cover him. This only fires if
        // the act never returns to idle.
        public const float ForceClearMoveCap = 20f;
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
            // WebGL Development inspect hook only. Not a shipping-path change.
            // True only with skipOnboarding=1. After the star wipe, and after
            // an ostrich entrance has returned to idle, the boot clears the
            // board through the live win latch (TryScoreGarden), which is the
            // same latch a StageClearingCollect reaches. That opens the real
            // Collect / Watch Ad card. Absent the flag, nothing is cleared.
            public bool ForceClear;
            // WebGL Development inspect hook only. Not a shipping-path change.
            // True only with skipOnboarding=1. Does not change when forceClear
            // fires. After that wait, the boot makes Watch Ad / 2X offerable,
            // grants one album card, and unlocks the album Upgrade glove.
            // It does not grant No Ads. A later Watch Ad tap, while this hook
            // stays armed, completes through the real RewardedGrant path
            // (Begin, displayed, rewarded, flush) because WebGL has no LevelPlay
            // fill. That stub is not a shipping no-fill change.
            public bool ForceRewards;
        }

        // 1-based garden from ?fflevel=, or 0 when the page did not ask.
        public static int DisplayFromUrl(string url)
        {
            return QueryInt(url, QueryKey);
        }

        // WebGL Development inspect hook only. Not a shipping-path change.
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

        // WebGL Development inspect hook only. Not a shipping-path change.
        // True only for ?forceClear=1. Any other value is not a request.
        public static bool ForceClearFromUrl(string url)
        {
            return QueryInt(url, ForceClearKey) == 1;
        }

        // WebGL Development inspect hook only. Not a shipping-path change.
        // True only for ?forceRewards=1. Any other value is not a request.
        public static bool ForceRewardsFromUrl(string url)
        {
            return QueryInt(url, ForceRewardsKey) == 1;
        }

        // WebGL Development inspect hook only. Not a shipping-path change.
        // True only for ?forceMaxxed=1. Any other value is not a request.
        // The draw site is compiled only for WebGL development builds. This
        // does not write the album or grant an upgrade.
        public static bool ForceMaxxedFromUrl(string url)
        {
            return QueryInt(url, ForceMaxxedKey) == 1;
        }

        // DEV-ONLY inspect hook. Not a shipping-path proof.
        // ?forcePause=1 holds a caption indefinitely. ?forcePause=N (N > 1)
        // holds it for N seconds, then the normal timer may advance it.
        public static bool ForcePauseFromUrl(string url)
        {
            return QueryInt(url, ForcePauseKey) == 1;
        }

        public static bool ForcePauseBlocks(string url, float heldSeconds)
        {
            int n = QueryInt(url, ForcePauseKey);
            if (n == 1) return true;
            if (n > 1) return heldSeconds < n;
            return false;
        }

        // DEV-ONLY. A devConsole toggle so a held caption can be released
        // without reloading. Release builds never read it.
        public static bool DevPauseHeld;

        public static void DevPauseFlip()
        {
            DevPauseHeld = !DevPauseHeld;
        }

        public static bool CaptionHeld(string url, float heldSeconds)
        {
            return DevPauseHeld || ForcePauseBlocks(url, heldSeconds);
        }

        public const string TapLogKey = "tapLog";

        // DEV-ONLY inspect hook. Not a shipping-path proof. ?tapLog=1.
        public static bool TapLogFromUrl(string url)
        {
            return QueryInt(url, TapLogKey) == 1;
        }

        // presented: the star wipe is down and the garden tween is idle.
        // ostrich: this board has a live ostrich. moving: he is not idle
        // (stride, honk, shake, or the plant, which stays on the stride act
        // until the entrance ends). sawMove: that non-idle act has been seen,
        // so a first idle frame before he walks on is not "done".
        // No ostrich: ready as soon as the garden is presented.
        // Ostrich: ready once the entrance movement has been seen and he is idle.
        public static bool ForceClearReady(bool presented, bool ostrich, bool moving, bool idle, bool sawMove, float waited)
        {
            if (!presented) return false;
            if (!ostrich) return true;
            if (moving || !idle) return waited >= ForceClearMoveCap;
            if (sawMove) return true;
            return waited >= ForceClearIdleCap;
        }

        // WebGL Development inspect hook only. Not a shipping-path change.
        // Held stage pay plus the repeat payload once the grant is earned.
        // Purse.AddOwed is what the Repeat ticket does. Not earned leaves the 1x.
        public static int InspectRepeatTotal(int owed, int extra, bool earned)
        {
            if (owed < 0) owed = 0;
            if (extra < 0) extra = 0;
            if (!earned || extra == 0) return owed;
            return owed + extra;
        }

        // How long Zzz must be on the open garden before forceNap may raise the
        // plate. One frame is not enough at 412: the stage gate can open while
        // the star wipe is still the picture, and that frame was being counted.
        public const float NapPlateOpenSeconds = 0.4f;

        // True while the star wipe still owns the screen. StageHeld's realtime
        // failsafe can drop on a narrow phone before the paint clock finishes,
        // so a live rainbow still counts as covered.
        public static bool NapWipeCovers(bool rainbowLive, bool stageHeld)
        {
            return rainbowLive || stageHeld;
        }

        // forceNap draws Zzz under the level rainbow. The plate waits until that
        // pose has been drawn and the garden has been the picture (wipe gone)
        // for NapPlateOpenSeconds. openSeconds is that open-garden time.
        public static bool NapPlateReady(bool napBegun, bool stageHeld, float openSeconds)
        {
            return napBegun && !stageHeld && openSeconds >= NapPlateOpenSeconds;
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
        // forceFeeder, forceNap, openPoker, forceClear, and forceRewards are the
        // same: only a skipOnboarding=1 load sets them. Absent the flag, the
        // plate still waits for a real feeder turn, a real nap, or a real walk
        // to poker, and a clear still waits for a real solve. forceRewards does
        // not move the forceClear wait. All of these are WebGL Development
        // inspect hooks only, not shipping-path changes.
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
                ForceClear = skip && ForceClearFromUrl(url),
                ForceRewards = skip && ForceRewardsFromUrl(url),
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
