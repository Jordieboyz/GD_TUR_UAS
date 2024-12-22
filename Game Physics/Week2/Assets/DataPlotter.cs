using UnityEngine;
using XCharts.Runtime;


public class DataPlotter : MonoBehaviour
{
    public LineChart FPSChart;
    public LineChart nCollissionChart;
    public float elapsedTime = 0f;
    public bool StartRecording = false;
    private int frameCount = 0;
    private float nextUpdate = 1f; // Update every Time.fixedDeltaTime
    private float fps = 0;


    private void Start(){
        // Initialize chart
        FPSChart.ClearData();
        nCollissionChart.ClearData();

        foreach (CollisionManager.cAlg alg in System.Enum.GetValues(typeof(CollisionManager.cAlg)))
        {
            FPSChart.AddSerie<Line>(alg.ToString());
            // nCollissionChart.AddSerie<Line>(alg.ToString());
        }
    }
    
    void Update()
    {
        frameCount++;
        elapsedTime += Time.deltaTime;

        if (Time.time > nextUpdate)
        {
            // Calculate FPS
            fps = frameCount / 1f;
            frameCount = 0;
            nextUpdate += 1f;

            // Plot data against time
            if(StartRecording){

                FPSChart.AddData(CollisionManager.curr.ToString(), elapsedTime, fps);
                // nCollissionChart.AddData(CollisionManager.curr.ToString(), elapsedTime, CollisionManager.nCollissionChecks);
            }
        }
    }
}