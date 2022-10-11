using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LibraryOfRuination
{
    internal class FriendsCards
    {
        public class DiceCardSelfAbility_DragonScaling_CardEffect : DiceCardSelfAbilityBase
        {
            public override string[] Keywords => new string[] { "bstart_Keyword", "Strength_Keyword" };
            public static string Desc = "[Combat Start] All allies gain 1 Strength every scene; the page loses this ability afterwards";

            public override void OnStartBattle()
            {
                base.OnStartBattle();
                foreach (BattleUnitModel battleUnitModel in BattleObjectManager.instance.GetAliveList(base.owner.faction))
                {
                    BattleUnitBuf battleUnitBuf = battleUnitModel.bufListDetail.GetActivatedBufList().Find((BattleUnitBuf y) => y is BattleUnitBuf_DragonScale);
                    if (battleUnitBuf != null)
                    {
                        ++battleUnitBuf.stack;
                    }
                    else
                    {
                        battleUnitModel.bufListDetail.AddBuf(new BattleUnitBuf_DragonScale());
                    }
                }
                card.card.XmlData.Script = "";
            }
        }

        public class DiceCardSelfAbility_Onslaught_CardEffect : DiceCardSelfAbilityBase
        {
            public override string[] Keywords => new string[] { "Strength_Keyword" };
            public static string Desc = "[On Play] All allies gain 3 strength this scene. All enemies except the target become untargetable this scene.";

            public override void OnUseInstance(BattleUnitModel unit, BattleDiceCardModel self, BattleUnitModel targetUnit)
            {
                var allies = BattleObjectManager.instance.GetAliveList(Faction.Player);
                foreach (var ally in allies)
                {
                    ally.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.Strength, 3, ally);
                    ally.bufListDetail.AddBuf(new BattleUnitBuf_FixedTarget(targetUnit, true));
                }
                SingletonBehavior<BattleManagerUI>.Instance.ui_unitListInfoSummary.UpdateCharacterProfileAll();
                base.OnUseInstance(unit, self, targetUnit);
            }
        }
    }
}
