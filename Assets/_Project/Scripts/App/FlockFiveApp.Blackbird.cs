using System.Collections;
using UnityEngine;

namespace FlockFive
{
    public sealed partial class FlockFiveApp
    {
        bool _blackbirdHold;
        Coroutine _bbIntro;
        int _bbGen;

        void BeginBlackbirdVisit()
        {
            if (_board == null || _board.Blackbird == null)
            {
                if (MixDesk.Live != null) MixDesk.Live.SetBlackbirdBed(BlackbirdCue.Bed.Calm);
                return;
            }
            if (_garden.Root == null) return;
            StubBlackbirdPerch();
            if (_garden.Blackbird == null)
                _garden.Blackbird = BlackbirdMark.Attach(_garden.Root);
            var mark = _garden.Blackbird;
            int perch = _board.Blackbird.Perch;
            Transform seat = null;
            if (_garden.Branches != null && (uint)perch < (uint)_garden.Branches.Length && _garden.Branches[perch] != null)
                seat = _garden.Branches[perch].transform;
            if (mark != null)
            {
                mark.Shown(true);
                mark.Sit(seat);
                mark.SetRattled(false);
                mark.Shown(false);
            }
            if (_bbIntro != null) StopCoroutine(_bbIntro);
            _bbIntro = StartCoroutine(BlackbirdIntro());
        }

        Transform PerchSeat()
        {
            if (_board == null || _board.Blackbird == null || _garden.Branches == null) return null;
            int perch = _board.Blackbird.Perch;
            if ((uint)perch >= (uint)_garden.Branches.Length) return null;
            return _garden.Branches[perch] != null ? _garden.Branches[perch].transform : null;
        }

        Vector3 PerchHome()
        {
            var seat = PerchSeat();
            if (seat != null) return seat.position + Vector3.up * 0.42f;
            return _garden.Blackbird != null ? _garden.Blackbird.transform.position : Vector3.zero;
        }

        void StubBlackbirdPerch()
        {
            if (_board == null || _board.Blackbird == null || _garden.Branches == null) return;
            int perch = _board.Blackbird.Perch;
            if ((uint)perch < (uint)_garden.Branches.Length)
                BlackbirdPerch.Stub(_garden.Branches[perch]);
        }

        IEnumerator BlackbirdIntro()
        {
            int gen = ++_bbGen;
            if (MixDesk.Live != null) MixDesk.Live.SetBlackbirdBed(BlackbirdCue.Bed.Cut);
            float t = 0f;
            while (t < BlackbirdCue.IntroCutSec)
            {
                if (gen != _bbGen) yield break;
                t += AdPause.UnscaledDelta;
                yield return null;
            }
            if (gen != _bbGen) yield break;
            if (MixDesk.Live != null && _board != null && _board.Blackbird != null && !_board.Blackbird.Fled)
                MixDesk.Live.SetBlackbirdBed(BlackbirdCue.Bed.Calm);
            var mark = _garden.Blackbird;
            Vector3 home = PerchHome();
            if (mark != null)
            {
                mark.Shown(true);
                var start = BlackbirdRival.Flight.Drop(new BlackbirdRival.Point { X = home.x, Y = home.y }, 0f);
                mark.transform.position = new Vector3(start.X, start.Y, 0f);
            }
            float d = 0f;
            while (d < BlackbirdCue.DropSec)
            {
                if (gen != _bbGen) yield break;
                d += AdPause.UnscaledDelta;
                float u = Mathf.Clamp01(d / BlackbirdCue.DropSec);
                if (mark != null)
                {
                    var p = BlackbirdRival.Flight.Drop(new BlackbirdRival.Point { X = home.x, Y = home.y }, u);
                    mark.transform.position = new Vector3(p.X, p.Y, 0f);
                }
                yield return null;
            }
            if (gen != _bbGen) yield break;
            if (mark != null) mark.Sit(PerchSeat());
            Sfx.BlackbirdTaunt();
            t = 0f;
            while (t < BlackbirdCue.TauntSec)
            {
                if (gen != _bbGen) yield break;
                t += AdPause.UnscaledDelta;
                yield return null;
            }
            _bbIntro = null;
        }

#if UNITY_EDITOR
        public void EditorOpenBlackbird()
        {
            int index;
            if (!LevelData.TryIndex(BlackbirdSchedule.Visit.First, out index)) index = 0;
            Load(index);
        }
#endif

