using UnityEngine;

namespace FlockFive
{
    public static partial class Sfx
    {
        static AudioClip[] _riffles;
        static AudioClip[] _cardWhoosh;
        static AudioClip[] _cardSlap;
        static AudioClip[] _cardBump;
        static AudioClip[] _cardTap;
        static int _lastRiffle = -1;
        static int _lastCardWhoosh = -1;
        static int _lastCardSlap = -1;
        static int _lastCardBump = -1;
        static int _lastCardTap = -1;

        static void WarmPokerClips()
        {
            if (_riffles != null) return;
            _riffles = new AudioClip[4];
            for (int k = 0; k < _riffles.Length; k++)
                _riffles[k] = MakeRiffle(k);
            _cardWhoosh = new AudioClip[8];
            for (int k = 0; k < _cardWhoosh.Length; k++)
                _cardWhoosh[k] = MakeCardWhoosh(k);
            _cardSlap = new AudioClip[8];
            for (int k = 0; k < _cardSlap.Length; k++)
                _cardSlap[k] = MakeCardSlap(k);
            _cardBump = new AudioClip[4];
            for (int k = 0; k < _cardBump.Length; k++)
                _cardBump[k] = MakeCardBump(k);
            _cardTap = new AudioClip[6];
            for (int k = 0; k < _cardTap.Length; k++)
                _cardTap[k] = MakeCardTap(k);
        }

        public static void Riffle()
        {
            Ensure();
            WarmPokerClips();
            int i = Next(_riffles.Length, ref _lastRiffle);
            Shot(_riffles[i], Random.Range(0.98f, 1.03f), 0.78f, MixLayer.Lead, MixDesk.DuckWhoosh);
            if (MixDesk.Live != null) MixDesk.Live.MarkLead(0.58f, MixDesk.DuckWhoosh);
        }

        public static void CardWhoosh()
        {
            Ensure();
            WarmPokerClips();
            int i = Next(_cardWhoosh.Length, ref _lastCardWhoosh);
            Shot(_cardWhoosh[i], Random.Range(0.97f, 1.03f), 0.72f, MixLayer.Lead, MixDesk.DuckWhoosh);
            if (MixDesk.Live != null) MixDesk.Live.MarkLead(0.22f, MixDesk.DuckWhoosh);
        }

        public static void CardSlap()
        {
            Ensure();
            WarmPokerClips();
            int i = Next(_cardSlap.Length, ref _lastCardSlap);
            Shot(_cardSlap[i], Random.Range(0.97f, 1.03f), 0.70f, MixLayer.Lead, MixDesk.DuckChirp);
            if (MixDesk.Live != null) MixDesk.Live.MarkLead(0.12f, MixDesk.DuckChirp);
        }

        public static void CardBump()
        {
            Ensure();
            WarmPokerClips();
            int i = Next(_cardBump.Length, ref _lastCardBump);
            Shot(_cardBump[i], Random.Range(0.97f, 1.02f), 0.80f, MixLayer.Lead, MixDesk.DuckWhoosh);
            if (MixDesk.Live != null) MixDesk.Live.MarkLead(0.20f, MixDesk.DuckWhoosh);
        }

        public static void CardTap()
        {
            Ensure();
            WarmPokerClips();
            int i = Next(_cardTap.Length, ref _lastCardTap);
            Shot(_cardTap[i], Random.Range(0.98f, 1.03f), 0.58f, MixLayer.Lead, MixDesk.DuckChirp);
            if (MixDesk.Live != null) MixDesk.Live.MarkLead(0.10f, MixDesk.DuckChirp);
        }

        public static void CardRustle()
        {
            Ensure();
            WarmPokerClips();
            int i = Next(_cardWhoosh.Length, ref _lastCardWhoosh);
            Shot(_cardWhoosh[i], Random.Range(0.92f, 0.98f), 0.48f, MixLayer.Lead, MixDesk.DuckWhoosh);
            if (MixDesk.Live != null) MixDesk.Live.MarkLead(0.16f, MixDesk.DuckWhoosh);
        }

        public static void ChainLock()
        {
            Ensure();
            WarmPokerClips();
            CardBump();
        }

        public static void ChainSnap()
        {
            Ensure();
            Crack();
            CardBump();
        }

        public static void FlameUp() => CardPop();

