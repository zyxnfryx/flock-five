using System.Collections;
using UnityEngine;

namespace FlockFive
{
    // Garden pest: rarer than sparrow. Perches on a feeder and blocks it until
    // TWO full collect scraps (HitsNeeded=2). First scrap wounds and perches
    // angry until the second scrap flees. Landing boots any live sparrow.
    // Never sets _busy. No tap-scare.
    public sealed class HawkView : MonoBehaviour
    {
        public static HawkView Live { get; private set; }

        public const int HitsNeeded = 2;
        public int HitsTaken { get; private set; }
        public int BlockingSlot { get; private set; } = -1;
        public bool IsBlocking => Live != null && BlockingSlot >= 0 && !_done;
        public bool InScrap => _scrap || _fleeing;
        // True only after the perch settle, so the tutorial does not talk over the arrival.
        public bool Settled { get; private set; }

        float _scale = 1.1f;
        SpriteRenderer _art;
        bool _done;
        bool _scrap;   // collect owns pose during dive-fight
        bool _evict;   // final flee — Visit yields to PanicFlee
        bool _fleeing;
        bool _abort;
        float _exitX;
        Color _tint = new Color(0.42f, 0.30f, 0.20f, 1f);
        float _flap;
        bool _angry;
        bool _angerFresh;
        bool _burst;
        float _twitchIn;
        float _twitchT;
        bool _twitching;
        Coroutine _hitCo;
        int _hitGen;
        int _hitIndex = -1;
        static readonly Color AngryTint = new Color(1f, 0.6f, 0.55f, 1f);

        public static IEnumerator Patrol(System.Func<bool> allow, System.Func<bool> armed, int visits, FeederView[] feeders, Transform parent)
        {
            if (visits <= 0) yield break;
            while (armed != null && !armed())
            {
                if (parent == null) yield break;
                yield return null;
            }
            yield return PlayClock.Wait(Random.Range(40f, 70f));
            if (!PestSchedule.TimerOpens(PestSchedule.PestKind.Hawk, true)) yield break;
            int left = visits;
            while (parent != null && left > 0)
            {
                if (PestSchedule.SolvedNow || PestSchedule.StageFull) yield break;
                if (allow != null && allow() && HasEnabled(feeders) && Live == null && PestSchedule.TryReserve())
                {
                    yield return Visit(feeders, parent);
                    PestSchedule.NoteGone();
                    left--;
                    if (left <= 0) yield break;
                }
                if (parent == null) yield break;
                yield return null;
            }
        }

        public static IEnumerator Visit(FeederView[] feeders, Transform parent)
        {
            if (PestSchedule.SolvedNow) yield break;
            if (parent == null || feeders == null || Live != null) yield break;
            var picks = Enabled(feeders);
            if (picks.Length == 0) yield break;

            bool fromLeft = Random.value < 0.5f;
            float edgeX = fromLeft ? -7.6f : 7.6f;
            float exitX = -edgeX;
            var start = new Vector3(edgeX, Random.Range(5.8f, 9.0f), 0f);
            var target = picks[Random.Range(0, picks.Length)];
            var mouth = target.Mouth + new Vector3(Random.Range(-0.12f, 0.12f), 0.42f, 0f);

            float scale = Random.Range(1.05f, 1.15f);
            GameObject go = null;
            HawkView view = null;
            try
            {
            go = WorldBuilder.Sprite("Hawk", SpriteCatalog.Hawk, start, scale, 43, parent);
            view = go.AddComponent<HawkView>();
            view._scale = scale;
            view._art = go.GetComponent<SpriteRenderer>();
            view._art.color = SpriteCatalog.HawkIsPlaceholder ? view._tint : Color.white;
            view._art.flipX = !fromLeft;
            view._exitX = exitX;
            Live = view;

            // Fly in. Same warm-up as the sparrow so a hawk scrap does not allocate feathers.
            float t = 0f;
            const float inDur = 1.05f;
            bool cried = false;
            while (t < inDur && !view._evict)
            {
                if (parent == null || go == null) yield break;
                PestPool.Prewarm(parent, 4);
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / inDur));
                var p = Vector3.Lerp(start, mouth, u);
                p.y += Mathf.Sin(u * Mathf.PI) * 1.55f;
                go.transform.position = p;
                view.Flap(u < 0.92f);
                if (!cried && u > 0.78f)
                {
                    cried = true;
                    Sfx.HawkCry();
                }
                yield return null;
            }

