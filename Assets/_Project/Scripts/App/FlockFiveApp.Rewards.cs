using System.Collections;
using UnityEngine;

namespace FlockFive
{
    public sealed partial class FlockFiveApp
    {
        int _branchJuiceToken = -1;
        int _rewardBranch = -1;
        Vector3 _rewardPos;

        // Shared with every rewarded placement. A skip never arrives here.
        bool ApplyRewardedTicket(RewardedGrant.Ticket t)
        {
            switch (t.Place)
            {
                case RewardedGrant.Place.Branch: return ApplyBranchTicket(t);
                case RewardedGrant.Place.Wheel: return ApplyWheelTicket(t);
                case RewardedGrant.Place.Keep: return ApplyKeepTicket(t);
                case RewardedGrant.Place.Clear: return ApplyClearTicket(t);
                case RewardedGrant.Place.Repeat: return ApplyRepeatTicket(t);
                default: return false;
            }
        }

        bool ApplyBranchTicket(RewardedGrant.Ticket t)
        {
            if (_board == null) return false;
            int n = RewardEffects.Branch(_board, _bonusOn, t.Payload, out int index);
            if (n < 0) return false;
            _rewardBranch = index;
            if (index >= 0) _rewardPos = ShowBonusOpen(index);
            if (t.TutorialGoal) FinishAdHandGoal();
            MaybePresentBranch();
            return true;
        }

        bool ApplyWheelTicket(RewardedGrant.Ticket t)
        {
            int before = Purse.Coins;
            if (!RewardEffects.Wheel(t.Payload)) return false;
            int gained = Purse.Coins - before;
            if (gained > 0) BeginRewardPay(gained);
            CloseDailyAfterReward();
            return true;
        }

        bool ApplyKeepTicket(RewardedGrant.Ticket t)
        {
            if (!RewardEffects.Keep(t.Payload)) return false;
            // The popup stays up so the badge can seal. WatchKeepStreak closes it
            // after the sweep. A grant with no card (late restore) just clears.
            if (_restartAsk == RestartAsk.Keep)
                BeginStreakHeal();
            else
                _restartAsk = RestartAsk.None;
            return true;
        }

        // REWARD popup 2X: the same prize again, flown to the piggy like the first pay.
        bool ApplyRepeatTicket(RewardedGrant.Ticket t)
        {
            if (t.Payload <= 0) return false;
            // b87 (Brandon: double grant): the doubled amount rides the single Collect pay.
            if (Purse.AddOwed(t.Payload)) return true;
            Purse.Credit(t.Payload);
            BeginRewardPay(t.Payload);
            return true;
        }

        bool ApplyClearTicket(RewardedGrant.Ticket t)
        {
            return RewardEffects.ClearDouble();
        }

        // The glove step's goal is the branch. Finish it when that reward lands,
        // even if coach already cleared the flag while the ad was up.
        void FinishAdHandGoal()
        {
            if (_adHand) DismissAdHand();
            else NoteLessonDismiss(CoachStep.AdHand);
        }

        void MaybePresentBranch()
        {
            if (_watchGiftLive || _splash || _restarting) return;
            int token = RewardedGrant.ActiveToken;
            if (token != 0 && _branchJuiceToken == token) return;
            StartCoroutine(PresentBranchReward());
        }

        // Ice, then thanks. The claim itself already happened in the grant.
        IEnumerator PresentBranchReward()
        {
            int token = RewardedGrant.ActiveToken;
            if (token != 0 && _branchJuiceToken == token) yield break;
            _branchJuiceToken = token;
            if (_frozen && _garden.Ice != null)
                yield return _garden.Ice.Shatter(_garden.Root);
            RewardedGrant.Flush();
            var popAt = _rewardPos;
            StillBirds(false);
            _frozen = false;
            _freezeOffer = false;
            if (_board != null)
                yield return GardenFit.Tween(_garden, _board, false);
            _giftThanksAt = AdPause.Unscaled;
            _gift = GiftFace.Thanks;
            if (_garden.Root != null)
                StartCoroutine(GiftPopBursts(popAt));
            BeginRewardPay(0, 2.45f);
            yield return WaitRewardPay();
            _gift = GiftFace.None;
            _swallowTapsUntil = PlayClock.Now + 0.45f;
            _suppressGiftUntil = PlayClock.Now + 1f;
            _busy = false;
            Conserve("bonus");
            CheckOver();
        }

        void CloseDailyAfterReward()
        {
            // The doubled pay already launched the coins. Hold the card until they land.
            if (_dailyOpen)
            {
                _dailyPayHold = true;
                _dailyTutorSpin = false;
                _wheelPayMul = 1;
                return;
            }
            EndDailyPopup();
        }
    }
}
