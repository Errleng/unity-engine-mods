using LOR_DiceSystem;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;

namespace LibraryOfRuinaQolMod
{
    internal class MockBattleParryingManager : MonoBehaviour
    {
        static readonly List<string> librarianDiceRolls = new List<string>();
        static readonly List<string> enemyDiceRolls = new List<string>();
        static readonly Dictionary<string, List<string>> cachedEnemyDiceRolls = new Dictionary<string, List<string>>();
        static readonly Dictionary<string, List<string>> cachedLibrarianDiceRolls = new Dictionary<string, List<string>>();
        static BattlePlayingCardDataInUnitModel origCardEnemy;
        static BattlePlayingCardDataInUnitModel origCardLibrarian;

        //static readonly int drawDiff = 1;

        static readonly BattleParryingManager.ParryingTeam _teamEnemy = new BattleParryingManager.ParryingTeam();

        static readonly BattleParryingManager.ParryingTeam _teamLibrarian = new BattleParryingManager.ParryingTeam();

        static BattleParryingManager.ParryingDecisionResult _decisionResult;

        static BattleParryingManager.ParryingTeam _currentWinnerTeam;

        static BattleParryingManager.ParryingTeam _currentLoserTeam;

        static BattleParryingManager.ParryingTeam _currentAttackerTeam;

        static BattleParryingManager.ParryingTeam _currentDefenderTeam;

        static BattleCardTotalResult _enemyCardBehavioursResult;

        static BattleCardTotalResult _librarianCardBehavioursResult;

        //static bool cannAddForAction = true;

        public static void ResetCache()
        {
            cachedEnemyDiceRolls.Clear();
            cachedLibrarianDiceRolls.Clear();
        }

        static string CreateKey(BattlePlayingCardDataInUnitModel cardEnemy, BattlePlayingCardDataInUnitModel cardLibrarian)
        {
            return $"{cardEnemy.owner.id}-{cardEnemy.GetHashCode()}-{cardLibrarian.owner.id}-{cardLibrarian.GetHashCode()}";
        }

        static void UpdateCache(string key)
        {
            cachedEnemyDiceRolls[key] = new List<string>(enemyDiceRolls);
            cachedLibrarianDiceRolls[key] = new List<string>(librarianDiceRolls);
        }

        public static List<string> GetEnemyDiceRolls(BattlePlayingCardDataInUnitModel cardEnemy, BattlePlayingCardDataInUnitModel cardLibrarian)
        {
            var key = CreateKey(cardEnemy, cardLibrarian);
            if (cachedEnemyDiceRolls.ContainsKey(key))
            {
                Plugin.logger.LogDebug($"Using cached dice rolls for {key}: {cardEnemy.card.GetName()} ({cardEnemy.GetHashCode()}) vs. {cardLibrarian.card.GetName()} ({cardLibrarian.GetHashCode()})");
                return new List<string>(cachedEnemyDiceRolls[key]);
            }

            librarianDiceRolls.Clear();
            enemyDiceRolls.Clear();
            MockStartParryingFastAsync(cardEnemy, cardLibrarian);
            return null;
        }

        public static List<string> GetLibrarianDiceRolls(BattlePlayingCardDataInUnitModel cardEnemy, BattlePlayingCardDataInUnitModel cardLibrarian)
        {
            var key = CreateKey(cardEnemy, cardLibrarian);
            if (cachedLibrarianDiceRolls.ContainsKey(key))
            {
                Plugin.logger.LogDebug($"Using cached dice rolls for {key}: {cardEnemy.card.GetName()} ({cardEnemy.GetHashCode()}) vs. {cardLibrarian.card.GetName()} ({cardLibrarian.GetHashCode()})");
                return new List<string>(cachedLibrarianDiceRolls[key]);
            }

            librarianDiceRolls.Clear();
            enemyDiceRolls.Clear();
            MockStartParryingFastAsync(cardEnemy, cardLibrarian);
            return null;
        }

        static void MockStartParryingFastAsync(BattlePlayingCardDataInUnitModel cardEnemy, BattlePlayingCardDataInUnitModel cardLibrarian)
        {
            var key = CreateKey(cardEnemy, cardLibrarian);
            cachedEnemyDiceRolls[key] = null;
            cachedLibrarianDiceRolls[key] = null;

            origCardEnemy = cardEnemy;
            origCardLibrarian = cardLibrarian;

            var cardEnemyCopy = Utils.Clone(cardEnemy);
            var cardLibrarianCopy = Utils.Clone(cardLibrarian);
            Plugin.logger.LogDebug($"cardEnemy: {cardEnemy?.owner?.UnitData.unitData.name} and copy: {cardEnemyCopy?.owner?.UnitData.unitData.name}, cardEnemy same instance: {ReferenceEquals(cardEnemy, cardEnemyCopy)}, owner same instance: {ReferenceEquals(cardEnemy?.owner, cardEnemyCopy?.owner)}");

            Singleton<StageController>.Instance.dontUseUILog = true;
            _enemyCardBehavioursResult = new BattleCardTotalResult(cardEnemyCopy);
            _librarianCardBehavioursResult = new BattleCardTotalResult(cardLibrarianCopy);
            cardEnemyCopy.owner.battleCardResultLog = _enemyCardBehavioursResult;
            cardLibrarianCopy.owner.battleCardResultLog = _librarianCardBehavioursResult;
            _teamEnemy.Init(cardEnemyCopy.owner, cardEnemyCopy);
            _teamLibrarian.Init(cardLibrarianCopy.owner, cardLibrarianCopy);
            _teamEnemy.SetOpponent(_teamLibrarian);
            _teamLibrarian.SetOpponent(_teamEnemy);
            cardEnemyCopy.owner.cardHistory.AddCardHistory(cardEnemyCopy, Singleton<StageController>.Instance.RoundTurn);
            cardLibrarianCopy.owner.cardHistory.AddCardHistory(cardLibrarianCopy, Singleton<StageController>.Instance.RoundTurn);
            cardEnemyCopy.OnUseCard_before();
            cardLibrarianCopy.OnUseCard_before();
            _teamEnemy.unit.OnUseCard(cardEnemyCopy);
            _teamLibrarian.unit.OnUseCard(cardLibrarianCopy);
            _teamEnemy.unit.OnStartCardAction(cardEnemyCopy);
            _teamLibrarian.unit.OnStartCardAction(cardLibrarianCopy);
            cardEnemyCopy.OnStartParrying();
            cardLibrarianCopy.OnStartParrying();
            _teamEnemy.unit.OnParryingStart(cardEnemyCopy);
            _teamLibrarian.unit.OnParryingStart(cardLibrarianCopy);
            Singleton<StageController>.Instance.dontUseUILog = false;
            _enemyCardBehavioursResult.usedDiceList = cardEnemyCopy.GetDiceBehaviorList();
            _librarianCardBehavioursResult.usedDiceList = cardLibrarianCopy.GetDiceBehaviorList();
            _teamEnemy.NextDice();
            _teamLibrarian.NextDice();
            _librarianCardBehavioursResult.SetBehaviourDiceResultUI(CompareBehaviourUIType.Start);
            _enemyCardBehavioursResult.SetBehaviourDiceResultUI(CompareBehaviourUIType.Start);
            //if (_teamEnemy.unit.IsDead() || _teamLibrarian.unit.IsDead() || (!_teamEnemy.DiceExists() && !_teamLibrarian.DiceExists()) || _teamEnemy.unit.IsExtinction() || _teamLibrarian.unit.IsExtinction())
            //{
            //    EndParrying();
            //    return;
            //}
            //FirstApproachPhase();
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            SimulateDiceRolls();
            stopwatch.Stop();
            Plugin.logger.LogDebug($"Finished asynchronous parry dice roll simulations after {stopwatch.Elapsed.Seconds} seconds");
            UpdateCache(key);
        }

