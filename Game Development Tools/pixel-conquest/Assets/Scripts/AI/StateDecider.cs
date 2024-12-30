using System;
using System.Collections.Generic;
using System.Linq;
using Controllers;
using DataStructures;
using Enums;
using Managers;
using UnityEngine;
using Utils;

namespace AI
{
    public class StateDecider
    {
        public class DistanceComparer : IComparer<KeyValuePair<int, float>>
        {
            public int Compare(KeyValuePair<int, float> x, KeyValuePair<int, float> y)
            {
                return x.Value.CompareTo(y.Value);
            }
        }

        private AIDifficulty _difficulty = AIDifficulty.Normal;
        private List<PlayerController> _players;
        private PlayerController _player;
        private Dictionary<int, Vector3Int> _playerBases;

        private Dictionary<int, int> _playerTerritorySizes;
        private Dictionary<int, int> _playerTroopCounts;
        private Dictionary<int, int> _playerResources;
        private KeyValuePair<int, float> _nearestEnemyBaseDistance;
        private KeyValuePair<int, float> _nearestEnemyTroopDistance;

        public StateDecider(PlayerController player)
        {
            _player = player;
            
            RefreshVariables();
        }

        private void RefreshVariables()
        {
            _players = PlayerManager.GetPlayers();
            _playerBases = _players.ToDictionary(player => player.Id, player => player.GetBaseTile());
        }

        public AIStates DecideState()
        {
            RefreshVariables();
            _playerTerritorySizes = GetPlayerTerritorySizes();
            _playerTroopCounts = GetPlayerTroopCounts();
            _playerResources = GetPlayerResources();
            _nearestEnemyBaseDistance = GetNearestEnemyBaseDistance();
            _nearestEnemyTroopDistance = GetNearestEnemyTroopsDistance();

            return _difficulty switch
            {
                AIDifficulty.Easy => DecideStateEasy(),
                AIDifficulty.Normal => DecideStateNormal(),
                AIDifficulty.Hard => DecideStateHard(),
                _ => DecideStateNormal()
            };
        }
        
        private Dictionary<int, int> GetPlayerTerritorySizes()
        {
            return _players.ToDictionary(player => player.Id, player => player.GetTerritorySize());
        }

        private Dictionary<int, int> GetPlayerTroopCounts()
        {
            return _players.ToDictionary(player => player.Id,
                player => player.TroopCount.Amount);
        }

        private Dictionary<int, int> GetPlayerResources()
        {
            return _players.ToDictionary(player => player.Id,
                player => player.GetIronTileCount() + player.GetWoodTileCount());
        }

        private KeyValuePair<int, float> GetNearestEnemyBaseDistance()
        {
            var enemyBases = _playerBases.Where(pair => pair.Key != _player.Id).Select(pair => pair.Value).ToList();

            return enemyBases.Select(baseTile =>
                    new KeyValuePair<int, float>(_players.First(player => player.GetBaseTile() == baseTile).Id,
                        Static.HexDistance(_player.GetBaseTile(), baseTile)))
                .OrderBy(pair => pair, new DistanceComparer()).First();
        }

        private KeyValuePair<int, float> GetNearestEnemyTroopsDistance()
        {
            var enemyTroops = _players.Where(player => player.Id != _player.Id).SelectMany(player => player.GetTroops())
                .ToList();

            if (enemyTroops.Count == 0)
            {
                return new KeyValuePair<int, float>(-1, -1);
            }

            return enemyTroops.GroupBy(troop => _players.First(player => player.GetTroops().Contains(troop)).Id)
                .Select(group => new KeyValuePair<int, float>(group.Key,
                    group.Min(troop => Static.HexDistance(_player.GetBaseTile(), troop.Position))))
                .OrderBy(pair => pair, new DistanceComparer()).First();
        }

        private AIStates DecideStateEasy()
        {
            return AIStates.Farm;
        }

        private AIStates DecideStateNormal()
        {
            if (Mathf.Approximately(_nearestEnemyTroopDistance.Value, -1))
            {
                return AIStates.Farm;
            }

            // if enemy base or troops are nearby, attack
            if (_nearestEnemyTroopDistance.Value < 10 &&
                _player.GetIronTileCount() + _player.GetWoodTileCount() > 1)
            {
                return AIStates.Attack;
            }

            switch (_nearestEnemyTroopDistance.Value)
            {
                // if enemy base is not nearby, enemy troops are nearby and player has fewer troops than nearby enemy player, defend
                case < 6 when
                    _playerTroopCounts[_nearestEnemyTroopDistance.Key] > _player.TroopCount.Amount || _playerTroopCounts[_nearestEnemyTroopDistance.Key] == _player.TroopCount.Amount:
                    return AIStates.Defend;
                // if enemy base is not nearby, enemy troops are nearby and player has more troops than nearby enemy player, attack
                case < 8 when
                    _playerTroopCounts[_nearestEnemyTroopDistance.Key] < _player.TroopCount.Amount:
                    return AIStates.Attack;
            }

            // if player has fewer resources than average, farm
            if (!(_playerResources[_player.Id] > _playerResources.Values.Average()))
            {
                return AIStates.Farm;
            }
            
            // if player has less territory than average, expand, else attack
            return _playerTerritorySizes[_player.Id] < _playerTerritorySizes.Values.Average() ? AIStates.Expand : AIStates.Attack;
        }

        private AIStates DecideStateHard()
        {
            return AIStates.Farm;
        }
    }
}