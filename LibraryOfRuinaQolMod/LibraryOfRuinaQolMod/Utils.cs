using LOR_DiceSystem;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LibraryOfRuinaQolMod
{
    internal class Utils
    {
        static readonly BattleUnitView cachedView = UnityEngine.Object.Instantiate(BattleObjectLayer.instance.battleUnitPrefab);

        public static bool InCallStack(string methodName)
        {
            var trace = new StackTrace(true);
            foreach (var frame in trace.GetFrames())
            {
                var method = frame.GetMethod();
                if (method.Name == methodName)
                {
                    return true;
                }
                Plugin.logger.LogDebug($"InCallStack method: {method.Name} != {methodName}");
            }
            return false;
        }

        public static UnitDataModel Clone(UnitDataModel orig)
        {
            if (orig == null)
            {
                Plugin.logger.LogDebug($"Cloning UnitBattleDataModel: original is null");
                return null;
            }
            var clone = new UnitDataModel(orig.defaultBook.BookId, orig.OwnerSephirah, orig.isSephirah);
            if (orig.isSephirah)
            {
                clone.isSephirah = true;
                clone.SetCustomName(Singleton<CharactersNameXmlList>.Instance.GetName((int)orig.OwnerSephirah));
                switch (orig.OwnerSephirah)
                {
                    case SephirahType.Malkuth:
                        clone.giftInventory.AddGift(2);
                        break;
                    case SephirahType.Yesod:
                        clone.giftInventory.AddGift(3);
                        break;
                    case SephirahType.Hod:
                        clone.giftInventory.AddGift(4);
                        break;
                    case SephirahType.Netzach:
                        clone.giftInventory.AddGift(5);
                        break;
                    case SephirahType.Tiphereth:
                        clone.giftInventory.AddGift(190);
                        break;
                    case SephirahType.Gebura:
                        clone.giftInventory.AddGift(191);
                        break;
                    case SephirahType.Chesed:
                        clone.giftInventory.AddGift(192);
                        break;
                    case SephirahType.Binah:
                        clone.giftInventory.AddGift(193);
                        break;
                    case SephirahType.Hokma:
                        clone.giftInventory.AddGift(194);
                        break;
                    case SephirahType.Keter:
                        clone.giftInventory.AddGift(1);
                        break;
                }
            }
            else
            {
                clone.SetNameId(Singleton<LibrariansNameXmlList>.Instance.GetRandomNameID());
            }
            clone.giftInventory.AddGift(6);
            return clone;
        }

        public static UnitBattleDataModel Clone(UnitBattleDataModel orig)
        {
            if (orig == null)
            {
                Plugin.logger.LogDebug($"Cloning UnitBattleDataModel: original is null");
                return null;
            }
            var clone = new UnitBattleDataModel(Singleton<StageController>.Instance.GetStageModel(), Clone(orig.unitData));
            clone.Init();
            return clone;
        }

        public static BattleUnitModel Clone(BattleUnitModel orig)
        {
            if (orig == null)
            {
                Plugin.logger.LogDebug($"Cloning BattleUnitModel: original is null");
                return null;
            }
            var clone = BattleObjectManager.CreateDefaultUnit(orig.faction);
            // maybe need to instantiate new UnitBattleDataModel (following AddUnitDefault)
            clone.SetUnitData(orig.UnitData);
            //clone.OnWaveStart(); // perhaps not needed

            // copy passives
            //clone.passiveDetail = new BattleUnitPassiveDetail(clone);
            //foreach (var origPassive in orig.passiveDetail.PassiveList)
            //{
            //    var passiveCopy = Activator.CreateInstance(origPassive.GetType()) as PassiveAbilityBase;
            //    passiveCopy.Init(clone);
            //    if (passiveCopy.SpeedDiceNumAdder() == 0 && passiveCopy.SpeedDiceBreakedAdder() == 0)
            //    {
            //        passiveCopy.rare = origPassive.rare;
            //        clone.passiveDetail.AddPassive(passiveCopy);
            //    }
            //}

            // copy cards being played
            //clone.cardSlotDetail = new BattlePlayingCardSlotDetail(this);
            //foreach(var cardSlot in orig.cardSlotDetail.)
            //{

            //}

            // copy buffs

            // copy deck
            //clone.allyCardDetail = orig.bufListDetail;

            clone.OnCreated();

            clone.Book.SetOriginalResists();
            //BattleUnitView battleUnitView = cachedView;
            //clone.view = battleUnitView;
            //battleUnitView.model = clone;
            //battleUnitView.CreateSkin();
            //battleUnitView.model.UpdateDirection(Vector3.zero);
            //battleUnitView.transform.SetParent(BattleObjectLayer.instance.transform, false);
            //battleUnitView.transform.localPosition = SingletonBehavior<HexagonalMapManager>.Instance.CellToWorldPos(orig.formationCellPos + SingletonBehavior<HexagonalMapManager>.Instance.CenterCell);
            //battleUnitView.gameObject.SetActive(/*true*/ false);
            //battleUnitView.abCardSelector.selectable.parentSelectable = BattleObjectLayer.instance.selectablePanel;

            clone.passiveDetail.OnUnitCreated();

            clone.speedDiceResult = orig.speedDiceResult.Select((x) =>
            {
                var newSpeedDice = new SpeedDice();
                newSpeedDice.min = x.min;
                newSpeedDice.value = x.value;
                newSpeedDice.faces = x.faces;
                newSpeedDice.breaked = x.breaked;
                newSpeedDice.isControlable = x.isControlable;
                return newSpeedDice;
            }).ToList();

            return clone;
        }

        public static BattleDiceCardModel Clone(BattleDiceCardModel orig)
        {
            if (orig == null)
            {
                Plugin.logger.LogDebug($"Cloning BattleDiceCardModel: original is null");
                return null;
            }
            var cardItem = ItemXmlDataList.instance.GetCardItem(orig.GetID(), false);
            if (cardItem == null)
            {
                Plugin.logger.LogDebug($"Cloning BattleDiceCardModel: cardItem is null");
                return null;
            }
            var clone = BattleDiceCardModel.CreatePlayingCard(cardItem);
            return clone;
        }

        public static BattlePlayingCardDataInUnitModel Clone(BattlePlayingCardDataInUnitModel orig)
        {
            if (orig == null)
            {
                Plugin.logger.LogDebug($"Cloning BattlePlayingCardDataInUnitModel: original is null");
                return null;
            }

            var clone = new BattlePlayingCardDataInUnitModel();
            clone.owner = Clone(orig.owner);

            clone.card = Clone(orig.card);
            if (clone.card != null)
            {
                clone.card.owner = clone.owner;
            }

            clone.target = Clone(orig.target);
            clone.subTargets = new List<BattlePlayingCardDataInUnitModel.SubTarget>();
            foreach (var subTarget in orig.subTargets)
            {
                var clonedSubTarget = new BattlePlayingCardDataInUnitModel.SubTarget();
                clonedSubTarget.target = Clone(subTarget.target);
                clonedSubTarget.targetSlotOrder = subTarget.targetSlotOrder;
                clone.subTargets.Add(clonedSubTarget);
            }
            clone.earlyTarget = Clone(orig.earlyTarget);
            clone.earlyTargetOrder = orig.earlyTargetOrder;

            if (orig.card != null)
            {
                clone.cardAbility = orig.card.CreateDiceCardSelfAbilityScript();
                if (clone.cardAbility != null)
                {
                    clone.cardAbility.card = clone;
                    clone.cardAbility.OnApplyCard();
                }
            }
            else
            {
                Plugin.logger.LogWarning($"Clone: {orig} card is null!");
            }

            clone.ResetCardQueue();

            // these are just primitives so it should be safe to assign
            clone.speedDiceResultValue = orig.speedDiceResultValue;
            clone.slotOrder = orig.slotOrder;
            clone.targetSlotOrder = orig.targetSlotOrder;

            return clone;
        }
    }
}
