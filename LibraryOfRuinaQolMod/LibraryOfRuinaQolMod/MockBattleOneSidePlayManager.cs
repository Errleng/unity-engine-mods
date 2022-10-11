using LOR_DiceSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;

namespace LibraryOfRuinaQolMod
{
    internal class MockBattleOneSidePlayManager : MonoBehaviour
    {
        public static List<string> diceRolls = new List<string>();
        static readonly Dictionary<string, List<string>> cachedDiceRolls = new Dictionary<string, List<string>>();

        static BattlePlayingCardDataInUnitModel origCard;
        static int diceMin;
        static int diceMax;

        //static int resultKnockbackEnergy;
        static BattlePlayingCardDataInUnitModel _playingCard;
        static BattleUnitModel attacker;
        static BattleUnitModel victim;
        static BattleCardTotalResult _attackerCardBehavioursResult;
        static BattleCardTotalResult _victimCardBehavioursResult;

        public static void ResetCache()
        {
            cachedDiceRolls.Clear();
        }

        static string CreateKey(BattlePlayingCardDataInUnitModel card)
        {
            return $"{card.owner.id}-{card.GetHashCode()}";
        }

        static void UpdateCache(string key)
        {
            cachedDiceRolls[key] = new List<string>(diceRolls);
        }

        public static List<string> GetDiceRolls(BattlePlayingCardDataInUnitModel card)
        {
            var key = CreateKey(card);
            if (cachedDiceRolls.ContainsKey(key))
            {
                Plugin.logger.LogDebug($"Using cached dice rolls for {card.card.GetName()} ({card.GetHashCode()})");
                return new List<string>(cachedDiceRolls[key]);
            }
            diceRolls.Clear();
            MockStartActionFast(card);
            return null;
        }

        static void MockStartActionFast(BattlePlayingCardDataInUnitModel card)
        {
            var key = CreateKey(card);
            cachedDiceRolls[key] = null;

            origCard = card;
            var cardCopy = Utils.Clone(card);

            Plugin.logger.LogDebug($"card: {card?.owner?.UnitData.unitData.name} and copy: {cardCopy?.owner?.UnitData.unitData.name}, card same instance: {ReferenceEquals(card, cardCopy)}, owner same instance: {ReferenceEquals(card?.owner, cardCopy?.owner)}");

            Singleton<StageController>.Instance.dontUseUILog = true;
            //resultKnockbackEnergy = 0;
            _playingCard = cardCopy;
            attacker = cardCopy.owner;
            victim = cardCopy.target;
            attacker.currentDiceAction = cardCopy;
            _attackerCardBehavioursResult = new BattleCardTotalResult(cardCopy);
            _victimCardBehavioursResult = new BattleCardTotalResult(null);
            attacker.battleCardResultLog = _attackerCardBehavioursResult;
            victim.battleCardResultLog = _victimCardBehavioursResult;
            //int speedDiceResultValue = cardCopy.speedDiceResultValue;
            //int value = victim.GetSpeedDiceResult(cardCopy.targetSlotOrder).value;
            cardCopy.OnUseCard_before();
            cardCopy.owner.cardHistory.AddCardHistory(cardCopy, Singleton<StageController>.Instance.RoundTurn);
            attacker.OnUseCard(cardCopy);
            attacker.OnStartCardAction(cardCopy);
            cardCopy.OnStartOneSideAction();
            attacker.OnStartOneSideAction(cardCopy);
            victim.OnStartTargetedOneSide(cardCopy);
            _attackerCardBehavioursResult.usedDiceList = cardCopy.GetDiceBehaviorList();
            Singleton<StageController>.Instance.dontUseUILog = false;
            //int count = cardCopy.GetDiceBehaviorList().Count;
            NextDice();
            //if (victim.IsDead() || attacker.IsDead() || _playingCard.currentBehavior == null || victim.IsExtinction() || attacker.IsExtinction())
            //{
            //    //EndCardAction();
            //    return;
            //}
            //PreDecision();

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            SimulateDiceRolls();
            stopwatch.Stop();
            Plugin.logger.LogDebug($"Finished asynchronous one-sided attack dice roll simulations after {stopwatch.Elapsed.Seconds} seconds");
            UpdateCache(key);
        }

        static void SimulateDiceRolls()
        {
            int diceNum = 0;
            while (_playingCard.currentBehavior != null)
            {
                diceMin = -1;
                diceMax = -1;

                var oldBufListDetail = _playingCard.currentBehavior.owner.bufListDetail;
                _playingCard.currentBehavior.owner.bufListDetail = origCard.owner.bufListDetail;

                SimulateDiceRollCoroutine(0);

                _playingCard.currentBehavior.owner.bufListDetail = oldBufListDetail;

                diceRolls.Add($"({diceMin}-{diceMax})");

                _playingCard.NextDice();
                ++diceNum;
            }
        }

