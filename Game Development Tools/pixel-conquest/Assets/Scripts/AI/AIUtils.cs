using System.Collections.Generic;
using System.Linq;
using Controllers;
using DataStructures;
using Managers;
using UnityEngine;

namespace AI
{
    public class AIUtils
    {
        public Dictionary<Troop, Vector3Int> GetClosestWoodToTroop(PlayerController playerController)
        {
            return playerController.GetTroops()
                .ToDictionary(troop => troop,
                    troop => GetClosestPositionNotInTerritory(playerController, WorldManager.woodTiles,
                        troop.Position));
        }

        public Dictionary<Troop, Vector3Int> GetClosestIronToTroop(PlayerController playerController)
        {
            return playerController.GetTroops()
                .ToDictionary(troop => troop,
                    troop => GetClosestPositionNotInTerritory(playerController, WorldManager.ironTiles,
                        troop.Position));
        }

        public PlayerController GetClosestEnemy(PlayerController playerController)
        {
            return PlayerManager.GetPlayers()
                .Where(player => player != playerController)
                .Select(player => new
                {
                    Player = player,
                    Distance = Vector3Int.Distance(playerController.GetBaseTile(), player.GetBaseTile())
                })
                .OrderBy(p => p.Distance)
                .FirstOrDefault()?.Player;
        }

        public List<Troop> GetClosestEnemyTroops(PlayerController playerController, int amount = 1)
        {
            var enemyTroops = Troop.Troops
                .Where(troop => troop.OwnerID != playerController.Id)
                .ToList();

            return enemyTroops.OrderBy(troop =>
                    Vector3Int.Distance(
                        GetClosestPositionInTerritory(playerController, playerController.GetTerritory(),
                            troop.Position), troop.Position))
                .Take(amount)
                .ToList();
        }

        public Dictionary<Troop, Troop> GetClosestEnemyTroop(PlayerController playerController)
        {
            var enemy = GetClosestEnemy(playerController);
            return playerController.GetTroops()
                .ToDictionary(troop => troop, troop => GetClosestPosition(enemy.GetTroops(), troop.Position));
        }

        public Dictionary<Troop, Troop> GetClosestEnemyTroop(PlayerController playerController,
            PlayerController enemy)
        {
            return playerController.GetTroops()
                .ToDictionary(troop => troop, troop => GetClosestPosition(enemy.GetTroops(), troop.Position));
        }

        public Troop GetClosestEnemyTroop(PlayerController playerController, Troop enemyTroop)
        {
            return Troop.Troops.Where(troop => troop.OwnerID != playerController.Id)
                .OrderBy(troop => Vector3Int.Distance(enemyTroop.Position, troop.Position))
                .FirstOrDefault();
        }

        public static Vector3Int GetClosestPositionNotInTerritory(PlayerController playerController,
            List<Vector3Int> targets, Vector3Int refPos)
        {
            return targets
                .Where(t => !playerController.HasTileInTerritory(t))
                .OrderBy(t => Vector3Int.Distance(refPos, t))
                .FirstOrDefault();
        }

        public static Vector3Int GetClosestPositionInTerritory(PlayerController playerController,
            List<Vector3Int> targets, Vector3Int refPos)
        {
            return targets
                .Where(playerController.HasTileInTerritory)
                .OrderBy(t => Vector3Int.Distance(refPos, t))
                .FirstOrDefault();
        }

        private static Vector3Int GetClosestPosition(List<Vector3Int> targets, Vector3Int refPos)
        {
            return targets
                .OrderBy(t => Vector3Int.Distance(refPos, t))
                .FirstOrDefault();
        }

        private static Troop GetClosestPosition(IEnumerable<Troop> targets, Vector3Int refPos)
        {
            return targets
                .OrderBy(t => Vector3Int.Distance(refPos, t.Position))
                .FirstOrDefault();
        }
    }
}