using System.Collections;
using UnityEngine;

namespace FlockFive
{
    public sealed class BranchView : MonoBehaviour
    {
        public int Index;
        public bool FromRight;
        public SpriteRenderer Wood;
        public readonly Transform[] Seats = new Transform[BranchState.Cap];
        public readonly SpriteRenderer[] Birds = new SpriteRenderer[BranchState.Cap];
        public static readonly Vector3 BirdScale = new Vector3(0.42f, 0.42f, 1f);
        // Seat is the wood; this lifts the sprite so gripping toes sit on the limb.
        public const float RestLift = 0.41f;
        int _count;
        Vector3 _planted;
        float _shake;
        bool _breaking;
        bool _sleeping;
        float _nextSnooze;
        BeeSwarm _swarm;
        readonly Transform[] _zz = new Transform[3];
        readonly SpriteRenderer[] _zzSr = new SpriteRenderer[3];

        public void Shake() => _shake = 0.22f;

        public void FlutterTip()
        {
            int run = ReadyRun();
            if (run <= 0) return;
            for (int i = _count - run; i < _count; i++)
            {
                if (Birds[i] == null || !Birds[i].enabled) continue;
                var idle = Birds[i].GetComponent<BirdIdle>();
                if (idle == null || idle.Shrouded || idle.Sleeping) continue;
                idle.Flutter(0.7f);
            }
            Sfx.Flaps(run);
        }

        public Vector3 SeatWorld(int seat)
        {
            return Seats[seat] != null ? Seats[seat].position : transform.position;
        }

        public void Sync(BranchState state, bool sleeping = false)
        {
            if (state.Broken)
            {
                gameObject.SetActive(false);
                ShowZzz(false);
                return;
            }
            gameObject.SetActive(true);
            _count = state.Count;
            if (sleeping && !_sleeping)
                _nextSnooze = Time.unscaledTime + Random.Range(0.25f, 0.9f);
            _sleeping = sleeping;
            int lastHid = -1;
            for (int i = 0; i < BranchState.Cap; i++)
            {
                var bird = Birds[i];
                if (bird == null) continue;
                var leftover = bird.GetComponent<BeeSwarm>();
                if (leftover != null) Destroy(leftover);
                bool show = i < state.Count;
                bool hid = show && state.IsShrouded(i);
                if (hid) lastHid = i;
                bird.gameObject.SetActive(show);
                bird.enabled = show;
                bird.sortingOrder = hid ? 7 : 12;
                bird.transform.SetParent(transform, false);
                var rest = Seats[i].localPosition + new Vector3(0f, RestLift, 0f);
                bird.transform.localRotation = Quaternion.identity;
                var idle = bird.GetComponent<BirdIdle>();
                if (idle == null) idle = bird.gameObject.AddComponent<BirdIdle>();
                idle.RestScale = BirdScale;
                idle.FaceLeft = FromRight;
                idle.Frozen = false;
                idle.Flapping = false;
                idle.Sleeping = sleeping && show;
                bird.flipX = FromRight;
                if (show)
                {
                    idle.Bind(state.Birds[i], rest);
                    idle.Sleeping = sleeping;
                    idle.Shrouded = hid;
                    bird.sprite = SpriteCatalog.Bird(state.Birds[i]);
                    bird.color = hid ? new Color(0.04f, 0.03f, 0.05f, 1f) : Color.white;
                }
                else
                {
                    idle.Lift = 0f;
                    idle.Sleeping = false;
                    idle.Shrouded = false;
                    idle.RestLocal = rest;
                    bird.color = Color.white;
                }
            }
            if (_swarm == null) _swarm = gameObject.GetComponent<BeeSwarm>();
            if (_swarm == null) _swarm = gameObject.AddComponent<BeeSwarm>();
            _swarm.TrunkDir = FromRight ? 1f : -1f;
            if (lastHid >= 0)
                _swarm.Cover(true, Seats, lastHid, state.Count);
            else
                _swarm.Cover(false, Seats, -1, state.Count);
            ShowZzz(sleeping);
            if (Wood != null && !sleeping) Wood.color = Color.white;
            else if (Wood != null && sleeping) Wood.color = new Color(0.78f, 0.74f, 0.92f);
        }