        static IEnumerator SimulateDiceRollCoroutine(int iteration)
        {
            _playingCard.currentBehavior.BeforeRollDice(null);
            _playingCard.currentBehavior.RollDice();
            _playingCard.currentBehavior.UpdateDiceFinalValue();
            if (iteration == 0)
            {
                diceMin = _playingCard.currentBehavior.DiceResultValue;
                diceMax = _playingCard.currentBehavior.DiceResultValue;
            }
            else
            {
                diceMin = Math.Min(diceMin, _playingCard.currentBehavior.DiceResultValue);
                diceMax = Math.Max(diceMax, _playingCard.currentBehavior.DiceResultValue);
            }
            yield return null;
        }

        static void MockStartAction(BattlePlayingCardDataInUnitModel card)
        {
            origCard = card;
            var cardCopy = Utils.Clone(card);

            Plugin.logger.LogDebug($"card: {card?.owner?.UnitData.unitData.name} and copy: {cardCopy?.owner?.UnitData.unitData.name}, card same instance: {ReferenceEquals(card, cardCopy)}, owner same instance: {ReferenceEquals(card?.owner, cardCopy?.owner)}");

            Singleton<StageController>.Instance.dontUseUILog = true;
            //resultKnockbackEnergy = 0;
            _playingCard = cardCopy;
            attacker = cardCopy.owner;
            victim = cardCopy.target;
            attacker.currentDiceAction = cardCopy;
            _attackerCardBehavioursResult = new BattleCardTotalResult(cardCopy);
            _victimCardBehavioursResult = new BattleCardTotalResult(null);
            attacker.battleCardResultLog = _attackerCardBehavioursResult;
            victim.battleCardResultLog = _victimCardBehavioursResult;
            //int speedDiceResultValue = cardCopy.speedDiceResultValue;
            //int value = victim.GetSpeedDiceResult(cardCopy.targetSlotOrder).value;
            cardCopy.OnUseCard_before();
            cardCopy.owner.cardHistory.AddCardHistory(cardCopy, Singleton<StageController>.Instance.RoundTurn);
            attacker.OnUseCard(cardCopy);
            attacker.OnStartCardAction(cardCopy);
            cardCopy.OnStartOneSideAction();
            attacker.OnStartOneSideAction(cardCopy);
            victim.OnStartTargetedOneSide(cardCopy);
            _attackerCardBehavioursResult.usedDiceList = cardCopy.GetDiceBehaviorList();
            Singleton<StageController>.Instance.dontUseUILog = false;
            //int count = cardCopy.GetDiceBehaviorList().Count;
            NextDice();
            if (victim.IsDead() || attacker.IsDead() || _playingCard.currentBehavior == null || victim.IsExtinction() || attacker.IsExtinction())
            {
                //EndCardAction();
                return;
            }
            PreDecision();
        }

        static void NextDice()
        {
            _playingCard.NextDice();
        }

        static void PreDecision()
        {
            if (_playingCard.currentBehavior != null)
            {
                Decision();
                return;
            }
            //EndCardAction();
        }

        static void Decision()
        {
            if (_playingCard.currentBehavior != null)
            {
                _attackerCardBehavioursResult.SetBehaviourDiceResultUI(CompareBehaviourUIType.Start);
                _attackerCardBehavioursResult.SetSkip(DiceUITiming.Start);
                if (_playingCard.currentBehavior.Type == BehaviourType.Atk)
                {
                    //BeforeRollDice(); // sets the buffs/debuffs that are shown (manually rolling / not in quick mode)

                    int diceMin = -1;
                    int diceMax = -1;

                    var oldBufListDetail = _playingCard.currentBehavior.owner.bufListDetail;
                    _playingCard.currentBehavior.owner.bufListDetail = origCard.owner.bufListDetail;

                    for (int i = 0; i < Plugin.DiceRollSimulationIterations.Value; i++)
                    {
                        _playingCard.currentBehavior.BeforeRollDice(null);
                        _playingCard.currentBehavior.RollDice();
                        _playingCard.currentBehavior.UpdateDiceFinalValue();
                        if (i == 0)
                        {
                            diceMin = _playingCard.currentBehavior.DiceResultValue;
                            diceMax = _playingCard.currentBehavior.DiceResultValue;
                        }
                        else
                        {
                            diceMin = Math.Min(diceMin, _playingCard.currentBehavior.DiceResultValue);
                            diceMax = Math.Max(diceMax, _playingCard.currentBehavior.DiceResultValue);
                        }
                    }
                    _playingCard.currentBehavior.owner.bufListDetail = oldBufListDetail;

                    diceRolls.Add($"({diceMin}-{diceMax})");

                    //AfterRollDice();
                }
                Action();
                return;
            }
            if (_playingCard.cardBehaviorQueue.Count > 0)
            {
                _playingCard.currentBehavior = _playingCard.cardBehaviorQueue.Dequeue();
                _attackerCardBehavioursResult.SetBehaviourDiceResultUI(CompareBehaviourUIType.Start);
                _attackerCardBehavioursResult.SetSkip(DiceUITiming.Start);
                return;
            }
            //EndCardAction();
        }

