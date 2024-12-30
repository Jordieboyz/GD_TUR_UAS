using System.Collections.Generic;
using Enums;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;
using System.Linq;
using Utils;
using static Utils.Static;


namespace Managers
{
    public class WorldManager
    {
        const int NO_PATTERN = -1;

        // internal settings generated based on initialisation
        private bool RandomMap;
        private float PlayersAmount;
        private float minBaseDistance;
        public static Vector2Int mapSize;

        public static List<Vector3Int> ironTiles = new();
        public static List<Vector3Int> woodTiles = new();
        public List<Vector3Int> baseTiles = new();
        public static Dictionary<Vector3Int, TileType> World = new();

        private readonly Dictionary<TileType, Pattern> patterns = new();
        private readonly PatternPainter defaultPP = new() { pattern = default, paintIndex = NO_PATTERN };

        public int mapWidth = 5; // should scale with amount of player
        public int mapHeight = 5;
        public int minBaseDist = 5;
        public int noiseScale = 2;

        private bool skip_remaining_generation = false;

        public void Init()
        {
            // a way to transfer data to this class from the mainMenu;
            RandomMap = StaticGameSettings.Get<bool>(nameof(RandomMap));
            PlayersAmount = StaticGameSettings.Get<float>(nameof(PlayersAmount));

            // int scaleFactor = PlayersAmount+(PlayersAmount/2);
            mapSize = new(mapWidth * (int)PlayersAmount - mapWidth, mapHeight * (int)PlayersAmount - mapHeight);
            minBaseDistance = minBaseDist * (int)PlayersAmount / 2;

            // patterns configuration for easier access
            ConfigurePatterns();
        }

        private void ConfigurePatterns()
        {
            patterns[TileType.Base] = new Pattern
            {
                center = Assets.BaseCenterTile,
                adjTiles = Assets.BaseTiles,
                Count = Assets.BaseTiles.Count()
            };
            patterns[TileType.IronResource] = new Pattern
            {
                center = Assets.IronCenterTile,
                adjTiles = Assets.IronTiles,
                Count = Assets.IronTiles.Count(),
            };
            patterns[TileType.WoodResource] = new Pattern
            {
                center = Assets.WoodCenterTile,
                adjTiles = Assets.WoodTiles,
                Count = Assets.WoodTiles.Count(),
            };
            patterns[TileType.Mountains] = new Pattern
            {
                center = Assets.MountainCenterTile,
                adjTiles = Assets.MountainTiles,
                Count = Assets.MountainTiles.Count(),
            };
            patterns[TileType.Farm] = new Pattern
            {
                center = Assets.FarmCenterTile,
                adjTiles = Assets.FarmTiles,
                Count = Assets.FarmTiles.Count(),
            };
        }

        private void ResetWorld()
        {
            Assets.BaseTilemap.ClearAllTiles();
            ironTiles.Clear();
            woodTiles.Clear();
            baseTiles.Clear();
            World.Clear();
        }

        private void GenerateRandomTiles(List<Vector3Int> tileList, TileType type, int max,
            IEnumerable<Vector3Int> checkDist = null, float minDist = 0)
        {
            if (PlayersAmount < 2 || PlayersAmount > 6)
            {
                Debug.Log("Not a valid amount of players!");
                return;
            }

            float timer = 0;
            while (tileList.Count < max && (timer += Time.deltaTime) < 100 && !skip_remaining_generation)
            {
                var newRandBasePos = new Vector3Int(Random.Range(-mapSize.x + 1, mapSize.y - 1),
                    Random.Range(-mapSize.y + 1, mapSize.y - 1), 0);
                if (World.ContainsKey(newRandBasePos))
                {
                    continue;
                }

                bool isOutsideRadius = true;

                // check if the previously spawn units are not too close.
                foreach (var pos in tileList)
                {
                    if (HexDistance(newRandBasePos, pos) <= 2)
                    {
                        isOutsideRadius = false;
                        break;
                    }
                }

                // check if all other "so far spawned" objects are not too close.
                if (checkDist != null)
                {
                    foreach (var pos in checkDist)
                    {
                        if (HexDistance(newRandBasePos, pos) <= minDist)
                        {
                            isOutsideRadius = false;
                            break;
                        }
                    }
                }

                if (isOutsideRadius)
                {
                    World.Add(newRandBasePos, type);
                    tileList.Add(newRandBasePos);
                    ConfigurePattern(GetAdjacentTiles(newRandBasePos), type + 1);
                }
            }

            if (timer > 100)
            {
                Debug.Log(type + " Could not generate gameState in time. Please choose some other variables...");
                skip_remaining_generation = true; 
            }
        }