        static void SimulateDiceRolls()
        {
            int diceNum = 0;
            while (_teamEnemy.DiceExists() || _teamLibrarian.DiceExists())
            {
                int enemyMin = -1;
                int enemyMax = -1;
                int librarianMin = -1;
                int librarianMax = -1;

                var oldEnemyBufListDetail = _teamEnemy.playingCard.currentBehavior?.owner.bufListDetail;
                var oldLibrarianBufListDetail = _teamLibrarian.playingCard.currentBehavior?.owner.bufListDetail;

                if (_teamEnemy.DiceExists())
                {
                    //enemyMin = _teamEnemy.playingCard.currentBehavior.GetDiceMin();
                    //enemyMax = _teamEnemy.playingCard.currentBehavior.GetDiceMax();
                    _teamEnemy.playingCard.currentBehavior.owner.bufListDetail = origCardEnemy.owner.bufListDetail;
                }
                if (_teamLibrarian.DiceExists())
                {
                    //librarianMin = _teamEnemy.playingCard.currentBehavior.GetDiceMin();
                    //librarianMax = _teamEnemy.playingCard.currentBehavior.GetDiceMax();
                    _teamLibrarian.playingCard.currentBehavior.owner.bufListDetail = origCardLibrarian.owner.bufListDetail;
                }

                for (int i = 0; i < Plugin.DiceRollSimulationIterations.Value; i++)
                {
                    _teamEnemy.BeforeRollDice(_teamLibrarian.playingCard.currentBehavior);
                    _teamLibrarian.BeforeRollDice(_teamEnemy.playingCard.currentBehavior);
                    _teamEnemy.RollDice();
                    _teamLibrarian.RollDice();
                    _teamEnemy.UpdateDiceFinalValue();
                    _teamLibrarian.UpdateDiceFinalValue();
                    if (i == 0)
                    {
                        enemyMin = _teamEnemy.diceValue;
                        enemyMax = _teamEnemy.diceValue;
                        librarianMin = _teamLibrarian.diceValue;
                        librarianMax = _teamLibrarian.diceValue;
                    }
                    else
                    {
                        enemyMin = Math.Min(enemyMin, _teamEnemy.diceValue);
                        enemyMax = Math.Max(enemyMax, _teamEnemy.diceValue);
                        librarianMin = Math.Min(librarianMin, _teamLibrarian.diceValue);
                        librarianMax = Math.Max(librarianMax, _teamLibrarian.diceValue);
                    }
                }

                if (_teamEnemy.DiceExists())
                {
                    if (enemyMin == _teamEnemy.playingCard.currentBehavior.GetDiceMin()
                        && enemyMax == _teamEnemy.playingCard.currentBehavior.GetDiceMax())
                    {
                        enemyDiceRolls.Add($"{enemyMin}-{enemyMax}");
                    }
                    else
                    {
                        enemyDiceRolls.Add($"({enemyMin}-{enemyMax})");
                    }
                    _teamEnemy.playingCard.currentBehavior.owner.bufListDetail = oldEnemyBufListDetail;
                    _teamEnemy.NextDice();
                }
                if (_teamLibrarian.DiceExists())
                {
                    if (librarianMin == _teamLibrarian.playingCard.currentBehavior.GetDiceMin()
                        && librarianMax == _teamLibrarian.playingCard.currentBehavior.GetDiceMax())
                    {
                        enemyDiceRolls.Add($"{librarianMin}-{librarianMax}");
                    }
                    else
                    {
                        enemyDiceRolls.Add($"({librarianMin}-{librarianMax})");
                    }
                    _teamLibrarian.playingCard.currentBehavior.owner.bufListDetail = oldLibrarianBufListDetail;
                    _teamLibrarian.NextDice();
                }

                ++diceNum;
            }
        }

