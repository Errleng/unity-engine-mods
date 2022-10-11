using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI;
using UnityEngine;

namespace LibraryOfRuination
{
    public class DiceCardAbility_AlivePowerUp_DiceEffect : DiceCardAbilityBase
    {
        public static string Desc = "Gain power equal to the number of allies currently alive";

        public override void BeforeRollDice()
        {
            base.BeforeRollDice();
            var aliveList = BattleObjectManager.instance.GetAliveList(owner.faction);
            behavior.ApplyDiceStatBonus(new DiceStatBonus
            {
                power = aliveList.Count
            });
        }
    }

    public class DiceCardSelfAbility_SummoningPortal_CardEffect : DiceCardSelfAbilityBase
    {
        public static string Desc = "[On Use] At the end of the scene, summon a random character to fight on your side.";

        public override void OnUseCard()
        {
            base.OnUseCard();
            var enemyUnitClassInfoList = Initializer.spawnList;
            Debug.Log($"Enemy unit class info length: {enemyUnitClassInfoList.Count}");
            if (enemyUnitClassInfoList.Count == 0)
            {
                return;
            }
            var randomEnemyInfo = enemyUnitClassInfoList[UnityEngine.Random.Range(0, enemyUnitClassInfoList.Count)];
            if (randomEnemyInfo == null)
            {
                return;
            }
            var summonBuff = new BattleUnitBuf_SummonCharacterLimited(randomEnemyInfo);
            owner.bufListDetail.AddBuf(summonBuff);
        }
    }

    public class DiceCardSelfAbility_GreaterSummoningPortal_CardEffect : DiceCardSelfAbilityBase
    {
        public static string Desc = "[On Use] At the end of the scene, summon two random characters to fight on your side.";

        public override void OnUseCard()
        {
            base.OnUseCard();
            var enemyUnitClassInfoList = Initializer.spawnList;
            Debug.Log($"Enemy unit class info length: {enemyUnitClassInfoList.Count}");
            if (enemyUnitClassInfoList.Count == 0)
            {
                return;
            }
            for (int i = 0; i < 2; i++)
            {
                var randomEnemyInfo = enemyUnitClassInfoList[UnityEngine.Random.Range(0, enemyUnitClassInfoList.Count)];
                if (randomEnemyInfo == null)
                {
                    continue;
                }
                owner.bufListDetail.AddBuf(new BattleUnitBuf_SummonCharacterUnlimited(randomEnemyInfo));
            }
        }
    }

    public class PassiveAbility_EredarLord_PassiveEffect : PassiveAbilityBase
    {
        public override void OnWaveStart()
        {
            base.OnWaveStart();
            AudioClip jaraxxusClip;
            if (Initializer.audioClips.TryGetValue("you-face-jaraxxus", out jaraxxusClip))
            {
                GameObject gameObj = new GameObject();
                AudioSource audioSource = gameObj.AddComponent<AudioSource>();
                audioSource.clip = jaraxxusClip;
                audioSource.Play();
            }
        }

        public override void OnRoundStart()
        {
            base.OnRoundStart();
            var aliveFriendlies = BattleObjectManager.instance.GetAliveList(owner.faction);
            owner.RecoverHP(aliveFriendlies.Count * 2);
        }
    }

    public class PassiveAbility_EredarLordOppress_PassiveEffect : PassiveAbilityBase
    {
        int roundCount;
        public override void OnWaveStart()
        {
            base.OnWaveStart();
            roundCount = 0;
        }

        public override void OnRoundStart()
        {
            base.OnRoundStart();

            var aliveFriendlies = BattleObjectManager.instance.GetAliveList(owner.faction);
            foreach (var ally in aliveFriendlies)
            {
                if (ally != owner)
                {
                    int damage = (int)(ally.MaxHp * 0.05);
                    ally.TakeDamage(damage);
                }
            }

            if (roundCount >= 6)
            {
                foreach (var ally in aliveFriendlies)
                {
                    if (ally != owner)
                    {
                        Debug.Log($"Killing ally {ally.UnitData.unitData.name}");
                        ally.Die();
                        owner.allyCardDetail.AddNewCard(Initializer.Inferno, false);
                    }
                }
                roundCount = 0;
            }
            else
            {
                roundCount++;
            }
        }
    }