        public void GeneratePerlinNoiseWorld()
        {
            ResetWorld();

            // These are the "priority tiles"
            GenerateRandomTiles(baseTiles, TileType.Base, (int)PlayersAmount, baseTiles, minBaseDistance);
            GenerateRandomTiles(ironTiles, TileType.IronResource, (int)PlayersAmount, baseTiles, 3);
            GenerateRandomTiles(woodTiles, TileType.WoodResource, (int)PlayersAmount + 2, Combine(baseTiles, ironTiles),
                3);

            // These are the "cosmetic/filler tiles"
            List<Vector3Int> Terrain = new();
            GenerateRandomTiles(Terrain, TileType.Mountains, (int)PlayersAmount * 2,
                Combine(baseTiles, ironTiles, woodTiles), 3);
            GenerateRandomTiles(Terrain, TileType.Farm, (int)PlayersAmount * 3,
                Combine(baseTiles, ironTiles, woodTiles), 3);

            // fill the rest up with single tiles
            float _noiseSeed;
            for (int x = -mapSize.x; x <= mapSize.x; x++)
            {
                for (int y = -mapSize.y; y <= mapSize.y; y++)
                {
                    Vector3Int gridCoord = new(x, y, 0);
                    if (World.ContainsKey(gridCoord))
                    {
                        continue;
                    }

                    _noiseSeed = RandomMap ? Random.Range(0, 10000) : 0;
                    Vector3 randomGridOffset = new((x + _noiseSeed) / noiseScale, (y + _noiseSeed) / noiseScale, 0);
                    TileType selectedTile =
                        GetTileTypeFromNoise(Mathf.PerlinNoise(randomGridOffset.x, randomGridOffset.y));
                    World.Add(gridCoord, selectedTile);
                }
            }

            UpdateTilemap();
        }

        private void UpdateTilemap()
        {
            PatternPainter PP = defaultPP;
            foreach (var pos in World)
            {
                if (PP.paintIndex != NO_PATTERN)
                {
                    Assets.BaseTilemap.SetTile(pos.Key, PP.pattern.adjTiles[PP.paintIndex]);
                    if (PP.paintIndex++ >= PP.pattern.Count - 1)
                    {
                        PP = defaultPP;
                    }
                }
                else
                {
                    PP = GetPatternPainter(pos.Value);
                    Assets.BaseTilemap.SetTile(pos.Key, GetTileBase(pos.Value));
                }
            }
        }

        private PatternPainter GetPatternPainter(TileType type)
        {
            return patterns.ContainsKey(type) ? new() { pattern = patterns[type], paintIndex = 0 } : defaultPP;
        }

        private void ConfigurePattern(List<Vector3Int> offsets, TileType adjTile)
        {
            foreach (var pos in offsets)
            {
                World.Add(pos, adjTile);
            }
        }

        // Determine the tile type based on the Perlin noise value
        private TileType GetTileTypeFromNoise(float perlinValue)
        {
            return perlinValue switch
            {
                float value when value <= .15f => TileType.LakeTiles,
                float value when value <= .1f => TileType.MountainsAdj,
                _ => TileType.GrassField
            };
        }


        private static TileBase GetTileBase(TileType tileType)
        {
            return tileType switch
            {
                // for patterns
                TileType.Base => Assets.BaseCenterTile,
                TileType.IronResource => Assets.IronCenterTile,
                TileType.WoodResource => Assets.WoodCenterTile,
                TileType.Mountains => Assets.MountainCenterTile,
                TileType.Farm => Assets.FarmCenterTile,

                // single Tiles
                TileType.MountainsAdj => Assets.MountainTiles[Random.Range(0, Assets.MountainTiles.Length)],
                TileType.LakeTiles => Assets.LakeTile[Random.Range(0, Assets.LakeTile.Length)],
                TileType.GrassField => Assets.GrassTiles[Random.Range(0, Assets.GrassTiles.Length)],

                // we should never reach this
                _ => null
            };
        }

        public static TileType GetTileType(Vector3Int position)
        {
            return World[position];
        }
        
        public static List<Vector3Int> GetAllResourceTiles()
        {
            return Combine(ironTiles, woodTiles);
        }
    }
}