        static void MockStartParrying(BattlePlayingCardDataInUnitModel cardEnemy, BattlePlayingCardDataInUnitModel cardLibrarian)
        {
            origCardEnemy = cardEnemy;
            origCardLibrarian = cardLibrarian;

            var cardEnemyCopy = Utils.Clone(cardEnemy);
            var cardLibrarianCopy = Utils.Clone(cardLibrarian);
            Plugin.logger.LogDebug($"cardEnemy: {cardEnemy?.owner?.UnitData.unitData.name} and copy: {cardEnemyCopy?.owner?.UnitData.unitData.name}, cardEnemy same instance: {ReferenceEquals(cardEnemy, cardEnemyCopy)}, owner same instance: {ReferenceEquals(cardEnemy?.owner, cardEnemyCopy?.owner)}");

            Singleton<StageController>.Instance.dontUseUILog = true;
            _enemyCardBehavioursResult = new BattleCardTotalResult(cardEnemyCopy);
            _librarianCardBehavioursResult = new BattleCardTotalResult(cardLibrarianCopy);
            cardEnemyCopy.owner.battleCardResultLog = _enemyCardBehavioursResult;
            cardLibrarianCopy.owner.battleCardResultLog = _librarianCardBehavioursResult;
            _teamEnemy.Init(cardEnemyCopy.owner, cardEnemyCopy);
            _teamLibrarian.Init(cardLibrarianCopy.owner, cardLibrarianCopy);
            _teamEnemy.SetOpponent(_teamLibrarian);
            _teamLibrarian.SetOpponent(_teamEnemy);
            cardEnemyCopy.owner.cardHistory.AddCardHistory(cardEnemyCopy, Singleton<StageController>.Instance.RoundTurn);
            cardLibrarianCopy.owner.cardHistory.AddCardHistory(cardLibrarianCopy, Singleton<StageController>.Instance.RoundTurn);
            cardEnemyCopy.OnUseCard_before();
            cardLibrarianCopy.OnUseCard_before();
            _teamEnemy.unit.OnUseCard(cardEnemyCopy);
            _teamLibrarian.unit.OnUseCard(cardLibrarianCopy);
            _teamEnemy.unit.OnStartCardAction(cardEnemyCopy);
            _teamLibrarian.unit.OnStartCardAction(cardLibrarianCopy);
            cardEnemyCopy.OnStartParrying();
            cardLibrarianCopy.OnStartParrying();
            _teamEnemy.unit.OnParryingStart(cardEnemyCopy);
            _teamLibrarian.unit.OnParryingStart(cardLibrarianCopy);
            Singleton<StageController>.Instance.dontUseUILog = false;
            _enemyCardBehavioursResult.usedDiceList = cardEnemyCopy.GetDiceBehaviorList();
            _librarianCardBehavioursResult.usedDiceList = cardLibrarianCopy.GetDiceBehaviorList();
            _teamEnemy.NextDice();
            _teamLibrarian.NextDice();
            _librarianCardBehavioursResult.SetBehaviourDiceResultUI(CompareBehaviourUIType.Start);
            _enemyCardBehavioursResult.SetBehaviourDiceResultUI(CompareBehaviourUIType.Start);
            if (_teamEnemy.unit.IsDead() || _teamLibrarian.unit.IsDead() || (!_teamEnemy.DiceExists() && !_teamLibrarian.DiceExists()) || _teamEnemy.unit.IsExtinction() || _teamLibrarian.unit.IsExtinction())
            {
                EndParrying();
                return;
            }
            FirstApproachPhase();
        }

        static void FirstApproachPhase()
        {
            Decision();
        }

