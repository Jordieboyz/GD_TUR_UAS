using UnityEngine;

public class FloatCube : MonoBehaviour
{
    // public Rigidbody body;
    // public float depthBeforeSubmerged = 1f;
    // public float displacementAmount = 3f;
    // public float waweHeight = 0f;

    // void FixedUpdate()
    // {
    //     // waweHeight = WaveManager.GetWaweHeight(transform.position.x);
    //     if(transform.position.y < waweHeight)
    //     {
    //         float displacementMultiplier = Mathf.Clamp01((1 - transform.position.y) / depthBeforeSubmerged ) * displacementAmount;
    //         body.AddForceAtPosition(new Vector3(0f, Mathf.Abs(Physics.gravity.y) * displacementMultiplier, 0f), transform.position, ForceMode.Acceleration);
    //     }
    // }
}
