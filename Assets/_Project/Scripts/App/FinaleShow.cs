using System.Collections;
using UnityEngine;

namespace FlockFive
{
    public static class FinaleShow
    {
        public static IEnumerator Play(WorldBuilder.Garden garden, MonoBehaviour host)
        {
            var root = garden.Root;
            if (root == null) yield break;
            SkyCycle.RushNight(2.35f);
            Sfx.GardenWake();
            Sfx.Rumble();

            var fx = new GameObject("Finale").transform;
            fx.SetParent(root, false);

            Live = true;
            host.StartCoroutine(Fireworks(fx, host));
            host.StartCoroutine(RumbleTrain());

            yield return new WaitForSeconds(0.28f);
            yield return SlamLogo(fx, host);
            Live = false;
            SweepSparkles(fx);
        }

        static IEnumerator RumbleTrain()
        {
            for (int i = 0; i < 3; i++)
            {
                Sfx.Rumble();
                yield return new WaitForSeconds(0.4f);
            }
        }

        static bool Live;
        static bool Glow;
        const float PulseDur = 1.6f;

        // Logo / halo / mascots stay. Sparkle leftovers do not.
        static void SweepSparkles(Transform root)
        {
            if (root == null) return;
            var kids = root.GetComponentsInChildren<Transform>(true);
            var drop = new GameObject[kids.Length];
            int n = 0;
            for (int i = 0; i < kids.Length; i++)
            {
                if (kids[i] == null) continue;
                if (!IsLeftoverSparkle(kids[i].name)) continue;
                drop[n++] = kids[i].gameObject;
            }
            for (int i = 0; i < n; i++)
                if (drop[i] != null) Object.Destroy(drop[i]);
        }

        static bool IsLeftoverSparkle(string name)
        {
            return name.StartsWith("Glint")
                || name.StartsWith("Fly")
                || name.StartsWith("Burst")
                || name.StartsWith("Boom")
                || name == "Rocket"
                || name == "Shine";
        }

        const float CapH = 1.78f;
        const float RowGap = 0.34f;
        const float Tracking = -0.055f;
        const float Smile = 0.05f;
        const float MaxWordW = 7.5f;

        static IEnumerator SlamLogo(Transform parent, MonoBehaviour host)
        {
            var hold = new GameObject("LogoHold").transform;
            hold.SetParent(parent, false);
            hold.position = new Vector3(0f, 1.18f, 0f);
            Stage(hold);
            DimStage(hold);

            float flockY = 0.98f;
            float fiveY = flockY - CapH - RowGap;
            var row1 = PlaceWord("FLOCK", flockY, 28, hold, 1f);
            var row2 = PlaceWord("FIVE", fiveY, 28, hold, -1f);

            var ruby = Mascot(BirdColor.Ruby, new Vector3(-2.62f, flockY + CapH * 0.82f, 0f), false, 24, hold);
            var gold = Mascot(BirdColor.Gold, new Vector3(2.62f, flockY + CapH * 0.82f, 0f), true, 24, hold);
            var teal = Mascot(BirdColor.Teal, new Vector3(-3.55f, fiveY - CapH * 0.08f, 0f), false, 24, hold);
            var violet = Mascot(BirdColor.Violet, new Vector3(3.55f, fiveY - CapH * 0.08f, 0f), true, 24, hold);
            ruby.localScale = Vector3.zero;
            gold.localScale = Vector3.zero;
            teal.localScale = Vector3.zero;
            violet.localScale = Vector3.zero;

            var all = new Transform[row1.Length + row2.Length];
            row1.CopyTo(all, 0);
            row2.CopyTo(all, row1.Length);
            var restScale = new Vector3[all.Length];
            var restPos = new Vector3[all.Length];
            for (int i = 0; i < all.Length; i++)
            {
                restScale[i] = all[i].localScale;
                restPos[i] = all[i].localPosition;
                all[i].localScale = Vector3.zero;
            }

            host.StartCoroutine(FadeStage(hold, 0.7f));
            yield return new WaitForSeconds(0.1f);

            const float letterDur = 0.56f;
            const float stagger = 0.055f;
            for (int i = 0; i < all.Length; i++)
                host.StartCoroutine(Popcorn(all[i], restScale[i], restPos[i], letterDur, i * stagger));

            float lettersDone = (all.Length - 1) * stagger + letterDur;
            yield return new WaitForSeconds(lettersDone * 0.62f);

            Sfx.Takeoff(4);
            Sfx.Rumble();
            host.StartCoroutine(PopBird(ruby, 0.50f, 0f));
            host.StartCoroutine(PopBird(gold, 0.50f, 0.06f));
            host.StartCoroutine(PopBird(teal, 0.52f, 0.14f));
            host.StartCoroutine(PopBird(violet, 0.52f, 0.2f));

            yield return new WaitForSeconds(lettersDone * 0.38f + 0.08f);
            yield return Breathe(hold, 0.62f, 1.035f);
            Sfx.Takeoff(4);

            Glow = true;
            for (int i = 0; i < all.Length; i++)
                if (all[i] != null) Glints(all[i].gameObject, 28);
            host.StartCoroutine(Shine(hold));
            host.StartCoroutine(Twinkle(hold, PulseDur));
            host.StartCoroutine(Fireflies(hold));
            yield return new WaitForSeconds(0.22f);
            host.StartCoroutine(Burst(hold, true));

            yield return PulseHold(hold, PulseDur);
            Settle(all, restScale, restPos);
            Glow = false;
            SweepSparkles(hold);

            var peach = Mascot(BirdColor.Peach, new Vector3(0f, 0.28f, 5.4f), false, 8, hold);
            yield return SlingPeach(peach, hold, host, all, new[] { ruby, gold, teal, violet });
        }