        static void Decision()
        {
            RollDice();
            bool flag = false;
            if (_teamEnemy.DiceExists() && !_teamLibrarian.DiceExists())
            {
                _decisionResult = BattleParryingManager.ParryingDecisionResult.WinEnemy;
                if (!_teamEnemy.isKeepedCard && _teamEnemy.GetBehaviourType() == BehaviourType.Def)
                {
                    if (!_teamEnemy.playingCard.currentBehavior.isBonusEvasion)
                    {
                        _teamEnemy.unit.cardSlotDetail.keepCard.AddBehaviour(_teamEnemy.playingCard.card, _teamEnemy.playingCard.currentBehavior);
                    }
                    else
                    {
                        _teamEnemy.playingCard.currentBehavior.passingEvasion = true;
                    }
                }
            }
            else if (!_teamEnemy.DiceExists() && _teamLibrarian.DiceExists())
            {
                _decisionResult = BattleParryingManager.ParryingDecisionResult.WinLibrarian;
                if (!_teamLibrarian.isKeepedCard && _teamLibrarian.GetBehaviourType() == BehaviourType.Def)
                {
                    if (!_teamLibrarian.playingCard.currentBehavior.isBonusEvasion)
                    {
                        _teamLibrarian.unit.cardSlotDetail.keepCard.AddBehaviour(_teamLibrarian.playingCard.card, _teamLibrarian.playingCard.currentBehavior);
                    }
                    else
                    {
                        _teamLibrarian.playingCard.currentBehavior.passingEvasion = true;
                    }
                }
            }
            else if (_teamEnemy.DiceExists() && _teamLibrarian.DiceExists())
            {
                flag = true;
                _decisionResult = GetDecisionResult(_teamEnemy, _teamLibrarian);
            }
            if (_teamEnemy.GetRange() == CardRange.Far && _teamLibrarian.GetRange() == CardRange.Far)
            {
                if (_teamEnemy.GetParryingDiceType() == BattleParryingManager.ParryingDiceType.Attack && _teamLibrarian.GetParryingDiceType() == BattleParryingManager.ParryingDiceType.Defense)
                {
                    _currentAttackerTeam = _teamEnemy;
                    _currentDefenderTeam = _teamLibrarian;
                }
                else if (_teamLibrarian.GetParryingDiceType() == BattleParryingManager.ParryingDiceType.Attack && _teamEnemy.GetParryingDiceType() == BattleParryingManager.ParryingDiceType.Defense)
                {
                    _currentAttackerTeam = _teamLibrarian;
                    _currentDefenderTeam = _teamEnemy;
                }
            }
            else if (_teamEnemy.GetRange() == CardRange.Far && _teamLibrarian.GetRange() == CardRange.Near)
            {
                _currentAttackerTeam = _teamEnemy;
                _currentDefenderTeam = _teamLibrarian;
            }
            else if (_teamEnemy.GetRange() == CardRange.Near && _teamLibrarian.GetRange() == CardRange.Far)
            {
                _currentAttackerTeam = _teamLibrarian;
                _currentDefenderTeam = _teamEnemy;
            }
            else if (_teamEnemy.GetParryingDiceType() == BattleParryingManager.ParryingDiceType.Attack && _teamLibrarian.GetParryingDiceType() == BattleParryingManager.ParryingDiceType.Defense)
            {
                _currentAttackerTeam = _teamEnemy;
                _currentDefenderTeam = _teamLibrarian;
            }
            else if (_teamLibrarian.GetParryingDiceType() == BattleParryingManager.ParryingDiceType.Attack && _teamEnemy.GetParryingDiceType() == BattleParryingManager.ParryingDiceType.Defense)
            {
                _currentAttackerTeam = _teamLibrarian;
                _currentDefenderTeam = _teamEnemy;
            }
            if (_teamEnemy.GetParryingDiceType() == BattleParryingManager.ParryingDiceType.Attack && _teamLibrarian.GetParryingDiceType() == BattleParryingManager.ParryingDiceType.Defense)
            {
                _currentAttackerTeam = _teamEnemy;
                _currentDefenderTeam = _teamLibrarian;
            }
            else if (_teamLibrarian.GetParryingDiceType() == BattleParryingManager.ParryingDiceType.Attack && _teamEnemy.GetParryingDiceType() == BattleParryingManager.ParryingDiceType.Defense)
            {
                _currentAttackerTeam = _teamLibrarian;
                _currentDefenderTeam = _teamEnemy;
            }
            int num = 0;
            int num2 = 0;
            if (_decisionResult == BattleParryingManager.ParryingDecisionResult.WinEnemy)
            {
                _currentWinnerTeam = _teamEnemy;
                _currentLoserTeam = _teamLibrarian;
                if (flag)
                {
                    num += _currentWinnerTeam.GetEmotionMultiplier();
                    num2 += _currentLoserTeam.GetEmotionMultiplier();
                    int count = _currentWinnerTeam.unit.emotionDetail.CreateEmotionCoin(EmotionCoinType.Positive, num);
                    int count2 = _currentLoserTeam.unit.emotionDetail.CreateEmotionCoin(EmotionCoinType.Negative, num2);
                    BattleCardTotalResult battleCardResultLog = _currentWinnerTeam.unit.battleCardResultLog;
                    if (battleCardResultLog != null)
                    {
                        battleCardResultLog.AddEmotionCoin(EmotionCoinType.Positive, count);
                    }
                    BattleCardTotalResult battleCardResultLog2 = _currentLoserTeam.unit.battleCardResultLog;
                    if (battleCardResultLog2 != null)
                    {
                        battleCardResultLog2.AddEmotionCoin(EmotionCoinType.Negative, count2);
                    }
                }
            }
            else if (_decisionResult == BattleParryingManager.ParryingDecisionResult.WinLibrarian)
            {
                _currentWinnerTeam = _teamLibrarian;
                _currentLoserTeam = _teamEnemy;
                if (flag)
                {
                    num += _currentWinnerTeam.GetEmotionMultiplier();
                    num2 += _currentLoserTeam.GetEmotionMultiplier();
                    int count3 = _currentWinnerTeam.unit.emotionDetail.CreateEmotionCoin(EmotionCoinType.Positive, num);
                    int count4 = _currentLoserTeam.unit.emotionDetail.CreateEmotionCoin(EmotionCoinType.Negative, num2);
                    BattleCardTotalResult battleCardResultLog3 = _currentWinnerTeam.unit.battleCardResultLog;
                    if (battleCardResultLog3 != null)
                    {
                        battleCardResultLog3.AddEmotionCoin(EmotionCoinType.Positive, count3);
                    }
                    BattleCardTotalResult battleCardResultLog4 = _currentLoserTeam.unit.battleCardResultLog;
                    if (battleCardResultLog4 != null)
                    {
                        battleCardResultLog4.AddEmotionCoin(EmotionCoinType.Negative, count4);
                    }
                }
            }
            else
            {
                if (_teamEnemy.GetParryingDiceType() == BattleParryingManager.ParryingDiceType.Attack && _teamLibrarian.GetParryingDiceType() == BattleParryingManager.ParryingDiceType.Attack)
                {
                    int emotionMultiplier = _teamLibrarian.GetEmotionMultiplier();
                    int emotionMultiplier2 = _teamEnemy.GetEmotionMultiplier();
                    int count5 = _teamLibrarian.unit.emotionDetail.CreateEmotionCoin(EmotionCoinType.Positive, emotionMultiplier);
                    int count6 = _teamEnemy.unit.emotionDetail.CreateEmotionCoin(EmotionCoinType.Positive, emotionMultiplier2);
                    BattleCardTotalResult battleCardResultLog5 = _teamLibrarian.unit.battleCardResultLog;
                    if (battleCardResultLog5 != null)
                    {
                        battleCardResultLog5.AddEmotionCoin(EmotionCoinType.Positive, count5);
                    }
                    BattleCardTotalResult battleCardResultLog6 = _teamEnemy.unit.battleCardResultLog;
                    if (battleCardResultLog6 != null)
                    {
                        battleCardResultLog6.AddEmotionCoin(EmotionCoinType.Positive, count6);
                    }
                }
                _currentWinnerTeam = null;
                _currentLoserTeam = null;
            }
            if (_teamLibrarian.DiceExists())
            {
                _teamLibrarian.playingCard.currentBehavior.isBonusEvasion = false;
                _teamLibrarian.playingCard.currentBehavior.isBonusAttack = false;
            }
            if (_teamEnemy.DiceExists())
            {
                _teamEnemy.playingCard.currentBehavior.isBonusEvasion = false;
                _teamEnemy.playingCard.currentBehavior.isBonusAttack = false;
            }
            ActionPhase();
        }

        static void RollDice()
        {
            BeforeRollDice(); // sets the buffs/debuffs that are shown (manually rolling / not in quick mode)

            int enemyMin = -1;
            int enemyMax = -1;
            int librarianMin = -1;
            int librarianMax = -1;

            var oldEnemyBufListDetail = _teamEnemy.playingCard.currentBehavior?.owner.bufListDetail;
            var oldLibrarianBufListDetail = _teamLibrarian.playingCard.currentBehavior?.owner.bufListDetail;

            if (_teamEnemy.DiceExists())
            {
                _teamEnemy.playingCard.currentBehavior.owner.bufListDetail = origCardEnemy.owner.bufListDetail;
            }
            if (_teamLibrarian.DiceExists())
            {
                _teamLibrarian.playingCard.currentBehavior.owner.bufListDetail = origCardLibrarian.owner.bufListDetail;
            }
            for (int i = 0; i < Plugin.DiceRollSimulationIterations.Value; i++)
            {
                _teamEnemy.BeforeRollDice(_teamLibrarian.playingCard.currentBehavior);
                _teamLibrarian.BeforeRollDice(_teamEnemy.playingCard.currentBehavior);
                _teamEnemy.RollDice();
                _teamLibrarian.RollDice();
                _teamEnemy.UpdateDiceFinalValue();
                _teamLibrarian.UpdateDiceFinalValue();
                if (i == 0)
                {
                    enemyMin = _teamEnemy.diceValue;
                    enemyMax = _teamEnemy.diceValue;
                    librarianMin = _teamLibrarian.diceValue;
                    librarianMax = _teamLibrarian.diceValue;
                }
                else
                {
                    enemyMin = Math.Min(enemyMin, _teamEnemy.diceValue);
                    enemyMax = Math.Max(enemyMax, _teamEnemy.diceValue);
                    librarianMin = Math.Min(librarianMin, _teamLibrarian.diceValue);
                    librarianMax = Math.Max(librarianMax, _teamLibrarian.diceValue);
                }
            }
            if (_teamEnemy.DiceExists())
            {
                _teamEnemy.playingCard.currentBehavior.owner.bufListDetail = oldEnemyBufListDetail;
            }
            if (_teamLibrarian.DiceExists())
            {
                _teamLibrarian.playingCard.currentBehavior.owner.bufListDetail = oldLibrarianBufListDetail;
            }

            enemyDiceRolls.Add($"({enemyMin}-{enemyMax})");
            librarianDiceRolls.Add($"({librarianMin}-{librarianMax})");

            AfterRollDice();
        }

