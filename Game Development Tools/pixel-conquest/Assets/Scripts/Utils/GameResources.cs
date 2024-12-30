using UnityEngine;
using UnityEngine.Tilemaps;

namespace Utils 
{
    public class GameResources : MonoBehaviour 
    {   
        // Singleton instance
        public static GameResources Instance { get; private set; }

        [Header("Player Related :")]
        public GameObject PlayerPrefab;
        public GameObject EnemyPrefab;
        
        [Header("Tilemaps :")]
        public Tilemap BaseTilemap;
        public Tilemap SelectedTilemap;

        [Header("Map Generation :")]
        public TileBase BaseCenterTile;
        public TileBase[] BaseTiles;

        public TileBase WoodCenterTile;
        public TileBase[] WoodTiles;
        
        public TileBase IronCenterTile;
        public TileBase[] IronTiles;

        public TileBase MountainCenterTile;
        public TileBase[] MountainTiles;

        public TileBase FarmCenterTile;
        public TileBase[] FarmTiles;

        // these are "single" fller tiles
        public TileBase[] GrassTiles;
        public TileBase[] LakeTile;

        [Header("Progress Bar :")]
        public GameObject pBarParent;
        public GameObject SliderPrefab;

        [Header("Troop Initialisation :")]
        public GameObject TroopPrefab;
        public GameObject TroopCountParent;
        public GameObject TroopCountPrefab;

        [Header("Territory Borders :")]
        public GameObject LineRenderPrefab;

        [Header("GUI :")]
        public GameObject GUIManager;
        public GameObject BuyTroopPopUp;
        public GameObject SplitTroopPopUp;

        [Header("PopUps :")]
        public GameObject[] PopUps;

        [Header("EndScreen :")]
        public GameObject EndScreenParent;
        public GameObject Banner;
        public GameObject VictoryBanner;
        public GameObject DefeatBanner;
        

        private void Awake()
        {
            // Ensure that only one instance exists
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject); // Destroy the duplicate instance
                return;
            }
            Static.Assets = Instance = this;
        }

        // yes we need this to initialize the default values :(
        private void Start(){
            StaticGameSettings _ =new();
            StaticSettings __=new();
        }
    }
}