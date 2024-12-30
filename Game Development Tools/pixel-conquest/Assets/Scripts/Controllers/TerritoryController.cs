using System.Collections.Generic;
using System.Linq;
using Managers;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Color = Enums.Color;
using UnityEngine.Events;
using static Utils.Static;
using Enums;


namespace Controllers
{
    public class TerritoryController : MonoBehaviour
    {
        private PlayerController _player;
        private ResourceController _resourceController;

        public Vector3Int Base { get; private set; }
        private readonly Dictionary<Vector3Int, List<LineRenderer>> TilesDict = new();

        public int IronTileCount { get; set; }
        public int WoodTileCount { get; set; }
        public bool IsDirty { get; set; } = true;
        public float TroopTimer { get; set; } = 3f; // currently 3 seconds to conquer a tile
        private float hexSize = .58f; // idk why this is the case tho..
        private readonly Dictionary<Vector3Int, ConquerTimer> _timerTiles = new();

        private void Start()
        {
            _player = gameObject.GetComponent<PlayerController>();
        }

        private void Update()
        {
            if (_timerTiles.Count != 0)
            {
                UpdateTimers();
            }

            if (!IsDirty)
            {
                return;
            }

            UpdateTerritory();

            IsDirty = false;
        }

        public void SetStartTerritory(Vector3Int baseTile, List<Vector3Int> startingTiles)
        {
            // befoere this point (start/awake), the player is not created thus not initialized
            _player = gameObject.GetComponent<PlayerController>();
            _resourceController = gameObject.GetComponent<ResourceController>();
            Base = baseTile;
            AddTiles(startingTiles.Append(Base));
        }

        private void UpdateTerritory()
        {
            UpdateBorders();
            
            foreach (var troop in _player.Troops.Where(troop => !ContainsTile(troop.Position)))
            {
                Debug.Log(_player.Id + " | Troop is not in territory");
                if (IsAdjacentTerritoryTile(troop.Position) && !_timerTiles.ContainsKey(troop.Position))
                {
                    _timerTiles[troop.Position] = new ConquerTimer(
                        Instantiate(Assets.SliderPrefab, Assets.pBarParent.transform).GetComponent<Slider>(),
                        troop.transform.position,
                        _player.Color,
                        TroopTimer);
                }
            }
        }

        private bool IsAdjacentTerritoryTile(Vector3Int position)
        {
            return GetAdjacentTiles(position).Any(ContainsTile);
        }

        public List<Vector3Int> GetTerritory()
        {
            return TilesDict.Keys.ToList();
        }
        
        public int GetTerritorySize()
        {
            return TilesDict.Count;
        }

        public void ClearTerritory()
        {
            foreach (var timer in _timerTiles)
            {
                Destroy(timer.Value.progressBar.gameObject);
            }
            TilesDict.Clear();
        }

        private void UpdateTimers()
        {
            var troopPositions =
                new HashSet<Vector3Int>(_player.Troops.Select(t => t.Position)); // Use HashSet for O(1) lookups

            foreach (var key in _timerTiles.Keys.ToArray())
            {
                // Check if there is still a troop on the tile in the dictionary
                if (troopPositions.Contains(key))
                {
                    _timerTiles[key]._time -= Time.deltaTime * _player.GetTroop(key).TroopCount;
                }
                else
                {
                    _timerTiles[key]._time += Time.deltaTime;
                }

                // Check if the timer needs to be removed
                if (_timerTiles[key]._time <= 0 || _timerTiles[key]._time >= TroopTimer)
                {
                    Destroy(_timerTiles[key].progressBar.gameObject);
                    _timerTiles.Remove(key);

                    if (troopPositions.Contains(key))
                    {
                        // Handle enemy territory logic
                        var enemyPlayer = TileIsEnemyTerritory(key);
                        if (enemyPlayer)
                        {
                            if (TileIsEnemyBase(key, enemyPlayer))
                            {
                                GameController.RemovePlayer(enemyPlayer);
                            }
                            else
                            {
                                enemyPlayer.RemoveTile(key);
                            }
                        }

                        AddTile(key);
                    }

                    UpdateBorders();
                }
            }
        }

        private void AddTiles(IEnumerable<Vector3Int> tiles)
        {
            foreach (var tile in tiles)
            {
                AddTile(tile);
            }
        }

        private void AddTile(Vector3Int tile)
        {
            if (!ContainsTile(tile))
            {
                // dictionaries don't initialize it's value to null, so we need to do this manually
                TilesDict.Add(tile, new List<LineRenderer>() { null, null, null });
            }

            switch (WorldManager.GetTileType(tile))
            {
                case TileType.IronResource:
                    IronTileCount++;
                    _resourceController.AddIronHandout(1);
                    break;
                case TileType.WoodResource:
                    WoodTileCount++;
                    _resourceController.AddWoodHandout(1);
                    break;
                default: break;
            }
        }

