using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Controllers;
using DataStructures;
using Managers;
using Pathfinding;
using UnityEngine;
using Utils;

namespace AI
{
    public abstract class State
    {
        protected readonly AIUtils AIUtils = new();
        private readonly List<Troop> _conqueringTroops = new();
        protected readonly Dictionary<Troop, Vector3Int> FarmTeam = new();
        protected readonly Dictionary<Troop, Vector3Int> ExpandTeam = new();
        protected readonly Dictionary<Troop, Vector3Int> DefendTeam = new();
        protected readonly Dictionary<Troop, Vector3Int> AttackTeam = new();

        private List<Vector3Int> _farmTargets = new();
        private List<Vector3Int> _expandTargets = new();
        private List<Vector3Int> _defendTargets = new();
        private List<Vector3Int> _attackTargets = new();
        private List<Vector3Int> _criticalTargets = new();
        private Dictionary<Troop, Vector3Int> _needsReinforcement = new();

        public virtual void OnEnter(PlayerController playerController)
        {
            ResetTeams();
        }

        public virtual void MoveTroops(PlayerController playerController)
        {
            // This is a bit buggy and not very optimized, but for now I will roll with it.
            CleanTeams(playerController.GetTroops());
            AssignIdleTroops(playerController);

            MoveFarmTeam(playerController);
            MoveExplorationTeam(playerController);
            MoveDefendTeam(playerController);
            MoveAttackTeam(playerController);
        }

        public virtual void BuyTroops(PlayerController playerController)
        {
            var newTroop = playerController.BuyTroop(1);

            if (newTroop is null || TroopAlreadyInTeam(newTroop))
            {
                return;
            }

            DecideNextTeam(playerController, newTroop);
        }

        protected virtual void DecideNextTeam(PlayerController playerController, Troop troop)
        {
        }

        protected void CleanTeams(List<Troop> existingTroops)
        {
            var existingTroopsSet = new HashSet<Troop>(existingTroops);

            _conqueringTroops.RemoveAll(troop => !existingTroopsSet.Contains(troop));

            foreach (var troop in FarmTeam.Keys.Except(existingTroopsSet).ToList())
            {
                FarmTeam.Remove(troop);
            }

            foreach (var troop in ExpandTeam.Keys.Except(existingTroopsSet).ToList())
            {
                ExpandTeam.Remove(troop);
            }

            foreach (var troop in DefendTeam.Keys.Except(existingTroopsSet).ToList())
            {
                DefendTeam.Remove(troop);
            }

            foreach (var troop in AttackTeam.Keys.Except(existingTroopsSet).ToList())
            {
                AttackTeam.Remove(troop);
            }
        }

        protected void ResetTeams()
        {
            _conqueringTroops.Clear();
            FarmTeam.Clear();
            ExpandTeam.Clear();
            DefendTeam.Clear();
            AttackTeam.Clear();
        }

        protected void AssignIdleTroops(PlayerController playerController)
        {
            var troops = playerController.GetTroops();
            foreach (var troop in troops.Where(troop => !TroopAlreadyInTeam(troop)))
            {
                if (!playerController.HasTileInTerritory(troop.Position))
                {
                    _conqueringTroops.Add(troop);
                }

                DecideNextTeam(playerController, troop);
            }
        }

        private bool TroopAlreadyInTeam(Troop troop)
        {
            return FarmTeam.ContainsKey(troop) || ExpandTeam.ContainsKey(troop) || DefendTeam.ContainsKey(troop) ||
                   AttackTeam.ContainsKey(troop);
        }

