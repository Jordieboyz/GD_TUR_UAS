using UnityEngine;

namespace Pathfinding
{
    public class Node
    {
        public Vector3Int Position;
        public float GCost;
        public float HCost;
        public float FCost => GCost + HCost;
        public Node Parent;

        public Node(Vector3Int position)
        {
            Position = position;
        }
    }
}