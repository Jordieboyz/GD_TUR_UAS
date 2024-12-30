using UnityEngine;

public class AdjustCamera : MonoBehaviour
{
    public Transform target;       // The object the camera will follow
    public Vector3 offset = new Vector3(-25, 10, -70);         // Offset position relative to the target
    public float smoothSpeed = 0.125f; // Speed of the camera movement

    public float scaleMultiplier = 1f; // Multiplier to adjust the effect of scaling on offset
    private Vector3 calculatedOffset;


    void LateUpdate()
    {
        if (target != null)
        {
            // Adjust the offset based on the target's scale
            calculatedOffset = offset * (1 + (target.localScale.magnitude - 1) * scaleMultiplier);

            // Calculate the desired position with the adjusted offset
            Vector3 desiredPosition = target.position + calculatedOffset;

            // Smoothly interpolate to the desired position
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);

            // Apply the smoothed position to the camera
            transform.position = smoothedPosition;

            // Optional: Make the camera look at the target
            transform.LookAt(target);
        }
    }
}