        private void MoveFarmTeam(PlayerController playerController)
        {
            if (FarmTeam.Count == 0)
            {
                return;
            }

            var woodDistance = AIUtils.GetClosestWoodToTroop(playerController);
            var ironDistance = AIUtils.GetClosestIronToTroop(playerController);
            Dictionary<Troop, Vector3Int> newFarmTargets = new();

            foreach (var troop in FarmTeam.Where(troop => !IsTroopConquering(playerController, troop.Key)))
            {
                if (troop.Value == troop.Key.Position)
                {
                    var closestWood = woodDistance[troop.Key];
                    var closestIron = ironDistance[troop.Key];
                    var closestResource = Vector3Int.Distance(troop.Key.Position, closestWood) <
                                          Vector3Int.Distance(troop.Key.Position, closestIron)
                        ? closestWood
                        : closestIron;

                    newFarmTargets.Add(troop.Key, closestResource);
                }

                MoveTroopToTarget(playerController, troop);
            }

            foreach (var target in newFarmTargets)
            {
                FarmTeam[target.Key] = target.Value;
            }
        }

        private void MoveExplorationTeam(PlayerController playerController)
        {
            if (ExpandTeam.Count == 0)
            {
                return;
            }

            foreach (var target in _expandTargets.Where(playerController.HasTileInTerritory).ToList())
            {
                _expandTargets.Remove(target);
            }

            Dictionary<Troop, Vector3Int> newTroops = new();
            Dictionary<Troop, Vector3Int> oldTroops = new();
            Dictionary<Troop, Vector3Int> newExpandTargets = new();

            foreach (var troop in ExpandTeam.Keys)
            {
                var closestUnexplored = AIUtils.GetClosestPositionNotInTerritory(playerController,
                    Static.GetTilesAsVectorList(), playerController.GetBaseTile());
                newExpandTargets.Add(troop, closestUnexplored);
                AddExpandTarget(closestUnexplored);
            }

            foreach (var troop in ExpandTeam.Where(troop => !IsTroopConquering(playerController, troop.Key)))
            {
                var troopsWithSameTarget = ExpandTeam.Where(t => t.Value == troop.Value).Select(t => t.Key).ToList();

                if (troopsWithSameTarget.Count > 1)
                {
                    var closestTroop = troopsWithSameTarget.OrderBy(t => Static.HexDistance(t.Position, troop.Value))
                        .First();

                    MoveTroopToTarget(playerController,
                        new KeyValuePair<Troop, Vector3Int>(troop.Key, closestTroop.Position));
                    return;
                }

                (Troop, Troop) newTroop = (null, null);
                if (troop.Key.TroopCount >= 6)
                {
                    var allTilesWithoutTargets = Static.GetTilesAsVectorList().Except(_expandTargets).ToList();
                    var newTarget = AIUtils.GetClosestPositionNotInTerritory(playerController, allTilesWithoutTargets,
                        playerController.GetBaseTile());
                    var nextStep = AStar.GetNextPosition(troop.Key.Position, newTarget);

                    newTroop = SplitTroop(playerController, troop.Key, nextStep, troop.Key.TroopCount / 2);

                    if (newTroop.Item2 is not null)
                    {
                        newTroops.Add(newTroop.Item2, newTarget);
                        AddExpandTarget(newTarget);
                    }
                }

                if (newTroop.Item1 is not null)
                {
                    oldTroops.Add(troop.Key, troop.Value);
                    MoveTroopToTarget(playerController,
                        new KeyValuePair<Troop, Vector3Int>(newTroop.Item1, troop.Value));
                    return;
                }

                MoveTroopToTarget(playerController, troop);
            }

            foreach (var troop in oldTroops)
            {
                ExpandTeam.Remove(troop.Key);
            }

            foreach (var troop in newTroops)
            {
                ExpandTeam.Add(troop.Key, troop.Value);
            }

            foreach (var target in newExpandTargets)
            {
                ExpandTeam[target.Key] = target.Value;
            }
        }

