using UnityEngine;

public class YachtsManager : MonoBehaviour
{
    public SailboatForces yachtControls;

    [Header("Boat Controls : ")]
    public float RudderRotateStep = 1f;
    public float MainSailRotateStep = 1f;
    public float HeadSailRotateStep = 1f;

    void Update() {
        if (Input.GetKey(KeyCode.A)){
            yachtControls.RotateRudder(RudderRotateStep);
        }

        if (Input.GetKey(KeyCode.D)) {
            yachtControls.RotateRudder(-RudderRotateStep);
        }

        if (Input.GetKey(KeyCode.Q)) {
            yachtControls.RotateHeadSail(MainSailRotateStep);
        }

        if (Input.GetKey(KeyCode.E)) {
            yachtControls.RotateHeadSail(-MainSailRotateStep);
        }

        if (Input.GetKey(KeyCode.Z)) {
            yachtControls.RotateMainSail(HeadSailRotateStep);
        }

        if (Input.GetKey(KeyCode.C)) {
            yachtControls.RotateMainSail(-HeadSailRotateStep);
        }
    }
}