        static void Action()
        {
            if (_playingCard.currentBehavior != null)
            {
                if (_playingCard.currentBehavior.behaviourInCard.Type == BehaviourType.Atk)
                {
                    //_playingCard.currentBehavior.GiveDamage(victim);
                    //victim.UpdateDirection(attacker.view.WorldPosition);
                    //int knockbackPower = _playingCard.currentBehavior.behaviourInCard.KnockbackPower;
                    //int num = 0;
                    //resultKnockbackEnergy += knockbackPower - num;
                    //_playingCard.AfterAction();
                }
                else if (_playingCard.currentBehavior.behaviourInCard.Type == BehaviourType.Def)
                {
                    _playingCard.owner.cardSlotDetail.keepCard.AddBehaviour(_playingCard.card, _playingCard.currentBehavior);
                }
            }
            //_attackerCardBehavioursResult.SetCurrentHp(attacker.hp);
            //_victimCardBehavioursResult.SetCurrentHp(victim.hp);
            //_attackerCardBehavioursResult.SetCurrentBreakGauge(attacker.breakDetail.breakGauge);
            //_victimCardBehavioursResult.SetCurrentBreakGauge(victim.breakDetail.breakGauge);
            //SetBehaviourResultData(_playingCard.currentBehavior, null, _attackerCardBehavioursResult, _playingCard.currentBehaviorUI);
            //SetBehaviourResultData(null, _playingCard.currentBehavior, _victimCardBehavioursResult, null);
            //_attackerCardBehavioursResult.SetCurrentBuf(attacker.bufListDetail.GetBufUIDataList());
            //_victimCardBehavioursResult.SetCurrentBuf(victim.bufListDetail.GetBufUIDataList());
            //_attackerCardBehavioursResult.SetBehaviourDiceResultUI(CompareBehaviourUIType.Start);
            //_attackerCardBehavioursResult.SetSkip(DiceUITiming.Start);
            BattleDiceBehavior currentBehavior = _playingCard.currentBehavior;
            if (currentBehavior != null && currentBehavior.isBonusAttack)
            {
                BattleDiceBehavior currentBehavior2 = _playingCard.currentBehavior;
                if (currentBehavior2 != null && !currentBehavior2.forbiddenBonusDice)
                {
                    if (_playingCard.currentBehavior != null)
                    {
                        _playingCard.currentBehavior.isBonusAttack = false;
                        CheckEndAction();
                        return;
                    }
                    CheckEndAction();
                    return;
                }
            }
            NextDice();
            CheckEndAction();
        }

        static void CheckEndAction()
        {
            if (victim.IsDead() || attacker.IsDead() || victim.IsExtinction() || attacker.IsExtinction() || _playingCard.currentBehavior == null)
            {
                //EndCardAction();
                return;
            }
            if (attacker.IsBreakLifeZero() || attacker.IsKnockout())
            {
                //EndCardAction();
                return;
            }
            //_attackerCardBehavioursResult.InitNextBehaviour();
            //_victimCardBehavioursResult.InitNextBehaviour();
            PreDecision();
        }

        //static void BeforeRollDice()
        //{
        //    SetAbilityDataBeforeRoll(attacker, _attackerCardBehavioursResult);
        //    SetAbilityDataBeforeRoll(victim, _victimCardBehavioursResult);
        //}

        //static void AfterRollDice()
        //{
        //    SetAbilityDataAfterRoll(attacker, _attackerCardBehavioursResult);
        //    SetAbilityDataAfterRoll(victim, _victimCardBehavioursResult);
        //}

        //static void SetAbilityDataBeforeRoll(BattleUnitModel model, BattleCardTotalResult result)
        //{
        //    foreach (BattleUnitBuf bufs in model.bufListDetail.GetActivatedBufList())
        //    {
        //        result.SetBufs(bufs);
        //    }
        //    foreach (BattleEmotionCardModel emotionAbility in model.emotionDetail.PassiveList)
        //    {
        //        result.SetEmotionAbility(emotionAbility);
        //    }
        //    foreach (GiftModel gift in model.UnitData.unitData.GetEquippedGiftList())
        //    {
        //        result.SetGift(gift);
        //    }
        //}

