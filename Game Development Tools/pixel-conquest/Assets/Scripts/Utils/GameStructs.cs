using System;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Utils 
{
    struct Pattern 
    {
        public TileBase center;
        public TileBase[] adjTiles;
        public int Count;
    }

    struct PatternPainter 
    {
        public Pattern pattern;
        public int paintIndex;
    }

    // Define a serializable class that holds a GameObject and a bool
    [Serializable]
    public struct PairedGameObject 
    {
        public GameObject obj;
        public bool PPSetting; 
        public bool StaticGameSetting; 
        public bool StaticPlayerSetting;
    }
}
