using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.Tilemaps;
using Color = Enums.Color;
using Random = UnityEngine.Random;

namespace Utils 
{
    public class Static {

        public static GameResources Assets = GameResources.Instance;

        /*--------------------------------------------------------------------------------------
            Protected Section
                GetProp(string)        : Retrieve PropertyInfo if the property exists with a
                                        "static public" identifier.
        ----------------------------------------------------------------------------------------*/
        public static PropertyInfo GetPropertyOfClass<T>(T _, string name,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public)
        {
            return typeof(T).GetProperty(name, flags);
        }

        public static PropertyInfo GetPropertyOfClass<T>(string name,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public)
        {
            return typeof(T).GetProperty(name, flags);
        }
        
        private static readonly Dictionary<Vector3Int, List<Vector3Int>> AdjacentCache = new();
        private static readonly List<Vector3Int> TileVectorCache = new();

        public static List<Vector3Int> GetAdjacentTiles(Vector3Int refPos)
        {
            if (AdjacentCache.TryGetValue(refPos, out var cachedNeighbors))
            {
                return cachedNeighbors;
            }
            
            // Clockwise offsets for even and odd rows
            Vector3Int[] offsets = refPos.y % 2 == 0
                ? new Vector3Int[]
                {
                    new(-1, 1),  // Top-left
                    new(0, 1),   // Top-right
                    new(1, 0),   // Right
                    new(0, -1),  // Bottom-right
                    new(-1, -1), // Bottom-left
                    new(-1, 0),  // Left    
                }
                : new Vector3Int[]
                {
                    new (0, 1),   // Top-left
                    new(1, 1),   // Top-right
                    new(1, 0),   // Right
                    new(1, -1),  // Bottom-right
                    new(0, -1),  // Bottom-left
                    new(-1, 0),  // Left
                };
            
            var adjacentTiles = offsets.Select(offset => refPos + offset).ToList();
            
            AdjacentCache[refPos] = adjacentTiles;
            return adjacentTiles;
        }

        public static List<Vector3Int> GetTilesAsVectorList()
        {
            if (TileVectorCache.Count > 0)
            {
                return TileVectorCache;
            }
            
            var tilemap = GameResources.Instance.BaseTilemap;
            var bounds = tilemap.cellBounds;
            
            foreach (var pos in bounds.allPositionsWithin)
            {
                if (tilemap.HasTile(pos))
                {
                    TileVectorCache.Add(pos);
                }
            }

            return TileVectorCache;
        }
        
        public static UnityEngine.Color ConvertColor(Color c, float a = 1)
        {
            return c switch{
                Color.Red => new UnityEngine.Color(1f, 0f, 0f, a),
                Color.Green => new UnityEngine.Color(0f, 1f, 0f, a),
                Color.Blue => new UnityEngine.Color(0f, 0f, 1f, a),
                Color.Yellow => new UnityEngine.Color(1f, 1f, 0f, a),
                Color.Orange => new UnityEngine.Color(1f, 0.5f, 0f, a),
                Color.Purple => new UnityEngine.Color(0.5f, 0f, 0.5f, a),
                _ => new UnityEngine.Color(1f, 1f, 1f, a), // Default to white with custom alpha
            };
        }

        // Generate a randomized list of Colors
        public static List<Color> GetRandColors(int n = 1){
            return Enumerable.Range(0, Enum.GetValues(typeof(Color)).Length).OrderBy(_ => Guid.NewGuid())
                .Take(n)
                .Select(i => (Color)i)
                .ToList();
        }

        // Calculate the distance between hexagons in hexagons width.
        public static Vector3Int OffsetToAxial(Vector3Int hex){
            return new Vector3Int(hex.x-(hex.y-(hex.y&1))/2, hex.y, 0);
        }

        public static Vector3Int SubAxialCoord(Vector3Int hexa, Vector3Int hexb){
            return new Vector3Int(hexa.x-hexb.x, hexa.y-hexb.y, 0);
        }

        // calculates the distance in hex coordinates (not units!)
        public static int HexDistance(Vector3Int a, Vector3Int b)
        {
            var sub = SubAxialCoord(OffsetToAxial(a),OffsetToAxial(b));
            return (Mathf.Abs(sub.x)+Mathf.Abs(sub.x+sub.y)+Mathf.Abs(sub.y))/2;
        }

        public static Vector3 GetHexCenter(Vector3Int tilePosition, Tilemap tilemap)
        {
            // Convert tilemap cell position to world position
            return tilemap.GetCellCenterWorld(tilePosition);
        }

        public static List<T> Combine<T>(params IEnumerable<T>[] tileLists)
        {
            return tileLists.SelectMany(list => list).ToList();
        }
    }
}