        static void ActionPhase()
        {
            if (_decisionResult == BattleParryingManager.ParryingDecisionResult.Draw)
            {
                if (_teamEnemy.GetParryingDiceType() == BattleParryingManager.ParryingDiceType.Attack)
                {
                    if (_teamLibrarian.GetParryingDiceType() == BattleParryingManager.ParryingDiceType.Attack)
                    {
                        ActionPhaseAtkVSAtkDraw();
                    }
                    else
                    {
                        ActionPhaseAtkVSDfnDraw();
                    }
                }
                else if (_teamLibrarian.GetParryingDiceType() == BattleParryingManager.ParryingDiceType.Attack)
                {
                    ActionPhaseAtkVSDfnDraw();
                }
                else
                {
                    ActionPhaseDfnVSDfnDraw();
                }
            }
            else if (_currentWinnerTeam.GetParryingDiceType() == BattleParryingManager.ParryingDiceType.Attack)
            {
                if (_currentLoserTeam.GetParryingDiceType() == BattleParryingManager.ParryingDiceType.Attack)
                {
                    ActionPhaseAtkVSAtk();
                }
                else
                {
                    ActionPhaseAtkVSDfn();
                }
            }
            else if (_currentLoserTeam.DiceExists())
            {
                if (_currentLoserTeam.GetParryingDiceType() == BattleParryingManager.ParryingDiceType.Attack)
                {
                    ActionPhaseAtkVSDfn();
                }
                else
                {
                    ActionPhaseDfnVSDfn();
                }
            }
            EndAction();
        }

        static void EndAction()
        {
            if (_teamEnemy.DiceExists())
            {
                _teamEnemy.playingCard.AfterAction();
            }
            if (_teamLibrarian.DiceExists())
            {
                _teamLibrarian.playingCard.AfterAction();
            }
            BattleCardTotalResult enemyCardBehavioursResult = _enemyCardBehavioursResult;
            if (enemyCardBehavioursResult != null)
            {
                enemyCardBehavioursResult.SetCurrentHp(_teamEnemy.unit.hp);
            }
            BattleCardTotalResult enemyCardBehavioursResult2 = _enemyCardBehavioursResult;
            if (enemyCardBehavioursResult2 != null)
            {
                enemyCardBehavioursResult2.SetCurrentBreakGauge(_teamEnemy.unit.breakDetail.breakGauge);
            }
            if (!_teamEnemy.DiceExists() || !_teamEnemy.playingCard.currentBehavior.isBonusEvasion || _teamLibrarian.DiceExists())
            {
                SetBehaviourResultData(_teamEnemy, _teamLibrarian, _enemyCardBehavioursResult);
            }
            BattleCardTotalResult enemyCardBehavioursResult3 = _enemyCardBehavioursResult;
            if (enemyCardBehavioursResult3 != null)
            {
                enemyCardBehavioursResult3.SetCurrentBuf(_teamEnemy.unit.bufListDetail.GetBufUIDataList());
            }
            BattleCardTotalResult librarianCardBehavioursResult = _librarianCardBehavioursResult;
            if (librarianCardBehavioursResult != null)
            {
                librarianCardBehavioursResult.SetCurrentHp(_teamLibrarian.unit.hp);
            }
            BattleCardTotalResult librarianCardBehavioursResult2 = _librarianCardBehavioursResult;
            if (librarianCardBehavioursResult2 != null)
            {
                librarianCardBehavioursResult2.SetCurrentBreakGauge(_teamLibrarian.unit.breakDetail.breakGauge);
            }
            if (!_teamLibrarian.DiceExists() || !_teamLibrarian.playingCard.currentBehavior.isBonusEvasion || _teamEnemy.DiceExists())
            {
                SetBehaviourResultData(_teamLibrarian, _teamEnemy, _librarianCardBehavioursResult);
            }
            BattleCardTotalResult librarianCardBehavioursResult3 = _librarianCardBehavioursResult;
            if (librarianCardBehavioursResult3 != null)
            {
                librarianCardBehavioursResult3.SetCurrentBuf(_teamLibrarian.unit.bufListDetail.GetBufUIDataList());
            }
            _enemyCardBehavioursResult.SetBehaviourDiceResultUI(CompareBehaviourUIType.Start);
            if (_teamEnemy.DiceExists() && ((!_teamEnemy.playingCard.currentBehavior.isBonusEvasion && !_teamEnemy.playingCard.currentBehavior.isBonusAttack) || _teamEnemy.playingCard.currentBehavior.forbiddenBonusDice))
            {
                _teamEnemy.NextDice();
            }
            _librarianCardBehavioursResult.SetBehaviourDiceResultUI(CompareBehaviourUIType.Start);
            if (_teamLibrarian.DiceExists() && ((!_teamLibrarian.playingCard.currentBehavior.isBonusEvasion && !_teamLibrarian.playingCard.currentBehavior.isBonusAttack) || _teamLibrarian.playingCard.currentBehavior.forbiddenBonusDice))
            {
                _teamLibrarian.NextDice();
            }
            CheckParryingEnd();
        }

