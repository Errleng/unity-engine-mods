using System;
using System.Collections.Generic;
using UnityEngine;

namespace LibraryOfRuination
{
    public class BattleUnitBuf_SummonCharacterLimited : BattleUnitBuf
    {
        private readonly EnemyUnitClassInfo spawnUnitInfo;
        private BattleUnitModel owner;

        public BattleUnitBuf_SummonCharacterLimited(EnemyUnitClassInfo unitInfo)
        {
            spawnUnitInfo = unitInfo;
        }

        protected override string keywordId
        {
            get
            {
                return "Summon_Character_Limited";
            }
        }

        protected override string keywordIconId
        {
            get
            {
                return "Wolf_Claw";
            }
        }

        public override void Init(BattleUnitModel owner)
        {
            base.Init(owner);
            this.owner = owner;
            stack = 0;
        }

        public override void OnRoundEnd()
        {
            base.OnRoundEndTheLast();
            Utils.SpawnUnit(owner, spawnUnitInfo);
            Destroy();
            Debug.Log($"Spawn round end last: {owner.UnitData.unitData.name} {IsDestroyed()}");
        }
    }

    public class BattleUnitBuf_SummonCharacterUnlimited : BattleUnitBuf
    {
        private readonly EnemyUnitClassInfo spawnUnitInfo;
        private BattleUnitModel owner;

        public BattleUnitBuf_SummonCharacterUnlimited(EnemyUnitClassInfo unitInfo)
        {
            spawnUnitInfo = unitInfo;
        }

        protected override string keywordId
        {
            get
            {
                return "Summon_Character_Unlimited";
            }
        }

        protected override string keywordIconId
        {
            get
            {
                return "BlackSilenceSpecialCard";
            }
        }

        public override void Init(BattleUnitModel owner)
        {
            base.Init(owner);
            this.owner = owner;
            stack = 0;
        }

        public override void OnRoundEnd()
        {
            base.OnRoundEnd();
            Utils.SpawnUnitUnlimited(owner, spawnUnitInfo);
            Destroy();
            Debug.Log($"Unlimited spawn round end last: {owner.UnitData.unitData.name} {IsDestroyed()}");
        }
    }

    public class BattleUnitBuf_DragonScale : BattleUnitBuf
    {
        public override void OnRoundStart()
        {
            base.OnRoundStart();
            if (stack > 0)
            {
                _owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Strength, stack, _owner);
            }
        }
    }

    public class BattleUnitBuf_FixedTarget : BattleUnitBuf
    {
        private bool active;
        private readonly BattleUnitModel target;

        protected override string keywordId
        {
            get
            {
                return "BigBird_Eye";
            }
        }

        public BattleUnitBuf_FixedTarget(BattleUnitModel fixedTarget, bool activateImmediately)
        {
            target = fixedTarget;
            active = activateImmediately;
            stack = 1;
        }

        public override List<BattleUnitModel> GetFixedTarget()
        {
            List<BattleUnitModel> list = new List<BattleUnitModel>();
            if (active && target != null)
            {
                list.Add(target);
            }
            return list;
        }

        public override void OnRoundStart()
        {
            base.OnRoundStart();
            active = true;
        }

        public override void OnRoundEndTheLast()
        {
            base.OnRoundEndTheLast();
            if (!active)
            {
                return;
            }
            if (stack > 1)
            {
                --stack;
            }
            else
            {
                Destroy();
            }
        }
    }
}
