using System.Collections;
using UnityEngine;

namespace FlockFive
{
    public sealed class BranchView : MonoBehaviour
    {
        public int Index;
        public bool FromRight;
        public bool IsGift;
        public SpriteRenderer Wood;
        public Transform Sign;
        public readonly Transform[] Seats = new Transform[BranchState.Cap];
        public readonly SpriteRenderer[] Birds = new SpriteRenderer[BranchState.Cap];
        public static readonly Vector3 BirdScale = new Vector3(0.42f, 0.42f, 1f);
        public Vector3 Planted => _planted;

        public void Fit(Vector3 planted, Vector3 scale)
        {
            _planted = planted;
            transform.localScale = scale;
            if (!_breaking) transform.position = planted;
        }
        // Seat is the wood; this lifts the sprite so gripping toes sit on the limb.
        public const float RestLift = 0.41f;

        // Every rest bird sprite's lowest toe row is baked to WorldBuilder.SeatToeRowPx (831 of
        // the 1024 canvas), so all birds share one seat height with no per-bird offset.
        int _count;
        Vector3 _planted;
        Vector3 _woodScale = new Vector3(0.64f, 0.50f, 1f);
        Vector3 _woodPos;
        bool _woodRemembered;
        float _shake;
        bool _breaking;
        GameObject _fly;
        int _act;
        bool _sleeping;
        float _nextSnooze;
        BeeSwarm _swarm;
        LeafCover _leaves;
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
        }

        public Vector3 SeatWorld(int seat)
        {
            return Seats[seat] != null ? Seats[seat].position : transform.position;
        }

        public float NearestPadSqr(Vector2 world)
        {
            float best = ((Vector2)transform.position - world).sqrMagnitude;
            for (int s = 0; s < Seats.Length; s++)
            {
                if (Seats[s] == null) continue;
                float d = ((Vector2)Seats[s].position - world).sqrMagnitude;
                if (d < best) best = d;
            }
            if (Sign != null && Sign.gameObject.activeInHierarchy)
            {
                float d = ((Vector2)Sign.position - world).sqrMagnitude;
                if (d < best) best = d;
            }
            return best;
        }

