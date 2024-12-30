using DataStructures;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameGUI
{
    public class SplitTroops : MonoBehaviour
    {
        public Button plusButton;
        public Button minusButton;
        public Button maxButton;
        public TextMeshProUGUI amountText;
        public int amount { get; set; } = 1;
        private Camera _camera;
        private Animator _animator;
        private Vector3 _troopPosition;
        private Troop _troopScript;

        private void Start()
        {
            plusButton.onClick.AddListener(IncrementAmount);
            minusButton.onClick.AddListener(DecrementAmount);
            maxButton.onClick.AddListener(SetMaxAmount);
            _animator = gameObject.GetComponent<Animator>();
            _camera = Camera.main;
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
            gameObject.transform.position = _camera.WorldToScreenPoint(_troopPosition); 
        }
    
        private void UpdateAmount()
        {
            amountText.text = amount.ToString();
        }

        public void Show(GameObject troop)
        {
            _troopPosition = troop.transform.position;
            _troopScript = troop.GetComponent<Troop>();
            amount = _troopScript.TroopCount;
            UpdateAmount();
            _animator.SetBool("show", true);
        }

        private void IncrementAmount()
        {
            if (EnoughTroopsAvalible(amount + 1)) 
            {
                amount++;
                UpdateAmount();
            }
        }

        private void DecrementAmount()
        {
            if (amount > 1)
            {
                amount--;
                UpdateAmount();
            }
        }

        private void SetMaxAmount()
        {
            amount = _troopScript.TroopCount;
            UpdateAmount();
        }

        private void ResetPopUp()
        {
            gameObject.GetComponent<Animator>().SetBool("show", false);
        }

        private bool EnoughTroopsAvalible(int newValue)
        {
            return _troopScript.TroopCount >= newValue;
        }
    }
}