        public void RemoveTile(Vector3Int tile)
        {
            TilesDict.Remove(tile);
            UpdateBorders();

            switch (WorldManager.GetTileType(tile))
            {
                case TileType.IronResource:
                    IronTileCount--;
                    _resourceController.RemoveIronHandout(1);
                    break;
                case TileType.WoodResource:
                    WoodTileCount--;
                    _resourceController.RemoveWoodHandout(1);
                    break;
                default:
                    break;
            }
        }

        public bool ContainsTile(Vector3Int tile)
        {
            return TilesDict.ContainsKey(tile);
        }

        private PlayerController TileIsEnemyTerritory(Vector3Int position)
        {
            return PlayerManager.GetPlayers()
                .FirstOrDefault(player => player.HasTileInTerritory(position) && player != _player);
        }

        private bool TileIsEnemyBase(Vector3Int position, PlayerController player)
        {
            return player.GetBaseTile() == position;
        }

        private void ResetBorders()
        {
            // this absolutely has to go, but it works for now...
            for (int i = 0; i < transform.childCount; i++)
            {
                Destroy(transform.GetChild(i).gameObject);
            }

            // we need this setup for the next iteration of positions
            // TODO: make sure we don't have to recalculate, but just overwrite the old ones.
            foreach (var tile in TilesDict)
            {
                tile.Value[0] = null;
                tile.Value[1] = null;
                tile.Value[2] = null;
            }
        }

        public void UpdateBorders()
        {
            // if we "update" instead of "recalculate, this would not be needed.
            ResetBorders();
            foreach (var tile in TilesDict)
            {
                // reset for new calculations, need to be improved, so we don't append positions
                int lr_index = 0;
                var neighbours = GetAdjacentTiles(tile.Key);

                for (int i = 0; i < 6; i++)
                {
                    Vector3Int neighbor = neighbours[i % 6];

                    // add when the neighbour is not part of our territory
                    if (!TilesDict.ContainsKey(neighbor))
                    {
                        // calcualte start and end of the line
                        Vector3 edgeStart = HexCorner(tile.Key, i);
                        Vector3 edgeEnd = HexCorner(tile.Key, i + 1);

                        if (tile.Value[lr_index] != null)
                        {
                            if (tile.Value[lr_index].GetPosition(tile.Value[lr_index].positionCount - 1) != edgeStart)
                            {
                                lr_index++;
                            }
                        }

                        if (tile.Value[lr_index] == null)
                        {
                            var hierPos = lr_index == 0 ? transform : tile.Value[lr_index - 1].transform;
                            tile.Value[lr_index] = Instantiate(Assets.LineRenderPrefab, hierPos)
                                .GetComponent<LineRenderer>();

                            tile.Value[lr_index].startColor = ConvertColor(_player.Color);
                            tile.Value[lr_index].endColor = ConvertColor(_player.Color);
                            tile.Value[lr_index].positionCount = 0;
                        }

                        tile.Value[lr_index].SetPosition(tile.Value[lr_index].positionCount++, edgeStart);
                        tile.Value[lr_index].SetPosition(tile.Value[lr_index].positionCount++, edgeEnd);
                    }
                }
            }
        }

        private Vector3 HexCorner(Vector3Int hex, int cornerIndex)
        {
            // we start at an angle of 150
            float angleDeg = (150 - 60 * cornerIndex) % 360;
            if (angleDeg < 0) angleDeg += 360;

            // Base angle for the first corner (Bottom-right), starting from 0 degrees
            float angleRad = Mathf.Deg2Rad * angleDeg;

            // Calculate the center position of the hex
            float centerY = hex.y * 1.5f * hexSize; // Center y position

            // Calculate x offset
            float horizontalOffset = (hex.y >= 0 ? Mathf.Floor(hex.y / 2f) : Mathf.Ceil(hex.y / 2f));
            float centerX = (hex.x + hex.y * 0.5f - horizontalOffset) * Mathf.Sqrt(3) * hexSize;

            // Correct for odd negative y rows
            if (hex.y < 0 && hex.y % 2 != 0)
            {
                centerX += Mathf.Sqrt(3) * hexSize; // Shift right by half a hex width
            }


            return new Vector3(
                centerX + Mathf.Cos(angleRad) * hexSize,
                centerY + Mathf.Sin(angleRad) * hexSize,
                0 // z is always zero in your case
            );
        }
    }

    // Utility class for timer indicators
    public class ConquerTimer
    {
        private float _timeBackingField; // Backing field for the _time property

        public float _time
        {
            get => _timeBackingField;
            set
            {
                _timeBackingField = value;
                OnValueChanged.Invoke(_timeBackingField);
            }
        }

        public Slider progressBar;
        private UnityEvent<float> OnValueChanged = new();

        public ConquerTimer(Slider pBar, Vector3 position, Color c, float time = 3f)
        {
            progressBar = pBar;
            progressBar.fillRect.GetComponent<Image>().color = ConvertColor(c);
            progressBar.maxValue = time;

            // make sure the position will be updated when the progress bar is in use
            OnValueChanged.AddListener(timeVal =>
            {
                progressBar.value = progressBar.maxValue - timeVal;
                progressBar.transform.position = Camera.main.WorldToScreenPoint(position) - new Vector3(0, 15, 0);
            });

            _time = time;
        }
    }
}