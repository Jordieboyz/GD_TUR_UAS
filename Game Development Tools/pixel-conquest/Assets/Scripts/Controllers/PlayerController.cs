using System.Collections.Generic;
using Actions;
using DataStructures;
using Enums;
using GameGUI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using Color = Enums.Color;

namespace Controllers
{
    [RequireComponent(typeof(ResourceController))]
    [RequireComponent(typeof(TileController))]
    [RequireComponent(typeof(ActionHandler))]
    [RequireComponent(typeof(TerritoryController))]
    [RequireComponent(typeof(TroopController))]
    public class PlayerController : MonoBehaviour
    {
        public int Id { get; private set; }
        public bool IsLocalPlayer { get; set; }
        public Color Color { get; set; }
        public Resource Iron { get; } = new(10);
        public Resource Wood { get; } = new(20);
        public Resource TroopCount { get; } = new();
        public List<Troop> Troops { get; } = new();

        private ResourceController _resourceController;
        private TileController _tileController;
        private ActionHandler _actionHandler;
        private TerritoryController _territoryController;
        private TroopController _troopController;

        private void Awake()
        {
            _resourceController = gameObject.GetComponent<ResourceController>();
            _tileController = gameObject.GetComponent<TileController>();
            _actionHandler = gameObject.GetComponent<ActionHandler>();
            _territoryController = gameObject.GetComponent<TerritoryController>();
            _troopController = gameObject.GetComponent<TroopController>();
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0) && IsLocalPlayer && !GameMenuActions.isPaused)
            {
                if (clickedPositionIsNotAButton())
                {
                    _actionHandler.HandleAction(ClickActions.TileClicked, _tileController.GetCellUnderMouse());
                }
            }

            if (Input.GetKeyDown(KeyCode.T) && IsLocalPlayer && !GameMenuActions.isPaused)
            {
                _actionHandler.HandleAction(ClickActions.BuyTroop, _tileController.GetCellUnderMouse());
            }
        }

        public void SetID(int id)
        {
            Id = id;

            if (Id != 0)
            {
                return;
            }

            FindObjectOfType<CameraController>().SnapToPlayer(this);
            IsLocalPlayer = true;
        }

        public void SetStartTerritory(Vector3Int baseTile, List<Vector3Int> startingTiles)
        {
            _territoryController.SetStartTerritory(baseTile, startingTiles);
        }

        public void Defeat()
        {
            foreach (var t in Troops)
            {
                Destroy(t.gameObject);
            }

            _territoryController.ClearTerritory();
            Troops.Clear();

            Destroy(gameObject);
        }

        public (Troop, Troop) SplitAndMoveTroop(Troop troop, Vector3Int newPosition, int splittingAmount)
        {
            _territoryController.IsDirty = true;
            return _troopController.SplitAndMoveTroop(troop, newPosition, splittingAmount);
        }

        public void MoveTroop(Vector3Int position, GameObject troop)
        {
            _troopController.MoveTroop(position, troop);
            _territoryController.IsDirty = true;
        }

        public Troop BuyTroop(int amount)
        {
            return _troopController.BuyTroop(amount);
        }

        public void AddIron(int amount)
        {
            Iron.Add(amount);
        }

        public void RemoveIron(int amount)
        {
            Iron.Remove(amount);
        }

        public void AddWood(int amount)
        {
            Wood.Add(amount);
        }

        public void RemoveWood(int amount)
        {
            Wood.Remove(amount);
        }

        public Troop GetTroop(Vector3Int position)
        {
            return Troops.Find(troop => troop.Position == position);
        }

        public List<Troop> GetTroops()
        {
            return Troops;
        }

        public int GetTerritorySize()
        {
            return _territoryController.GetTerritorySize();
        }

        public int GetWoodTileCount()
        {
            return _territoryController.WoodTileCount;
        }

        public int GetIronTileCount()
        {
            return _territoryController.IronTileCount;
        }

        public void AddTroop(Troop troop)
        {
            Troops.Add(troop);
            TroopCount.Add(troop.TroopCount);
        }

        public void RemoveTroop(Troop troop, bool updateCount = true)
        {
            Troops.Remove(troop);

            if (updateCount)
            {
                TroopCount.Remove(troop.TroopCount);
            }
        }

        public void AddToTroop(Troop troop, int amount)
        {
            troop.AddTroops(amount);
            TroopCount.Add(amount);
        }

        public void RemoveTile(Vector3Int position)
        {
            _territoryController.RemoveTile(position);
        }

        public Vector3Int GetBaseTile()
        {
            return _territoryController.Base;
        }

        public List<Vector3Int> GetTerritory()
        {
            return _territoryController.GetTerritory();
        }
        
        public bool HasTileInTerritory(Vector3Int position)
        {
            return _territoryController.ContainsTile(position);
        }

        public void AddResourceListener(string res, UnityAction<int> method)
        {
            switch (res)
            {
                case nameof(Iron):
                    Iron.OnValueChanged.AddListener(method);
                    break;
                case nameof(Wood):
                    Wood.OnValueChanged.AddListener(method);
                    break;
                case nameof(TroopCount):
                    TroopCount.OnValueChanged.AddListener(method);
                    break;
                default: break;
            }
        }

        private bool clickedPositionIsNotAButton()
        {
            GameObject target = EventSystem.current.currentSelectedGameObject;
            return !((target != null) && (target.GetComponent<Button>() != null));
        }
    }
}