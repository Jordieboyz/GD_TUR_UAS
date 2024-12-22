using UnityEngine;
using XCharts.Runtime;

public class ChartData : MonoBehaviour
{
public Rigidbody cart;
    public GameObject loop;

    public LineChart velocityChart;
    public LineChart GforceChart;
    
    // Timer for updating the chart
    private float chartUpdateTimer = 0f;
    private const float chartUpdateInterval = 0.1f; // interval of 0.01 seconds
    private float elapsedTime = 0f;
    private float radius = 10f;

    private void Start(){
        velocityChart.ClearData();
        GforceChart.ClearData();

        velocityChart.SetSerieActive(0,true);
        GforceChart.SetSerieActive(0,true);

    }
    // Update is called once per frame
    private void FixedUpdate()
    {
        elapsedTime += Time.fixedDeltaTime;
        chartUpdateTimer += Time.fixedDeltaTime;
        // update charts ever 0.1s
        if (chartUpdateTimer >= chartUpdateInterval) {

            // Calculate the angle of the cart relative to the loop's center
            Vector3 directionToCenter = loop.transform.position - cart.transform.position;
            float theta = Vector3.Angle(directionToCenter, Vector3.up) * Mathf.Deg2Rad;

            float centripetalG = cart.velocity.magnitude * cart.velocity.magnitude / radius / Physics.gravity.magnitude;

            // Total G-force: centripetal component + gravitational component
            float G = centripetalG + Mathf.Cos(theta);

            velocityChart.AddData(0, elapsedTime, cart.velocity.magnitude);
            GforceChart.AddData(0, elapsedTime, G);

            chartUpdateTimer = 0f;
        }
    }
}
