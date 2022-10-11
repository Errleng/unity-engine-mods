using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfRuination
{
    public class DiceCardAbility_WillOfTheJavascript_DiceEffect : DiceCardAbilityBase
    {
        public static string Desc = "[On Hit] Deal \"(0-1)\" + \"(0-9)\" damage";

        public override void OnSucceedAttack(BattleUnitModel target)
        {
            int tens = RandomUtil.Range(0, 1);
            int ones = RandomUtil.Range(0, 9);
            int damage = int.Parse($"{tens}{ones}");
            target.TakeDamage(damage);
        }
    }

    public class DiceCardSelfAbility_WillOfTheJavascript_CardEffect : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "OnlyOne_Keyword", "DrawCard_Keyword" };
        public static string Desc = "[On Use] If Singleton, draw the rest of the deck";

        public override void OnUseCard()
        {
            base.OnUseCard();
            if (owner.allyCardDetail.IsHighlander())
            {
                owner.allyCardDetail.DrawCards(owner.allyCardDetail.GetDeck().Count);
            }
        }
    }

    public class PassiveAbility_Javascript_PassiveEffect : PassiveAbilityBase
    {
        public override void OnRoundStart()
        {
            base.OnRoundStart();

            foreach (var card in owner.allyCardDetail.GetHand())
            {
                if (card.GetOriginCost() <= 4)
                {
                    card.SetCurrentCost(RandomUtil.Range(0, 9));
                }
            }
            owner.cardSlotDetail.RecoverPlayPoint(owner.cardSlotDetail.PlayPoint);
        }
    }
}
