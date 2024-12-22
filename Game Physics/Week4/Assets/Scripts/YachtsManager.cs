using UnityEngine;
using XCharts.Runtime;

public class YachtsManager : MonoBehaviour
{
    public SailboatForces yachtControls;
    public GameObject boat;
    public LineChart linechart;

    [Header("Boat Controls : ")]
    public float RudderRotateStep = 10f;
    public float MainSailRotateStep = 1f;
    public float HeadSailRotateStep = 1f;

    private float elapsed = 0 ;
    void Update() {
        elapsed += Time.deltaTime;

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
        if(elapsed > .1){
            Debug.Log(boat.GetComponent<Rigidbody>().velocity);
            linechart.AddData(0, Time.timeSinceLevelLoad, System.Math.Round(boat.GetComponent<Rigidbody>().velocity.magnitude, 2));
            elapsed = 0;
        };
    }
}
