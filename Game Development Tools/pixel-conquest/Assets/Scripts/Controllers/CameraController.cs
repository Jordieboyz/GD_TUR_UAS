using GameGUI;
using Managers;
using UnityEngine;

namespace Controllers
{
    public class CameraController : MonoBehaviour
    {
        [Header("Camera Movement Settings : ")]
        [SerializeField] private float baseSpeed = 10f;
        [SerializeField] private float minZoom = 1f;
        [SerializeField] private float maxZoom = 10f;

        private Camera _camera;
        private Vector3 lastMousePosition;

        private void Start()
        {
            _camera = Camera.main;

            // This directly influences the size of the map, therefore we need a scaler for the zoom
            maxZoom *= StaticGameSettings.Get<float>("PlayersAmount") * .25f;
        }

        public void SnapToPlayer(PlayerController player)
        {
            transform.position = ClampCameraPosition(new Vector3(player.transform.position.x, player.transform.position.y, transform.position.z));
        }

        private void Update()
        {
            if (GameMenuActions.isPaused)
                return;

            HandleKeyboardMovement();
            HandleMouseDrag();
            HandleZoom();

            // Clamp the position once after all inputs
            transform.position = ClampCameraPosition(transform.position);
        }

        private void HandleKeyboardMovement()
        {
            Vector3 direction = Vector3.zero;

            if (Input.GetKey(KeyCode.W)) direction += Vector3.up;
            if (Input.GetKey(KeyCode.S)) direction += Vector3.down;
            if (Input.GetKey(KeyCode.A)) direction += Vector3.left;
            if (Input.GetKey(KeyCode.D)) direction += Vector3.right;

            float scaledSpeed = baseSpeed * (_camera.orthographicSize / maxZoom);
            transform.position += direction.normalized * (scaledSpeed * Time.deltaTime);
        }

        private void HandleMouseDrag()
        {
            if (Input.GetMouseButtonDown(1))
            {
                lastMousePosition = Input.mousePosition;
            }
            else if (Input.GetMouseButton(1))
            {
                Vector3 delta = _camera.ScreenToWorldPoint(lastMousePosition) - _camera.ScreenToWorldPoint(Input.mousePosition);
                transform.position += delta;
                lastMousePosition = Input.mousePosition;
            }
        }

        private void HandleZoom()
        {
            float scroll = Input.mouseScrollDelta.y;

            if (scroll != 0)
            {
                float newZoom = Mathf.Clamp(_camera.orthographicSize - scroll * 1f, minZoom, maxZoom);
                _camera.orthographicSize = newZoom;
            }
        }

        private Vector3 ClampCameraPosition(Vector3 position)
        {
            Vector2Int mapSize = WorldManager.mapSize;

            // Calculate the grid bounds
            float minX = -mapSize.x+.5f;
            float maxX = mapSize.x-0.5f;
            float minY = -mapSize.y+1f;
            float maxY = mapSize.y-1f;

            // Clamp the position to ensure the camera stays within the map
            float clampedX = Mathf.Clamp(position.x, minX , maxX);
            float clampedY = Mathf.Clamp(position.y, minY , maxY);

            return new Vector3(clampedX, clampedY, position.z);
        }
    }
}