        static void CheckParryingEnd()
        {
            if (_teamEnemy.unit.IsDead() || _teamLibrarian.unit.IsDead() || (!_teamEnemy.DiceExists() && !_teamLibrarian.DiceExists()) || _teamEnemy.unit.IsExtinction() || _teamLibrarian.unit.IsExtinction())
            {
                EndParrying();
                return;
            }
            if (_teamEnemy.playingCard.isKeepedCard && _teamEnemy.DiceExists() && !_teamLibrarian.DiceExists())
            {
                if (_teamEnemy.playingCard.currentBehavior != null && _teamEnemy.playingCard.currentBehavior.isUsed)
                {
                    _teamEnemy.playingCard.currentBehavior = null;
                }
                EndParrying();
                return;
            }
            if (_teamLibrarian.playingCard.isKeepedCard && _teamLibrarian.DiceExists() && !_teamEnemy.DiceExists())
            {
                if (_teamLibrarian.playingCard.currentBehavior != null && _teamLibrarian.playingCard.currentBehavior.isUsed)
                {
                    _teamLibrarian.playingCard.currentBehavior = null;
                }
                EndParrying();
                return;
            }
            _enemyCardBehavioursResult.InitNextBehaviour();
            _librarianCardBehavioursResult.InitNextBehaviour();
            _librarianCardBehavioursResult.SetBehaviourDiceResultUI(CompareBehaviourUIType.Start);
            _enemyCardBehavioursResult.SetBehaviourDiceResultUI(CompareBehaviourUIType.Start);
            Decision();
        }

        static BattleParryingManager.ParryingDecisionResult GetDecisionResult(BattleParryingManager.ParryingTeam teamA, BattleParryingManager.ParryingTeam teamB)
        {
            if (teamA.DiceExists() && teamB.DiceExists())
            {
                if (teamA.GetBehaviourDetail() == BehaviourDetail.Evasion && teamB.GetBehaviourDetail() == BehaviourDetail.Evasion)
                {
                    return BattleParryingManager.ParryingDecisionResult.Draw;
                }
                int num = teamA.diceValue - teamB.diceValue;
                if (num >= 1)
                {
                    return BattleParryingManager.ParryingDecisionResult.WinEnemy;
                }
                if (num <= -1)
                {
                    return BattleParryingManager.ParryingDecisionResult.WinLibrarian;
                }
                return BattleParryingManager.ParryingDecisionResult.Draw;
            }
            else
            {
                if (teamA.DiceExists() && !teamB.DiceExists())
                {
                    return BattleParryingManager.ParryingDecisionResult.WinEnemy;
                }
                if (!teamA.DiceExists() && teamB.DiceExists())
                {
                    return BattleParryingManager.ParryingDecisionResult.WinLibrarian;
                }
                return BattleParryingManager.ParryingDecisionResult.Draw;
            }
        }

        static void EndParrying()
        {
            //CheckStandyDiceBeforeEndParrying(_teamLibrarian);
            //CheckStandyDiceBeforeEndParrying(_teamEnemy);
            //_teamLibrarian.unit.OnEndParrying_Before(_teamLibrarian.playingCard);
            //_teamEnemy.unit.OnEndParrying_Before(_teamEnemy.playingCard);
            //Singleton<StageController>.Instance.OnEndParrying(_teamEnemy.unit, _teamLibrarian.unit, _teamEnemy.resultKnockbackEnergy, _teamLibrarian.resultKnockbackEnergy);
        }

        static void BeforeRollDice()
        {
            SetAbilityDataBeforeRoll(_teamEnemy.unit, _enemyCardBehavioursResult);
            SetAbilityDataBeforeRoll(_teamLibrarian.unit, _librarianCardBehavioursResult);
        }

        static void AfterRollDice()
        {
            SetAbilityDataAfterRoll(_teamEnemy.unit, _enemyCardBehavioursResult);
            SetAbilityDataAfterRoll(_teamLibrarian.unit, _enemyCardBehavioursResult);
        }

        static void ActionPhaseAtkVSAtkDraw()
        {
            _teamEnemy.playingCard.OnDrawParrying();
            _teamLibrarian.playingCard.OnDrawParrying();
        }

        static void ActionPhaseAtkVSDfnDraw()
        {
            _teamEnemy.playingCard.OnDrawParrying();
            _teamLibrarian.playingCard.OnDrawParrying();
        }

        static void ActionPhaseDfnVSDfnDraw()
        {
            _teamEnemy.playingCard.OnDrawParrying();
            _teamLibrarian.playingCard.OnDrawParrying();
        }

        static void ActionPhaseAtkVSAtk()
        {
            //BattleUnitModel unit = _currentWinnerTeam.unit;
            BattleUnitModel unit2 = _currentLoserTeam.unit;
            if (_currentWinnerTeam.playingCard.currentBehavior != null)
            {
                if (_currentLoserTeam.GetRange() == CardRange.Far)
                {
                    if (_currentWinnerTeam.GetRange() == CardRange.Near)
                    {
                        if (!_currentWinnerTeam.playingCard.currentBehavior.forbiddenBonusDice)
                        {
                            if (_currentWinnerTeam.GetBehaviourType() == BehaviourType.Standby)
                            {
                                _currentWinnerTeam.playingCard.currentBehavior.isBonusAttack = true;
                            }
                            else
                            {
                                BattleDiceBehavior currentBehavior = _currentWinnerTeam.playingCard.currentBehavior;
                                BattleDiceBehavior battleDiceBehavior = _currentWinnerTeam.playingCard.CopyDiceBehaviour(currentBehavior);
                                battleDiceBehavior.winAgainstFarAtk = true;
                                _currentWinnerTeam.playingCard.AddDice(battleDiceBehavior);
                            }
                        }
                    }
                    else
                    {
                        //_currentWinnerTeam.playingCard.currentBehavior.GiveDamage(unit2);
                        if (_currentWinnerTeam.GetBehaviourType() == BehaviourType.Standby && !_currentLoserTeam.unit.IsDead())
                        {
                            _currentWinnerTeam.playingCard.currentBehavior.isBonusAttack = true;
                        }
                    }
                    if (_currentLoserTeam.DiceExists())
                    {
                        _currentWinnerTeam.playingCard.OnWinParryingDefense();
                        _currentLoserTeam.playingCard.OnLoseParrying();
                        return;
                    }
                }
                else
                {
                    //_currentWinnerTeam.playingCard.currentBehavior.GiveDamage(unit2);
                    int knockbackPower = _currentWinnerTeam.playingCard.currentBehavior.behaviourInCard.KnockbackPower;
                    int num = 0;
                    _currentLoserTeam.resultKnockbackEnergy += knockbackPower - num;
                    if (_currentLoserTeam.DiceExists())
                    {
                        _currentWinnerTeam.playingCard.OnWinParryingAttack();
                        _currentLoserTeam.playingCard.OnLoseParrying();
                    }
                    if (_currentWinnerTeam.GetBehaviourType() == BehaviourType.Standby && !_currentLoserTeam.unit.IsDead())
                    {
                        _currentWinnerTeam.playingCard.currentBehavior.isBonusAttack = true;
                    }
                }
            }
        }