        public void SetReady(bool on)
        {
            if (_sleeping) on = false;
            Highlight(on);
            int run = on ? ReadyRun() : 0;
            if (on && run > 0) Sfx.Flaps(run);
            else if (!on && !_sleeping && _count > 0) Sfx.FlapSoft();
            for (int i = 0; i < BranchState.Cap; i++)
            {
                if (Birds[i] == null) continue;
                var idle = Birds[i].GetComponent<BirdIdle>();
                if (idle == null) continue;
                if (idle.Sleeping || idle.Shrouded)
                {
                    idle.Lift = 0f;
                    idle.Flapping = false;
                    continue;
                }
                bool tip = on && i >= _count - run && i < _count;
                idle.Lift = tip ? 1.15f : 0f;
                idle.Flapping = tip;
            }
        }

        int ReadyRun()
        {
            if (_count <= 0) return 0;
            var idle = Birds[_count - 1] != null ? Birds[_count - 1].GetComponent<BirdIdle>() : null;
            if (idle == null || idle.Shrouded) return 0;
            var c = idle.Color;
            int n = 1;
            for (int i = _count - 2; i >= 0; i--)
            {
                var o = Birds[i] != null ? Birds[i].GetComponent<BirdIdle>() : null;
                if (o == null || o.Shrouded || o.Color != c) break;
                n++;
            }
            return n;
        }

        public void Highlight(bool on)
        {
            if (Wood == null) return;
            if (_sleeping) { Wood.color = new Color(0.78f, 0.74f, 0.92f); return; }
            Wood.color = on ? new Color(1f, 0.82f, 0.15f) : Color.white;
        }

        void Awake()
        {
            _planted = transform.position;
            EnsureZzz();
        }

        void EnsureZzz()
        {
            if (_zz[0] != null) return;
            for (int i = 0; i < 3; i++)
            {
                var go = WorldBuilder.Sprite("Z" + i, SpriteCatalog.Zee, transform.position, 0.14f + 0.04f * i, 12, transform);
                go.transform.localPosition = new Vector3(FromRight ? 0.55f - 0.45f * i : -0.55f + 0.45f * i, 1.12f + 0.22f * i, 0f);
                _zz[i] = go.transform;
                _zzSr[i] = go.GetComponent<SpriteRenderer>();
                go.SetActive(false);
            }
        }

        void ShowZzz(bool on)
        {
            EnsureZzz();
            for (int i = 0; i < 3; i++)
                if (_zz[i] != null) _zz[i].gameObject.SetActive(on);
        }

        void LateUpdate()
        {
            if (_breaking) return;
            if (_shake > 0f)
            {
                _shake -= Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(_shake / 0.22f);
                transform.position = _planted + new Vector3(Mathf.Sin(Time.time * 48f) * 0.08f * u, 0f, 0f);
            }
            else transform.position = _planted;

            if (!_sleeping) return;
            if (Time.unscaledTime >= _nextSnooze)
            {
                Sfx.Snooze();
                _nextSnooze = Time.unscaledTime + Random.Range(1.35f, 2.7f);
            }
            for (int i = 0; i < 3; i++)
            {
                if (_zz[i] == null || !_zz[i].gameObject.activeSelf) continue;
                float u = Time.time * (0.55f + 0.12f * i) + i * 1.7f;
                float rise = Mathf.Repeat(u, 1f);
                float along = FromRight
                    ? Mathf.Lerp(0.85f, -0.15f, i / 2f)
                    : Mathf.Lerp(-0.85f, 0.15f, i / 2f);
                _zz[i].localPosition = new Vector3(along, 1.08f + rise * 0.55f, 0f);
                float pulse = 0.10f + 0.028f * i + 0.012f * Mathf.Sin(u * 6f);
                _zz[i].localScale = Vector3.one * pulse;
                if (_zzSr[i] != null)
                {
                    var c = Color.white;
                    c.a = 0.35f + 0.65f * (1f - rise);
                    _zzSr[i].color = c;
                }
            }
        }

        public IEnumerator BreakAway()
        {
            _breaking = true;
            Sfx.Break();
            float t = 0f;
            var start = transform.position;
            while (t < 0.55f)
            {
                t += Time.deltaTime;
                float u = t / 0.55f;
                float kick = u < 0.12f ? Mathf.Sin(u / 0.12f * Mathf.PI) * 0.18f : 0f;
                transform.position = start + new Vector3(kick, -4.8f * u * u, 0f);
                transform.rotation = Quaternion.Euler(0f, 0f, -36f * u * u);
                if (Wood != null)
                {
                    var c = Wood.color;
                    c.a = 1f - u;
                    Wood.color = c;
                }
                yield return null;
            }
            gameObject.SetActive(false);
            transform.position = _planted;
            transform.rotation = Quaternion.identity;
            if (Wood != null) Wood.color = Color.white;
            _breaking = false;
        }
    }
}
