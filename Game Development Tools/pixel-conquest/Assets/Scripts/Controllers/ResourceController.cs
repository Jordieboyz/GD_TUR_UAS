using UnityEngine;

namespace Controllers
{
    public class ResourceController : MonoBehaviour
    {
        private PlayerController _player;
        private float _ironTimer;
        private float _woodTimer;
        private float _ironInterval = 4.0f;
        private float _woodInterval = 2.0f;
        private int _ironHandout = 1;
        private int _woodHandout = 1;
        
        private void Start()
        {
            _player = gameObject.GetComponent<PlayerController>();
        }

        private void Update()
        {
            _ironTimer += Time.deltaTime;
            _woodTimer += Time.deltaTime;

            if (_ironTimer >= _ironInterval)
            {
                _player.AddIron(_ironHandout);
                _ironTimer = 0f;
            }

            if (_woodTimer >= _woodInterval)
            {
                _player.AddWood(_woodHandout);
                _woodTimer = 0f;
            }
        }
        
        public void SetIronInterval(float interval)
        {
            _ironInterval = interval;
        }
        
        public void SetWoodInterval(float interval)
        {
            _woodInterval = interval;
        }
        
        public void AddIronHandout(int amount)
        {
            _ironHandout += amount;
        }
        
        public void RemoveIronHandout(int amount)
        {
            _ironHandout -= amount;
        }
        
        public void AddWoodHandout(int amount)
        {
            _woodHandout += amount;
        }
        
        public void RemoveWoodHandout(int amount)
        {
            _woodHandout -= amount;
        }
    }
}
