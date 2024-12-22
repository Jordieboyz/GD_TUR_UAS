using UnityEngine;
using XCharts.Runtime;


public class DataPlotter : MonoBehaviour
{
    public LineChart FPSChart;
    public LineChart nTimeTook;

    public float elapsedTime = 0f;
    public bool StartRecording = false;
    private int frameCount = 0;
    private float nextUpdate = 1f; // Update every Time.fixedDeltaTime
    private float fps = 0;


    private void Start(){
        // Initialize chart
        FPSChart.ClearData();
        nTimeTook.ClearData();

        foreach (cAlg alg in System.Enum.GetValues(typeof(cAlg)))
        {
            FPSChart.AddSerie<Line>(alg.ToString());
            nTimeTook.AddSerie<Line>(alg.ToString());
        }
    }
    
    void Update()
    {
        if(StartRecording){
        
            frameCount++;
            elapsedTime += Time.deltaTime;

            if (Time.time > nextUpdate)
            {
                // Calculate FPS
                fps = frameCount / 1f;
                frameCount = 0;
                nextUpdate += 1f;
        
                FPSChart.AddData(CollisionManager.curr.ToString(), elapsedTime, fps);
            }
        }
    }

    public void UpdateTimePerAmount(float takenTime){
        nTimeTook.AddData(CollisionManager.curr.ToString(), CollisionManager.currAmountOfCircles, takenTime);
    }
}