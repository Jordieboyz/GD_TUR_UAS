using AI;
using DataStructures;
using UnityEngine;

namespace Controllers
{
    public class EnemyController : MonoBehaviour
    {
        private PlayerController _playerController;
        private StateMachine _stateMachine;
        private StateDecider _stateDecider;

        private const float StateChangeTime = 3f;
        private const float MoveTroopsTime = 1.25f;
        private const float BuyTroopsTime = 0.75f;

        private float _stateChangeTimer = 4f;
        private float _moveTroopsTimer;
        private float _buyTroopsTimer;

        private void Start()
        {
            _playerController = gameObject.GetComponent<PlayerController>();
            _stateMachine = new StateMachine(_playerController);
            _stateDecider = new StateDecider(_playerController);
        }

        private void Update()
        {
            _stateChangeTimer += Time.deltaTime;
            _moveTroopsTimer += Time.deltaTime;
            _buyTroopsTimer += Time.deltaTime;

            if (_stateChangeTimer >= StateChangeTime)
            {
                _stateChangeTimer = 0f;
                var lastState = _stateMachine.CurrentState;
                _stateMachine.CurrentState = _stateDecider.DecideState();
                if (lastState != _stateMachine.CurrentState)
                {
                    _stateMachine.OnEnter();
                }
            }

            if (_moveTroopsTimer >= MoveTroopsTime)
            {
                _moveTroopsTimer = 0f;
                _stateMachine.MoveTroops();
            }

            if (_buyTroopsTimer >= BuyTroopsTime)
            {
                _buyTroopsTimer = 0f;
                _stateMachine.BuyTroops();
            }
        }
        }
}