        static IEnumerator SlingPeach(Transform peach, Transform hold, MonoBehaviour host, Transform[] letters, Transform[] crew)
        {
            if (peach == null) yield break;
            var idle = peach.GetComponent<BirdIdle>();
            if (idle != null)
            {
                idle.Frozen = true;
                idle.Flapping = true;
                idle.Lift = 0.2f;
                idle.RestScale = Vector3.one * 0.5f;
            }
            // Leave the lockup so the letter blast cannot drag her with it.
            if (hold != null && hold.parent != null)
                peach.SetParent(hold.parent, true);

            var sr = peach.GetComponent<SpriteRenderer>();
            peach.localPosition = new Vector3(0f, 1.4f, 5.6f);
            peach.localScale = Vector3.one * 0.07f;
            if (sr != null) sr.sortingOrder = 8;

            float t = 0f;
            const float pull = 0.28f;
            while (t < pull && peach != null)
            {
                t += Time.deltaTime;
                float u = Smooth01(t / pull);
                peach.localPosition = new Vector3(0f, 1.4f, Mathf.Lerp(5.6f, 6.8f, u));
                peach.localScale = Vector3.one * Mathf.Lerp(0.07f, 0.05f, u);
                yield return null;
            }

            Sfx.FlockFlutter(1);
            Sfx.Rumble();
            t = 0f;
            const float approach = 0.36f;
            while (t < approach && peach != null)
            {
                t += Time.deltaTime;
                float k = Mathf.Pow(Mathf.Clamp01(t / approach), 2.4f);
                peach.localPosition = new Vector3(0f, Mathf.Lerp(1.4f, 1.18f, k), Mathf.Lerp(6.8f, 0.18f, k));
                peach.localScale = Vector3.one * Mathf.Lerp(0.05f, 1.15f, k);
                if (sr != null) sr.sortingOrder = 8;
                yield return null;
            }

            Sfx.Break();
            Sfx.Rumble();
            if (CamShake.Live != null) CamShake.Live.Punch(0.38f, 0.18f, 2.6f, 0.14f);
            if (sr != null) sr.sortingOrder = 72;
            host.StartCoroutine(Burst(hold, false));
            host.StartCoroutine(ScatterOutro(hold, letters, crew));

            t = 0f;
            const float through = 0.62f;
            var face = peach != null ? peach.Find("Mood") : null;
            var faceSr = face != null ? face.GetComponent<SpriteRenderer>() : null;
            while (t < through && peach != null)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / through);
                float k = 1f - (1f - u) * (1f - u);
                peach.localPosition = new Vector3(0f, Mathf.Lerp(1.18f, 0.55f, k), Mathf.Lerp(0.18f, -8.4f, k));
                peach.localScale = Vector3.one * Mathf.Lerp(1.15f, 5.4f, k);
                int order = 72 + Mathf.RoundToInt(k * 20f);
                if (sr != null)
                {
                    sr.sortingOrder = order;
                    var c = sr.color;
                    c.a = u > 0.62f ? 1f - Smooth01((u - 0.62f) / 0.38f) : 1f;
                    sr.color = c;
                }
                if (faceSr != null)
                {
                    faceSr.sortingOrder = order + 1;
                    var c = faceSr.color;
                    c.a = sr != null ? sr.color.a : 1f;
                    faceSr.color = c;
                }
                yield return null;
            }
            if (peach != null) Object.Destroy(peach.gameObject);
            yield return new WaitForSeconds(0.85f);
        }

        static float Smooth01(float u)
        {
            u = Mathf.Clamp01(u);
            return u * u * u * (u * (u * 6f - 15f) + 10f);
        }

        static float EaseOutCubic(float u)
        {
            u = Mathf.Clamp01(u);
            float i = 1f - u;
            return 1f - i * i * i;
        }

        static float EaseOutBack(float u, float k)
        {
            u = Mathf.Clamp01(u);
            float c = k + 1f;
            float p = u - 1f;
            return 1f + c * p * p * p + k * p * p;
        }

        static IEnumerator Breathe(Transform hold, float dur, float peak)
        {
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float u = Smooth01(t / dur);
                float k = 1f + (peak - 1f) * Mathf.Sin(u * Mathf.PI);
                hold.localScale = Vector3.one * k;
                yield return null;
            }
            hold.localScale = Vector3.one;
        }

        static IEnumerator PulseHold(Transform hold, float dur)
        {
            float t = 0f;
            const float fadeIn = 0.38f;
            const float fadeOut = 0.55f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float env;
                if (t < fadeIn) env = Smooth01(t / fadeIn);
                else if (t > dur - fadeOut) env = 1f - Smooth01((t - (dur - fadeOut)) / fadeOut);
                else env = 1f;
                float k = 1f + 0.026f * env * Mathf.Sin(t * 5.1f);
                hold.localScale = Vector3.one * k;
                yield return null;
            }
            hold.localScale = Vector3.one;
        }

        static void DimStage(Transform hold)
        {
            var srs = hold.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < srs.Length; i++)
            {
                if (srs[i] == null) continue;
                if (srs[i].name != "Halo" && srs[i].name != "HaloCore" && srs[i].name != "Floor") continue;
                var c = srs[i].color;
                c.a = 0f;
                srs[i].color = c;
            }
        }

        static IEnumerator FadeStage(Transform hold, float dur)
        {
            var srs = hold.GetComponentsInChildren<SpriteRenderer>(true);
            var from = new Color[srs.Length];
            var to = new Color[srs.Length];
            for (int i = 0; i < srs.Length; i++)
            {
                if (srs[i] == null) continue;
                if (srs[i].name != "Halo" && srs[i].name != "HaloCore" && srs[i].name != "Floor") continue;
                to[i] = srs[i].name == "Halo" ? new Color(1f, 0.66f, 0.18f, 0.72f)
                    : srs[i].name == "HaloCore" ? new Color(1f, 0.92f, 0.62f, 0.78f)
                    : new Color(1f, 0.55f, 0.16f, 0.48f);
                from[i] = to[i];
                from[i].a = 0f;
                srs[i].color = from[i];
            }
            float t = 0f;
            while (t < dur && hold != null)
            {
                t += Time.deltaTime;
                float u = EaseOutCubic(t / dur);
                for (int i = 0; i < srs.Length; i++)
                {
                    if (srs[i] == null) continue;
                    if (to[i].a <= 0f) continue;
                    srs[i].color = Color.Lerp(from[i], to[i], u);
                }
                yield return null;
            }
        }

        static IEnumerator ScatterOutro(Transform hold, Transform[] letters, Transform[] birds)
        {
            for (int i = 0; i < birds.Length; i++)
            {
                if (birds[i] == null) continue;
                var idle = birds[i].GetComponent<BirdIdle>();
                if (idle == null) continue;
                idle.Frozen = true;
                idle.Flapping = false;
            }

            Sfx.Takeoff(4);
            hold.localScale = Vector3.one;

            int n = letters.Length;
            var vel = new Vector3[n];
            var spin = new float[n];
            var delay = new float[n];
            var srs = new SpriteRenderer[n][];
            var baseA = new float[n][];
            var rest = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                if (letters[i] == null) continue;
                rest[i] = letters[i].localScale;
                var d = letters[i].localPosition;
                d.z = 0f;
                if (d.sqrMagnitude < 0.05f) d = Vector3.up;
                d.y += 0.45f;
                d.Normalize();
                vel[i] = d * Random.Range(9.2f, 14.5f);
                spin[i] = (letters[i].localPosition.x >= 0f ? 1f : -1f) * Random.Range(280f, 520f);
                delay[i] = Mathf.Clamp01(letters[i].localPosition.magnitude / 4.4f) * 0.03f;
                srs[i] = letters[i].GetComponentsInChildren<SpriteRenderer>(true);
                baseA[i] = new float[srs[i].Length];
                for (int k = 0; k < srs[i].Length; k++)
                    baseA[i][k] = srs[i][k] != null ? srs[i][k].color.a : 0f;
            }

            int bn = birds.Length;
            var bVel = new Vector3[bn];
            var bSrs = new SpriteRenderer[bn][];
            var bA = new float[bn][];
            var bScale = new Vector3[bn];
            for (int i = 0; i < bn; i++)
            {
                if (birds[i] == null) continue;
                float side = birds[i].localPosition.x >= 0f ? 1f : -1f;
                bVel[i] = new Vector3(side * Random.Range(5.2f, 8.4f), Random.Range(4.2f, 6.8f), 0f);
                bSrs[i] = birds[i].GetComponentsInChildren<SpriteRenderer>(true);
                bA[i] = new float[bSrs[i].Length];
                for (int k = 0; k < bSrs[i].Length; k++)
                    bA[i][k] = bSrs[i][k] != null ? bSrs[i][k].color.a : 0f;
                bScale[i] = birds[i].localScale;
            }

            var halo = hold.GetComponentsInChildren<SpriteRenderer>(true);
            var haloA = new float[halo.Length];
            var haloOk = new bool[halo.Length];
            for (int i = 0; i < halo.Length; i++)
            {
                if (halo[i] == null) continue;
                string nm = halo[i].name;
                haloOk[i] = nm == "Halo" || nm == "HaloCore" || nm == "Floor";
                if (haloOk[i]) haloA[i] = halo[i].color.a;
            }

            const float dur = 1.18f;
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float dt = Time.deltaTime;
                for (int i = 0; i < n; i++)
                {
                    if (letters[i] == null) continue;
                    float age = t - delay[i];
                    if (age < 0f) continue;
                    vel[i].y -= 7.4f * dt;
                    vel[i] *= Mathf.Exp(-1.05f * dt);
                    letters[i].localPosition += vel[i] * dt;
                    letters[i].Rotate(0f, 0f, spin[i] * dt);
                    float u = Smooth01(Mathf.Clamp01(age / Mathf.Max(0.2f, dur - delay[i])));
                    float pop = 1f + 0.14f * (1f - u) * Mathf.Sin(Mathf.Clamp01(age * 8f) * Mathf.PI);
                    letters[i].localScale = rest[i] * Mathf.Lerp(1f, 0.42f, u) * pop;
                    float a = (1f - u) * (1f - u);
                    FadeSprites(srs[i], baseA[i], a);
                }
                float bu = Smooth01(Mathf.Clamp01(t / dur));
                float ba = (1f - bu) * (1f - bu);
                for (int i = 0; i < bn; i++)
                {
                    if (birds[i] == null) continue;
                    bVel[i].y -= 4.6f * dt;
                    bVel[i] *= Mathf.Exp(-0.7f * dt);
                    birds[i].localPosition += bVel[i] * dt;
                    birds[i].Rotate(0f, 0f, (birds[i].localPosition.x >= 0f ? -1f : 1f) * 90f * dt);
                    birds[i].localScale = bScale[i] * Mathf.Lerp(1f, 0.55f, bu);
                    FadeSprites(bSrs[i], bA[i], ba);
                }
                float hu = 1f - Smooth01(Mathf.Clamp01(t / 0.48f));
                for (int i = 0; i < halo.Length; i++)
                {
                    if (!haloOk[i] || halo[i] == null) continue;
                    var c = halo[i].color;
                    c.a = haloA[i] * hu;
                    halo[i].color = c;
                }
                yield return null;
            }

            for (int i = 0; i < n; i++)
                if (letters[i] != null) Object.Destroy(letters[i].gameObject);
            for (int i = 0; i < bn; i++)
                if (birds[i] != null) Object.Destroy(birds[i].gameObject);
        }

        static void FadeSprites(SpriteRenderer[] srs, float[] baseA, float a)
        {
            if (srs == null || baseA == null) return;
            for (int k = 0; k < srs.Length; k++)
            {
                if (srs[k] == null) continue;
                var c = srs[k].color;
                c.a = baseA[k] * a;
                srs[k].color = c;
            }
        }

        static void Stage(Transform hold)
        {
            var halo = WorldBuilder.Sprite("Halo", SpriteCatalog.Glow, hold.position, 1f, 8, hold);
            halo.transform.localPosition = new Vector3(0f, -0.12f, 0f);
            halo.transform.localScale = new Vector3(16f, 12f, 1f);
            halo.GetComponent<SpriteRenderer>().color = new Color(1f, 0.66f, 0.18f, 0.72f);

            var core = WorldBuilder.Sprite("HaloCore", SpriteCatalog.Glow, hold.position, 1f, 9, hold);
            core.transform.localPosition = new Vector3(0f, 0.08f, 0f);
            core.transform.localScale = new Vector3(8.5f, 6.6f, 1f);
            core.GetComponent<SpriteRenderer>().color = new Color(1f, 0.92f, 0.62f, 0.78f);

            var floor = WorldBuilder.Sprite("Floor", SpriteCatalog.Glow, hold.position, 1f, 7, hold);
            floor.transform.localPosition = new Vector3(0f, -2.45f, 0f);
            floor.transform.localScale = new Vector3(12f, 4.2f, 1f);
            floor.GetComponent<SpriteRenderer>().color = new Color(1f, 0.55f, 0.16f, 0.48f);
        }

        static void Glints(GameObject face, int order)
        {
            float[] xs = { -0.22f, 0.18f };
            float[] ys = { 0.30f, -0.06f };
            float[] sc = { 0.15f, 0.10f };
            for (int i = 0; i < 2; i++)
            {
                var go = WorldBuilder.Sprite("Glint" + i, SpriteCatalog.Sparkle, face.transform.position, sc[i], order + 3, face.transform);
                go.transform.localPosition = new Vector3(xs[i], ys[i], 0f);
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.zero;
                go.GetComponent<SpriteRenderer>().color = new Color(1f, 0.96f, 0.82f, 0f);
            }
        }

        static IEnumerator Shine(Transform hold)
        {
            var go = WorldBuilder.Sprite("Shine", SpriteCatalog.Glow, hold.position, 1f, 40, hold);
            go.transform.localScale = new Vector3(2.2f, 7.5f, 1f);
            var sr = go.GetComponent<SpriteRenderer>();
            float t = 0f;
            const float dur = 0.82f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float u = Smooth01(t / dur);
                go.transform.localPosition = new Vector3(Mathf.Lerp(-4.8f, 4.8f, u), 0.05f, 0f);
                sr.color = new Color(1f, 0.98f, 0.86f, Mathf.Sin(u * Mathf.PI) * 0.48f);
                yield return null;
            }
            Object.Destroy(go);
        }

        static IEnumerator Burst(Transform parent, bool gentle)
        {
            int n = gentle ? 8 : 11;
            float life = gentle ? 0.95f : 0.8f;
            float peakA = gentle ? 0.42f : 0.62f;
            float vmin = gentle ? 1.4f : 2.2f;
            float vmax = gentle ? 2.8f : 4.2f;
            var rs = new SpriteRenderer[n];
            var vel = new Vector3[n];
            var origin = parent.position + Vector3.up * 0.15f;
            for (int i = 0; i < n; i++)
            {
                float ang = (i / (float)n) * Mathf.PI * 2f + 0.2f;
                vel[i] = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * Random.Range(vmin, vmax);
                var go = WorldBuilder.Sprite("Burst", SpriteCatalog.Sparkle, origin, 0.04f, 36, parent);
                rs[i] = go.GetComponent<SpriteRenderer>();
                rs[i].color = new Color(1f, 0.96f, 0.82f, 0f);
            }
            float b = 0f;
            while (b < life)
            {
                b += Time.deltaTime;
                float u = Mathf.Clamp01(b / life);
                float fadeIn = Smooth01(Mathf.Clamp01(u / 0.22f));
                float fadeOut = 1f - Smooth01(Mathf.Clamp01((u - 0.28f) / 0.72f));
                float env = fadeIn * fadeOut * peakA;
                float sc = Mathf.Lerp(0.04f, gentle ? 0.13f : 0.17f, EaseOutCubic(u));
                for (int i = 0; i < n; i++)
                {
                    if (rs[i] == null) continue;
                    rs[i].transform.position += vel[i] * Time.deltaTime;
                    vel[i] *= 0.97f;
                    rs[i].transform.localScale = Vector3.one * sc;
                    var c = rs[i].color;
                    c.a = env;
                    rs[i].color = c;
                }
                yield return null;
            }
            for (int i = 0; i < n; i++)
                if (rs[i] != null) Object.Destroy(rs[i].gameObject);
        }

        static IEnumerator Fireflies(Transform hold)
        {
            const int n = 6;
            var flies = new GameObject[n];
            var srs = new SpriteRenderer[n];
            var home = new Vector3[n];
            var tint = new[]
            {
                new Color(1f, 0.32f, 0.36f, 0.7f),
                new Color(1f, 0.78f, 0.25f, 0.7f),
                new Color(0.2f, 0.86f, 0.78f, 0.7f),
                new Color(0.7f, 0.42f, 1f, 0.7f)
            };
            for (int i = 0; i < n; i++)
            {
                float a = i / (float)n * Mathf.PI * 2f;
                home[i] = new Vector3(Mathf.Cos(a) * 3.4f, Mathf.Sin(a) * 2.1f - 0.15f, 0f);
                flies[i] = WorldBuilder.Sprite("Fly" + i, SpriteCatalog.Firefly, hold.position, 0.16f, 21, hold);
                flies[i].transform.localPosition = home[i];
                flies[i].transform.localScale = Vector3.zero;
                srs[i] = flies[i].GetComponent<SpriteRenderer>();
                var c = tint[i % 4];
                c.a = 0f;
                srs[i].color = c;
            }
            float t = 0f;
            const float inDur = 0.55f;
            while (hold != null && Glow && t < PulseDur)
            {
                t += Time.deltaTime;
                float gate = Smooth01(Mathf.Clamp01(t / inDur));
                if (t > PulseDur - 0.4f)
                    gate *= 1f - Smooth01((t - (PulseDur - 0.4f)) / 0.4f);
                for (int i = 0; i < n; i++)
                {
                    if (flies[i] == null) yield break;
                    float local = Smooth01(Mathf.Clamp01((t - i * 0.05f) / inDur));
                    float wob = t * (1.1f + i * 0.17f);
                    flies[i].transform.localPosition = home[i] + new Vector3(Mathf.Sin(wob) * 0.22f, Mathf.Cos(wob * 0.8f) * 0.18f, 0f);
                    flies[i].transform.localScale = Vector3.one * (0.16f * local);
                    var c = srs[i].color;
                    c.a = gate * local * (0.28f + 0.32f * (0.5f + 0.5f * Mathf.Sin(t * 4.2f + i)));
                    srs[i].color = c;
                }
                yield return null;
            }
            for (int i = 0; i < n; i++)
                if (flies[i] != null) Object.Destroy(flies[i]);
        }

        static IEnumerator Twinkle(Transform hold, float dur)
        {
            var srs = hold.GetComponentsInChildren<SpriteRenderer>(true);
            var rest = new Vector3[srs.Length];
            float[] xs = { 0.15f, 0.10f };
            int g = 0;
            for (int i = 0; i < srs.Length; i++)
            {
                if (srs[i] == null || !srs[i].name.StartsWith("Glint")) continue;
                rest[i] = Vector3.one * xs[g++ % 2];
            }
            float t = 0f;
            const float inDur = 0.48f;
            while (t < dur && hold != null && Glow)
            {
                t += Time.deltaTime;
                float gate = Smooth01(Mathf.Clamp01(t / inDur));
                if (t > dur - 0.4f)
                    gate *= 1f - Smooth01((t - (dur - 0.4f)) / 0.4f);
                int k = 0;
                for (int i = 0; i < srs.Length; i++)
                {
                    if (srs[i] == null) continue;
                    if (!srs[i].name.StartsWith("Glint")) continue;
                    float local = Smooth01(Mathf.Clamp01((t - k * 0.028f) / inDur));
                    float tw = 0.5f + 0.5f * Mathf.Sin(t * 5.2f + k * 1.3f);
                    var c = srs[i].color;
                    c.a = gate * local * (0.18f + 0.42f * tw);
                    srs[i].color = c;
                    srs[i].transform.localScale = rest[i] * (0.35f + 0.65f * local) * (0.92f + 0.08f * tw);
                    k++;
                }
                yield return null;
            }
        }

        static float Inflate(int i, int n)
        {
            if (n <= 1) return 1f;
            float u = i / (float)(n - 1);
            return 1.10f + 0.015f * Mathf.Sin(u * Mathf.PI);
        }

        static float Optical(char c)
        {
            switch (c)
            {
                case 'F':
                case 'K': return 1.12f;
                case 'O':
                case 'C': return 1.10f;
                case 'E':
                case 'L': return 1.08f;
                case 'I':
                case 'V': return 1.10f;
                default: return 1f;
            }
        }

        static Vector3 LetterScale(char c, float s)
        {
            if (c == 'K') return new Vector3(s * 1.05f, s * 1.04f, 1f);
            return new Vector3(s, s, 1f);
        }

        static Sprite Glyph(char c)
        {
            if (c == 'V') return PointedV();
            return SpriteCatalog.Letter(c);
        }

        static Sprite _pointedV;

        static Sprite PointedV()
        {
            if (_pointedV != null) return _pointedV;
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            if (!tex.LoadImage(FinaleVBytes.Png))
                return SpriteCatalog.Letter('V');
            _pointedV = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 200f);
            _pointedV.name = "FinaleV";
            return _pointedV;
        }

        static readonly Color ExtrudeNear = new Color(28f / 255f, 44f / 255f, 102f / 255f, 1f);
        static readonly Color ExtrudeFar = new Color(4f / 255f, 7f / 255f, 18f / 255f, 1f);
        static readonly Color DropShadow = new Color(3f / 255f, 5f / 255f, 12f / 255f, 0.62f);
        const int ExtrudeLayers = 12;
        const float ExtrudeStep = 0.026f;
        const float ExtrudeX = 0.78f;

        static Transform[] PlaceWord(string word, float centerY, int order, Transform parent, float curve)
        {
            var letters = new Transform[word.Length];
            var scales = new Vector3[word.Length];
            var widths = new float[word.Length];
            float total = 0f;
            for (int i = 0; i < word.Length; i++)
            {
                var spr = Glyph(word[i]);
                float h = spr != null ? Mathf.Max(0.01f, spr.bounds.size.y) : 1f;
                float w = spr != null ? spr.bounds.size.x : 1f;
                float inf = Inflate(i, word.Length) * Optical(word[i]);
                scales[i] = LetterScale(word[i], CapH / h * inf);
                widths[i] = w * scales[i].x;
                total += widths[i] + (i > 0 ? Tracking : 0f);
            }
            if (total > MaxWordW)
            {
                float k = MaxWordW / total;
                for (int i = 0; i < word.Length; i++)
                {
                    scales[i] *= k;
                    widths[i] *= k;
                }
                total = MaxWordW;
            }
            float x = -total * 0.5f;
            for (int i = 0; i < word.Length; i++)
            {
                x += widths[i] * 0.5f;
                float u = word.Length <= 1 ? 0.5f : i / (float)(word.Length - 1);
                float y = centerY + curve * Smile * Mathf.Sin(u * Mathf.PI);
                var spr = Glyph(word[i]);
                var go = WorldBuilder.Sprite("L" + word[i] + i, spr, parent.position, 1f, order, parent);
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = scales[i];
                go.transform.localPosition = new Vector3(x, y, 0f);
                Extrude(go, spr, order);
                letters[i] = go.transform;
                x += widths[i] * 0.5f + Tracking;
            }
            return letters;
        }

        static void Extrude(GameObject face, Sprite spr, int order)
        {
            if (spr == null) return;
            var t = face.transform;
            var faceSr = face.GetComponent<SpriteRenderer>();
            if (faceSr != null) faceSr.sortingOrder = order + 1;
            var sh = WorldBuilder.Sprite("Sh", spr, t.position, 1f, order - ExtrudeLayers - 1, t);
            sh.transform.localRotation = Quaternion.identity;
            sh.transform.localScale = Vector3.one;
            float depth = (ExtrudeLayers + 6) * ExtrudeStep;
            sh.transform.localPosition = new Vector3(depth * ExtrudeX, -depth, 0f);
            sh.GetComponent<SpriteRenderer>().color = DropShadow;
            for (int d = ExtrudeLayers; d >= 1; d--)
            {
                var go = WorldBuilder.Sprite("Ex" + d, spr, t.position, 1f, order - d, t);
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;
                go.transform.localPosition = new Vector3(d * ExtrudeStep * ExtrudeX, -d * ExtrudeStep, 0f);
                float u = d / (float)ExtrudeLayers;
                go.GetComponent<SpriteRenderer>().color = Color.Lerp(ExtrudeNear, ExtrudeFar, u);
            }
        }

        static void Settle(Transform[] letters, Vector3[] restScale, Vector3[] restPos)
        {
            if (letters == null) return;
            for (int i = 0; i < letters.Length; i++)
            {
                if (letters[i] == null) continue;
                letters[i].localScale = restScale[i];
                letters[i].localPosition = restPos[i];
                letters[i].localRotation = Quaternion.identity;
            }
        }

        static IEnumerator Popcorn(Transform tr, Vector3 restScale, Vector3 restPos, float dur, float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            if (tr == null) yield break;
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / dur);
                float pop = EaseOutBack(u, 0.58f);
                float drop = 1f - EaseOutCubic(u);
                float spin = (1f - Smooth01(u)) * 4.2f * Mathf.Sin(u * Mathf.PI);
                tr.localScale = restScale * pop;
                tr.localPosition = restPos + Vector3.up * drop * 0.42f;
                tr.localRotation = Quaternion.Euler(0f, 0f, spin);
                yield return null;
            }
            tr.localScale = restScale;
            tr.localPosition = restPos;
            tr.localRotation = Quaternion.identity;
        }

        static Transform Mascot(BirdColor col, Vector3 local, bool faceLeft, int order, Transform parent)
        {
            var go = WorldBuilder.Sprite("Mascot" + col, SpriteCatalog.Bird(col), parent.position, 1f, order, parent);
            go.transform.localPosition = local;
            go.transform.localScale = Vector3.zero;
            var idle = go.AddComponent<BirdIdle>();
            idle.RestScale = new Vector3(0.46f, 0.46f, 1f);
            idle.RestLocal = local;
            idle.FaceLeft = faceLeft;
            idle.Lift = 0.12f;
            idle.Frozen = true;
            idle.Flapping = false;
            // Clear celebration: plain birds — no bow/crown kits.
            idle.Bind(new Bird(col, BirdSex.Neutral), local);
            idle.Frozen = true;
            idle.Flapping = false;
            var sr = go.GetComponent<SpriteRenderer>();
            sr.flipX = faceLeft;
            sr.sortingOrder = order;
            return go.transform;
        }

        static IEnumerator PopBird(Transform tr, float scale, float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            if (tr == null) yield break;
            Sfx.FlapHard();
            float t = 0f;
            const float dur = 0.46f;
            var idle = tr.GetComponent<BirdIdle>();
            var from = tr.localPosition + Vector3.up * 0.28f;
            var rest = idle != null ? idle.RestLocal : tr.localPosition;
            while (t < dur)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / dur);
                float s = EaseOutBack(u, 0.48f) * scale;
                tr.localScale = new Vector3(s, s, 1f);
                tr.localPosition = Vector3.Lerp(from, rest, EaseOutCubic(u));
                tr.localRotation = Quaternion.Euler(0f, 0f, (1f - Smooth01(u)) * 5f * Mathf.Sin(u * Mathf.PI));
                yield return null;
            }
            tr.localScale = Vector3.one * scale;
            tr.localPosition = rest;
            tr.localRotation = Quaternion.identity;
            if (idle != null)
            {
                idle.RestScale = Vector3.one * scale;
                idle.RestLocal = rest;
                idle.Flapping = true;
                idle.Frozen = false;
            }
        }

        static IEnumerator Fireworks(Transform parent, MonoBehaviour host)
        {
            var colors = new[] { BirdColor.Ruby, BirdColor.Gold, BirdColor.Teal, BirdColor.Violet };
            for (int n = 0; n < 18; n++)
            {
                if (!Live) yield break;
                var col = colors[n % 4];
                float x;
                float peak;
                if (n % 3 == 0)
                {
                    x = Random.Range(-1.6f, 1.6f);
                    peak = Random.Range(5.0f, 7.0f);
                }
                else
                {
                    float side = (n % 2 == 0) ? -1f : 1f;
                    x = side * Random.Range(3.35f, 4.15f);
                    peak = Random.Range(2.4f, 5.8f);
                }
                host.StartCoroutine(Rocket(parent, new Vector3(x, -7.4f, 0f), peak, col, n % 3 == 0));
                yield return new WaitForSeconds(0.12f);
            }
            yield return new WaitForSeconds(1.2f);
        }

        static IEnumerator Rocket(Transform parent, Vector3 from, float peakY, BirdColor col, bool boom)
        {
            if (!Live || parent == null) yield break;
            var spark = WorldBuilder.Sprite("Rocket", SpriteCatalog.Glow, from, 0.22f, 17, parent);
            var sr = spark.GetComponent<SpriteRenderer>();
            sr.color = Wow.Of(col);
            float t = 0f;
            const float up = 0.42f;
            while (t < up)
            {
                if (spark == null || !Live) yield break;
                t += Time.deltaTime;
                float u = t / up;
                spark.transform.position = Vector3.Lerp(from, new Vector3(from.x, peakY, 0f), u * u);
                yield return null;
            }
            if (spark == null || !Live) yield break;
            var pos = spark.transform.position;
            Object.Destroy(spark);
            if (boom) Sfx.Firework();
            int bits = 18;
            var rs = new SpriteRenderer[bits];
            var vel = new Vector3[bits];
            var tint = Wow.Of(col);
            for (int i = 0; i < bits; i++)
            {
                float ang = (i / (float)bits) * Mathf.PI * 2f + Random.Range(-0.12f, 0.12f);
                float spd = Random.Range(2.4f, 5.6f);
                vel[i] = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * spd;
                bool star = i % 3 != 0;
                var go = WorldBuilder.Sprite("Boom", star ? SpriteCatalog.Sparkle : SpriteCatalog.Glow, pos,
                    star ? 0.14f : 0.26f, 19, parent);
                rs[i] = go.GetComponent<SpriteRenderer>();
                rs[i].color = star ? Color.Lerp(Color.white, tint, 0.35f) : new Color(tint.r, tint.g, tint.b, 0.95f);
            }
            float b = 0f;
            while (b < 0.85f)
            {
                b += Time.deltaTime;
                float u = b / 0.85f;
                for (int i = 0; i < bits; i++)
                {
                    if (rs[i] == null) continue;
                    vel[i].y -= 6.5f * Time.deltaTime;
                    rs[i].transform.position += vel[i] * Time.deltaTime;
                    var c = rs[i].color;
                    c.a = (1f - u) * (1f - u);
                    rs[i].color = c;
                }
                yield return null;
            }
            for (int i = 0; i < bits; i++)
                if (rs[i] != null) Object.Destroy(rs[i].gameObject);
        }
    }
}
