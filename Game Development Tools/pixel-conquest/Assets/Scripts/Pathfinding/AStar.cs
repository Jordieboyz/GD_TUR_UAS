using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils;

namespace Pathfinding
{
    public class AStar
    {
        private static readonly Dictionary<(Vector3Int, Vector3Int), List<Vector3Int>> PathCache = new();

        public static List<Vector3Int> FindPath(Vector3Int start, Vector3Int target)
        {
            if (PathCache.TryGetValue((start, target), out var cachedPath))
            {
                return cachedPath;
            }

            var openSet = new List<Node>();
            var closedSet = new HashSet<Vector3Int>();
            var startNode = new Node(start);

            // Add the starting node to the open set
            openSet.Add(startNode);

            // Loop until the open set is empty
            while (openSet.Count > 0)
            {
                var currentNode = openSet.OrderBy(n => n.FCost).ThenBy(n => n.HCost).First();

                // Check if the current node is the target node, if so, retrace the path
                if (currentNode.Position == target)
                {
                    var path = RetracePath(startNode, currentNode);
                    PathCache[(start, target)] = path;
                    return path;
                }

                // Remove the current node from the open set and add it to the closed set
                openSet.Remove(currentNode);
                closedSet.Add(currentNode.Position);

                // Loop through the current node's neighbors
                foreach (var neighborPosition in GetNeighbors(currentNode.Position))
                {
                    // Skip if the neighbor if it is in the closed set
                    if (closedSet.Contains(neighborPosition))
                    {
                        continue;
                    }

                    // Calculate the tentative G cost
                    var tentativeGCost =
                        currentNode.GCost + Vector3Int.Distance(currentNode.Position, neighborPosition);
                    var neighborNode = openSet.FirstOrDefault(n => n.Position == neighborPosition);

                    // Add the neighbor to the open set if it is not already there
                    if (neighborNode == null)
                    {
                        neighborNode = new Node(neighborPosition)
                        {
                            GCost = tentativeGCost,
                            HCost = Vector3Int.Distance(neighborPosition, target),
                            Parent = currentNode
                        };
                        openSet.Add(neighborNode);
                    }
                    // Update the neighbor's G cost and parent if the tentative G cost is lower
                    else if (tentativeGCost < neighborNode.GCost)
                    {
                        neighborNode.GCost = tentativeGCost;
                        neighborNode.Parent = currentNode;
                    }
                }
            }

            // No path found
            return null;
        }

        public static Vector3Int GetNextPosition(Vector3Int start, Vector3Int target)
        {
            var path = FindPath(start, target);

            if (path is null)
            {
                return start;
            }

            return path.Count > 1 ? path[0] : target;
        }

        private static IEnumerable<Vector3Int> GetNeighbors(Vector3Int position)
        {
            return Static.GetAdjacentTiles(position);
        }

        private static List<Vector3Int> RetracePath(Node startNode, Node endNode)
        {
            var path = new List<Vector3Int>();
            var currentNode = endNode;

            while (currentNode != startNode)
            {
                path.Add(currentNode.Position);
                currentNode = currentNode.Parent;
            }

            path.Reverse();
            return path;
        }
    }
}