        private void MoveDefendTeam(PlayerController playerController)
        {
            if (DefendTeam.Count == 0)
            {
                return;
            }

            foreach (var criticalTarget in _criticalTargets.Where(criticalTarget =>
                         Static.HexDistance(playerController.GetBaseTile(), criticalTarget) > 7))
            {
                _criticalTargets.Remove(criticalTarget);
            }

            if (_criticalTargets.Count > 0)
            {
                var closestCriticalTarget = _criticalTargets.OrderBy(target =>
                    Static.HexDistance(playerController.GetBaseTile(), target)).First();

                foreach (var troop in DefendTeam)
                {
                    MoveTroopToTarget(playerController,
                        new KeyValuePair<Troop, Vector3Int>(troop.Key, closestCriticalTarget));
                }

                return;
            }

            // Remove targets that are already in territory
            foreach (var target in _defendTargets.Where(t => !playerController.HasTileInTerritory(t)).ToList())
            {
                _defendTargets.Remove(target);
            }

            var resourceTiles = WorldManager.GetAllResourceTiles();

            // Target all resources in territory
            foreach (var resourceTile in resourceTiles.Where(resourceTile =>
                         playerController.HasTileInTerritory(resourceTile) && !_defendTargets.Contains(resourceTile)))
            {
                AddDefendTarget(resourceTile);
            }

            Dictionary<Troop, Vector3Int> newDefendTargets = new();
            var resourceTileCount = playerController.GetWoodTileCount() + playerController.GetIronTileCount();

            var territoryDefenderCount = DefendTeam.Count - resourceTileCount;

            if (territoryDefenderCount <= 0 && resourceTileCount > 0)
            {
                if (DefendTeam.Any(t => t.Key.TroopCount > 1))
                {
                    foreach (var troop in DefendTeam.Where(t => t.Key.TroopCount > 1))
                    {
                        var newTroop = SplitTroop(playerController, troop.Key, playerController.GetBaseTile(),
                            troop.Key.TroopCount / 2);
                        newDefendTargets.Add(newTroop.Item1,
                            AIUtils.GetClosestPositionInTerritory(playerController, _defendTargets,
                                newTroop.Item1.Position));
                    }

                    territoryDefenderCount = DefendTeam.Count - playerController.GetWoodTileCount() -
                                             playerController.GetIronTileCount();
                }
            }

            var closestEnemyTroops = AIUtils.GetClosestEnemyTroops(playerController, territoryDefenderCount);

            if (closestEnemyTroops.Count > 0 &&
                Static.HexDistance(playerController.GetBaseTile(), closestEnemyTroops.First().Position) < 3)
            {
                _criticalTargets.Add(closestEnemyTroops.First().Position);

                foreach (var troop in DefendTeam)
                {
                    newDefendTargets.Add(troop.Key, closestEnemyTroops.First().Position);
                }
            }
            else
            {
                foreach (var troop in DefendTeam.Keys)
                {
                    if (closestEnemyTroops.Count == 0 || newDefendTargets.ContainsKey(troop))
                    {
                        continue;
                    }

                    var closestEnemy = closestEnemyTroops.OrderBy(enemy =>
                        Vector3Int.Distance(troop.Position, enemy.Position)).First();

                    if (Static.HexDistance(troop.Position, closestEnemy.Position) < 4)
                    {
                        newDefendTargets.Add(troop, closestEnemy.Position);
                        closestEnemyTroops.Remove(closestEnemy);
                        continue;
                    }

                    if (Static.HexDistance(troop.Position, closestEnemy.Position) < 6)
                    {
                        var closestTerritoryTile = AIUtils.GetClosestPositionInTerritory(playerController,
                            playerController.GetTerritory(), closestEnemy.Position);
                        newDefendTargets.Add(troop, closestTerritoryTile);
                        closestEnemyTroops.Remove(closestEnemy);
                        continue;
                    }

                    if (DefendTeam.Count > 1)
                    {
                        // only move further troop to closer to target troop
                        var closestDefendingTroop = DefendTeam.Where(t => t.Key != troop)
                            .OrderBy(t => Vector3Int.Distance(t.Key.Position, troop.Position)).First();
                        var distanceToTarget = Vector3Int.Distance(troop.Position, closestEnemy.Position);
                        if (closestDefendingTroop.Value != troop.Position)
                        {
                            var defendingTroopDistanceToTarget =
                                Vector3Int.Distance(closestDefendingTroop.Key.Position, closestEnemy.Position);
                            if (defendingTroopDistanceToTarget < distanceToTarget)
                            {
                                newDefendTargets.Add(troop, closestDefendingTroop.Key.Position);
                            }
                        }

                        continue;
                    }

                    newDefendTargets.Add(troop, AIUtils.GetClosestPositionInTerritory(playerController,
                        playerController.GetTerritory(),
                        AIUtils.GetClosestEnemyTroop(playerController, troop).Position));
                }
            }

            foreach (var target in newDefendTargets)
            {
                DefendTeam[target.Key] = target.Value;
            }

            // Defend team should defend borders and resources
            foreach (var troop in DefendTeam.Where(troop => !IsTroopConquering(playerController, troop.Key)))
            {
                MoveTroopToTarget(playerController, troop);
            }
        }