            // Land + boot any sparrow (hawk presence clears sparrow).
            // A solve that started the flee skips the perch.
            if (!view._evict)
                BootSparrow();

            if (!view._evict && go != null && target != null)
            {
                go.transform.position = target.Mouth + new Vector3(0f, 0.42f, 0f);
                view.BlockingSlot = target.Slot;
                target.Poke();

                float settle = 0f;
                while (settle < 0.28f && !view._evict)
                {
                    settle += Time.deltaTime;
                    if (go == null || parent == null) yield break;
                    if (view._scrap || view._burst) { yield return null; continue; }
                    go.transform.position = target.Mouth + new Vector3(
                        Mathf.Sin(Time.time * 8f) * 0.025f,
                        0.42f + Mathf.Sin(Time.time * 6f) * 0.035f,
                        0f);
                    view.Flap(true);
                    yield return null;
                }
                if (!view._evict)
                    view.Settled = true;

                while (!view._evict)
                {
                    if (parent == null || go == null) yield break;
                    if (target == null) break;
                    if (view._scrap || view._burst)
                    {
                        yield return null;
                        continue;
                    }
                    if (!IsOn(target))
                    {
                        // Feeder vanished — leave quietly.
                        break;
                    }
                    var lunge = view.AngryLunge();
                    go.transform.position = target.Mouth + new Vector3(
                        Mathf.Sin(Time.time * 5.8f) * 0.04f,
                        0.42f + Mathf.Sin(Time.time * 4.6f) * 0.05f,
                        0f) + lunge;
                    go.transform.localRotation = Quaternion.Euler(0f, 0f, -lunge.y * 70f);
                    view.Flap(Mathf.Sin(Time.time * 2.6f) > 0.4f);
                    yield return null;
                }
            }