    public class PassiveAbility_EredarLordBossPhase1_PassiveEffect : PassiveAbilityBase
    {
        private readonly int PHASE2_HP_THRESHOLD = 300;
        private bool goToNextPhase;

        public override void OnWaveStart()
        {
            base.OnWaveStart();
            goToNextPhase = false;
        }

        public override void OnRoundEndTheLast()
        {
            base.OnRoundEndTheLast();
            if (goToNextPhase)
            {
                owner.passiveDetail.AddPassive(Initializer.Passive_EradarLordPhase2);
                owner.RecoverHP(owner.MaxHp);
                owner.RecoverBreakLife(1, false);
                owner.ResetBreakGauge();
                owner.breakDetail.nextTurnBreak = false;
                owner.view.ChangeHeight(400);
                owner.passiveDetail.DestroyPassive(this);
                owner.passiveDetail.OnCreated();
            }
        }

        public override bool BeforeTakeDamage(BattleUnitModel attacker, int dmg)
        {
            if (owner.hp - dmg <= PHASE2_HP_THRESHOLD)
            {
                goToNextPhase = true;
                //owner.SetHp(PHASE2_HP_THRESHOLD);
            }
            return base.BeforeTakeDamage(attacker, dmg);
        }

        public override int GetDamageReductionAll()
        {
            if (goToNextPhase)
            {
                return 9999;
            }
            return 0;
        }
    }

    public class PassiveAbility_EredarLordBossPhase2_PassiveEffect : PassiveAbilityBase
    {
        public override void OnCreated()
        {
            base.OnCreated();
            AudioClip jaraxxusSongClip;
            if (Initializer.audioClips.TryGetValue("you-face-jaraxxus-song", out jaraxxusSongClip))
            {
                SingletonBehavior<BattleSceneRoot>.Instance.currentMapObject.mapBgm = null;
                var soundManager = SingletonBehavior<BattleSoundManager>.Instance;
                Debug.Log($"Current theme: {soundManager.CurrentPlayingTheme.clip.name}, loops?: {soundManager.CurrentPlayingTheme.loop}, length: {soundManager.CurrentPlayingTheme.clip.length}, volume: {soundManager.CurrentPlayingTheme.volume}, stage type: {Singleton<StageController>.Instance.stageType}");
                if (soundManager.CurrentPlayingTheme.clip != jaraxxusSongClip)
                {
                    AudioClip[] enemyThemes = { jaraxxusSongClip };
                    soundManager.SetEnemyTheme(enemyThemes);
                    soundManager.ChangeEnemyTheme(0);
                    soundManager.CurrentPlayingTheme.loop = true;
                    Debug.Log($"New theme: {soundManager.CurrentPlayingTheme.clip.name}, loops?: {soundManager.CurrentPlayingTheme.loop}, length: {soundManager.CurrentPlayingTheme.clip.length}, volume: {soundManager.CurrentPlayingTheme.volume}, stage type: {Singleton<StageController>.Instance.stageType}");
                }
            }

            for (int i = 0; i < 5; i++)
            {
                owner.allyCardDetail.AddNewCard(Initializer.Inferno, false);
            }
            int numSummoningPortalCards = owner.allyCardDetail.GetAllDeck().Count(card => card.GetID() == Initializer.SummoningPortal);
            owner.allyCardDetail.ExhaustCard(Initializer.SummoningPortal);
            for (int i = 0; i < numSummoningPortalCards; i++)
            {
                owner.allyCardDetail.AddNewCard(Initializer.GreaterSummoningPortal, false);
            }
        }

        public override void OnWaveStart()
        {
            base.OnWaveStart();
        }

        public override void OnRoundStart()
        {
            base.OnRoundStart();

            owner.cardSlotDetail.RecoverPlayPoint(3);
            owner.allyCardDetail.DrawCards(3);
        }

        public override int SpeedDiceNumAdder()
        {
            return 2;
        }
    }
}
