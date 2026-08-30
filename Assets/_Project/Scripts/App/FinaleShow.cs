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

            yield return new WaitForSeconds(0.28f);
            yield return SlamLogo(fx);
            yield return new WaitForSeconds(3.2f);
        }

        static IEnumerator RumbleTrain()
        {
            for (int i = 0; i < 3; i++)
            {
                Sfx.Rumble();
                yield return new WaitForSeconds(0.4f);
            }
        }

        const float CapH = 1.96f;
        const float RowGap = 0.10f;
        const float Tracking = -0.10f;
        const float Smile = 0.045f;

        static IEnumerator SlamLogo(Transform parent)
        {
            var hold = new GameObject("LogoHold").transform;
            hold.SetParent(parent, false);
            hold.position = new Vector3(0f, 1.18f, 0f);

            float flockY = 0.98f;
            float fiveY = flockY - CapH - RowGap;
            var row1 = PlaceWord("FLOCK", flockY, 28, hold, 1f);
            var row2 = PlaceWord("FIVE", fiveY, 28, hold, -1f);

            var ruby = Mascot(BirdColor.Ruby, new Vector3(-2.48f, flockY + CapH * 0.82f, 0f), false, 24, hold);
            var gold = Mascot(BirdColor.Gold, new Vector3(2.48f, flockY + CapH * 0.82f, 0f), true, 24, hold);
            var teal = Mascot(BirdColor.Teal, new Vector3(-3.42f, fiveY - CapH * 0.08f, 0f), false, 24, hold);
            var violet = Mascot(BirdColor.Violet, new Vector3(3.42f, fiveY - CapH * 0.08f, 0f), true, 24, hold);
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

            float slam = 0f;
            const float slamDur = 0.22f;
            while (slam < slamDur)
            {
                slam += Time.deltaTime;
                float u = Mathf.Clamp01(slam / slamDur);
                float k = u < 0.45f
                    ? Mathf.SmoothStep(1f, 1.10f, u / 0.45f)
                    : Mathf.SmoothStep(1.10f, 1f, (u - 0.45f) / 0.55f);
                hold.localScale = Vector3.one * k;
                yield return null;
            }
            hold.localScale = Vector3.one;
            Settle(all, restScale, restPos);

            Sfx.Takeoff(4);
            Sfx.Rumble();
            yield return PopBird(ruby, 0.50f);
            yield return PopBird(gold, 0.50f);
            yield return PopBird(teal, 0.52f);
            yield return PopBird(violet, 0.52f);
            Sfx.Takeoff(4);

            float pulse = 0f;
            while (pulse < 1.6f)
            {
                pulse += Time.deltaTime;
                float b = 1f + 0.028f * Mathf.Sin(pulse * 6.5f);
                hold.localScale = Vector3.one * b;
                yield return null;
            }
            hold.localScale = Vector3.one;
            Settle(all, restScale, restPos);
        }

        static float Inflate(int i, int n)
        {
            if (n <= 1) return 1f;
            float u = i / (float)(n - 1);
            return 1.20f + 0.02f * Mathf.Sin(u * Mathf.PI);
        }

        static float Optical(char c)
        {
            switch (c)
            {
                case 'F':
                case 'K': return 1.12f;
                case 'E':
                case 'L': return 1.08f;
                case 'I':
                case 'V': return 1.10f;
                default: return 1f;
            }
        }

        // Slight unsmoosh: K sprite reads squat at lockup size. A hair wider opens the arms, a hair taller lifts the squat.
        static Vector3 LetterScale(char c, float s)
        {
            if (c == 'K') return new Vector3(s * 1.06f, s * 1.04f, 1f);
            return new Vector3(s, s, 1f);
        }

        static Sprite Glyph(char c)
        {
            if (c == 'V') return PointedV();
            return SpriteCatalog.Letter(c);
        }

        static Sprite _pointedV;

        // Pointed V in the FLOCK family. Loaded from FinaleVBytes so FIVE does not use the old bucket glyph.
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

        static readonly Color ExtrudeNear = new Color(8f / 255f, 14f / 255f, 36f / 255f, 1f);
        static readonly Color ExtrudeFar = new Color(4f / 255f, 7f / 255f, 18f / 255f, 1f);
        static readonly Color DropShadow = new Color(3f / 255f, 5f / 255f, 12f / 255f, 0.55f);
        const int ExtrudeLayers = 8;
        const float ExtrudeStep = 0.018f;
        const float ExtrudeX = 0.85f;

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

        // Navy extrusion + drop shadow parented to the face so popcorn/settle/slam carry the 3D stack. Face stays order; layers sit behind.
        static void Extrude(GameObject face, Sprite spr, int order)
        {
            if (spr == null) return;
            var t = face.transform;
            var sh = WorldBuilder.Sprite("Sh", spr, t.position, 1f, order - ExtrudeLayers - 1, t);
            sh.transform.localRotation = Quaternion.identity;
            sh.transform.localScale = Vector3.one;
            float depth = (ExtrudeLayers + 4) * ExtrudeStep;
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

        static IEnumerator Popcorn(Transform tr, Vector3 restScale, Vector3 restPos)
        {
            float t = 0f;
            const float dur = 0.26f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / dur);
                float pop;
                if (u < 0.42f)
                    pop = Mathf.SmoothStep(0f, 1.22f, u / 0.42f);
                else if (u < 0.72f)
                    pop = Mathf.Lerp(1.22f, 0.96f, (u - 0.42f) / 0.3f);
                else
                    pop = Mathf.Lerp(0.96f, 1f, (u - 0.72f) / 0.28f);
                tr.localScale = restScale * pop;
                tr.localPosition = restPos + Vector3.up * (1f - u) * (1f - u) * 0.36f;
                tr.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(u * Mathf.PI) * 6f * (1f - u));
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
            for (int n = 0; n < 18; n++)
            {
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
            var spark = WorldBuilder.Sprite("Rocket", SpriteCatalog.Glow, from, 0.22f, 17, parent);
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
