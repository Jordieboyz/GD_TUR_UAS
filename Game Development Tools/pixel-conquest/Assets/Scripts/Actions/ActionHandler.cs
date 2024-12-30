using Controllers;
using DataStructures;
using Enums;
using UnityEngine;
using UnityEngine.Tilemaps;
using static Utils.Static;

namespace Actions
{
    public class ActionHandler : MonoBehaviour
    {
        private PlayerController _player;
        private TileActionHandler _tileActionHandler;
        private TroopActionHandler _troopActionHandler;
        
        public Tile selectedTile;
        public Tile activatedTile;

        private void Start()
        {
            _player = gameObject.GetComponent<PlayerController>();
            _tileActionHandler = new TileActionHandler(_player, this, 
                                                            Assets.BaseTilemap, 
                                                            Assets.SelectedTilemap, 
                                                            selectedTile, 
                                                            activatedTile);
            _troopActionHandler = new TroopActionHandler(_player);
        }

        public void HandleAction(ClickActions action, Vector3Int position, Troop troop = null)
        {
            GameObject[] popUps = Assets.PopUps;
            foreach (GameObject popUp in popUps)
            {
                popUp.GetComponent<Animator>().SetBool("show", false);
            }
            switch (action)
            {
                case ClickActions.TileClicked:
                    _tileActionHandler.HandleTileClicked(position);
                    break;
                case ClickActions.BuyTroop:
                    _troopActionHandler.HandleBuyTroop();
                    break;
                case ClickActions.MoveTroop:
                    _troopActionHandler.HandleMoveTroop(position, troop);
                    break;
                case ClickActions.SelectTroop:
                    _troopActionHandler.HandleSelectTroop(troop);
                    break;
                default:
                    break;
            }
        }
    }
}