        IEnumerator BlackbirdAnswer(int motionGen)
        {
            if (_board == null || _board.Blackbird == null || _board.Blackbird.Fled) yield break;
            int flocks = BlackbirdRival.FlocksDone(_board);
            bool will = BlackbirdRival.Owes(_board.Blackbird, flocks, _board.Blackbird.PlayerMoves + 1);
            var choice = BlackbirdRival.Choose(_board);
            if (will && choice.WillMove)
            {
                if (_garden.Blackbird != null) _garden.Blackbird.BeginTell();
                Sfx.BlackbirdTell();
                float t = 0f;
                while (t < BlackbirdCue.TellSec)
                {
                    if (motionGen != _motionGen) yield break;
                    t += AdPause.UnscaledDelta;
                    yield return null;
                }
                if (_garden.Blackbird != null) _garden.Blackbird.EndTell();
            }
            int from = choice.From;
            int to = choice.To;
            int run = choice.Run;
            int fc = 0;
            int tc = 0;
            var col = default(BirdColor);
            bool animate = will && choice.WillMove
                && (uint)from < (uint)_board.Branches.Count
                && (uint)to < (uint)_board.Branches.Count
                && _garden.Branches != null
                && from < _garden.Branches.Length
                && to < _garden.Branches.Length;
            if (animate)
            {
                fc = _board.Branches[from].Count;
                tc = _board.Branches[to].Count;
                if (_board.Branches[from].Tip.HasValue)
                    col = _board.Branches[from].Tip.Value.Color;
                Lock(from);
                Lock(to);
            }
            var ans = BlackbirdRival.Commit(_board);
            if (animate && ans.Moved && motionGen == _motionGen)
            {
                yield return Hop(ans.From, ans.To, ans.Run > 0 ? ans.Run : run, fc, tc, col);
                Unlock(from);
                Unlock(to);
                SyncAll();
            }
            else if (animate)
            {
                Unlock(from);
                Unlock(to);
            }
            ApplyBlackbirdBed(ans.FledNow);
            if (ans.FledNow && motionGen == _motionGen)
                yield return BlackbirdFlee(motionGen);
        }

        void ApplyBlackbirdBed(bool fledNow)
        {
            if (_board == null || _board.Blackbird == null) return;
            bool rush = !fledNow && !_board.Blackbird.Fled && BlackbirdRival.FlocksDone(_board) >= BlackbirdCue.RattleAt;
            if (_garden.Blackbird != null) _garden.Blackbird.SetRattled(rush);
            if (MixDesk.Live == null) return;
            if (fledNow || _board.Blackbird.Fled)
                MixDesk.Live.SetBlackbirdBed(BlackbirdCue.Bed.Silence);
            else if (rush)
                MixDesk.Live.SetBlackbirdBed(BlackbirdCue.Bed.Rush);
            else
                MixDesk.Live.SetBlackbirdBed(BlackbirdCue.Bed.Calm);
        }

        IEnumerator BlackbirdFlee(int motionGen)
        {
            var mark = _garden.Blackbird;
            Vector3 home = mark != null ? mark.transform.position : Vector3.zero;
            if (_garden.Root != null) BlackbirdPerch.Sparks(_garden.Root, home);
            float half = 4.2f;
            var cam = _garden.Cam != null ? _garden.Cam : Camera.main;
            if (cam != null && cam.orthographic) half = cam.orthographicSize * cam.aspect;
            var perch = new BlackbirdRival.Point { X = home.x, Y = home.y };
            float t = 0f;
            const float bolt = 0.7f;
            while (t < bolt)
            {
                if (motionGen != _motionGen) yield break;
                t += AdPause.UnscaledDelta;
                float u = Mathf.Clamp01(t / bolt);
                if (mark != null)
                {
                    var p = BlackbirdRival.Flight.Bolt(perch, u, half);
                    mark.transform.position = new Vector3(p.X, p.Y, 0f);
                }
                yield return null;
            }
            if (mark != null) mark.Hide();
            t = 0f;
            while (t < BlackbirdCue.SilenceSec)
            {
                if (motionGen != _motionGen) yield break;
                t += AdPause.UnscaledDelta;
                yield return null;
            }
            if (motionGen != _motionGen) yield break;
            if (MixDesk.Live != null) MixDesk.Live.SetBlackbirdBed(BlackbirdCue.Bed.Calm);
        }
    }
}