        //static void SetAbilityDataAfterRoll(BattleUnitModel model, BattleCardTotalResult result)
        //{
        //    foreach (BattleUnitBuf newBufs in model.bufListDetail.GetActivatedBufList())
        //    {
        //        result.SetNewBufs(newBufs);
        //    }
        //    foreach (BattleEmotionCardModel emotionAbility in model.emotionDetail.PassiveList)
        //    {
        //        result.SetEmotionAbility(emotionAbility);
        //    }
        //    foreach (GiftModel gift in model.UnitData.unitData.GetEquippedGiftList())
        //    {
        //        result.SetGift(gift);
        //    }
        //}

        //static void SetBehaviourResultData(BattleDiceBehavior behaviour, BattleDiceBehavior opponentBehaviour, BattleCardTotalResult result, BattleDiceBehavior behaviorForDiceUI = null)
        //{
        //    DiceBehaviourResultData behaviourResult = default(DiceBehaviourResultData);
        //    behaviourResult.passingEvasion = (behaviorForDiceUI != null && behaviorForDiceUI.passingEvasion);
        //    behaviourResult.BreakState = (behaviorForDiceUI != null && behaviorForDiceUI.breakState);
        //    if (behaviour != null)
        //    {
        //        behaviourResult.result = Result.Win;
        //        if (behaviour.behaviourInCard.Type == BehaviourType.Def)
        //        {
        //            behaviourResult.skip = true;
        //        }
        //        else
        //        {
        //            behaviourResult.skip = false;
        //        }
        //        behaviourResult.actionType = ((behaviour.behaviourInCard.Type == BehaviourType.Atk) ? ActionType.Atk : ActionType.Def);
        //        behaviourResult.behaviourDetail = behaviour.behaviourInCard.Detail;
        //        if (result.usedCard != null)
        //        {
        //            bool flag = result.usedCard.GetSpec().Ranged == CardRange.Near;
        //        }
        //        behaviourResult.actionDetail = MotionConverter.MotionToAction(behaviour.behaviourInCard.MotionDetail);
        //        if (behaviour.behaviourInCard.MotionDetail == MotionDetail.N)
        //        {
        //            behaviourResult.actionType = ActionType.None;
        //        }
        //        if (behaviour.behaviourInCard.MotionDetailDefault != MotionDetail.N && behaviour.owner != null && !behaviour.owner.customBook.ContainsCategory(behaviour.card.card.GetCategory()))
        //        {
        //            behaviourResult.actionDetail = MotionConverter.MotionToAction(behaviour.behaviourInCard.MotionDetail);
        //        }
        //        behaviourResult.range = behaviour.card.card.GetSpec().Ranged;
        //        behaviourResult.actionStartDir = ActionDirection.Front;
        //    }
        //    else
        //    {
        //        behaviourResult.range = CardRange.Near;
        //        behaviourResult.result = Result.Lose;
        //        behaviourResult.actionType = ActionType.None;
        //        behaviourResult.behaviourDetail = BehaviourDetail.None;
        //        behaviourResult.actionDetail = ActionDetail.Default;
        //        if (opponentBehaviour.behaviourInCard.Type == BehaviourType.Atk)
        //        {
        //            behaviourResult.actionDetail = ActionDetail.Damaged;
        //        }
        //    }
        //    result.SetBehaviourResult(behaviourResult);
        //    SetDiceResultData(behaviour, result, behaviorForDiceUI);
        //}

        //static void SetDiceResultData(BattleDiceBehavior diceBehavior, BattleCardTotalResult result, BattleDiceBehavior diceBehaviorforUI = null)
        //{
        //    result.SetBehaviour((diceBehavior != null) ? diceBehavior : diceBehaviorforUI);
        //    if (diceBehavior == null)
        //    {
        //        result.SetVanillaDiceValue(-1);
        //        result.SetVanillaDiceFace(-1, -1);
        //        result.SetResultDiceValue(-1);
        //        result.SetResultDiceFace(-1, -1);
        //        return;
        //    }
        //    result.SetVanillaDiceValue(diceBehavior.DiceVanillaValue);
        //    result.SetVanillaDiceFace(diceBehavior.GetDiceVanillaMin(), diceBehavior.GetDiceVanillaMax());
        //    result.SetResultDiceValue(diceBehavior.DiceResultValue);
        //    result.SetResultDiceFace(diceBehavior.GetDiceMin(), diceBehavior.GetDiceMax());
        //}
    }
}
