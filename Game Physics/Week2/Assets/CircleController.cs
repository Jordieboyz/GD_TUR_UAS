using UnityEngine;
using UnityEngine.Events;

public class SphereController : MonoBehaviour
{
    public float radius; // Radius of the sphere
    public Vector3 velocity; // Movement velocity of the sphere
    public Vector3 boxSize; // Size of the bounding box

    public UnityEvent<float> OnPositionXChanged = new();
    private float _previousX;

    public void Init(Vector3 _velocity, float _radius)
    {
        radius = _radius;
        velocity = _velocity;
        transform.localScale = Vector3.one * 2 * radius;
        _previousX = transform.position.x;
        boxSize = CollisionManager.BoxSize;
    }

    private void Update()
    {
        // Notify if the x-position has changed significantly
        if (Mathf.Abs(transform.position.x - _previousX) > Mathf.Epsilon)
        {
            OnPositionXChanged?.Invoke(transform.position.x);
            _previousX = transform.position.x;
        }

        // Update position based on velocity
        transform.position += velocity * Time.deltaTime;

        // Handle collisions with the bounding box
        ResolveBoxCollision();
    }

    /// <summary>
    /// Resolves collisions with the bounding box in 3D space.
    /// </summary>
    private void ResolveBoxCollision()
    {
        Vector3 pos = transform.position;

        // X-axis collision
        if (pos.x - radius < 0 || pos.x + radius > boxSize.x)
        {
            velocity.x *= -1;
            pos.x = Mathf.Clamp(pos.x, radius, boxSize.x - radius);
        }

        // Y-axis collision
        if (pos.y - radius < 0 || pos.y + radius > boxSize.y)
        {
            velocity.y *= -1;
            pos.y = Mathf.Clamp(pos.y, radius, boxSize.y - radius);
        }

        // Z-axis collision
        if (pos.z - radius < 0 || pos.z + radius > boxSize.z)
        {
            velocity.z *= -1;
            pos.z = Mathf.Clamp(pos.z, radius, boxSize.z - radius);
        }

        transform.position = pos;
    }
}