        private void MoveAttackTeam(PlayerController playerController)
        {
            if (AttackTeam.Count == 0)
            {
                return;
            }

            foreach (var target in _attackTargets.Where(t => !IsValidAttackTarget(playerController, t)).ToList())
            {
                _attackTargets.Remove(target);
            }

            Dictionary<Troop, Vector3Int> newAttackTargets = new();

            // Fix: change target if there is a closer target even if its valid
            
            foreach (var troop in AttackTeam)
            {
                if (_needsReinforcement.Count > 0)
                {
                    if (_needsReinforcement.ContainsKey(troop.Key) && _needsReinforcement.Where(t => t.Key != troop.Key)
                            .Any(t => t.Key.Position == troop.Key.Position))
                    {
                        var sameTargets = _needsReinforcement.Where(t => t.Value == troop.Value).ToList();
                        var closestSameTarget = sameTargets.OrderBy(t =>
                            Static.HexDistance(t.Key.Position, troop.Key.Position)).First();

                        if (closestSameTarget.Key != troop.Key)
                        {
                            _needsReinforcement.Remove(troop.Key);
                            newAttackTargets.Add(troop.Key, closestSameTarget.Key.Position);
                        }
                    }

                    var closestTroop = _needsReinforcement.OrderBy(t =>
                        Static.HexDistance(t.Key.Position, troop.Key.Position)).First();
                    newAttackTargets.Add(troop.Key, closestTroop.Key.Position);
                    continue;
                }

                var closestEnemy = AIUtils.GetClosestEnemy(playerController);
                var closestEnemyTroop = AIUtils.GetClosestEnemyTroop(playerController, troop.Key);

                var closestEnemyDistance =
                    Static.HexDistance(playerController.GetBaseTile(), closestEnemy.GetBaseTile());
                var closestEnemyTroopDistance = closestEnemyTroop ? Static.HexDistance(troop.Key.Position, closestEnemyTroop.Position) : int.MaxValue;

                var target = closestEnemyDistance < closestEnemyTroopDistance
                    ? closestEnemy.GetBaseTile()
                    : closestEnemyTroop.Position;

                AddAttackTarget(target);
                newAttackTargets.Add(troop.Key, target);
            }

            foreach (var target in newAttackTargets)
            {
                AttackTeam[target.Key] = target.Value;
            }

            foreach (var troop in AttackTeam.Where(troop => !IsTroopConquering(playerController, troop.Key)))
            {
                if (_attackTargets.Count == 0)
                {
                    return;
                }

                if (PlayerManager.GetPlayers().Any(player => player.GetBaseTile() == troop.Value))
                {
                    MoveTroopToTarget(playerController, troop);
                    return;
                }

                var target = Troop.Troops.FirstOrDefault(t => t.Position == troop.Value);

                if (target is null)
                {
                    MoveTroopToTarget(playerController, troop);
                    return;
                }
                
                if (target.OwnerID == playerController.Id)
                {
                    MoveTroopToTarget(playerController, troop);
                    continue;
                }

                if (_needsReinforcement.ContainsKey(troop.Key))
                {
                    if (troop.Key.TroopCount <= target.TroopCount)
                    {
                        return;
                    }

                    _needsReinforcement.Remove(troop.Key);
                    MoveTroopToTarget(playerController,
                        new KeyValuePair<Troop, Vector3Int>(troop.Key, troop.Key.Position));
                    return;
                }

                if (target.TroopCount > troop.Key.TroopCount)
                {
                    if (!_needsReinforcement.ContainsKey(troop.Key))
                    {
                        _needsReinforcement.Add(troop.Key, target.Position);
                    }
                }

                if (troop.Key.TroopCount > target.TroopCount)
                {
                    var newTroop = SplitTroop(playerController, troop.Key, playerController.GetBaseTile(), target.TroopCount + 1);
                    MoveTroopToTarget(playerController,
                        new KeyValuePair<Troop, Vector3Int>(newTroop.Item1, target.Position));
                    return;
                }

                MoveTroopToTarget(playerController, troop);
                return;
            }
        }

