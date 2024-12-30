using UnityEngine;
using static Utils.Static;

namespace Controllers
{
    public class TileController : MonoBehaviour
    {
        private PlayerController _player;

        private void Start()
        {
            _player = gameObject.GetComponent<PlayerController>();
        }
        
        public Vector3Int GetCellUnderMouse()
        {
            var mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            return Assets.BaseTilemap.WorldToCell(mouseWorldPos);
        }
    }
}