        public void Sync(BranchState state, bool sleeping = false)
        {
            if (state.Broken)
            {
                if (_swarm != null) _swarm.Cover(false, Seats, -1, 0);
                if (_leaves != null) _leaves.Cover(false, Seats, 0);
                // Collect/hop reparents seat birds to the garden root. Reclaim and
                // hide them here so a leftover same-color sprite cannot flash at
                // feeder/world coords (looks like an off-screen stray bird).
                for (int i = 0; i < BranchState.Cap; i++)
                {
                    var bird = Birds[i];
                    if (bird == null) continue;
                    var rest = Seats[i] != null
                        ? Seats[i].localPosition + new Vector3(0f, RestLift, 0f)
                        : Vector3.zero;
                    bird.transform.SetParent(transform, false);
                    bird.transform.localPosition = rest;
                    bird.transform.localRotation = Quaternion.identity;
                    bird.transform.localScale = BirdScale;
                    bird.gameObject.SetActive(false);
                    bird.enabled = false;
                    var idle = bird.GetComponent<BirdIdle>();
                    if (idle != null)
                    {
                        idle.Frozen = false;
                        idle.Flapping = false;
                        idle.Lift = 0f;
                        idle.Sleeping = false;
                        idle.Shrouded = false;
                        idle.RestLocal = rest;
                    }
                }
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
            bool tipLocked = state.TipLocked;
            for (int i = 0; i < BranchState.Cap; i++)
            {
                var bird = Birds[i];
                if (bird == null) continue;
                var leftoverBee = bird.GetComponent<BeeSwarm>();
                if (leftoverBee != null) Destroy(leftoverBee);
                var leftoverLeaf = bird.GetComponent<LeafCover>();
                if (leftoverLeaf != null) Destroy(leftoverLeaf);
                bool show = i < state.Count;
                bool hid = show && state.IsShrouded(i);
                // Inner bees stay visible under a leaf lock. Tip shroud is the leaf.
                if (hid && i < state.Count - 1) lastHid = i;
                else if (hid && !tipLocked) lastHid = i;
                bird.gameObject.SetActive(show);
                bird.enabled = show;
                bird.sortingOrder = hid ? 7 : 12;
                bird.transform.SetParent(transform, false);
                var rest = Seats[i].localPosition + new Vector3(0f, RestLift, 0f);
                // SetParent(..., false) keeps local coords — snap to the seat so a
                // bird that flew under the garden root cannot reappear off-screen.
                bird.transform.localPosition = rest;
                bird.transform.localRotation = Quaternion.identity;
                bird.transform.localScale = BirdScale;
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
                    bird.transform.localPosition = rest;
                    idle.Bind(state.Birds[i], rest);
                    idle.Sleeping = sleeping;
                    idle.Shrouded = hid;
                    bird.sprite = SpriteCatalog.Bird(state.Birds[i].Color, state.Birds[i].Sex);
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
            if (_leaves == null) _leaves = gameObject.GetComponent<LeafCover>();
            if (_leaves == null) _leaves = gameObject.AddComponent<LeafCover>();
            _leaves.TrunkDir = FromRight ? 1f : -1f;
            _leaves.Cover(tipLocked, Seats, state.Count);
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
                if (tip)
                    Wow.Shed(Birds[i].transform.position, idle.Color, transform.parent);
            }
        }

        int ReadyRun()
        {
            if (_count <= 0) return 0;
            var idle = Birds[_count - 1] != null ? Birds[_count - 1].GetComponent<BirdIdle>() : null;
            if (idle == null || idle.Shrouded) return 0;
            var c = idle.Color;
            var sex = idle.Sex;
            int n = 1;
            for (int i = _count - 2; i >= 0; i--)
            {
                var o = Birds[i] != null ? Birds[i].GetComponent<BirdIdle>() : null;
                if (o == null || o.Shrouded || o.Color != c || o.Sex != sex) break;
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
            RememberWood();
            EnsureZzz();
        }

        public void RememberWood()
        {
            if (_woodRemembered || Wood == null) return;
            _woodScale = Wood.transform.localScale;
            _woodPos = Wood.transform.localPosition;
            _woodRemembered = true;
        }

        public void RetakeWood()
        {
            _woodRemembered = false;
            RememberWood();
        }

        // Drop snap-off copies and extra colliders left by a break or a rebuild.
        public void ClearDebris()
        {
            HaltBreak();
            _breaking = false;
            RememberWood();
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child == null) continue;
                if (Wood != null && child == Wood.transform) continue;
                if (child.name.StartsWith("Wood"))
                    Destroy(child.gameObject);
            }
            var cols = GetComponents<BoxCollider2D>();
            for (int i = 1; i < cols.Length; i++)
                Destroy(cols[i]);
            transform.rotation = Quaternion.identity;
            if (Wood != null)
            {
                Wood.color = Color.white;
                Wood.transform.localScale = _woodScale;
                Wood.transform.localPosition = _woodPos;
                Wood.transform.localRotation = Quaternion.identity;
            }
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
                float baseX = 0f;
                if (_count > 0 && Seats[_count - 1] != null)
                    baseX = Seats[_count - 1].localPosition.x;
                float along = baseX + (FromRight ? -0.22f : 0.22f) * i;
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

        // Restart takes the flock. Stop a limb break so it cannot deactivate the
        // branch after the birds have already been sent off and the round restored.
        public bool Breaking => _breaking;

        public void HaltBreak()
        {
            _act++;
            DropFly();
        }

        void DropFly()
        {
            if (_fly == null) return;
            var go = _fly;
            _fly = null;
            Object.Destroy(go);
        }

        void OnDisable()
        {
            DropFly();
            if (!_breaking) return;
            _breaking = false;
            transform.rotation = Quaternion.identity;
            if (Wood != null && _woodRemembered)
            {
                Wood.color = Color.white;
                Wood.transform.localScale = _woodScale;
                Wood.transform.localPosition = _woodPos;
                Wood.transform.localRotation = Quaternion.identity;
            }
        }

        public IEnumerator BreakAway()
        {
            int act = _act;
            _breaking = true;
            Sfx.Break();
            RememberWood();
            float dir = FromRight ? 1f : -1f;
            SpriteRenderer fly = null;
            GameObject flyGo = null;
            Vector3 stubScale = _woodScale;
            Vector3 stubPos = _woodPos;
            if (Wood != null)
            {
                flyGo = Object.Instantiate(Wood.gameObject, transform);
                _fly = flyGo;
                fly = flyGo.GetComponent<SpriteRenderer>();
                var stub = new Vector3(_woodScale.x * 0.52f, _woodScale.y, _woodScale.z);
                Wood.transform.localScale = stub;
                flyGo.transform.localScale = stub;
                Wood.transform.localPosition = new Vector3(-dir * 0.62f, 0f, 0f);
                flyGo.transform.localPosition = new Vector3(dir * 0.62f, 0.04f, 0f);
            }

            float t = 0f;
            const float dur = 0.62f;
            var start = transform.position;
            while (t < dur)
            {
                if (act != _act)
                {
                    RestoreBreak(flyGo, stubScale, stubPos);
                    yield break;
                }
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / dur);
                float k = u * u;
                if (Wood != null)
                    Wood.transform.localRotation = Quaternion.Euler(0f, 0f, dir * -32f * k);
                if (fly != null)
                {
                    fly.transform.localPosition += new Vector3(dir * 6.2f, -9.5f, 0f) * Time.deltaTime * (0.35f + u);
                    fly.transform.Rotate(0f, 0f, dir * 260f * Time.deltaTime);
                    var c = fly.color;
                    c.a = 1f - u;
                    fly.color = c;
                }
                if (Wood != null)
                {
                    var c = Wood.color;
                    c.a = 1f - u * 0.85f;
                    Wood.color = c;
                }
                transform.position = start + new Vector3(0f, -1.15f * k, 0f);
                yield return null;
            }
            if (act != _act)
            {
                RestoreBreak(flyGo, stubScale, stubPos);
                yield break;
            }
            if (_fly == flyGo) _fly = null;
            if (flyGo != null) Object.Destroy(flyGo);
            gameObject.SetActive(false);
            transform.position = _planted;
            transform.rotation = Quaternion.identity;
            if (Wood != null)
            {
                Wood.color = Color.white;
                Wood.transform.localScale = stubScale;
                Wood.transform.localPosition = stubPos;
                Wood.transform.localRotation = Quaternion.identity;
            }
            _breaking = false;
        }

        void RestoreBreak(GameObject flyGo, Vector3 stubScale, Vector3 stubPos)
        {
            if (_fly == flyGo) _fly = null;
            if (flyGo != null) Object.Destroy(flyGo);
            _breaking = false;
            gameObject.SetActive(true);
            transform.position = _planted;
            transform.rotation = Quaternion.identity;
            if (Wood != null)
            {
                Wood.color = Color.white;
                Wood.transform.localScale = stubScale;
                Wood.transform.localPosition = stubPos;
                Wood.transform.localRotation = Quaternion.identity;
            }
        }

        public void Revive()
        {
            ClearDebris();
            _shake = 0f;
            gameObject.SetActive(true);
            transform.position = _planted;
        }
    }
}

