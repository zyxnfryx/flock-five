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

            host.StartCoroutine(Fireworks(fx, host));
            host.StartCoroutine(RumbleTrain());

            yield return new WaitForSeconds(0.35f);
            yield return SlamLogo(fx);
            yield return new WaitForSeconds(2.8f);
        }

        static IEnumerator RumbleTrain()
        {
            for (int i = 0; i < 3; i++)
            {
                Sfx.Rumble();
                yield return new WaitForSeconds(0.4f);
            }
        }

        const float CapH = 2.12f;
        const float RowGap = -0.04f;

        static IEnumerator SlamLogo(Transform parent)
        {
            var hold = new GameObject("LogoHold").transform;
            hold.SetParent(parent, false);
            hold.position = new Vector3(0f, 1.42f, 0f);

            float flockY = 1.08f;
            float fiveY = flockY - CapH - RowGap;
            var row1 = PlaceWord("FLOCK", flockY, 26, hold, 1f);
            var row2 = PlaceWord("FIVE", fiveY, 26, hold, -1f);

            var ruby = Mascot(BirdColor.Ruby, new Vector3(-1.95f, flockY + CapH * 0.48f, 0f), false, 24, hold);
            var gold = Mascot(BirdColor.Gold, new Vector3(1.9f, flockY + CapH * 0.48f, 0f), true, 24, hold);
            var teal = Mascot(BirdColor.Teal, new Vector3(-3.85f, fiveY, 0f), false, 24, hold);
            var violet = Mascot(BirdColor.Violet, new Vector3(3.85f, fiveY, 0f), true, 24, hold);
            ruby.transform.localScale = Vector3.zero;
            gold.transform.localScale = Vector3.zero;
            teal.transform.localScale = Vector3.zero;
            violet.transform.localScale = Vector3.zero;

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

            Sfx.Chirp();
            for (int i = 0; i < all.Length; i++)
                yield return Popcorn(all[i], restScale[i], restPos[i]);

            Settle(all, restScale, restPos);

            Sfx.Takeoff(4);
            Sfx.Rumble();
            yield return PopBird(ruby, 0.48f);
            yield return PopBird(gold, 0.48f);
            yield return PopBird(teal, 0.5f);
            yield return PopBird(violet, 0.5f);
            Sfx.Takeoff(4);

            float pulse = 0f;
            while (pulse < 2.2f)
            {
                pulse += Time.deltaTime;
                float b = 1f + 0.04f * Mathf.Sin(pulse * 6.5f);
                hold.localScale = Vector3.one * b;
                yield return null;
            }
            hold.localScale = Vector3.one;
            Settle(all, restScale, restPos);
        }

        static float Inflate(int i, int n)
        {
            if (n <= 1) return 1f;
            // Mild pillow only. Ends used to sit at 1.0 against a 1.34 middle,
            // which made F and K look tiny. Keep the outsides close to the rest.
            float u = i / (float)(n - 1);
            return 1.16f + 0.06f * Mathf.Sin(u * Mathf.PI);
        }

        static Transform[] PlaceWord(string word, float centerY, int order, Transform parent, float curve)
        {
            var letters = new Transform[word.Length];
            var scales = new float[word.Length];
            var widths = new float[word.Length];
            const float tracking = -0.12f;
            float total = 0f;
            for (int i = 0; i < word.Length; i++)
            {
                var spr = SpriteCatalog.Letter(word[i]);
                float h = spr != null ? Mathf.Max(0.01f, spr.bounds.size.y) : 1f;
                float w = spr != null ? spr.bounds.size.x : 1f;
                float inf = Inflate(i, word.Length);
                // FLOCK's F and K need a bit extra so the outside lettering holds
                // its weight against O/C.
                if (word == "FLOCK" && (i == 0 || i == word.Length - 1))
                    inf *= 1.06f;
                scales[i] = CapH / h * inf;
                widths[i] = w * scales[i];
                total += widths[i] + (i > 0 ? tracking : 0f);
            }
            float x = -total * 0.5f;
            for (int i = 0; i < word.Length; i++)
            {
                x += widths[i] * 0.5f;
                float u = word.Length <= 1 ? 0.5f : i / (float)(word.Length - 1);
                float y = centerY + curve * 0.08f * Mathf.Sin(u * Mathf.PI);
                var go = WorldBuilder.Sprite("L" + word[i] + i, SpriteCatalog.Letter(word[i]), parent.position, 1f, order, parent);
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one * scales[i];
                go.transform.localPosition = new Vector3(x, y, 0f);
                letters[i] = go.transform;
                x += widths[i] * 0.5f + tracking;
            }
            return letters;
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

        static IEnumerator Popcorn(Transform tr, Vector3 restScale, Vector3 restPos)
        {
            float t = 0f;
            const float dur = 0.28f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / dur);
                float pop;
                if (u < 0.42f)
                    pop = Mathf.SmoothStep(0f, 1.28f, u / 0.42f);
                else if (u < 0.72f)
                    pop = Mathf.Lerp(1.28f, 0.94f, (u - 0.42f) / 0.3f);
                else
                    pop = Mathf.Lerp(0.94f, 1f, (u - 0.72f) / 0.28f);
                tr.localScale = restScale * pop;
                tr.localPosition = restPos + Vector3.up * (1f - u) * (1f - u) * 0.42f;
                tr.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(u * Mathf.PI) * 8f * (1f - u));
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
            go.transform.localScale = Vector3.one * 0.46f;
            var idle = go.AddComponent<BirdIdle>();
            idle.RestScale = new Vector3(0.46f, 0.46f, 1f);
            idle.RestLocal = local;
            idle.FaceLeft = faceLeft;
            idle.Flapping = true;
            idle.Lift = 0.12f;
            idle.Bind(col, local);
            idle.Flapping = true;
            var sr = go.GetComponent<SpriteRenderer>();
            sr.flipX = faceLeft;
            sr.sortingOrder = order;
            return go.transform;
        }

        static IEnumerator PopBird(Transform tr, float scale)
        {
            Sfx.FlapHard();
            float t = 0f;
            while (t < 0.22f)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, t / 0.22f);
                float s = Mathf.Lerp(0f, scale * 1.18f, u);
                tr.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            tr.localScale = Vector3.one * scale;
        }

        static IEnumerator Fireworks(Transform parent, MonoBehaviour host)
        {
            var colors = new[] { BirdColor.Ruby, BirdColor.Gold, BirdColor.Teal, BirdColor.Violet };
            for (int n = 0; n < 3; n++)
            {
                var col = colors[n % 4];
                float x = Random.Range(-2.4f, 2.4f);
                host.StartCoroutine(Rocket(parent, new Vector3(x, -7.2f, 0f), Random.Range(3.6f, 5.6f), col, n == 0 || n == 2));
                yield return new WaitForSeconds(0.55f);
            }
            yield return new WaitForSeconds(1.1f);
        }

        static IEnumerator Rocket(Transform parent, Vector3 from, float peakY, BirdColor col, bool boom)
        {
            var spark = WorldBuilder.Sprite("Rocket", SpriteCatalog.Glow, from, 0.22f, 19, parent);
            var sr = spark.GetComponent<SpriteRenderer>();
            sr.color = Wow.Of(col);
            float t = 0f;
            const float up = 0.42f;
            while (t < up)
            {
                t += Time.deltaTime;
                float u = t / up;
                spark.transform.position = Vector3.Lerp(from, new Vector3(from.x, peakY, 0f), u * u);
                yield return null;
            }
            var pos = spark.transform.position;
            Object.Destroy(spark);
            if (boom) Sfx.Firework();
            int bits = 16;
            var rs = new SpriteRenderer[bits];
            var vel = new Vector3[bits];
            var tint = Wow.Of(col);
            for (int i = 0; i < bits; i++)
            {
                float ang = (i / (float)bits) * Mathf.PI * 2f + Random.Range(-0.12f, 0.12f);
                float spd = Random.Range(2.2f, 5.4f);
                vel[i] = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * spd;
                bool star = i % 3 != 0;
                var go = WorldBuilder.Sprite("Boom", star ? SpriteCatalog.Sparkle : SpriteCatalog.Glow, pos,
                    star ? 0.14f : 0.26f, 21, parent);
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
