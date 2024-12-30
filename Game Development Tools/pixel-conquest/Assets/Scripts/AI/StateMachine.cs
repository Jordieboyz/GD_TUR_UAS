using System;
using Controllers;
using DataStructures;
using Enums;

namespace AI
{
    public class StateMachine
    {
        private PlayerController _playerController;

        public StateMachine(PlayerController playerController)
        {
            _playerController = playerController;
        }

        public AIStates CurrentState
        {
            get => _state switch
            {
                Farm => AIStates.Farm,
                Expand => AIStates.Expand,
                Attack => AIStates.Attack,
                Defend => AIStates.Defend,
                _ => throw new ArgumentOutOfRangeException()
            };
            set => _state = value switch
            {
                AIStates.Farm => new Farm(),
                AIStates.Expand => new Expand(),
                AIStates.Attack => new Attack(),
                AIStates.Defend => new Defend(),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private State _state = new Farm();

        public void OnEnter()
        {
            _state.OnEnter(_playerController);
        }

        public void MoveTroops()
        {
            _state.MoveTroops(_playerController);
        }

        public void BuyTroops()
        {
            _state.BuyTroops(_playerController);
        }
    }
}