        static void ActionPhaseAtkVSDfn()
        {
            if (!_currentDefenderTeam.DiceExists())
            {
                //_currentAttackerTeam.playingCard.currentBehavior.GiveDamage(_currentDefenderTeam.unit);
                int knockbackPower = _currentAttackerTeam.playingCard.currentBehavior.behaviourInCard.KnockbackPower;
                int num = 0;
                _currentDefenderTeam.resultKnockbackEnergy += knockbackPower - num;
                return;
            }
            if (_currentDefenderTeam.GetBehaviourDetail() == BehaviourDetail.Guard)
            {
                if (_currentDefenderTeam == _currentLoserTeam)
                {
                    _currentAttackerTeam.playingCard.currentBehavior.SetDamageRedution(_currentDefenderTeam.GetDiceValue());
                    //_currentAttackerTeam.playingCard.currentBehavior.GiveDamage(_currentDefenderTeam.unit);
                    int knockbackPower2 = _currentAttackerTeam.playingCard.currentBehavior.behaviourInCard.KnockbackPower;
                    int num2 = 0;
                    _currentDefenderTeam.resultKnockbackEnergy += knockbackPower2 - num2;
                    if (_currentLoserTeam.DiceExists())
                    {
                        _currentWinnerTeam.playingCard.OnWinParryingAttack();
                        _currentLoserTeam.playingCard.OnLoseParrying();
                    }
                    if (_currentAttackerTeam.GetBehaviourType() == BehaviourType.Standby && !_currentLoserTeam.unit.IsDead())
                    {
                        _currentAttackerTeam.playingCard.currentBehavior.isBonusAttack = true;
                        return;
                    }
                }
                else
                {
                    CardRange ranged = _currentAttackerTeam.playingCard.currentBehavior.card.card.GetSpec().Ranged;
                    if (ranged == CardRange.Near || ranged == CardRange.Special)
                    {
                        //_currentDefenderTeam.playingCard.currentBehavior.GiveDeflectDamage(_currentAttackerTeam.playingCard.currentBehavior);
                    }
                    _currentDefenderTeam.playingCard.OnWinParryingDefense();
                    _currentAttackerTeam.playingCard.OnLoseParrying();
                    if (_currentDefenderTeam.GetBehaviourType() == BehaviourType.Standby && !_currentLoserTeam.unit.IsDead())
                    {
                        _currentDefenderTeam.playingCard.currentBehavior.isBonusEvasion = true;
                        return;
                    }
                }
            }
            else if (_currentDefenderTeam.GetBehaviourDetail() == BehaviourDetail.Evasion)
            {
                if (_currentDefenderTeam == _currentLoserTeam)
                {
                    //_currentAttackerTeam.playingCard.currentBehavior.GiveDamage(_currentDefenderTeam.unit);
                    int knockbackPower3 = _currentAttackerTeam.playingCard.currentBehavior.behaviourInCard.KnockbackPower;
                    int num3 = 0;
                    _currentDefenderTeam.resultKnockbackEnergy += knockbackPower3 - num3;
                    if (_currentLoserTeam.DiceExists())
                    {
                        _currentWinnerTeam.playingCard.OnWinParryingAttack();
                        _currentLoserTeam.playingCard.OnLoseParrying();
                    }
                    if (_currentAttackerTeam.GetBehaviourType() == BehaviourType.Standby && !_currentLoserTeam.unit.IsDead())
                    {
                        _currentAttackerTeam.playingCard.currentBehavior.isBonusAttack = true;
                        return;
                    }
                }
                else
                {
                    if (_currentAttackerTeam.GetBehaviourType() == BehaviourType.Atk)
                    {
                        int diceResultValue = _currentDefenderTeam.playingCard.currentBehavior.DiceResultValue;
                        _currentDefenderTeam.unit.breakDetail.OnRecoverBreakByEvaision(diceResultValue);
                        _currentDefenderTeam.playingCard.currentBehavior.isBonusEvasion = true;
                    }
                    else if (_currentDefenderTeam.GetBehaviourType() == BehaviourType.Standby && !_currentLoserTeam.unit.IsDead())
                    {
                        _currentDefenderTeam.playingCard.currentBehavior.isBonusEvasion = true;
                    }
                    _currentDefenderTeam.playingCard.OnWinParryingDefense();
                    _currentAttackerTeam.playingCard.OnLoseParrying();
                }
            }
        }

        static void ActionPhaseDfnVSDfn()
        {
            if (_currentWinnerTeam.DiceExists() && _currentLoserTeam.DiceExists())
            {
                if (_currentWinnerTeam.GetBehaviourDetail() == BehaviourDetail.Guard)
                {
                    if (_currentLoserTeam.GetBehaviourDetail() == BehaviourDetail.Guard)
                    {
                        CardRange range = _currentLoserTeam.GetRange();
                        if (range == CardRange.Near || range == CardRange.Far || range == CardRange.Special)
                        {
                            //_currentWinnerTeam.playingCard.currentBehavior.GiveDeflectDamage(_currentLoserTeam.playingCard.currentBehavior);
                        }
                        _currentWinnerTeam.playingCard.OnWinParryingDefense();
                        _currentLoserTeam.playingCard.OnLoseParrying();
                    }
                    else
                    {
                        CardRange range2 = _currentLoserTeam.GetRange();
                        if (range2 == CardRange.Near || range2 == CardRange.Far || range2 == CardRange.Special)
                        {
                            //_currentWinnerTeam.playingCard.currentBehavior.GiveDeflectDamage(_currentLoserTeam.playingCard.currentBehavior);
                        }
                        _currentWinnerTeam.playingCard.OnWinParryingDefense();
                        _currentLoserTeam.playingCard.OnLoseParrying();
                    }
                }
                else if (_currentLoserTeam.GetBehaviourDetail() == BehaviourDetail.Guard)
                {
                    int diceResultValue = _currentWinnerTeam.playingCard.currentBehavior.DiceResultValue;
                    _currentWinnerTeam.unit.breakDetail.OnRecoverBreakByEvaision(diceResultValue);
                    _currentWinnerTeam.playingCard.OnWinParryingDefense();
                    _currentLoserTeam.playingCard.OnLoseParrying();
                }
                if (_currentWinnerTeam.GetBehaviourType() == BehaviourType.Standby && !_currentLoserTeam.unit.IsDead())
                {
                    _currentWinnerTeam.playingCard.currentBehavior.isBonusEvasion = true;
                }
            }
        }

