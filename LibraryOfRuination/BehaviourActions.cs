using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace LibraryOfRuination
{
    public class BehaviourAction_Pinball : BehaviourActionBase
    {
        static readonly int MAX_BOUNCES = 100;

        private BattleUnitModel _opponent;
        private Vector3 _knockbackDir;
        private float _speed;
        private float _distance;
        private int bounceCount;

        public override List<RencounterManager.MovingAction> GetMovingAction(ref RencounterManager.ActionAfterBehaviour self, ref RencounterManager.ActionAfterBehaviour opponent)
        {
            if (self.result == Result.Win)
            {
                bounceCount = 0;
                _self = self.view.model;
                _opponent = opponent.view.model;
                List<RencounterManager.MovingAction> list = new List<RencounterManager.MovingAction>();
                List<RencounterManager.MovingAction> infoList = opponent.infoList;
                if (infoList != null && infoList.Count > 0)
                {
                    opponent.infoList.Clear();
                }
                RencounterManager.MovingAction movingAction = new RencounterManager.MovingAction(ActionDetail.Special, CharMoveState.Custom, 0f, true, 0f, 1f);
                movingAction.customEffectRes = "FX_PC_BinahUP_ATK4";
                movingAction.SetEffectTiming(EffectTiming.PRE, EffectTiming.NONE, EffectTiming.NONE);
                movingAction.SetCustomMoving(new RencounterManager.MovingAction.MoveCustomEventWithElapsed(WaitForPillar));
                list.Add(movingAction);
                RencounterManager.MovingAction item = new RencounterManager.MovingAction(ActionDetail.Damaged, CharMoveState.Stop, 0f, true, 0f, 1f);
                opponent.infoList.Add(item);
                RencounterManager.MovingAction movingAction2 = new RencounterManager.MovingAction(ActionDetail.S3, CharMoveState.Stop, 0f, true, 1f, 1f);
                movingAction2.customEffectRes = "none";
                movingAction2.SetEffectTiming(EffectTiming.PRE, EffectTiming.POST, EffectTiming.POST);
                list.Add(movingAction2);
                RencounterManager.MovingAction movingAction3 = new RencounterManager.MovingAction(ActionDetail.Damaged, CharMoveState.Custom, 1f, true, 0.125f, 1f);
                movingAction3.SetCustomMoving(new RencounterManager.MovingAction.MoveCustomEventWithElapsed(OpponentKnockbackMoving));
                opponent.infoList.Add(movingAction3);
                _knockbackDir = (opponent.view.WorldPosition - self.view.WorldPosition).normalized;
                _knockbackDir.y += RandomUtil.RangeFloat(-1f, 1f); // for bouncy fun
                _knockbackDir.Normalize();
                return list;
            }
            return base.GetMovingAction(ref self, ref opponent);
        }

        private bool WaitForPillar(float deltaTime, float elapsedTime)
        {
            if (elapsedTime < Mathf.Epsilon)
            {
                _distance = (_opponent.view.WorldPosition - _self.view.WorldPosition).magnitude;
            }
            return elapsedTime >= 1f + _distance / 500f;
        }

        private bool OpponentKnockbackMoving(float deltaTime, float elapsedTime)
        {
            if (bounceCount >= MAX_BOUNCES)
            {
                return true;
            }
            //if (elapsedTime < Mathf.Epsilon)
            //{
            //    _speed = 100f;
            //}
            Vector3 b = _knockbackDir * deltaTime * _speed;
            //Debug.Log($"Knockback vector: {_knockbackDir}, movement vector: {b}, current position: {_opponent.view.WorldPosition}");

            _opponent.view.WorldPosition += b;

            var hexMapManagerInfo = Traverse.Create(SingletonBehavior<HexagonalMapManager>.Instance);

            Tilemap map = hexMapManagerInfo.Field("_map").GetValue<Tilemap>();
            Vector3Int cellPos = map.WorldToCell(_opponent.view.WorldPosition);
            bool bounced = false;

            int xMin = -100;
            int xMax = 100;
            int yMin = -25;
            int yMax = 25;

            //Debug.Log($"Cell position: {cellPos}, boundaries: ({xMin},{xMax}), ({yMin},{yMax})");

            //if (cellPos.x < xMin || cellPos.x > xMax)
            //{
            //    Debug.Log($"Bounce horizontal");
            //    _knockbackDir.x *= -1f;
            //    while (cellPos.x < xMin || cellPos.x > xMax)
            //    {
            //        _opponent.view.WorldPosition -= b;
            //        cellPos = map.WorldToCell(_opponent.view.WorldPosition);
            //    }
            //    bounced = true;
            //}
            //if (cellPos.y < yMin || cellPos.y > yMax)
            //{
            //    Debug.Log($"Bounce vertical");
            //    _knockbackDir.y *= -1f;
            //    while (cellPos.y < yMin || cellPos.y > yMax)
            //    {
            //        _opponent.view.WorldPosition -= b;
            //        cellPos = map.WorldToCell(_opponent.view.WorldPosition);
            //    }
            //    bounced = true;
            //}
            var worldPos = _opponent.view.WorldPosition;
            if (worldPos.x < xMin || worldPos.x > xMax)
            {
                //Debug.Log($"Bounce horizontal");
                _knockbackDir.x *= -1f;
                while (worldPos.x < xMin || worldPos.x > xMax)
                {
                    _opponent.view.WorldPosition -= b;
                    worldPos = map.WorldToCell(_opponent.view.WorldPosition);
                }
                bounced = true;
            }
            if (worldPos.y < yMin || worldPos.y > yMax)
            {
                //Debug.Log($"Bounce vertical");
                _knockbackDir.y *= -1f;
                while (worldPos.y < yMin || worldPos.y > yMax)
                {
                    _opponent.view.WorldPosition -= b;
                    worldPos = map.WorldToCell(_opponent.view.WorldPosition);
                }
                bounced = true;
            }
            if (bounced)
            {
                ++bounceCount;
            }
            _speed += deltaTime * 100;
            return false;
        }
    }

    //[HarmonyPatch(typeof(AssemblyManager), "CreateInstance_BehaviourAction")]
    //public class BehaviourAction_Everything : BehaviourActionBase
    //{
    //    static void Postfix(AssemblyManager __instance, string name, ref BehaviourActionBase __result)
    //    {
    //        if (name != "Everything")
    //        {
    //            return;
    //        }
    //        var info = Traverse.Create(__instance);
    //        var dict = info.Field("_behaviourActionDict").Field("_dict").GetValue<Dictionary<string, Type>>();
    //        var chosenName = RandomUtil.SelectOne(dict.Keys.ToList());
    //        var chosenType = dict[chosenName];
    //        var chosenInstance = Activator.CreateInstance(chosenType) as BehaviourActionBase;
    //        Debug.Log($"Replacing BehaviourActionBase {name} with {chosenName} {chosenInstance}");
    //        __result = chosenInstance;
    //    }
    //}
}