        static AudioClip MakeRiffle(int kind)
        {
            // Warm rag-paper riffle: many short gated ticks, no open noise floor.
            float dur = 0.50f + 0.04f * (kind % 3);
            int n = Mathf.CeilToInt(Rate * dur);
            var data = new float[n];
            int seed = 22011 + kind * 911;
            int snaps = 18 + kind * 2;
            for (int s = 0; s < snaps; s++)
            {
                float at = 0.02f + (s / (float)snaps) * (dur - 0.08f);
                at += Hash(seed + s * 17) * 0.012f;
                float f = Mathf.Lerp(110f, 210f, (s / (float)snaps) * 0.55f + (Hash(seed + s) + 1f) * 0.22f);
                float amp = 0.22f + 0.10f * ((Hash(seed + s * 3) + 1f) * 0.5f);
                float cd = 0.016f + 0.008f * ((Hash(seed + s * 5) + 1f) * 0.5f);
                float lp = 0f;
                for (int i = 0; i < n; i++)
                {
                    float t = i / (float)Rate - at;
                    if (t < 0f || t > cd) continue;
                    float e = Mathf.Clamp01(t / 0.0018f) * Mathf.Exp(-t / (cd * 0.28f));
                    float ph = 2f * Mathf.PI * f * t;
                    float tick = Mathf.Sin(ph) + 0.22f * Mathf.Sin(2f * ph);
                    float grain = Soft(ref lp, seed + s * 13, i, 0.18f) * 0.08f * e;
                    data[i] += (tick * 0.85f + grain) * amp * e;
                }
            }
            return Clip("riffle" + kind, data);
        }

        static AudioClip MakeCardWhoosh(int kind)
        {
            float dur = Mathf.Lerp(0.16f, 0.24f, (kind % 8) / 7f);
            int n = Mathf.CeilToInt(Rate * dur);
            var data = new float[n];
            int seed = 33001 + kind * 47;
            float f0 = Mathf.Lerp(160f, 240f, (Hash(seed) + 1f) * 0.5f);
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float u = t / dur;
                float env = Mathf.Sin(Mathf.PI * Mathf.Pow(u, 0.65f)) * Mathf.Exp(-u * 1.6f);
                float f = Mathf.Lerp(f0, f0 * 0.72f, u);
                float air = Soft(ref lp, seed, i, 0.10f);
                float s = Mathf.Sin(2f * Mathf.PI * f * t) * 0.70f;
                s += Mathf.Sin(2f * Mathf.PI * (f * 1.28f) * t) * 0.16f;
                s += air * 0.10f * (1f - u);
                data[i] = s * env * 0.32f;
            }
            return Clip("card-whoosh" + kind, data);
        }

        static AudioClip MakeCardSlap(int kind)
        {
            float dur = 0.090f + 0.012f * (kind % 4);
            int n = Mathf.CeilToInt(Rate * dur);
            var data = new float[n];
            int seed = 44021 + kind * 59;
            float f = Mathf.Lerp(90f, 150f, (Hash(seed) + 1f) * 0.5f);
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float e = Mathf.Clamp01(t / 0.002f) * Mathf.Exp(-t / 0.022f);
                float tick = Mathf.Sin(2f * Mathf.PI * f * t) + 0.18f * Mathf.Sin(2f * Mathf.PI * f * 2.1f * t);
                float grain = Soft(ref lp, seed, i, 0.22f) * 0.07f;
                data[i] = (tick * 0.80f + grain) * e * 0.42f;
            }
            return Clip("card-slap" + kind, data);
        }

        static AudioClip MakeCardBump(int kind)
        {
            float dur = 0.16f + 0.02f * (kind % 3);
            int n = Mathf.CeilToInt(Rate * dur);
            var data = new float[n];
            int seed = 55031 + kind * 67;
            float f = Mathf.Lerp(68f, 110f, (Hash(seed) + 1f) * 0.5f);
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float e = Mathf.Clamp01(t / 0.004f) * Mathf.Exp(-t / 0.045f);
                float body = Mathf.Sin(2f * Mathf.PI * f * t) + 0.28f * Mathf.Sin(2f * Mathf.PI * f * 1.7f * t);
                float paper = Soft(ref lp, seed, i, 0.16f) * 0.06f * Mathf.Exp(-t / 0.02f);
                data[i] = (body * 0.78f + paper) * e * 0.48f;
            }
            return Clip("card-bump" + kind, data);
        }

        static AudioClip MakeCardTap(int kind)
        {
            float dur = 0.055f + 0.008f * (kind % 3);
            int n = Mathf.CeilToInt(Rate * dur);
            var data = new float[n];
            int seed = 66041 + kind * 41;
            float f = Mathf.Lerp(140f, 190f, (Hash(seed) + 1f) * 0.5f);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float e = Mathf.Clamp01(t / 0.0015f) * Mathf.Exp(-t / 0.012f);
                data[i] = Mathf.Sin(2f * Mathf.PI * f * t) * e * 0.36f;
            }
            return Clip("card-tap" + kind, data);
        }
    }
}