        private void MoveTroopToTarget(PlayerController playerController, KeyValuePair<Troop, Vector3Int> troop)
        {
            var nextPosition = AStar.GetNextPosition(troop.Key.Position, troop.Value);

            if (nextPosition == troop.Key.Position)
            {
                return;
            }

            MoveTroopToPosition(playerController, troop.Key, nextPosition);
        }

        private bool IsTroopConquering(PlayerController playerController, Troop troop)
        {
            if (!_conqueringTroops.Contains(troop))
            {
                return false;
            }

            if (!playerController.HasTileInTerritory(troop.Position))
            {
                return true;
            }

            _conqueringTroops.Remove(troop);
            return false;
        }

        private void MoveTroopToPosition(PlayerController playerController, Troop troop, Vector3Int position)
        {
            if (!playerController.HasTileInTerritory(position))
            {
                _conqueringTroops.Add(troop);
            }

            playerController.MoveTroop(position, troop.gameObject);
        }

        private (Troop, Troop) SplitTroop(PlayerController playerController, Troop troop, Vector3Int position,
            int remainingAmount)
        {
            if (troop.TroopCount <= remainingAmount)
            {
                return (troop, null);
            }
            
            var nextStep = AStar.GetNextPosition(troop.Position, position);
            var newTroop =
                playerController.SplitAndMoveTroop(troop, nextStep, troop.TroopCount - remainingAmount);

            if (newTroop.Item1 is not null && !playerController.HasTileInTerritory(newTroop.Item1.Position))
            {
                _conqueringTroops.Add(newTroop.Item1);
            }

            if (newTroop.Item2 is not null && !playerController.HasTileInTerritory(newTroop.Item2.Position))
            {
                _conqueringTroops.Add(newTroop.Item2);
            }

            return newTroop;
        }

        private void AddFarmTarget(Vector3Int target)
        {
            if (!_farmTargets.Contains(target))
            {
                _farmTargets.Add(target);
            }
        }

        private void AddExpandTarget(Vector3Int target)
        {
            if (!_expandTargets.Contains(target))
            {
                _expandTargets.Add(target);
            }
        }

        private void AddDefendTarget(Vector3Int target)
        {
            if (!_defendTargets.Contains(target))
            {
                _defendTargets.Add(target);
            }
        }

        private void AddAttackTarget(Vector3Int target)
        {
            if (!_attackTargets.Contains(target))
            {
                _attackTargets.Add(target);
            }
        }

        private bool IsValidAttackTarget(PlayerController playerController, Vector3Int target)
        {
            var isValidTroop = Troop.Troops.Any(troop => troop.Position == target);
            var isValidPlayer = PlayerManager.GetPlayers().Where(player => player.Id != playerController.Id)
                .Any(player => player.GetBaseTile() == target);

            return isValidTroop || isValidPlayer;
        }
    }
}