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
    public class Attack : State
    {
        private const float ExpandTeamPercentage = 0.1f;
        private const float DefendTeamPercentage = 0.1f;
        private const float AttackTeamPercentage = 0.8f;

        public override void OnEnter(PlayerController playerController)
        {
            ResetTeams();
        }

        public override void MoveTroops(PlayerController playerController)
        {
            // DrawLines(playerController);
            
            base.MoveTroops(playerController);
        }

        public override void BuyTroops(PlayerController playerController)
        {
            base.BuyTroops(playerController);
        }

        private void DrawLines(PlayerController playerController)
        {
            var baseLocation = playerController.GetBaseTile();
            var randomEnemyBase = PlayerManager.GetPlayers().Find(player => player != playerController).GetBaseTile();

            var path = AStar.FindPath(baseLocation, randomEnemyBase);
            var tilemap = GameResources.Instance.BaseTilemap;

            for (var i = 0; i < path.Count - 1; i++)
            {
                // Convert tilemap positions to world positions
                var startWorldPosition = Static.GetHexCenter(path[i], tilemap);
                var endWorldPosition = Static.GetHexCenter(path[i + 1], tilemap);

                // Draw a line between the two world positions
                Debug.DrawLine(startWorldPosition, endWorldPosition, Color.white, 0.5f);
            }
        }

        protected override void DecideNextTeam(PlayerController playerController, Troop troop)
        {
            var troopCount = playerController.TroopCount;
            var expandTeamCount = ExpandTeam.Sum(expandTroop => expandTroop.Key.TroopCount);
            var defendTeamCount = DefendTeam.Sum(defendTroop => defendTroop.Key.TroopCount);
            var attackTeamCount = AttackTeam.Sum(attackTroop => attackTroop.Key.TroopCount);

            if (attackTeamCount <= AttackTeamPercentage * troopCount)
            {
                AttackTeam.Add(troop, AIUtils.GetClosestEnemy(playerController).GetBaseTile());
            }
            else if (defendTeamCount <= DefendTeamPercentage * troopCount)
            {
                DefendTeam.Add(troop, troop.Position);
            }
            else if (expandTeamCount <= ExpandTeamPercentage * troopCount)
            {
                ExpandTeam.Add(troop, troop.Position);
            }
            else
            {
                AttackTeam.Add(troop, AIUtils.GetClosestEnemy(playerController).GetBaseTile());
            }
        }
    }
}