            if (view._evict)
            {
                // Collect owns PanicFlee; wait until retired.
                while (go != null && !view._done)
                    yield return null;
            }
            else
            {
                view.BlockingSlot = -1;
                if (go != null)
                    yield return view.FlyOut(exitX);
            }

            }
            finally
            {
                if (view != null && Live == view) Live = null;
                if (go != null) Object.Destroy(go);
            }
        }

        // The board solved under this hawk. A scrap still in the air finishes
        // itself; a perched hawk drops the feeder and bolts.
        public void LeaveForSolve()
        {
            if (_done || _fleeing || _evict || _scrap) return;
            _evict = true;
            BlockingSlot = -1;
            Settled = false;
            Calm();
            StartCoroutine(PanicFlee(false));
        }

        // Collect dive-fight owns pose; BlockingSlot stays set (still blocking).
        public void BeginScrap()
        {
            if (_done || _fleeing) return;
            _scrap = true;
        }

        public void EndScrap()
        {
            _scrap = false;
            // The wound's own EndScrap follows BeginAnger in the same call.
            // A later EndScrap is restart releasing the perch.
            if (_angerFresh)
            {
                _angerFresh = false;
                return;
            }
            Calm();
            if (_fleeing) _abort = true;
        }

        // Cosmetic return-pass only. Stage load and restart both call this.
        public static void ClearShow() => HawkPass.Clear();

        // One full collect counted. Returns true when hawk should PanicFlee.
        public bool AbsorbCollect()
        {
            if (_done || _fleeing) return false;
            HitsTaken = Mathf.Min(HitsNeeded, HitsTaken + 1);
            if (HitsTaken >= HitsNeeded)
            {
                _evict = true;
                BlockingSlot = -1;
                return true;
            }
            if (HitsTaken == 1)
                BeginAnger();
            return false;
        }

        public void Smack() => TakeHit(0);

        public void TakeHit(int index = 0)
        {
            if (_art == null) return;
            if (_done || _fleeing)
            {
                AdLog.Add("hawk hit ignored (defeat)");
                return;
            }
            if (_hitCo != null && _hitIndex == index)
            {
                AdLog.Add("hawk hit ignored (duplicate)");
                return;
            }
            _hitIndex = index;
            int gen = ++_hitGen;
            if (_hitCo != null) StopCoroutine(_hitCo);
            _hitCo = StartCoroutine(TakeHitCo(index, gen));
        }

        void StopHit()
        {
            _hitGen++;
            if (_hitCo == null) return;
            var co = _hitCo;
            _hitCo = null;
            StopCoroutine(co);
        }

        IEnumerator TakeHitCo(int index, int gen)
        {
            var pos = transform.position;
            var parent = transform.parent;
            var baseScale = Vector3.one * _scale;
            float side = Random.value < 0.5f ? -1f : 1f;
            float hopX = side * Random.Range(0.06f, 0.14f);
            float hopY = Random.Range(0.10f, 0.22f);

            Sfx.HawkCry();
            if (index == 0 || index == 3)
                Sfx.FeederRattle();
            if (CamShake.Live != null)
                CamShake.Live.Punch(0.12f, 0.08f + 0.012f * index, 2.6f, 0.09f);
            SparrowBits.Burst(pos + new Vector3(0f, 0.1f, 0f), parent, _tint);

            float flash = 0f;
            const float flashDur = 0.06f;
            while (flash < flashDur && !_fleeing && !_burst)
            {
                flash += Time.deltaTime;
                float u = Mathf.Clamp01(flash / flashDur);
                transform.localScale = new Vector3(
                    baseScale.x * Mathf.Lerp(1.18f, 1.04f, u),
                    baseScale.y * Mathf.Lerp(0.66f, 0.90f, u),
                    1f);
                Flap(true);
                if (_art != null)
                    _art.color = Color.Lerp(Color.white, RestColor(), u);
                yield return null;
            }

            float crouch = 0f;
            const float crouchDur = 0.075f;
            while (crouch < crouchDur && !_fleeing && !_burst)
            {
                crouch += Time.deltaTime;
                float u = Mathf.Clamp01(crouch / crouchDur);
                transform.localScale = new Vector3(
                    baseScale.x * Mathf.Lerp(1.06f, 1.14f, u),
                    baseScale.y * Mathf.Lerp(0.80f, 0.62f, u),
                    1f);
                transform.position = pos + new Vector3(0f, -0.035f * u, 0f);
                Flap(true);
                yield return null;
            }

            float hop = 0f;
            const float hopDur = 0.15f;
            float tilt = side * Random.Range(6f, 12f);
            while (hop < hopDur && !_fleeing && !_burst)
            {
                hop += Time.deltaTime;
                float u = Mathf.Clamp01(hop / hopDur);
                float arc = Mathf.Sin(u * Mathf.PI);
                transform.position = pos + new Vector3(hopX * u, hopY * arc - 0.015f * (1f - arc), 0f);
                transform.localScale = new Vector3(
                    baseScale.x * Mathf.Lerp(1.10f, 0.96f, u),
                    baseScale.y * Mathf.Lerp(0.64f, 1.06f, arc),
                    1f);
                transform.localRotation = Quaternion.Euler(0f, 0f, tilt * arc);
                Flap(true);
                yield return null;
            }

            if (!_fleeing && !_burst)
            {
                transform.localScale = baseScale;
                transform.localRotation = Quaternion.identity;
                transform.position = pos + new Vector3(hopX * 0.35f, 0.02f, 0f);
            }
            if (gen == _hitGen) _hitCo = null;
        }

        public IEnumerator PanicFlee(bool defeated = false)
        {
            if (_done || _fleeing)
            {
                AdLog.Add(defeated ? "hawk defeat ignored (duplicate)" : "hawk flee ignored (duplicate)");
                yield break;
            }
            _fleeing = true;
            Settled = false;
            StopHit();
            _scrap = false;
            Calm();
            BlockingSlot = -1;
            if (defeated)
            {
                yield return DefeatExit();
                yield break;
            }
            var from = transform.position;
            float dir = Mathf.Sign(_exitX - from.x);
            if (dir == 0f) dir = _exitX >= 0f ? 1f : -1f;
            var dest = new Vector3(_exitX, from.y + Random.Range(1.8f, 3.4f), 0f);
            transform.localScale = Vector3.one * _scale;
            transform.localRotation = Quaternion.identity;
            if (_art != null) _art.flipX = dest.x < from.x;
            Sfx.FlockFlutter(1);
            Sfx.HawkCry();

            var a = from + new Vector3(dir * Random.Range(0.65f, 1.1f), Random.Range(0.6f, 1.15f), 0f);
            var b = a + new Vector3(-dir * Random.Range(0.8f, 1.35f), Random.Range(0.5f, 1.05f), 0f);
            var legs = new[] { a, b, dest };
            var durs = new[] { 0.24f, 0.28f, 0.42f };
            var prev = from;
            for (int leg = 0; leg < legs.Length && !_abort; leg++)
            {
                var next = legs[leg];
                if (_art != null) _art.flipX = next.x < prev.x;
                float t = 0f;
                float dur = durs[leg];
                float sway = (leg % 2 == 0 ? 1f : -1f) * Random.Range(0.25f, 0.48f);
                while (t < dur && !_abort)
                {
                    t += Time.deltaTime;
                    float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / dur));
                    float ease = u * u * (3f - 2f * u);
                    var p = Vector3.Lerp(prev, next, ease);
                    p.x += Mathf.Sin(u * Mathf.PI) * sway;
                    p.y += Mathf.Sin(u * Mathf.PI) * (0.4f + 0.22f * leg);
                    transform.position = p;
                    transform.localScale = Vector3.one * (_scale * Mathf.Lerp(1.1f, 0.7f, (leg + u) / 3f));
                    transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(u * Mathf.PI * 2.2f) * 18f * (1f - u * 0.5f));
                    Flap(true);
                    if (_art != null)
                    {
                        var c = _art.color;
                        c.a = 1f - ((leg + u) / 3f) * 0.22f;
                        _art.color = c;
                    }
                    yield return null;
                }
                prev = next;
            }
            _done = true;
        }

        // Second-collect clear. Full size, slow limp off the screen. The flock
        // cheer waits until this yield ends, so the exit is not covered.
        IEnumerator DefeatExit()
        {
            var parent = transform.parent;
            var from = transform.position;
            transform.localScale = Vector3.one * _scale;
            transform.localRotation = Quaternion.identity;
            if (_art != null) _art.color = Color.white;
            if (CamShake.Live != null)
                CamShake.Live.Punch(0.14f, 0.08f, 1.6f, 0.05f);

            float hold = 0f;
            while (hold < 0.12f && !_abort)
            {
                hold += Time.deltaTime;
                transform.localScale = Vector3.one * _scale;
                transform.localRotation = Quaternion.identity;
                yield return null;
            }
            if (_abort)
            {
                _done = true;
                yield break;
            }
            Sfx.HawkCryHurt();
            Sfx.FlockFlutter(1);
            SparrowBits.Burst(from + new Vector3(0f, 0.14f, 0f), parent, _tint, 6);

            float dir = Mathf.Sign(_exitX - from.x);
            if (dir == 0f) dir = _exitX >= 0f ? 1f : -1f;
            var dest = new Vector3(_exitX, from.y + 0.25f, 0f);
            if (_art != null) _art.flipX = dest.x < from.x;
            float t = 0f;
            const float dur = 2.5f;
            int dropped = 0;
            while (t < dur && !_abort)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / dur);
                float smooth = u * u * (3f - 2f * u);
                float ease = Mathf.Lerp(u * 0.55f, smooth, 0.45f);
                var p = Vector3.Lerp(from, dest, ease);
                float wave = Mathf.Sin(u * Mathf.PI * 3f);
                float sag = wave < 0f ? wave * 0.95f : wave * 0.22f;
                p.y += sag;
                p.x += Mathf.Sin(u * Mathf.PI * 4f) * 0.28f;
                transform.position = p;
                transform.localScale = Vector3.one * _scale;
                float tilt = (wave * 16f + Mathf.Sin(u * Mathf.PI * 7f) * 8f) * -dir;
                transform.localRotation = Quaternion.Euler(0f, 0f, tilt);
                LimpFlap(LimpOpen());
                if (_art != null)
                {
                    var c = _art.color;
                    c.a = u > 0.90f ? Mathf.Lerp(1f, 0.12f, (u - 0.90f) / 0.10f) : 1f;
                    _art.color = c;
                }
                if (parent != null && dropped < 4 && u > (dropped + 1) * 0.18f)
                {
                    SparrowBits.Drop(p + new Vector3(0f, -0.2f, 0f), parent, _tint);
                    dropped++;
                }
                yield return null;
            }
            if (!_abort)
                PestCheer.Mark(from, parent, true);
            _done = true;
        }

        // Uneven beat: a short open, a long sag, a late weak flick.
        static bool LimpOpen()
        {
            float phase = Mathf.Repeat(Time.time * 1.15f, 1f);
            if (phase < 0.18f) return true;
            if (phase < 0.62f) return false;
            return phase < 0.74f;
        }

        void LimpFlap(bool open)
        {
            if (_art == null) return;
            _flap += Time.deltaTime * (open ? 2.2f : 0.40f);
            _art.sprite = SpriteCatalog.HawkFrame(_flap);
            float dip = open ? 1f : 0.82f;
            Color hurt = SpriteCatalog.HawkIsPlaceholder
                ? Color.Lerp(_tint, new Color(0.55f, 0.30f, 0.26f), 0.5f)
                : new Color(0.78f * dip, 0.62f * dip, 0.56f * dip, 1f);
            _art.color = hurt;
        }

        IEnumerator FlyOut(float exitX)
        {
            _fleeing = true;
            Calm();
            var from = transform.position;
            var dest = new Vector3(exitX, from.y + Random.Range(0.5f, 1.8f), 0f);
            if (_art != null) _art.flipX = dest.x < from.x;
            float t = 0f;
            const float outDur = 0.95f;
            while (t < outDur)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / outDur));
                var p = Vector3.Lerp(from, dest, u);
                p.y += Mathf.Sin(u * Mathf.PI) * 1.35f;
                transform.position = p;
                Flap(true);
                yield return null;
            }
            _done = true;
        }

        void Flap(bool hard)
        {
            if (_art == null) return;
            // Wingbeats per second; poses step at a steady rate so the flap never aliases.
            float rate = hard ? 2.8f : 1.8f;
            if (_angry) rate *= 1.5f;
            _flap += Time.deltaTime * rate;
            _art.sprite = SpriteCatalog.HawkFrame(_flap);
            _art.color = RestColor();
        }

        // First collect wound. Burst owns pose; perch resumes angry until clear.
        void BeginAnger()
        {
            if (_angry || _done || _fleeing) return;
            _angry = true;
            _angerFresh = true;
            _twitching = false;
            _twitchIn = Random.Range(2f, 3f);
            StartCoroutine(AngerBurst());
        }

        IEnumerator AngerBurst()
        {
            if (_done || _fleeing) yield break;
            _burst = true;
            Sfx.HawkCryHot();
            var origin = transform.position;
            var baseScale = Vector3.one * _scale;
            float t = 0f;
            const float dur = 0.25f;
            var hot = new Color(1f, 0.28f, 0.24f, 1f);
            while (t < dur && _angry && !_fleeing && !_done)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / dur);
                float amp = (1f - u) * 0.065f;
                transform.position = origin + new Vector3(
                    Mathf.Sin(t * 86f) * amp,
                    Mathf.Cos(t * 71f) * amp * 0.7f,
                    0f);
                float puff = Mathf.Sin(u * Mathf.PI);
                transform.localScale = baseScale * Mathf.Lerp(1f, 1.15f, puff);
                Flap(true);
                if (_art != null)
                    _art.color = Color.Lerp(AngryTint, hot, puff);
                yield return null;
            }
            if (_angry && !_fleeing && !_done)
            {
                transform.localScale = baseScale;
                transform.localRotation = Quaternion.identity;
                transform.position = origin;
                if (_art != null) _art.color = RestColor();
            }
            _burst = false;
        }

        // Periodic peck toward the feeder mouth while the wounded hawk waits.
        Vector3 AngryLunge()
        {
            if (!_angry || _fleeing || _burst) return Vector3.zero;
            if (!_twitching)
            {
                _twitchIn -= Time.deltaTime;
                if (_twitchIn > 0f) return Vector3.zero;
                _twitching = true;
                _twitchT = 0f;
                _twitchIn = Random.Range(2f, 3f);
            }
            const float twitchDur = 0.28f;
            _twitchT += Time.deltaTime;
            float u = Mathf.Clamp01(_twitchT / twitchDur);
            if (u >= 1f) _twitching = false;
            float k = Mathf.Sin(u * Mathf.PI);
            float face = (_art != null && _art.flipX) ? -1f : 1f;
            return new Vector3(0.10f * k * face, -0.16f * k, 0f);
        }

        Color RestColor()
        {
            if (_angry)
            {
                float pulse = Mathf.Lerp(0.84f, 1f, 0.5f + 0.5f * Mathf.Sin(Time.time * 2.2f));
                return new Color(AngryTint.r * pulse, AngryTint.g * pulse, AngryTint.b * pulse, 1f);
            }
            return SpriteCatalog.HawkIsPlaceholder ? _tint : Color.white;
        }

        void Calm()
        {
            _angry = false;
            _angerFresh = false;
            _burst = false;
            _twitching = false;
        }

        void OnDisable()
        {
            if (Live == this) Live = null;
            BlockingSlot = -1;
            Settled = false;
            _done = true;
            _angry = false;
            _angerFresh = false;
            _burst = false;
        }

        static void BootSparrow()
        {
            var s = SparrowView.Live;
            if (s == null || !s.IsBlocking) return;
            s.BeginEvict();
            s.StartCoroutine(s.PanicFlee());
        }

        static bool HasEnabled(FeederView[] feeders)
        {
            if (feeders == null) return false;
            for (int i = 0; i < feeders.Length; i++)
                if (IsOn(feeders[i])) return true;
            return false;
        }

        static FeederView[] Enabled(FeederView[] feeders)
        {
            int n = 0;
            for (int i = 0; i < feeders.Length; i++)
                if (IsOn(feeders[i])) n++;
            var got = new FeederView[n];
            int w = 0;
            for (int i = 0; i < feeders.Length; i++)
                if (IsOn(feeders[i])) got[w++] = feeders[i];
            return got;
        }

        static bool IsOn(FeederView f) =>
            f != null && f.Art != null && f.Art.enabled;
    }

    // Beaten flyby after a hawk clear. Cosmetic only: no collider, not HawkView.Live,
    // so it cannot block a feeder or input. Clear() on stage load and restart.
    sealed class HawkPass : MonoBehaviour
    {
        static HawkPass _live;

        public static void Clear()
        {
            if (_live != null)
                Object.Destroy(_live.gameObject);
            _live = null;
        }

        public static void Begin(Transform parent, bool fromLeft, float y, float scale, float edge, Color body, Color feather)
        {
            Clear();
            if (parent == null) return;
            var go = new GameObject("HawkPass");
            go.transform.SetParent(parent, false);
            var pass = go.AddComponent<HawkPass>();
            _live = pass;
            pass.StartCoroutine(pass.Run(fromLeft, y, scale, edge, body, feather));
        }

        IEnumerator Run(bool fromLeft, float y, float scale, float edge, Color body, Color feather)
        {
            yield return new WaitForSeconds(0.6f);
            if (this == null) yield break;
            float x0 = fromLeft ? -edge : edge;
            float x1 = -x0;
            var go = WorldBuilder.Sprite("HawkFar", SpriteCatalog.Hawk, new Vector3(x0, y, 0f), scale, 36, transform);
            if (go == null)
            {
                Clear();
                yield break;
            }
            var art = go.GetComponent<SpriteRenderer>();
            art.flipX = !fromLeft;
            art.color = body;
            float droop = fromLeft ? -13f : 13f;
            go.transform.rotation = Quaternion.Euler(0f, 0f, droop);
            float dur = 0.48f;
            float t = 0f;
            float flap = 0f;
            int dropped = 0;
            while (t < dur)
            {
                if (go == null || art == null)
                {
                    Clear();
                    yield break;
                }
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / dur);
                float x = Mathf.Lerp(x0, x1, u);
                go.transform.position = new Vector3(x, y + Mathf.Sin(u * Mathf.PI) * 0.18f, 0f);
                flap += Time.deltaTime * 1.5f;
                art.sprite = SpriteCatalog.HawkFrame(flap);
                art.color = body;
                art.flipX = !fromLeft;
                go.transform.rotation = Quaternion.Euler(0f, 0f, droop + Mathf.Sin(u * 8f) * 2.2f);
                if (dropped < 4 && u >= (dropped + 1) * 0.18f)
                {
                    dropped++;
                    SparrowBits.Drop(go.transform.position, transform, feather);
                }
                yield return null;
            }
            if (go != null) Object.Destroy(go);
            // Feathers are children of this host. Let them finish, unless Clear()
            // already tore the pass down (restart / stage load).
            yield return new WaitForSeconds(0.72f);
            if (this != null) Clear();
        }

        void OnDestroy()
        {
            if (_live == this) _live = null;
        }
    }
}

