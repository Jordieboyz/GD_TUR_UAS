using System.Collections.Generic;
using System.Linq;
using Controllers;
using DataStructures;
using Managers;
using Pathfinding;
using UnityEngine;

namespace AI
{
    public class Farm : State
    {
        private const float FarmTeamPercentage = 0.5f;
        private const float ExpandTeamPercentage = 0.35f;
        private const float DefendTeamPercentage = 0.1f;
        private const float AttackTeamPercentage = 0.05f;

        public override void OnEnter(PlayerController playerController)
        {
            ResetTeams();
        }

        public override void BuyTroops(PlayerController playerController)
        {
            base.BuyTroops(playerController);
        }

        protected override void DecideNextTeam(PlayerController playerController, Troop troop)
        {
            var troopCount = playerController.TroopCount;
            var farmTeamCount = FarmTeam.Sum(farmTroop => farmTroop.Key.TroopCount);
            var expandTeamCount = ExpandTeam.Sum(expandTroop => expandTroop.Key.TroopCount);
            var defendTeamCount = DefendTeam.Sum(defendTroop => defendTroop.Key.TroopCount);
            var attackTeamCount = AttackTeam.Sum(attackTroop => attackTroop.Key.TroopCount);

            if (farmTeamCount <= FarmTeamPercentage * troopCount)
            {
                FarmTeam.Add(troop, troop.Position);
            }
            else if (expandTeamCount <= ExpandTeamPercentage * troopCount)
            {
                ExpandTeam.Add(troop, troop.Position);
            }
            else if (defendTeamCount <= DefendTeamPercentage * troopCount)
            {
                DefendTeam.Add(troop, troop.Position);
            }
            else if (attackTeamCount <= AttackTeamPercentage * troopCount)
            {
                AttackTeam.Add(troop, AIUtils.GetClosestEnemy(playerController).GetBaseTile());
            }
            else
            {
                FarmTeam.Add(troop, troop.Position);
            }
        }
    }
}