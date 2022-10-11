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
    public static class Utils
    {
        public static BattleUnitModel SpawnUnit(BattleUnitModel owner, EnemyUnitClassInfo spawnUnitInfo)
        {
            var formation = GetFormation(owner);
            if (formation == null)
            {
                Debug.LogWarning($"Could not find formation for {owner.UnitData.unitData.name} of faction {owner.faction}");
                return null;
            }
            var positionList = formation.PostionList;
            int spawnUnitIndex = GetSpawnUnitIndex(owner, positionList);
            if (spawnUnitIndex < 0 || spawnUnitIndex >= positionList.Count)
            {
                return null;
            }
            foreach (var model in BattleObjectManager.instance.GetList())
            {
                Debug.Log($"Before set {model.UnitData.unitData.name} {model.UnitData.unitData.EnemyUnitId} to {model.index}");
            }
            Debug.Log($"Spawning unit info: {spawnUnitInfo.name} {spawnUnitInfo.nameId}, id: {spawnUnitInfo.id} at position index {spawnUnitIndex}");

            BattleUnitModel battleUnitModel = SpawnUnit(owner, spawnUnitInfo, spawnUnitIndex);
            if (battleUnitModel == null)
            {
                Debug.LogWarning($"Failed to spawn unit");
                return null;
            }

            Debug.Log($"Spawned {battleUnitModel.UnitData.unitData.name} at index {battleUnitModel.index}");
            battleUnitModel.SetDeadSceneBlock(false);

            PosShuffle(owner.faction);

            UpdateStage(owner.faction);
            return battleUnitModel;
        }

        public static BattleUnitModel SpawnUnitUnlimited(BattleUnitModel owner, EnemyUnitClassInfo spawnUnitInfo)
        {
            var formation = GetFormation(owner);
            if (formation == null)
            {
                Debug.LogWarning($"Could not find formation for {owner.UnitData.unitData.name} of faction {owner.faction}");
                return null;
            }
            var positionList = formation.PostionList;
            int spawnUnitIndex = GetSpawnUnitIndex(owner, positionList);
            if (spawnUnitIndex < 0 || spawnUnitIndex >= positionList.Count)
            {
                spawnUnitIndex = 0;
                BattleUnitModel unitToMove = BattleObjectManager.instance.GetUnitWithIndex(owner.faction, 0);
                unitToMove.index = positionList.Count;
            }

            Debug.Log($"Unlimited: Spawning unit info: {spawnUnitInfo.name} {spawnUnitInfo.nameId}, id: {spawnUnitInfo.id} at position index {spawnUnitIndex}");

            BattleUnitModel battleUnitModel = SpawnUnit(owner, spawnUnitInfo, spawnUnitIndex);
            if (battleUnitModel == null)
            {
                Debug.LogWarning($"Failed to spawn unit");
                return null;
            }
            battleUnitModel.SetDeadSceneBlock(false);
            battleUnitModel.formation = new FormationPosition(battleUnitModel.formation._xmlInfo);

            PosShuffle(owner.faction);

            Debug.Log($"Unlimited: Spawned {battleUnitModel.UnitData.unitData.name} at index {battleUnitModel.index}");

            int positionIndex = 0;
            foreach (BattleUnitModel unitModel in BattleObjectManager.instance.GetList(owner.faction))
            {
                if (positionIndex < positionList.Count)
                {
                    unitModel.index = positionIndex;
                    SingletonBehavior<UICharacterRenderer>.Instance.SetCharacter(unitModel.UnitData.unitData, positionIndex++, true, false);
                }
                else
                {
                    unitModel.index = positionList.Count;
                    SingletonBehavior<UICharacterRenderer>.Instance.SetCharacter(unitModel.UnitData.unitData, positionList.Count, true, false);
                }
            }

            try
            {
                BattleObjectManager.instance.InitUI();
            }
            catch (Exception e)
            {
                Debug.Log($"Spawn unlimited InitUI() exception (intentional): {e.Message}");
            }

            return battleUnitModel;
        }

        private static BattleUnitModel SpawnUnit(BattleUnitModel owner, EnemyUnitClassInfo spawnUnitInfo, int index)
        {
            var stageController = Singleton<StageController>.Instance;
            if (owner.faction == Faction.Enemy)
            {
                return stageController.AddNewUnit(owner.faction, spawnUnitInfo.id, index);
            }
            else if (owner.faction == Faction.Player)
            {
                return AddNewPlayerUnit(owner, spawnUnitInfo.id, index);
            }
            Debug.LogWarning($"{owner.UnitData.unitData.name} has unknown faction: {owner.faction}");
            return null;
        }

        private static BattleUnitModel AddNewPlayerUnit(BattleUnitModel owner, LorId enemyUnitId, int index, int height = -1)
        {
            var faction = owner.faction;
            var stageController = Singleton<StageController>.Instance;

            UnitBattleDataModel unitBattleData = UnitBattleDataModel.CreateUnitBattleDataByEnemyUnitId(stageController.GetStageModel(), enemyUnitId);
            if (height != -1)
            {
                unitBattleData.unitData.customizeData.height = height;
            }
            BattleObjectManager.instance.UnregisterUnitByIndex(faction, index);

            StageLibraryFloorModel floor = stageController.GetCurrentStageFloorModel();
            UnitDataModel unitData = unitBattleData.unitData;
            Traverse.Create(unitData).Field("_ownerSephirah").SetValue(owner.UnitData.unitData.OwnerSephirah);

            BattleUnitModel battleUnitModel = BattleObjectManager.CreateDefaultUnit(Faction.Player);
            battleUnitModel.index = index;
            battleUnitModel.formation = floor.GetFormationPosition(battleUnitModel.index);
            if (unitBattleData.isDead)
            {
                return battleUnitModel;
            }
            battleUnitModel.grade = unitData.grade;
            battleUnitModel.SetUnitData(unitBattleData);
            battleUnitModel.OnCreated();
            var librarianTeam = Traverse.Create(stageController).Field("_librarianTeam").GetValue() as BattleTeamModel;
            librarianTeam.AddUnit(battleUnitModel);
            BattleObjectManager.instance.RegisterUnit(battleUnitModel);
            battleUnitModel.passiveDetail.OnUnitCreated();
            return battleUnitModel;
        }

        private static int GetSpawnUnitIndex(BattleUnitModel owner, List<FormationPosition> positionList)
        {
            Debug.Log($"Formation position list length: {positionList.Count}");

            for (int i = 0; i < positionList.Count; i++)
            {
                var ally = BattleObjectManager.instance.GetUnitWithIndex(owner.faction, i);
                if (ally != null)
                {
                    if (ally != owner && ally.IsDead())
                    {
                        return i;
                    }
                    Debug.Log($"Ally {ally.UnitData.unitData.name} at index {i}={ally.index}");
                }
                else
                {
                    Debug.Log($"No ally at index {i}");
                    return i;
                }
            }
            return -1;
        }

        private static FormationModel GetFormation(BattleUnitModel unit)
        {
            var stageController = Singleton<StageController>.Instance;
            if (unit.faction == Faction.Enemy)
            {
                return stageController.GetCurrentWaveModel().GetFormation();
            }
            if (unit.faction == Faction.Player)
            {
                return stageController.GetCurrentStageFloorModel().GetFormation();
            }
            Debug.LogWarning($"{unit.UnitData.unitData.name} has unknown faction: {unit.faction}");
            return null;
        }

        private static void UpdateStage(Faction faction)
        {
            int positionIndex = 0;
            foreach (BattleUnitModel battleUnitModel in BattleObjectManager.instance.GetList(faction))
            {
                Debug.Log($"Set {battleUnitModel.UnitData.unitData.name} {battleUnitModel.id} {battleUnitModel.UnitData.unitData.EnemyUnitId} to {battleUnitModel.index} / {positionIndex}");
                SingletonBehavior<UICharacterRenderer>.Instance.SetCharacter(battleUnitModel.UnitData.unitData, positionIndex++, true, false);
            }
            try
            {
                BattleObjectManager.instance.InitUI();
            }
            catch (Exception e)
            {
                Debug.Log($"InitUI() exception (intentional): {e.Message}");
            }
        }

        private static void PosShuffle(Faction faction)
        {
            // Debug.LogError("Finall: PosShuffle: Starting");
            var unitList = BattleObjectManager.instance.GetAliveList(faction);
            int maxPoints = unitList.Count;
            if (false)
            {
                Debug.Log("Finall: PosShuffle: Using Scattermode");
                int current = 0;
                int loopCounter = 0;
                int maxIterations = 65536 * maxPoints;
                var minClosestDistance = 16;
                int[] x = new int[maxPoints];
                int[] y = new int[maxPoints];
                while (current < maxPoints && loopCounter < maxIterations)
                {
                    int xPossible = RandomUtil.Range(1, 26);
                    int yPossible = RandomUtil.Range(-12, 12);
                    if (current == 0)
                    {
                        x[current] = xPossible;
                        y[current] = yPossible;
                        current++;
                        continue;
                    }
                    // float[] result1 = new float[current];
                    // float[] result2 = new float[current];
                    float[] distances = new float[current];
                    for (int i = 0; i < current; i++)
                    {
                        distances[i] = Mathf.Sqrt(Mathf.Pow(x[i] - xPossible, 2) + Mathf.Pow(y[i] - yPossible, 2));
                    }
                    // Debug.LogError("Finall: PosShuffle: "+current+"-min distance: "+distances.Min());
                    if (distances.Min() >= minClosestDistance)
                    {
                        x[current] = xPossible;
                        y[current] = yPossible;
                        current++;
                    }
                    loopCounter++;
                    if (new[] { 8192, 16384, 32768 }.Contains(loopCounter))
                    {
                        minClosestDistance /= 2;
                        Debug.Log(current + ": Too many loops, dropping max distance to " + minClosestDistance);
                    }
                }
                Debug.Log("Finall: PosShuffle: Found " + current + " points in " + loopCounter + " tries");
                if (current != maxPoints)
                {
                    Debug.Log("Finall: PosShuffle: Filling in " + (maxPoints - current) + " out of " + maxPoints + " entries");
                    while (current < maxPoints)
                    {
                        x[current] = RandomUtil.Range(1, 26);
                        y[current] = RandomUtil.Range(-12, 12);
                        current++;
                    }
                }
                current = 0;
                foreach (BattleUnitModel battleUnitModel in unitList)
                {
                    var newPos = new Vector2Int(x[current], y[current]);
                    battleUnitModel.formation.ChangePos(newPos);
                    // Debug.LogError(current+": "+newPos);
                    current++;
                }
            }
            else
            {
                Debug.Log("Finall: PosShuffle: Using Gridmode");
                if (maxPoints <= 1)
                {
                    foreach (BattleUnitModel battleUnitModel in unitList)
                    {
                        battleUnitModel.formation.ChangePos(new Vector2Int(11, 0));
                    }
                    return;
                }
                // Debug.LogError(maxPoints);
                float x = 1;
                float y;
                float incrementx = 24 / (Mathf.Sqrt(maxPoints));
                float incrementy;
                if (maxPoints == 2)
                {
                    y = 0;
                    incrementy = 0;
                }
                else
                {
                    y = 12;
                    incrementy = 24 / (Mathf.Sqrt(maxPoints) - 1);
                }
                // Debug.LogError(incrementx);
                // Debug.LogError(incrementy);
                Vector2Int[] newPos = new Vector2Int[maxPoints];
                int i;
                bool stepping = false;
                var incrementHalf = (incrementx / 2);
                for (i = 0; i < maxPoints; i++)
                {
                    //	Debug.LogError("x-"+i+": "+(x));
                    //	Debug.LogError("x-int"+i+": "+((int)x+1));
                    //	Debug.LogError("y-"+i+": "+(y));
                    //	Debug.LogError("y-int"+i+": "+((int)y+12));
                    //	Debug.LogError("");
                    if (stepping)
                    {
                        newPos[i] = new Vector2Int((int)(x + incrementHalf), (int)y);
                    }
                    else
                    {
                        newPos[i] = new Vector2Int((int)x, (int)y);
                    }
                    x += incrementx;
                    if (x >= 25)
                    {
                        x = 1;
                        if (stepping)
                        {
                            stepping = false;
                        }
                        else
                        {
                            stepping = true;
                        }
                        y -= incrementy;
                    }
                }
                i = 0;
                foreach (BattleUnitModel battleUnitModel in unitList)
                {
                    battleUnitModel.formation.ChangePos(newPos[i]);
                    i++;
                }
                Debug.Log("Finall: PosShuffle: Arranged " + maxPoints + " characters");
            }
        }
    }
}
