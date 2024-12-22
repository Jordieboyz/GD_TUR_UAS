using System.Collections.Generic;
using UnityEngine;

public class CollisionManager : MonoBehaviour
{
    
    [Header("Collision Test Settings :")]
    public float speed = 10f;
    public float radius = 1f;
    public int numberOfCircles = 100;
    public GameObject CirclePrefab;

    public DataPlotter DataCharts;

    // types of Collision Algorithm
    public enum cAlg { builtin, basic, sorted, sap ,sat, hash};

    // Data stucture for cAlg.builtin, basic and sorted
    public static List<GameObject> circles = new();

    // Data stucture for cAlg.sap
    HashSet<SphereController> touching = new ();
    // private readonly List<Edge> edges = new();

    // Data stucture for cAlg.hash
    public float cellSize = 1.0f; 
    public List<SphereController> objects = new(); 
    private Dictionary<Vector2Int, List<int>> grid = new();
    private List<Vector3> positions = new();

    static public Vector3 BoxSize = new Vector3(100, 100, 100);
    public static int nCollissionChecks = 0; 
    private bool randomRadius = false;

    // Used for changing "to be tested" algorithm
    static public cAlg curr = cAlg.builtin;

    public void RunBuiltin()                { RunTest(cAlg.builtin); }
    public void RunCustomCheckEverything()  { RunTest(cAlg.basic); }
    public void RunCustomSorted()           { RunTest(cAlg.sorted); }
    public void RunCustomSAP()              { RunTest(cAlg.sap); }
    public void RunCustomHash()             { RunTest(cAlg.hash); }
    public void ToggleRandRad()             { randomRadius = !randomRadius; }
    
    public void RunTest(cAlg col){

        ClearCircles(curr);
        curr = col;
        SpawnCircles(CirclePrefab, curr);
        
        // Reset data in both charts
        DataCharts.FPSChart.GetSerie(col.ToString()).ClearData();
        DataCharts.nCollissionChart.GetSerie(col.ToString()).ClearData();
        DataCharts.elapsedTime = 0;
        DataCharts.StartRecording = true;
    }

    public void SpawnCircles(GameObject circlePrefab, cAlg testAlg)
    {
        for (int i = 0; i < numberOfCircles; i++)
        {
            // generate Random values
            float randRadius = randomRadius ? Random.Range(0.1f, 4f) : radius;
            Vector3 randDirection = Random.insideUnitSphere * speed;
            Vector3 randPos = new(Random.Range(0, BoxSize.x), Random.Range(0, BoxSize.y), Random.Range(0, BoxSize.z));
            
            // Create Object
            GameObject newCircle = Instantiate(circlePrefab, randPos, Quaternion.identity);
            newCircle.AddComponent<SphereController>().Init(randDirection, randRadius);

            switch(testAlg){
                case cAlg.sap:
                    // edges.Add(new Edge(newCircle,true)); // left
                    // edges.Add(new Edge(newCircle,false)); // right
                    break;
                case cAlg.hash:
                    objects.Add(newCircle.GetComponent<SphereController>()); 
                    break;
                default: 
                    circles.Add(newCircle); 
                    break;
            }
        }
    }

    // make sure to destroy the gameobjects from the "previous test;
    public void ClearCircles(cAlg testedAlg)
    {   
        switch(testedAlg){
            case cAlg.sap: 
                // foreach(var c in edges){
                //     Destroy(c.Object.gameObject);
                // }
                // edges.Clear(); 
                break;
            case cAlg.hash: 
                foreach(var c in objects){
                    Destroy(c.gameObject);
                }
                objects.Clear(); 
                break;
            default: 
                foreach(var c in circles){
                    Destroy(c);
                }
                circles.Clear();
            break;
        }
    }
}
