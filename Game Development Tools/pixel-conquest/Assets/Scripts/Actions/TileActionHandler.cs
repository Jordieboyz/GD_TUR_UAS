using System.Collections.Generic;
using System.Linq;
using Controllers;
using DataStructures;
using Enums;
using Utils;
using Managers;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Actions
{
    public class TileActionHandler
    {
        private readonly PlayerController _player;
        private readonly ActionHandler _actionHandler;
        private readonly Tilemap _tilemap;
        private readonly Tilemap _selectedTileMap;
        private readonly Tile _selectedTile;
        private readonly Tile _activatedTile;

        private Vector3Int _clickedTilePosition;
        private List<Vector3Int> _activatedTiles = new();
        private bool _isTileClickedStateActive;

        public TileActionHandler(PlayerController player, ActionHandler actionHandler, Tilemap tilemap,
            Tilemap selectedTileMap,
            Tile selectedTile, Tile activatedTile)
        {
            _player = player;
            _actionHandler = actionHandler;
            _tilemap = tilemap;
            _selectedTileMap = selectedTileMap;
            _selectedTile = selectedTile;
            _activatedTile = activatedTile;
        }

        public void HandleTileClicked(Vector3Int position)
        {
            if (!_tilemap.HasTile(position))
            {
                return;
            }

            if (_isTileClickedStateActive)
            {
                if (_activatedTiles.Contains(position) && _clickedTilePosition != position)
                {
                    _actionHandler.HandleAction(ClickActions.MoveTroop, position,
                        _player.GetTroop(_clickedTilePosition));

                    DeactivateTileClickedState();
                    return;
                }

                DeactivateTileClickedState();
            }

            if (position == _player.GetBaseTile())
            {
                _actionHandler.HandleAction(ClickActions.BuyTroop, position);
            }

            _clickedTilePosition = position;
            ActivateTileClickedState(position);
        }

        private void ActivateTileClickedState(Vector3Int position)
        {
            _isTileClickedStateActive = true;
            _selectedTileMap.SetTile(position, _selectedTile);

            var troopOnTile = _player.GetTroop(position);
            
            if (troopOnTile is not null)
            {
                _actionHandler.HandleAction(ClickActions.SelectTroop, position, troopOnTile);
                var adjacentTiles = CalculateAdjacentTiles(position);

                foreach (var adjacentTile in adjacentTiles)
                {
                    _selectedTileMap.SetTile(adjacentTile, _activatedTile);
                }

                _activatedTiles = adjacentTiles;
            }

            _activatedTiles.Add(position);
        }

        private void DeactivateTileClickedState()
        {
            _isTileClickedStateActive = false;

            foreach (var tile in _activatedTiles)
            {
                _selectedTileMap.SetTile(tile, null);
            }

            _activatedTiles.Clear();
        }

        private List<Vector3Int> CalculateAdjacentTiles(Vector3Int position)
        {
            return FilterInvalidTiles(Static.GetAdjacentTiles(position));
        }

        private List<Vector3Int> FilterInvalidTiles(List<Vector3Int> tiles)
        {
            return tiles.Where(tile => _tilemap.HasTile(tile)).ToList();
        }
    }
}