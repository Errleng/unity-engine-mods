using HarmonyLib;
using LOR_DiceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LibraryOfRuination
{
    public class DiceCardSelfAbility_HearthstoneMage_CardEffect : DiceCardSelfAbilityBase
    {
        public static string Desc = "[On Use] Remove all dice on this page. For each character, add the dice of a random combat page in their hand to this page.";

        public override void OnUseCard()
        {
            base.OnUseCard();
            card.RemoveAllDice();
            foreach (var character in BattleObjectManager.instance.GetAliveList())
            {
                var randomCardInHand = character.allyCardDetail.GetRandomCardInHand();
                if (randomCardInHand != null)
                {
                    foreach (var battleDiceBehavior in randomCardInHand.CreateDiceCardBehaviorList())
                    {
                        card.AddDice(battleDiceBehavior);
                    }
                }
            }
        }
    }

    public class DiceCardSelfAbility_RuneOfTheWhiteMage_CardEffect : DiceCardSelfAbilityBase
    {
        public static string Desc = "[On Use] Remove all dice on this page. For each point of light, add a random dice to this page.";

        public override void OnUseCard()
        {
            base.OnUseCard();
            card.RemoveAllDice();
            var cardInfoTable = Traverse.Create(ItemXmlDataList.instance).Field("_cardInfoTable").GetValue<Dictionary<LorId, DiceCardXmlInfo>>();
            var keys = cardInfoTable.Keys.ToList();
            if (keys.Count == 0)
            {
                Debug.LogWarning($"Card info table has no keys!");
                return;
            }
            for (int i = 0; i < owner.cardSlotDetail.PlayPoint; i++)
            {

                var randomCardId = RandomUtil.SelectOne(keys);
                var randomCardInfo = cardInfoTable[randomCardId];
                var randomCard = BattleDiceCardModel.CreatePlayingCard(randomCardInfo);
                var diceList = randomCard.CreateDiceCardBehaviorList();
                if (diceList.Count == 0)
                {
                    Debug.LogWarning($"{randomCard.GetName()} has no dice!");
                    --i;
                    continue;
                }
                var randomDice = RandomUtil.SelectOne(diceList);
                card.AddDice(randomDice);
                Debug.Log($"Added dice from {randomCard.GetName()}: {randomDice.GetDiceMin()}~{randomDice.GetDiceMax()}");
            }
        }
    }

    public class PassiveAbility_Hearthstone_PassiveEffect : PassiveAbilityBase
    {
        private bool boostedThisScene;

        public override void OnStartBattle()
        {
            base.OnStartBattle();
            boostedThisScene = false;
        }

        public override void OnUseCard(BattlePlayingCardDataInUnitModel curCard)
        {
            base.OnUseCard(curCard);
            BattleDiceCardModel randomCardInHand = owner.allyCardDetail.GetRandomCardInHand();
            if (randomCardInHand == null || boostedThisScene)
            {
                return;
            }
            boostedThisScene = true;
            foreach (var battleDiceBehavior in randomCardInHand.CreateDiceCardBehaviorList())
            {
                if (battleDiceBehavior.Type != BehaviourType.Standby)
                {
                    curCard.AddDice(battleDiceBehavior);
                }
            }
        }
    }
}
