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
            _cut = false;
            Time.timeScale = 1f;
            host.StartCoroutine(Fireworks(fx, host));
            host.StartCoroutine(RumbleTrain());

            yield return Wait(0.28f);
            if (!_cut)
                yield return SlamLogo(fx, host);
            Live = false;
            Glow = false;
            Time.timeScale = 1f;
            SweepSparkles(fx);
        }

        static IEnumerator RumbleTrain()
        {
            for (int i = 0; i < 3; i++)
            {
                if (Cut()) yield break;
                Sfx.Rumble();
                yield return Wait(0.4f);
            }
        }

        static bool Live;
        static bool Glow;
        static bool _cut;
        const float PulseDur = 1.6f;

        static bool Cut()
        {
            if (_cut) return true;
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                _cut = true;
                return true;
            }
            var touch = UnityEngine.InputSystem.Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.wasPressedThisFrame)
            {
                _cut = true;
                return true;
            }
            return false;
        }

        static IEnumerator Wait(float dur)
        {
            float t = 0f;
            while (t < dur)
            {
                if (Cut()) yield break;
                t += Time.deltaTime;
                yield return null;
            }
        }

        static bool Stopped(Transform[] letters, Vector3[] restScale, Vector3[] restPos)
        {
            if (!_cut) return false;
            Time.timeScale = 1f;
            Glow = false;
            Settle(letters, restScale, restPos);
            return true;
        }

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
                || name.StartsWith("Petal")
                || name.StartsWith("Fountain")
                || name.StartsWith("Feather")
                || name.StartsWith("Ring")
                || name == "Rocket"
                || name == "Shine";
        }

        const float CapH = 1.78f;
        const float RowGap = 0.38f;
        const float Tracking = -0.04f;
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
            yield return Wait(0.1f);
            if (Stopped(all, restScale, restPos)) yield break;

            const float letterDur = 0.56f;
            const float stagger = 0.055f;
            for (int i = 0; i < all.Length; i++)
                host.StartCoroutine(Popcorn(all[i], restScale[i], restPos[i], letterDur, i * stagger));

            float lettersDone = (all.Length - 1) * stagger + letterDur;
            yield return Wait(lettersDone * 0.62f);
            if (Stopped(all, restScale, restPos)) yield break;

            Sfx.Takeoff(4);
            Sfx.Rumble();
            host.StartCoroutine(PopBird(ruby, 0.50f, 0f));
            host.StartCoroutine(PopBird(gold, 0.50f, 0.06f));
            host.StartCoroutine(PopBird(teal, 0.52f, 0.14f));
            host.StartCoroutine(PopBird(violet, 0.52f, 0.2f));

            yield return Wait(lettersDone * 0.38f + 0.08f);
            if (Stopped(all, restScale, restPos)) yield break;
            Settle(all, restScale, restPos);
            yield return Breathe(hold, 0.62f, 1.035f);
            if (Stopped(all, restScale, restPos)) yield break;
            Sfx.Takeoff(4);

            Glow = true;
            for (int i = 0; i < all.Length; i++)
                if (all[i] != null) Glints(all[i].gameObject, 28);
            host.StartCoroutine(Shine(hold));
            host.StartCoroutine(Twinkle(hold, PulseDur));
            host.StartCoroutine(Fireflies(hold));
            host.StartCoroutine(WarmGlow(hold));
            host.StartCoroutine(CoinFountain(hold));
            host.StartCoroutine(PetalShower(hold));
            host.StartCoroutine(CountUp(hold));
            host.StartCoroutine(MascotCheer(new[] { ruby, gold, teal, violet }));
            CheerFlock(hold);
            yield return Wait(0.22f);
            if (Stopped(all, restScale, restPos)) yield break;
            host.StartCoroutine(Burst(hold, true));

            yield return PulseHold(hold, PulseDur);
            if (Stopped(all, restScale, restPos)) yield break;
            Settle(all, restScale, restPos);
            Glow = false;
            SweepSparkles(hold);

            var peach = Mascot(BirdColor.Peach, new Vector3(0f, 0.28f, 5.4f), false, 8, hold);
            yield return SlingPeach(peach, hold, host, all, restScale, restPos);
            if (_cut) Stopped(all, restScale, restPos);
        }

        static IEnumerator SlingPeach(Transform peach, Transform hold, MonoBehaviour host, Transform[] letters, Vector3[] restScale, Vector3[] restPos)
        {
            if (peach == null) yield break;
            var idle = peach.GetComponent<BirdIdle>();
            if (idle != null)
            {
                idle.Frozen = true;
                idle.Flapping = true;
                idle.Lift = 0.2f;
                idle.FlapMul = 1.6f;
                idle.FaceLeft = false;
                idle.RestScale = Vector3.one * 0.55f;
            }
            // Leave the lockup so the letter blast cannot drag her with it.
            if (hold != null && hold.parent != null)
                peach.SetParent(hold.parent, true);

            var sr = peach.GetComponent<SpriteRenderer>();
            var face = peach.Find("Mood");
            var faceSr = face != null ? face.GetComponent<SpriteRenderer>() : null;
            PaintBird(sr, faceSr, 40, 1f);

            Vector3 impact = hold != null
                ? hold.localPosition + new Vector3(0f, -0.08f, 0.12f)
                : new Vector3(0f, 1.08f, 0.12f);
            var from = new Vector3(impact.x, impact.y - 0.62f, 4.2f);
            peach.localPosition = from;
            peach.localRotation = Quaternion.Euler(0f, 0f, -16f);
            peach.localScale = new Vector3(0.36f, 0.36f, 1f);

            float t = 0f;
            const float wind = 0.15f;
            while (t < wind && peach != null)
            {
                if (Cut()) { AbortPeach(peach, letters, restScale, restPos); yield break; }
                t += Time.deltaTime;
                float u = Smooth01(t / wind);
                float sc = Mathf.Lerp(0.36f, 0.27f, u);
                peach.localScale = new Vector3(sc * Mathf.Lerp(1f, 1.34f, u), sc * Mathf.Lerp(1f, 0.66f, u), 1f);
                peach.localPosition = Vector3.Lerp(from, new Vector3(from.x, from.y - 0.2f, 5.2f), u);
                peach.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-16f, -28f, u));
                yield return null;
            }
            if (peach == null || Cut()) { AbortPeach(peach, letters, restScale, restPos); yield break; }

            Sfx.FlapHard();
            if (idle != null) idle.FlapMul = 3.15f;
            Vector3 dipped = peach.localPosition;
            t = 0f;
            const float burst = 0.18f;
            while (t < burst && peach != null)
            {
                if (Cut()) { AbortPeach(peach, letters, restScale, restPos); yield break; }
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / burst);
                float k = u * u;
                float sc = Mathf.Lerp(0.30f, 1.14f, k);
                float stretch = Mathf.Lerp(1.06f, 1.48f, Mathf.Sin(u * Mathf.PI));
                peach.localPosition = Vector3.Lerp(dipped, impact, k);
                peach.localScale = new Vector3(sc / stretch, sc * stretch, 1f);
                peach.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-28f, 12f, EaseOutCubic(u)));
                PaintBird(sr, faceSr, 48 + Mathf.RoundToInt(k * 28f), 1f);
                yield return null;
            }
            if (peach == null || Cut()) { AbortPeach(peach, letters, restScale, restPos); yield break; }

            peach.localPosition = impact;
            peach.localScale = new Vector3(1.4f, 0.7f, 1f);
            peach.localRotation = Quaternion.Euler(0f, 0f, 6f);
            PaintBird(sr, faceSr, 80, 1f);
            Sfx.Break();
            Sfx.Rumble();
            yield return HitStop();
            if (peach == null || Cut()) { AbortPeach(peach, letters, restScale, restPos); yield break; }

            if (CamShake.Live != null) CamShake.Live.Punch(0.18f, 0.06f, 0.85f, 0.04f);
            if (host != null && hold != null)
            {
                host.StartCoroutine(ShockRing(hold));
                host.StartCoroutine(FeatherBurst(hold));
                host.StartCoroutine(KnockReform(letters, restScale, restPos));
            }

            if (idle != null) idle.FlapMul = 2.6f;
            t = 0f;
            const float loopDur = 0.70f;
            while (t < loopDur && peach != null)
            {
                if (Cut()) { AbortPeach(peach, letters, restScale, restPos); yield break; }
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / loopDur);
                float ang = u * Mathf.PI * 2f;
                const float rx = 1.9f;
                const float ry = 1.02f;
                peach.localPosition = new Vector3(
                    impact.x + Mathf.Sin(ang) * rx,
                    impact.y + (Mathf.Cos(ang) - 1f) * ry,
                    0.16f);
                float tx = Mathf.Cos(ang) * rx;
                float ty = -Mathf.Sin(ang) * ry;
                peach.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(ty, tx) * Mathf.Rad2Deg);
                const float sc = 0.64f;
                peach.localScale = new Vector3(sc * 1.22f, sc * 0.82f, 1f);
                PaintBird(sr, faceSr, 82, 1f);
                yield return null;
            }
            if (peach == null || Cut()) { AbortPeach(peach, letters, restScale, restPos); yield break; }

            if (idle != null) idle.FaceLeft = true;
            if (sr != null) sr.flipX = true;
            var land = new Vector3(impact.x + 2.4f, impact.y - 1.2f, 0f);
            var leave = peach.localPosition;
            t = 0f;
            const float landDur = 0.26f;
            while (t < landDur && peach != null)
            {
                if (Cut()) { AbortPeach(peach, letters, restScale, restPos); yield break; }
                t += Time.deltaTime;
                float u = EaseOutCubic(t / landDur);
                var p = Vector3.Lerp(leave, land, u);
                p.y += Mathf.Sin(u * Mathf.PI) * 0.38f;
                peach.localPosition = p;
                peach.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(18f, 0f, u));
                float sc = Mathf.Lerp(0.64f, 0.55f, u);
                float squash = u > 0.7f ? Mathf.Sin((u - 0.7f) / 0.3f * Mathf.PI) * 0.24f : 0f;
                peach.localScale = new Vector3(sc * (1f + squash), sc * (1f - squash * 0.9f), 1f);
                PaintBird(sr, faceSr, 36, 1f);
                yield return null;
            }
            if (peach == null || Cut()) { AbortPeach(peach, letters, restScale, restPos); yield break; }

            t = 0f;
            const float settle = 0.16f;
            while (t < settle && peach != null)
            {
                if (Cut()) { AbortPeach(peach, letters, restScale, restPos); yield break; }
                t += Time.deltaTime;
                float b = Mathf.Sin(Mathf.Clamp01(t / settle) * Mathf.PI);
                peach.localPosition = land;
                peach.localRotation = Quaternion.identity;
                peach.localScale = new Vector3(0.55f * (1f + 0.16f * b), 0.55f * (1f - 0.14f * b), 1f);
                yield return null;
            }
            if (peach == null) yield break;
            peach.localPosition = land;
            peach.localRotation = Quaternion.identity;
            peach.localScale = Vector3.one * 0.55f;
            PaintBird(sr, faceSr, 32, 1f);
            if (idle != null)
            {
                idle.RestLocal = land;
                idle.RestScale = Vector3.one * 0.55f;
                idle.FaceLeft = true;
                idle.FlapMul = 1f;
                idle.Flapping = true;
                idle.Lift = 0.06f;
                idle.Frozen = false;
            }
            yield return Wait(0.18f);
        }

        static void PaintBird(SpriteRenderer sr, SpriteRenderer face, int order, float alpha)
        {
            if (sr != null)
            {
                sr.sortingOrder = order;
                var c = sr.color;
                c.a = alpha;
                sr.color = c;
            }
            if (face != null)
            {
                face.sortingOrder = order + 1;
                var c = face.color;
                c.a = alpha;
                face.color = c;
            }
        }

        static void AbortPeach(Transform peach, Transform[] letters, Vector3[] restScale, Vector3[] restPos)
        {
            Time.timeScale = 1f;
            Settle(letters, restScale, restPos);
            if (peach != null) Object.Destroy(peach.gameObject);
        }

        static IEnumerator HitStop()
        {
            float prev = Time.timeScale;
            Time.timeScale = 0.02f;
            float t = 0f;
            while (t < 0.05f)
            {
                if (Cut()) break;
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            Time.timeScale = prev > 0.05f ? prev : 1f;
        }

        static IEnumerator KnockReform(Transform[] letters, Vector3[] restScale, Vector3[] restPos)
        {
            if (letters == null) yield break;
            int n = letters.Length;
            var vel = new Vector3[n];
            var spin = new float[n];
            var rot = new float[n];
            for (int i = 0; i < n; i++)
            {
                if (letters[i] == null) continue;
                var d = letters[i].localPosition;
                d.z = 0f;
                if (d.sqrMagnitude < 0.04f) d = Vector3.up;
                d.y += 0.42f;
                d.Normalize();
                vel[i] = d * Random.Range(3.5f, 5.5f);
                spin[i] = (letters[i].localPosition.x >= 0f ? 1f : -1f) * Random.Range(260f, 540f);
            }
            float t = 0f;
            const float outDur = 0.40f;
            while (t < outDur)
            {
                if (Cut()) { Settle(letters, restScale, restPos); yield break; }
                float dt = Time.deltaTime;
                t += dt;
                float u = Mathf.Clamp01(t / outDur);
                for (int i = 0; i < n; i++)
                {
                    if (letters[i] == null) continue;
                    vel[i].y -= 12.5f * dt;
                    letters[i].localPosition += vel[i] * dt;
                    rot[i] += spin[i] * dt;
                    letters[i].localRotation = Quaternion.Euler(0f, 0f, rot[i]);
                    if (restScale != null && i < restScale.Length)
                        letters[i].localScale = restScale[i] * Mathf.Lerp(1.1f, 0.9f, u);
                }
                yield return null;
            }
            var fromP = new Vector3[n];
            var fromR = new float[n];
            var fromS = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                if (letters[i] == null) continue;
                fromP[i] = letters[i].localPosition;
                fromR[i] = letters[i].localEulerAngles.z;
                fromS[i] = letters[i].localScale;
            }
            t = 0f;
            const float back = 0.42f;
            while (t < back)
            {
                if (Cut()) { Settle(letters, restScale, restPos); yield break; }
                t += Time.deltaTime;
                float u = EaseOutCubic(t / back);
                for (int i = 0; i < n; i++)
                {
                    if (letters[i] == null) continue;
                    var restP = restPos != null && i < restPos.Length ? restPos[i] : fromP[i];
                    var restS = restScale != null && i < restScale.Length ? restScale[i] : fromS[i];
                    letters[i].localPosition = Vector3.Lerp(fromP[i], restP, u);
                    letters[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.LerpAngle(fromR[i], 0f, u));
                    letters[i].localScale = Vector3.Lerp(fromS[i], restS, u);
                }
                yield return null;
            }
            Settle(letters, restScale, restPos);
        }

        static IEnumerator ShockRing(Transform parent)
        {
            if (parent == null) yield break;
            const int n = 16;
            var rs = new SpriteRenderer[n];
            var origin = parent.position + new Vector3(0f, -0.08f, 0f);
            for (int i = 0; i < n; i++)
            {
                var go = WorldBuilder.Sprite("Ring", SpriteCatalog.Glow, origin, 0.3f, 70, parent);
                rs[i] = go.GetComponent<SpriteRenderer>();
                rs[i].color = new Color(1f, 0.9f, 0.55f, 0.85f);
            }
            float t = 0f;
            const float dur = 0.36f;
            while (t < dur && parent != null)
            {
                if (_cut) break;
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / dur);
                float rad = Mathf.Lerp(0.2f, 3.35f, EaseOutCubic(u));
                float a = (1f - u) * (1f - u);
                for (int i = 0; i < n; i++)
                {
                    if (rs[i] == null) continue;
                    float ang = (i / (float)n) * Mathf.PI * 2f;
                    rs[i].transform.position = origin + new Vector3(Mathf.Cos(ang) * rad * 1.28f, Mathf.Sin(ang) * rad, 0f);
                    float sc = Mathf.Lerp(0.4f, 0.07f, u);
                    rs[i].transform.localScale = new Vector3(sc * 1.45f, sc * 0.5f, 1f);
                    rs[i].transform.localRotation = Quaternion.Euler(0f, 0f, ang * Mathf.Rad2Deg);
                    var c = rs[i].color;
                    c.a = a * 0.82f;
                    rs[i].color = c;
                }
                yield return null;
            }
            for (int i = 0; i < n; i++)
                if (rs[i] != null) Object.Destroy(rs[i].gameObject);
        }

        static IEnumerator FeatherBurst(Transform parent)
        {
            if (parent == null) yield break;
            const int n = 14;
            var rs = new SpriteRenderer[n];
            var vel = new Vector3[n];
            var spin = new float[n];
            var origin = parent.position + new Vector3(0f, -0.04f, 0f);
            var feather = SpriteCatalog.Feather;
            var spark = SpriteCatalog.Sparkle;
            for (int i = 0; i < n; i++)
            {
                bool soft = (i & 1) == 0 && feather != null;
                float ang = (i / (float)n) * Mathf.PI * 2f + Random.Range(-0.14f, 0.14f);
                vel[i] = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * Random.Range(2.6f, 5.2f);
                vel[i].y += 1.5f;
                spin[i] = Random.Range(-460f, 460f);
                var go = WorldBuilder.Sprite(soft ? "Feather" : "Burst", soft ? feather : spark, origin, soft ? 0.2f : 0.11f, 76, parent);
                rs[i] = go.GetComponent<SpriteRenderer>();
                rs[i].color = soft ? new Color(1f, 0.93f, 0.78f, 1f) : new Color(1f, 0.96f, 0.7f, 1f);
            }
            float t = 0f;
            const float dur = 0.7f;
            while (t < dur && parent != null)
            {
                if (_cut) break;
                float dt = Time.deltaTime;
                t += dt;
                float u = Mathf.Clamp01(t / dur);
                float a = (1f - u) * (1f - u);
                for (int i = 0; i < n; i++)
                {
                    if (rs[i] == null) continue;
                    vel[i].y -= 7.4f * dt;
                    rs[i].transform.position += vel[i] * dt;
                    rs[i].transform.Rotate(0f, 0f, spin[i] * dt);
                    float sc = Mathf.Lerp(rs[i].name == "Feather" ? 0.2f : 0.11f, 0.05f, u);
                    rs[i].transform.localScale = Vector3.one * sc;
                    var c = rs[i].color;
                    c.a = a;
                    rs[i].color = c;
                }
                yield return null;
            }
            for (int i = 0; i < n; i++)
                if (rs[i] != null) Object.Destroy(rs[i].gameObject);
        }

        static IEnumerator WarmGlow(Transform hold)
        {
            if (hold == null) yield break;
            var srs = hold.GetComponentsInChildren<SpriteRenderer>(true);
            var halo = new SpriteRenderer[srs.Length];
            var baseS = new Vector3[srs.Length];
            var baseC = new Color[srs.Length];
            int n = 0;
            for (int i = 0; i < srs.Length; i++)
            {
                if (srs[i] == null) continue;
                if (srs[i].name != "Halo" && srs[i].name != "HaloCore") continue;
                halo[n] = srs[i];
                baseS[n] = srs[i].transform.localScale;
                baseC[n] = srs[i].color;
                n++;
            }
            float t = 0f;
            while (Glow && hold != null && !_cut)
            {
                t += Time.deltaTime;
                float p = 0.5f + 0.5f * Mathf.Sin(t * 3.5f);
                for (int i = 0; i < n; i++)
                {
                    if (halo[i] == null) continue;
                    halo[i].transform.localScale = baseS[i] * (1f + 0.08f * p);
                    var warm = Color.Lerp(baseC[i], new Color(1f, 0.74f, 0.28f, baseC[i].a), 0.28f + 0.22f * p);
                    warm.a = baseC[i].a * (0.76f + 0.24f * p);
                    halo[i].color = warm;
                }
                yield return null;
            }
            for (int i = 0; i < n; i++)
            {
                if (halo[i] == null) continue;
                halo[i].transform.localScale = baseS[i];
                halo[i].color = baseC[i];
            }
        }

        static IEnumerator CoinFountain(Transform parent)
        {
            if (parent == null) yield break;
            const int n = 12;
            var rs = new SpriteRenderer[n];
            var vel = new Vector3[n];
            var spin = new float[n];
            var born = new float[n];
            var origin = parent.position + new Vector3(0f, -0.4f, 0f);
            for (int i = 0; i < n; i++)
            {
                var go = WorldBuilder.Sprite("Fountain", SpriteCatalog.Coin, origin, 0.16f, 58, parent);
                rs[i] = go.GetComponent<SpriteRenderer>();
                var c = rs[i].color;
                c.a = 0f;
                rs[i].color = c;
                vel[i] = new Vector3(Random.Range(-1.7f, 1.7f), Random.Range(4.4f, 6.6f), 0f);
                spin[i] = Random.Range(-320f, 320f);
                born[i] = i * 0.04f;
            }
            float t = 0f;
            const float life = 1.2f;
            while (t < life && parent != null && !_cut)
            {
                float dt = Time.deltaTime;
                t += dt;
                for (int i = 0; i < n; i++)
                {
                    if (rs[i] == null) continue;
                    float age = t - born[i];
                    if (age < 0f) continue;
                    vel[i].y -= 9.8f * dt;
                    rs[i].transform.position += vel[i] * dt;
                    rs[i].transform.Rotate(0f, 0f, spin[i] * dt);
                    float u = Mathf.Clamp01(age / 0.95f);
                    float a = u < 0.1f ? u / 0.1f : 1f - Smooth01(Mathf.Clamp01((u - 0.55f) / 0.45f));
                    var c = rs[i].color;
                    c.a = a;
                    rs[i].color = c;
                    rs[i].transform.localScale = Vector3.one * (0.2f * (1f + 0.08f * Mathf.Sin(age * 18f)));
                }
                yield return null;
            }
            for (int i = 0; i < n; i++)
                if (rs[i] != null) Object.Destroy(rs[i].gameObject);
        }

        static IEnumerator PetalShower(Transform parent)
        {
            if (parent == null) yield break;
            const int n = 16;
            var rs = new SpriteRenderer[n];
            var vel = new Vector3[n];
            var spin = new float[n];
            var sway = new float[n];
            var born = new float[n];
            var pink = SpriteCatalog.PetalPink;
            var peach = SpriteCatalog.PetalPeach;
            for (int i = 0; i < n; i++)
            {
                bool rose = (i & 1) == 0 || peach == null;
                var spr = rose ? pink : peach;
                if (spr == null) spr = pink != null ? pink : peach;
                var origin = parent.position + new Vector3(Random.Range(-3.3f, 3.3f), Random.Range(1.6f, 2.8f), 0f);
                var go = WorldBuilder.Sprite("Petal", spr, origin, 0.16f, 22, parent);
                rs[i] = go.GetComponent<SpriteRenderer>();
                vel[i] = new Vector3(Random.Range(-0.35f, 0.35f), Random.Range(-1.7f, -0.9f), 0f);
                spin[i] = Random.Range(-180f, 180f);
                sway[i] = Random.Range(1.4f, 2.6f);
                born[i] = i * 0.035f;
            }
            float t = 0f;
            const float life = 1.65f;
            while (t < life && parent != null && !_cut)
            {
                float dt = Time.deltaTime;
                t += dt;
                for (int i = 0; i < n; i++)
                {
                    if (rs[i] == null) continue;
                    float age = t - born[i];
                    if (age < 0f) continue;
                    var p = rs[i].transform.position;
                    p += vel[i] * dt;
                    p.x += Mathf.Sin(age * sway[i]) * 0.55f * dt;
                    rs[i].transform.position = p;
                    rs[i].transform.Rotate(0f, 0f, spin[i] * dt);
                    float u = Mathf.Clamp01(age / 1.35f);
                    float a = u < 0.08f ? u / 0.08f : 1f - Smooth01(Mathf.Clamp01((u - 0.62f) / 0.38f));
                    var c = rs[i].color;
                    c.a = a;
                    rs[i].color = c;
                }
                yield return null;
            }
            for (int i = 0; i < n; i++)
                if (rs[i] != null) Object.Destroy(rs[i].gameObject);
        }

        static int ClearPreview()
        {
            int streak = Mathf.Max(1, Purse.Streak + 1);
            int login = Mathf.Max(1, Purse.LoginMul);
            return Mathf.Max(1, Purse.StagePay * streak * login);
        }

        static IEnumerator CountUp(Transform parent)
        {
            if (parent == null) yield break;
            int pay = ClearPreview();
            var root = new GameObject("Tally").transform;
            root.SetParent(parent, false);
            root.localPosition = new Vector3(0f, -2.35f, 0f);
            root.localScale = Vector3.one * 0.01f;
            var glow = WorldBuilder.Sprite("TallyGlow", SpriteCatalog.Glow, root.position, 1f, 60, root);
            glow.transform.localPosition = Vector3.zero;
            glow.transform.localScale = new Vector3(2.4f, 1.1f, 1f);
            var glowSr = glow.GetComponent<SpriteRenderer>();
            glowSr.color = new Color(1f, 0.78f, 0.28f, 0.35f);
            var coin = WorldBuilder.Sprite("TallyCoin", SpriteCatalog.Coin, root.position, 1f, 66, root);
            var digs = new SpriteRenderer[8];
            float t = 0f;
            const float roll = 0.84f;
            int ticks = 0;
            const int steps = 4;
            while (t < roll + 0.42f && root != null && !_cut)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / roll);
                float e = EaseOutCubic(u);
                int shown = u >= 1f ? pay : Mathf.RoundToInt(pay * e);
                int q = Mathf.Clamp(Mathf.FloorToInt(e * steps + 0.001f), 0, steps);
                while (ticks < q)
                {
                    ticks++;
                    if (ticks < steps) Sfx.Clink();
                    else Sfx.FeederDone();
                }
                LayoutTally(root, coin.transform, digs, shown);
                float pop = 1f;
                if (t < 0.16f) pop = EaseOutBack(t / 0.16f, 0.7f);
                else if (u >= 1f)
                {
                    float k = Mathf.Clamp01((t - roll) / 0.22f);
                    pop = 1f + 0.28f * Mathf.Sin(Mathf.Clamp01(k) * Mathf.PI);
                }
                root.localScale = Vector3.one * pop;
                if (glowSr != null)
                {
                    var c = glowSr.color;
                    c.a = 0.22f + 0.2f * (0.5f + 0.5f * Mathf.Sin(t * 6f));
                    glowSr.color = c;
                }
                yield return null;
            }
        }

        static void LayoutTally(Transform root, Transform coin, SpriteRenderer[] digs, int value)
        {
            if (root == null || coin == null || digs == null) return;
            string text = Mathf.Max(0, value).ToString();
            const float h = 0.42f;
            var widths = new float[text.Length];
            var scales = new float[text.Length];
            float total = 0.08f;
            for (int i = 0; i < text.Length; i++)
            {
                var spr = SpriteCatalog.Digit(text[i] - '0');
                float bh = spr != null ? Mathf.Max(0.05f, spr.bounds.size.y) : 1f;
                float bw = spr != null ? spr.bounds.size.x : 0.6f;
                scales[i] = h / bh;
                widths[i] = bw * scales[i];
                total += widths[i] + 0.03f;
            }
            var coinSpr = coin.GetComponent<SpriteRenderer>() != null ? coin.GetComponent<SpriteRenderer>().sprite : null;
            float coinH = coinSpr != null ? Mathf.Max(0.05f, coinSpr.bounds.size.y) : 1f;
            float coinSc = h / coinH;
            float coinW = (coinSpr != null ? coinSpr.bounds.size.x : 1f) * coinSc;
            total += coinW;
            float cursor = -total * 0.5f;
            coin.localScale = Vector3.one * coinSc;
            coin.localPosition = new Vector3(cursor + coinW * 0.5f, 0f, 0f);
            cursor += coinW + 0.08f;
            for (int i = 0; i < text.Length && i < digs.Length; i++)
            {
                if (digs[i] == null)
                {
                    var go = WorldBuilder.Sprite("Digit", SpriteCatalog.Digit(0), root.position, 1f, 68, root);
                    digs[i] = go.GetComponent<SpriteRenderer>();
                }
                digs[i].enabled = true;
                digs[i].sprite = SpriteCatalog.Digit(text[i] - '0');
                digs[i].transform.localScale = Vector3.one * scales[i];
                digs[i].transform.localPosition = new Vector3(cursor + widths[i] * 0.5f, 0f, 0f);
                cursor += widths[i] + 0.03f;
            }
            for (int i = text.Length; i < digs.Length; i++)
                if (digs[i] != null) digs[i].enabled = false;
        }

        static IEnumerator MascotCheer(Transform[] birds)
        {
            if (birds == null || birds.Length == 0) yield break;
            int n = birds.Length;
            var idle = new BirdIdle[n];
            var baseP = new Vector3[n];
            var baseS = new Vector3[n];
            var ord = new int[n];
            int live = 0;
            for (int i = 0; i < n; i++)
            {
                if (birds[i] == null) continue;
                ord[live] = i;
                idle[i] = birds[i].GetComponent<BirdIdle>();
                baseP[i] = idle[i] != null ? idle[i].RestLocal : birds[i].localPosition;
                baseS[i] = idle[i] != null ? idle[i].RestScale : birds[i].localScale;
                if (baseS[i].sqrMagnitude < 0.0001f) baseS[i] = Vector3.one * 0.5f;
                if (idle[i] != null)
                {
                    idle[i].Frozen = true;
                    idle[i].Flapping = true;
                    idle[i].FlapMul = 1.8f;
                }
                live++;
            }
            for (int a = 1; a < live; a++)
            {
                int key = ord[a];
                float kx = birds[key] != null ? birds[key].localPosition.x : 0f;
                int j = a - 1;
                while (j >= 0 && birds[ord[j]] != null && birds[ord[j]].localPosition.x > kx)
                {
                    ord[j + 1] = ord[j];
                    j--;
                }
                ord[j + 1] = key;
            }
            var rank = new int[n];
            for (int k = 0; k < live; k++) rank[ord[k]] = k;
            float t = 0f;
            const float dur = 1.05f;
            while (t < dur && !_cut)
            {
                t += Time.deltaTime;
                for (int i = 0; i < n; i++)
                {
                    if (birds[i] == null) continue;
                    float local = t - rank[i] * 0.07f;
                    float hop = HopArc(local, 0.26f) * 0.32f + HopArc(local - 0.48f, 0.22f) * 0.18f;
                    float squash = LandSquash(local, 0.26f) + LandSquash(local - 0.48f, 0.22f);
                    birds[i].localPosition = baseP[i] + Vector3.up * hop;
                    birds[i].localScale = new Vector3(baseS[i].x * (1f + squash), baseS[i].y * (1f - squash * 0.85f), 1f);
                }
                yield return null;
            }
            for (int i = 0; i < n; i++)
            {
                if (birds[i] == null) continue;
                birds[i].localPosition = baseP[i];
                birds[i].localScale = baseS[i];
                birds[i].localRotation = Quaternion.identity;
                if (idle[i] == null) continue;
                idle[i].FlapMul = 1f;
                idle[i].RestLocal = baseP[i];
                idle[i].RestScale = baseS[i];
                idle[i].Flapping = true;
                idle[i].Frozen = false;
            }
        }

        static float HopArc(float t, float dur)
        {
            if (t <= 0f || t >= dur) return 0f;
            return Mathf.Sin(t / dur * Mathf.PI);
        }

        static float LandSquash(float t, float dur)
        {
            float start = dur * 0.78f;
            float end = dur + 0.07f;
            if (t < start || t > end) return 0f;
            return Mathf.Sin((t - start) / (end - start) * Mathf.PI) * 0.16f;
        }

        static void CheerFlock(Transform any)
        {
            if (any == null) return;
            var root = any;
            while (root.parent != null) root = root.parent;
            var idles = root.GetComponentsInChildren<BirdIdle>(false);
            int n = 0;
            var list = new BirdIdle[idles.Length];
            var xs = new float[idles.Length];
            for (int i = 0; i < idles.Length; i++)
            {
                var idle = idles[i];
                if (idle == null || !idle.isActiveAndEnabled) continue;
                if (idle.name.StartsWith("Mascot")) continue;
                var sr = idle.GetComponent<SpriteRenderer>();
                if (sr == null || !sr.enabled) continue;
                list[n] = idle;
                xs[n] = idle.transform.position.x;
                n++;
            }
            for (int a = 1; a < n; a++)
            {
                var key = list[a];
                float kx = xs[a];
                int j = a - 1;
                while (j >= 0 && xs[j] > kx)
                {
                    list[j + 1] = list[j];
                    xs[j + 1] = xs[j];
                    j--;
                }
                list[j + 1] = key;
                xs[j + 1] = kx;
            }
            float step = n <= 1 ? 0f : 0.62f / (n - 1);
            for (int i = 0; i < n; i++)
                list[i].HopCheer(i * step, 0.2f);
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
            while (t < dur && hold != null)
            {
                if (Cut()) break;
                t += Time.deltaTime;
                float u = Smooth01(t / dur);
                float k = 1f + (peak - 1f) * Mathf.Sin(u * Mathf.PI);
                hold.localScale = Vector3.one * k;
                yield return null;
            }
            if (hold != null) hold.localScale = Vector3.one;
        }

        static IEnumerator PulseHold(Transform hold, float dur)
        {
            float t = 0f;
            const float fadeIn = 0.38f;
            const float fadeOut = 0.55f;
            while (t < dur && hold != null)
            {
                if (Cut()) break;
                t += Time.deltaTime;
                float env;
                if (t < fadeIn) env = Smooth01(t / fadeIn);
                else if (t > dur - fadeOut) env = 1f - Smooth01((t - (dur - fadeOut)) / fadeOut);
                else env = 1f;
                float k = 1f + 0.026f * env * Mathf.Sin(t * 5.1f);
                hold.localScale = Vector3.one * k;
                yield return null;
            }
            if (hold != null) hold.localScale = Vector3.one;
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
                if (Cut()) yield break;
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
                float gap = i > 0 ? Tracking + PairGap(word[i - 1], word[i]) : 0f;
                total += widths[i] + gap;
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
                float nextGap = i + 1 < word.Length ? Tracking + PairGap(word[i], word[i + 1]) : 0f;
                x += widths[i] * 0.5f + nextGap;
            }
            return letters;
        }

        static float PairGap(char a, char b)
        {
            // Tip still: FI crushed, hole before V — open those pairs a hair.
            if (a == 'F' && b == 'I') return 0.045f;
            if (a == 'I' && b == 'V') return -0.02f;
            if (a == 'L' && b == 'O') return 0.02f;
            return 0f;
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
            if (delay > 0f) yield return Wait(delay);
            if (Cut() || tr == null) yield break;
            float t = 0f;
            while (t < dur)
            {
                if (Cut() || tr == null)
                {
                    if (tr != null)
                    {
                        tr.localScale = restScale;
                        tr.localPosition = restPos;
                        tr.localRotation = Quaternion.identity;
                    }
                    yield break;
                }
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / dur);
                float pop = EaseOutBack(u, 0.58f);
                float drop = 1f - EaseOutCubic(u);
                float spin = (1f - Smooth01(u)) * 2.0f * Mathf.Sin(u * Mathf.PI);
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
            if (delay > 0f) yield return Wait(delay);
            if (Cut() || tr == null) yield break;
            Sfx.FlapHard();
            float t = 0f;
            const float dur = 0.46f;
            var idle = tr.GetComponent<BirdIdle>();
            var from = tr.localPosition + Vector3.up * 0.28f;
            var rest = idle != null ? idle.RestLocal : tr.localPosition;
            while (t < dur)
            {
                if (Cut() || tr == null)
                {
                    if (tr != null)
                    {
                        tr.localScale = Vector3.one * scale;
                        tr.localPosition = rest;
                        tr.localRotation = Quaternion.identity;
                        if (idle != null)
                        {
                            idle.RestScale = Vector3.one * scale;
                            idle.RestLocal = rest;
                            idle.FlapMul = 1f;
                            idle.Flapping = true;
                            idle.Frozen = false;
                        }
                    }
                    yield break;
                }
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
                if (!Live || _cut) yield break;
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
                yield return Wait(0.12f);
            }
            yield return Wait(1.2f);
        }

        static IEnumerator Rocket(Transform parent, Vector3 from, float peakY, BirdColor col, bool boom)
        {
            if (!Live || _cut || parent == null) yield break;
            var spark = WorldBuilder.Sprite("Rocket", SpriteCatalog.Glow, from, 0.22f, 17, parent);
            var sr = spark.GetComponent<SpriteRenderer>();
            sr.color = Wow.Of(col);
            float t = 0f;
            const float up = 0.42f;
            while (t < up)
            {
                if (spark == null || !Live || _cut)
                {
                    if (spark != null) Object.Destroy(spark);
                    yield break;
                }
                t += Time.deltaTime;
                float u = t / up;
                spark.transform.position = Vector3.Lerp(from, new Vector3(from.x, peakY, 0f), u * u);
                yield return null;
            }
            if (spark == null || !Live || _cut)
            {
                if (spark != null) Object.Destroy(spark);
                yield break;
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
                if (_cut) break;
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
