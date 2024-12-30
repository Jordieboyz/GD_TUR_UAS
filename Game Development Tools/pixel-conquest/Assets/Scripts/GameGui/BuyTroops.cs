using Controllers;
using Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Utils.Static;

namespace GameGUI
{
    public class BuyTroops : MonoBehaviour
    {
        public Button plusButton;
        public Button minusButton;
        public Button maxButton;
        public Button buyButton;
        public TextMeshProUGUI amountText;
        public TextMeshProUGUI ironAmountText;
        public TextMeshProUGUI woodAmountText;
        private int _amount = 1;
        private Vector3 _baseTilePosition;
        private Camera _camera;
        private Animator _animator;

        private void Start()
        {
            plusButton.onClick.AddListener(IncrementAmount);
            minusButton.onClick.AddListener(DecrementAmount);
            maxButton.onClick.AddListener(SetMaxAmount);
            buyButton.onClick.AddListener(BuyTroop);
            _animator = gameObject.GetComponent<Animator>();
            _camera = Camera.main;
        }

        public void Init()
        {
            _baseTilePosition = Assets.BaseTilemap.GetCellCenterWorld(
                    PlayerManager.GetLocalPlayer().GetBaseTile());
        }

        private void Update()
        {
            if (_animator.GetBool("show"))
            {
                UpdatePosition();
            }
        }

        public void UpdatePosition()
        {
            gameObject.transform.position = _camera.WorldToScreenPoint(_baseTilePosition); 
        }
    
        public void UpdateAmount()
        {
            amountText.text = _amount.ToString();
            ironAmountText.text = "" + (TroopController.TroopPrice.Iron * _amount);
            woodAmountText.text = "" + (TroopController.TroopPrice.Wood * _amount);
        }

        private void IncrementAmount()
        {
            if (PlayerHasEnoughResources(_amount + 1)) 
            {
                _amount++;
                UpdateAmount();
            }
        }

        private void DecrementAmount()
        {
            if (_amount > 1 && PlayerHasEnoughResources(_amount - 1))
            {
                _amount--;
                UpdateAmount();
            }
        }

        private void SetMaxAmount()
        {
            int maxPossibleIron = PlayerManager.GetLocalPlayer().Iron.Amount / TroopController.TroopPrice.Iron;
            int maxPossibleWood = PlayerManager.GetLocalPlayer().Wood.Amount / TroopController.TroopPrice.Wood;

            int maxAmount = (int)Mathf.Min(maxPossibleIron, maxPossibleWood);
            if (maxAmount > 0)
            {
                _amount = maxAmount;
                UpdateAmount();
            }
        }

        private void BuyTroop()
        {
            if (PlayerHasEnoughResources(_amount))
            {
                PlayerManager.GetLocalPlayer().BuyTroop(_amount);
                ResetPopUp();
            }
        }


        private void ResetPopUp()
        {
            _amount = 1;
            UpdateAmount();
            gameObject.GetComponent<Animator>().SetBool("show", false);
        }

        private bool PlayerHasEnoughResources(int newValue)
        {
            var ironPrice = TroopController.TroopPrice.Iron * newValue;
            var woodPrice = TroopController.TroopPrice.Wood * newValue;
            return PlayerManager.GetLocalPlayer().Iron.HasEnough(ironPrice)
                   && PlayerManager.GetLocalPlayer().Wood.HasEnough(woodPrice);
        }
    }
}
