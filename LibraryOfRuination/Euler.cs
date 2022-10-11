using HarmonyLib;
using LOR_DiceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LibraryOfRuination
{
    public class DiceCardAbility_Euler_DiceEffect : DiceCardAbilityBase
    {
        public override string[] Keywords => new string[] { "Energy_Keyword", "DrawCard_Keyword" };

        public static string Desc = "[On Clash]  If the opposing die rolls a prime number, restore 1 Light and draw 1 page.";

        private bool IsPrime(int num)
        {
            for (int i = 2; i < num; i++)
            {
                if (num % i == 0)
                {
                    return false;
                }
            }
            return true;
        }

        private void OnClash()
        {
            if (IsPrime(behavior.TargetDice.DiceResultValue))
            {
                owner.cardSlotDetail.RecoverPlayPoint(1);
                owner.allyCardDetail.DrawCards(1);
            }
        }

        public override void OnDrawParrying()
        {
            base.OnDrawParrying();
            OnClash();
        }

        public override void OnLoseParrying()
        {
            base.OnLoseParrying();
            OnClash();
        }

        public override void OnWinParrying()
        {
            base.OnWinParrying();
            OnClash();
        }
    }

    public class PassiveAbility_Euler_PassiveEffect : PassiveAbilityBase
    {
        private readonly string theValueOfE = "27182818284590452353602874713526624977572470936999595749669676277240766303535475945713821785251664274";
        private int eIndex;

        public override void OnWaveStart()
        {
            base.OnWaveStart();
            eIndex = 0;
        }

        public override void OnRollDice(BattleDiceBehavior behavior)
        {
            base.OnRollDice(behavior);
            if (eIndex >= theValueOfE.Length)
            {
                eIndex = 0;
            }
            Traverse.Create(behavior).Field("_diceFinalResultValue").SetValue((int)char.GetNumericValue(theValueOfE[eIndex]));
            Debug.Log($"The {eIndex}th digit of E is {behavior.DiceResultValue}");
            ++eIndex;
        }
    }
}
