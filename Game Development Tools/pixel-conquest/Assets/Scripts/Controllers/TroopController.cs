using System.Collections.Generic;
using System.Linq;
using DataStructures;
using Managers;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using Utils;
using static Utils.Static;

namespace Controllers
{
    public class TroopController : MonoBehaviour
    {
        public static readonly Price TroopPrice = new Price(6, 3);
        private PlayerController _player;
        private Tilemap _tilemap;
        private GameObject _troopPrefab;

        private void Start()
        {
            _player = gameObject.GetComponent<PlayerController>();
            _tilemap = Assets.BaseTilemap;
            _troopPrefab = Assets.TroopPrefab;
        }

        public (Troop, Troop) SplitAndMoveTroop(Troop troop, Vector3Int newPosition, int splittingAmout)
        {
            troop.RemoveTroops(splittingAmout);

            var newTroop = CreateNewTroop(troop.Position, splittingAmout);
            newTroop = MoveTroop(newPosition, newTroop.gameObject);

            return (troop, newTroop);
        }

        public Troop MoveTroop(Vector3Int position, GameObject troop)
        {
            var troopComponent = troop.GetComponent<Troop>();

            // Check if there is an enemy troop in the tile
            if (EnemyInTile(position) != null)
            {
                var enemyPlayer = EnemyInTile(position).Keys.First();
                var enemyTroop = EnemyInTile(position).Values.First();

                // Check if enemy troop has more troops than player troop
                if (enemyTroop.TroopCount > troopComponent.TroopCount)
                {
                    // If so, remove player troop
                    enemyTroop.RemoveTroops(troopComponent.TroopCount);
                    _player.RemoveTroop(troopComponent);
                    Destroy(troop);
                    return null;
                }

                // Check if both troops have the same amount of troops
                if (enemyTroop.TroopCount == troopComponent.TroopCount)
                {
                    // If so, remove both troops
                    enemyPlayer.RemoveTroop(enemyTroop);
                    _player.RemoveTroop(troopComponent);
                    Destroy(enemyTroop.gameObject);
                    Destroy(troop);
                    return null;
                }

                // If player troop has more troops than enemy troop, move player troop to enemy tile and remove enemy troop
                troopComponent.transform.position = _tilemap.GetCellCenterWorld(position);
                troopComponent.Position = position;
                troopComponent.RemoveTroops(enemyTroop.TroopCount);
                enemyPlayer.RemoveTroop(enemyTroop);
                Destroy(enemyTroop.gameObject);
                return troopComponent;
            }

            // Check if player has a troop in the tile
            if (!_player.GetTroop(position))
            {
                // If not, move troop to tile
                troopComponent.transform.position = _tilemap.GetCellCenterWorld(position);
                troopComponent.Position = position;
                return troopComponent;
            }

            // Add troops to player troop in tile
            var playerTroop = _player.GetTroop(position);
            playerTroop.AddTroops(troopComponent.TroopCount);
            _player.RemoveTroop(troopComponent, false);
            Destroy(troop);
            return playerTroop;
        }

        public Troop BuyTroop(int amount)
        {
            var ironPrice = TroopPrice.Iron * amount;
            var woodPrice = TroopPrice.Wood * amount;

            // Check if player has enough resources
            if (!_player.Iron.HasEnough(ironPrice) ||
                !_player.Wood.HasEnough(woodPrice))
            {
                return null;
            }

            _player.RemoveIron(ironPrice);
            _player.RemoveWood(woodPrice);

            var troopAlreadyOnBase = Troop.Troops.Find(troop => troop.Position == _player.GetBaseTile());

            if (troopAlreadyOnBase is null)
            {
                return CreateNewTroop(_player.GetBaseTile(), amount);
            }
            
            if (troopAlreadyOnBase.OwnerID == _player.Id)
            {
                troopAlreadyOnBase.AddTroops(amount);
                return troopAlreadyOnBase;
            }

            if (troopAlreadyOnBase.TroopCount > amount)
            {
                troopAlreadyOnBase.RemoveTroops(amount);
                return null;
            }
                
            amount -= troopAlreadyOnBase.TroopCount;
            var enemyPlayer = PlayerManager.GetPlayer(troopAlreadyOnBase.OwnerID);
            enemyPlayer.RemoveTroop(troopAlreadyOnBase);
            Destroy(troopAlreadyOnBase.gameObject);

            return CreateNewTroop(_player.GetBaseTile(), amount);
        }

        private Dictionary<PlayerController, Troop> EnemyInTile(Vector3Int position)
        {
            // Check if there is an enemy troop in the tile
            return (from troops in Troop.Troops
                where troops.Position == position
                where troops.OwnerID != _player.Id
                select new Dictionary<PlayerController, Troop>
                {
                    { PlayerManager.GetPlayer(troops.OwnerID), troops }
                }).FirstOrDefault();
        }

        private Troop CreateNewTroop(Vector3Int position, int amount)
        {
            var newTroop = Instantiate(_troopPrefab, _tilemap.GetCellCenterWorld(position),
                Quaternion.identity);
            newTroop.GetComponent<MergeSprite>().troopColor = Static.ConvertColor(_player.Color);
            var troopComponent = newTroop.GetComponent<Troop>();
            troopComponent.OwnerID = _player.Id;
            troopComponent.TroopCount = amount;
            troopComponent.Position = position;
            _player.AddTroop(troopComponent);
            return troopComponent;
        }
    }
}