using Controllers;
using DataStructures;
using GameGUI;
using UnityEngine;
using static Utils.Static;

namespace Actions
{
    public class TroopActionHandler
    {
        private PlayerController _player;
        private Animator _buyTroopAnimator;
        private SplitTroops _splitTroop;

        public TroopActionHandler(PlayerController player)
        {
            _player = player;
            _buyTroopAnimator = Assets.BuyTroopPopUp.GetComponent<Animator>();
            _splitTroop = Assets.SplitTroopPopUp.GetComponent<SplitTroops>();
        }
 
        public void HandleMoveTroop(Vector3Int position, Troop troop)
        {
            if (_splitTroop.amount == troop.TroopCount)
            {
                _player.MoveTroop(position, troop.gameObject);
            } 
            else
            {
                _player.SplitAndMoveTroop(troop, position, _splitTroop.amount);
            }
        }

        public void HandleBuyTroop()
        {
            _buyTroopAnimator.SetBool("show", !_buyTroopAnimator.GetBool("show"));//ToDo change this line to use show method
        }

        internal void HandleSelectTroop(Troop troop)
        {
            _splitTroop.Show(troop.gameObject);
        }
    }
}