        static void SetAbilityDataBeforeRoll(BattleUnitModel model, BattleCardTotalResult result)
        {
            foreach (BattleUnitBuf bufs in model.bufListDetail.GetActivatedBufList())
            {
                result.SetBufs(bufs);
            }
            foreach (BattleEmotionCardModel emotionAbility in model.emotionDetail.PassiveList)
            {
                result.SetEmotionAbility(emotionAbility);
            }
            foreach (GiftModel gift in model.UnitData.unitData.GetEquippedGiftList())
            {
                result.SetGift(gift);
            }
        }

        static void SetAbilityDataAfterRoll(BattleUnitModel model, BattleCardTotalResult result)
        {
            foreach (BattleUnitBuf newBufs in model.bufListDetail.GetActivatedBufList())
            {
                result.SetNewBufs(newBufs);
            }
            foreach (BattleEmotionCardModel emotionAbility in model.emotionDetail.PassiveList)
            {
                result.SetEmotionAbility(emotionAbility);
            }
            foreach (GiftModel gift in model.UnitData.unitData.GetEquippedGiftList())
            {
                result.SetGift(gift);
            }
        }

        static void SetBehaviourResultData(BattleParryingManager.ParryingTeam team, BattleParryingManager.ParryingTeam opponentTeam, BattleCardTotalResult result)
        {
            DiceBehaviourResultData diceBehaviourResultData = default(DiceBehaviourResultData);
            BattleParryingManager.ParryingDiceType parryingDiceType = team.GetParryingDiceType();
            if (parryingDiceType != BattleParryingManager.ParryingDiceType.Attack)
            {
                if (parryingDiceType == BattleParryingManager.ParryingDiceType.Defense)
                {
                    diceBehaviourResultData.actionType = ActionType.Def;
                }
            }
            else
            {
                diceBehaviourResultData.actionType = ActionType.Atk;
            }
            if (diceBehaviourResultData.actionType == ActionType.Def && !opponentTeam.DiceExists())
            {
                diceBehaviourResultData.skip = true;
                result.SetSkip(DiceUITiming.Start);
            }
            else
            {
                diceBehaviourResultData.skip = false;
            }
            diceBehaviourResultData.passingEvasion = false;
            diceBehaviourResultData.BreakState = false;
            if (team.playingCard != null && team.playingCard.currentBehavior != null)
            {
                diceBehaviourResultData.passingEvasion = team.playingCard.currentBehavior.passingEvasion;
                diceBehaviourResultData.BreakState = team.playingCard.currentBehavior.breakState;
            }
            diceBehaviourResultData.behaviourDetail = team.GetBehaviourDetail();
            if (team.DiceExists() && team.playingCard.currentBehavior.card.card.GetSpec().Ranged == CardRange.Far)
            {
                diceBehaviourResultData.range = CardRange.Far;
            }
            else
            {
                diceBehaviourResultData.range = CardRange.Near;
            }
            diceBehaviourResultData.actionDetail = MotionConverter.MotionToAction(team.GetMotionDetail());
            if (team.GetMotionDetailDefault() != MotionDetail.N && !team.unit.customBook.ContainsCategory(team.playingCard.card.GetCategory()))
            {
                diceBehaviourResultData.actionDetail = MotionConverter.MotionToAction(team.GetMotionDetail());
            }
            diceBehaviourResultData.actionStartDir = ActionDirection.Front;
            if (_decisionResult == BattleParryingManager.ParryingDecisionResult.Draw)
            {
                diceBehaviourResultData.result = Result.Draw;
            }
            else if (_decisionResult == BattleParryingManager.ParryingDecisionResult.WinEnemy)
            {
                if (team == _teamEnemy)
                {
                    diceBehaviourResultData.result = Result.Win;
                }
                else
                {
                    diceBehaviourResultData.result = Result.Lose;
                }
            }
            else if (team == _teamLibrarian)
            {
                diceBehaviourResultData.result = Result.Win;
            }
            else
            {
                diceBehaviourResultData.result = Result.Lose;
            }
            if (diceBehaviourResultData.result == Result.Lose && opponentTeam.GetBehaviourType() == BehaviourType.Atk && diceBehaviourResultData.range != CardRange.Far)
            {
                diceBehaviourResultData.actionDetail = ActionDetail.Damaged;
            }
            result.SetBehaviourResult(diceBehaviourResultData);
            BattlePlayingCardDataInUnitModel playingCard = team.playingCard;
            BattleDiceBehavior diceBehavior = (playingCard != null) ? playingCard.currentBehavior : null;
            BattlePlayingCardDataInUnitModel playingCard2 = team.playingCard;
            SetDiceResultData(diceBehavior, result, (playingCard2 != null) ? playingCard2.currentBehaviorUI : null);
        }

        static void SetDiceResultData(BattleDiceBehavior diceBehavior, BattleCardTotalResult result, BattleDiceBehavior diceBehaviorforUI = null)
        {
            result.SetBehaviour((diceBehavior != null) ? diceBehavior : diceBehaviorforUI);
            if (diceBehavior == null)
            {
                result.SetVanillaDiceValue(-1);
                result.SetVanillaDiceFace(-1, -1);
                result.SetResultDiceValue(-1);
                result.SetResultDiceFace(-1, -1);
                return;
            }
            result.SetVanillaDiceValue(diceBehavior.DiceVanillaValue);
            result.SetVanillaDiceFace(diceBehavior.GetDiceVanillaMin(), diceBehavior.GetDiceVanillaMax());
            result.SetResultDiceValue(diceBehavior.DiceResultValue);
            result.SetResultDiceFace(diceBehavior.GetDiceMin(), diceBehavior.GetDiceMax());
        }
    }
}
