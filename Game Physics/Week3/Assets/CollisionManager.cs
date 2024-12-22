using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TMPro;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using Debug = UnityEngine.Debug;
// Datastructure for thhe SAP implementation
public struct Edge
{
    public int ObjectIndex;
    public float Xpos;
    public float Radius;
    public bool IsLeft;
    
    public Edge(int objectIndex, float _xPos, float _radius, bool _isLeft)
    {
        ObjectIndex = objectIndex;
        Xpos = _xPos;
        Radius = _radius;
        IsLeft = _isLeft;
        UpdateEdgePosX(_xPos);
    }

    public void UpdateEdgePosX(float pos){
        Xpos = IsLeft ?  pos - Radius : pos + Radius;
    }
}

public struct OverlapResult
{
    public int ObjectA;
    public int ObjectB;
}

public struct CircleData {
    public int objIndex;
    public Vector3 pos;
    public float radius;
    public Vector2 velocity;

    public CircleData(int _objIndex, Vector3 _pos, Vector2 _velocity, float _radius){
        objIndex = _objIndex;
        pos = _pos;
        radius = _radius;
        velocity = _velocity;
    }

    public void UpdatePosition(float step){
        pos += (Vector3)velocity * step;
    }
}

public enum cAlg { builtin, basic, sap , sapMulti, hash};


public class CollisionManager : MonoBehaviour
{
    [Header("Collision Test Settings :")]
    public float speed = 10f;
    public float radius = 1f;
    public int numberOfCircles = 100;
    public float bounceFactor = 1f;
    public GameObject CirclePrefab;
    public GameObject NoUniCirclePrefab;

    [Header("GUI :")]
    public DataPlotter DataCharts;
    public TMP_Text currAlg;
    public Slider nCircles;
    public TMP_Text nCircles_text;
    public Slider BounceFactor;
    public TMP_Text bounceFactor_text;


    // Data stucture for GameObjects with the CircleControllers
    public GameObject[] circleObjects;
    private NativeArray<CircleData> circleData = new();

    // Data stucture for cAlg.sap
    private NativeList<Edge> edgesArray = new();
    
    // Data stucture for cAlg.sap
    private HashSet<CircleData> touching = new ();
    private readonly List<Edge> edges = new();

    // Data stucture for cAlg.hash
    private float cellSize = 1.0f; 
    private List<CircleData> objects = new(); 
    private Dictionary<Vector2Int, List<int>> grid = new();
    private List<Vector3> positions = new();

    public static Vector2 BoxSize = new(150, 100);
    private bool randomRadius = false;

    // Used for changing "to be tested" algorithm
    static public cAlg curr = cAlg.builtin;
    static public int nCollissionChecks = 0;
    static public int currAmountOfCircles = 0;

    private Stopwatch stopwatch;
    private float[] executionTimes; // Store execution times
    private int timeTicks = 0;
    public int stopwatchInterval = 10;

    private void Start()
    {
        stopwatch = new Stopwatch();
        executionTimes = new float[stopwatchInterval]; // Initialize an array of 10 floats

        nCircles.onValueChanged.AddListener(value => nCircles_text.text = "nCircles   :   " +value.ToString());
        BounceFactor.onValueChanged.AddListener(value => bounceFactor_text.text = "bounceFactor   :   " + value.ToString());

    }

    public void RunBuiltin()                { RunTest(cAlg.builtin); }
    public void RunCustomCheckEverything()  { RunTest(cAlg.basic); }
    public void RunCustomSAP()              { RunTest(cAlg.sap); }
    public void RunCustomSAPMulti()         { RunTest(cAlg.sapMulti); }
    public void RunCustomHash()             { RunTest(cAlg.hash); }
    public void RunExponantialCircles()     { RunTest(curr, true); }
    public void ToggleRandRad()             { randomRadius = !randomRadius; }
    
    public void RunTest(cAlg col, bool gradually = false){
        // make sure the values are up to date
        numberOfCircles = (int)nCircles.value; 
        bounceFactor = (int)BounceFactor.value; 



        StopAllCoroutines();
        ClearCircles(curr);
        curr = col;
        if(col == cAlg.builtin){
            Physics2D.simulationMode = SimulationMode2D.FixedUpdate;

            SpawnCircles(CirclePrefab, curr, gradually);
            
        } 
        else 
        {
            // disable all time allocated for physics calculation
            Physics2D.simulationMode = SimulationMode2D.Script;
            SpawnCircles(NoUniCirclePrefab, curr, gradually);
        }

        // Reset data in both charts
        DataCharts.FPSChart.GetSerie(col.ToString()).ClearData();
        DataCharts.nTimeTook.GetSerie(col.ToString()).ClearData();
        DataCharts.elapsedTime = 0;
        DataCharts.StartRecording = true;

        currAlg.text = "Active Algorithm : " + curr.ToString();
    }

    void Update(){
        stopwatch.Restart();
        // update the velocity and check for box
        nCollissionChecks = 0;
        if(curr != cAlg.builtin){

            new UpdateBoxCollisionJob {
                Circles = circleData,
                BoxSize = BoxSize,
                DeltaTime = Time.deltaTime,
            }.Schedule(circleData.Length, numberOfCircles/6).Complete();
            
            // apply new positions
            for (int i = 0; i < currAmountOfCircles; i++){
                circleObjects[i].transform.position  = circleData[i].pos;
            }

            if(curr == cAlg.basic){

                // this runs in O(n^2) and is bad
                for (int i = 0; i < circleObjects.Length; i++) {
                    for (int j = i + 1; j < circleObjects.Length; j++) {

                        // Check for collicion between circles
                        if (CheckCircleCollision(circleData[i], circleData[j])) {

                            // make sure they bounce of each other
                            Bounce(i, j);
                        }
                    }
                }
            }
            else if(curr == cAlg.sap){
                InsertionSort(edges);

                foreach (var edge in edges)
                {
                    if (edge.IsLeft)
                    {
                        // Check for overlap with all objects currently in the "touching" set
                        foreach (var other in touching)
                        {
                            OnOverlapX(other.objIndex, circleData[edge.ObjectIndex].objIndex);
                        }
                        touching.Add(circleData[edge.ObjectIndex]);
                    }
                    else
                    {
                        touching.Remove(circleData[edge.ObjectIndex]);
                    }
                }
            }
            else if (curr == cAlg.hash)
            {

                // Clear grid and position cache
                grid.Clear();
                positions.Clear();

                // Populate the grid with objects
                for(int i = 0; i < objects.Count; i++)
                {
                    CircleData circle = circleData[i];
                    Vector3 position = circle.pos;
                    positions.Add(position);

                    Vector2Int cell = GetGridCell(position);
                    if (!grid.TryGetValue(cell, out var objectList))
                    {
                        objectList = new List<int>();
                        grid[cell] = objectList;
                    }

                    objectList.Add(i);
                }

                // Check collisions
                for (int i = 0; i < objects.Count; i++)
                {
                    CircleData circle = circleData[i];
                    Vector2Int cell = GetGridCell(positions[i]);

                    nCollissionChecks++;
                    CheckCollisions(circle.objIndex, cell);
                }
            }
            if(curr == cAlg.sapMulti){

                NativeArray<bool> touchingFlags = new(circleObjects.Length, Allocator.TempJob);
                NativeList<OverlapResult> overlaps = new(edgesArray.Length, Allocator.TempJob);

                new UpdateEdgePositionsJob {
                    CirclePositions = circleData,
                    Edges = edgesArray
                }.Schedule(edgesArray.Length, numberOfCircles/6).Complete();
                

                // wait for the UpdateEdges to complete
                new InsertionSortJob {
                    Edges = edgesArray,
                }.Schedule().Complete();
                

                // wait for the SortJoob to complete
                new CollisionDetectionSAPJob {
                    Edges = edgesArray,
                    cData = circleData,
                    Overlaps = overlaps.AsParallelWriter(),
                    TouchingFlags = touchingFlags
                }.Schedule(edgesArray.Length, 64).Complete();


                // After the job completes, handle the detected overlaps
                foreach (var overlap in overlaps)
                {
                    Bounce(overlap.ObjectA, overlap.ObjectB);
                }

                touchingFlags.Dispose();
                overlaps.Dispose();
            }
        }

        if(currAmountOfCircles < numberOfCircles && DataCharts.StartRecording){
            // Stop measuring time and record the result
            stopwatch.Stop();
            executionTimes[timeTicks] = (float)stopwatch.Elapsed.TotalMilliseconds;
            timeTicks++;

            if(timeTicks >= stopwatchInterval){
                // this will automatically fetch the current amount of circles in the scene
                DataCharts.UpdateTimePerAmount(executionTimes.Average());
                timeTicks = 0;
            }
        }
    }

    public void SpawnCircles(GameObject circlePrefab, cAlg testAlg, bool gradually)
    {
        if (testAlg == cAlg.sap || testAlg == cAlg.sapMulti)
            edgesArray = new NativeList<Edge>(numberOfCircles*2, Allocator.Persistent);

        circleData = new NativeArray<CircleData>(numberOfCircles, Allocator.Persistent);
        circleObjects = new GameObject[numberOfCircles];

        if (!gradually)
        {
            // Spawn all circles at once
            for (int i = 0; i < numberOfCircles; i++)
            {
                SpawnSingleCircle(circlePrefab, testAlg, i);
            }
        }
        else
        {
            // Start gradual spawning
            StartCoroutine(GradualSpawn(circlePrefab, testAlg));
        }

    }

    private IEnumerator GradualSpawn(GameObject circlePrefab, cAlg testAlg)
    {
        for (int i = 0; i < numberOfCircles; i++)
        {
            SpawnSingleCircle(circlePrefab, testAlg, i);
            yield return .01f; // Wait for the next frame
        }
    }

    private void SpawnSingleCircle(GameObject circlePrefab, cAlg testAlg, int index)
    {
        // Generate random values
        float randRadius = randomRadius ? Random.Range(0.1f, 4f) : radius;
        Vector2 randDirection = Random.insideUnitCircle * speed;
        Vector2 randPos = new Vector2(Random.Range(0, BoxSize.x), Random.Range(0, BoxSize.y));

        // Create the circle object
        GameObject newCircle = Instantiate(circlePrefab, randPos, Quaternion.identity);
        newCircle.transform.localScale = Vector3.one * 2 * randRadius;

        // Enable Unity components
        if (testAlg == cAlg.builtin){
            newCircle.GetComponent<Rigidbody2D>().velocity = randDirection;
            newCircle.GetComponent<Rigidbody2D>().sharedMaterial.bounciness = BounceFactor.value;
        }

        // Add to algorithm-specific collections
        switch (testAlg)
        {
            case cAlg.sap:
                edges.Add(new Edge(index, randPos.x, randRadius, true)); // left
                edges.Add(new Edge(index, randPos.x, randRadius, false)); // right
                break;
            case cAlg.sapMulti:
                edgesArray.Add(new Edge(index, randPos.x, randRadius, true)); // left
                edgesArray.Add(new Edge(index, randPos.x, randRadius, false)); // right
                break;
            case cAlg.hash:
                objects.Add(new CircleData(index, randPos, randDirection, randRadius));
                break;
        }

        // Store data
        circleData[index] = new CircleData(index, randPos, randDirection, randRadius);
        circleObjects[index] = newCircle;
        currAmountOfCircles++;
    }

    public void ClearCircles(cAlg testedAlg)
    {   
        switch(testedAlg){
            case cAlg.sap: 
                edges.Clear();
                touching.Clear(); 
                break;
            case cAlg.sapMulti: 
                edges.Clear();
                touching.Clear(); 
                circleData.Dispose();
                edgesArray.Dispose(); 
                break;
            case cAlg.hash: 
                objects.Clear();
                break;
        }
        foreach(var circle in circleObjects){
            Destroy(circle);
        }
        currAmountOfCircles = 0;
    }
    
    /*******************************************************************************
        Basic (naive) 
            Implementation of the basic Collision detection algorithm
    *******************************************************************************/
    private bool CheckCircleCollision(CircleData circle1, CircleData circle2){
        float distanceSquared = (circle1.pos - circle2.pos).sqrMagnitude;
        float radius = circle1.radius + circle2.radius;
        float radiusSq = radius * radius;
        return distanceSquared <= radiusSq;
    }   

    /*******************************************************************************
        Sweep and Prune 
            Implementation of the Sweep and Prune Collision detection algorithm
    *******************************************************************************/
    private void OnOverlapX(int c1, int c2){
        CircleData circle1 = circleData[c1];
        CircleData circle2 = circleData[c2];
        if(Vector2.Distance(circle1.pos, circle2.pos) < circle1.radius + circle2.radius){
            Bounce(c1, c2);
        }
    }

    // Insertion Sort for a List of Edge objects
    public void InsertionSort(List<Edge> edges)
    {
        for (int i = 1; i < edges.Count; i++)
        {
            for (int j = i - 1; j >= 0; j--)
            {
                // Compare the 'x' property of adjacent edges
                if (edges[j].Xpos < edges[j + 1].Xpos)
                {
                    break;
                }

                // Swap edges[j] and edges[j + 1] if necessary
                var temp = edges[j];
                edges[j] = edges[j + 1];
                edges[j + 1] = temp;
            }
        }
    }
    /*******************************************************************************
        Grid Hashing 
        Implementation of the Grid Hashing Collision detection algorithm
    *******************************************************************************/

    // Convert world position to grid cell
    Vector2Int GetGridCell(Vector3 position)
    {
        return new Vector2Int(
            Mathf.FloorToInt(position.x / cellSize),
            Mathf.FloorToInt(position.y / cellSize)
        );
    }

    // Check collisions for an object in its current cell and neighboring cells
    void CheckCollisions(int circleIndex, Vector2Int cell)
    {
        CircleData circle = circleData[circleIndex];

        foreach (Vector2Int neighborCell in GetNeighboringCells(cell))
        {
            if (grid.TryGetValue(neighborCell, out var objectList))
            {
                foreach (var otherCircleIndex in objectList)
                {
                    // Retrieve the other circle data
                    CircleData otherCircle = circleData[otherCircleIndex];

                    // Avoid duplicate checks and self-checks
                    if (circleIndex >= otherCircleIndex)
                        continue;

                    // Check for collision between the current circle and the other circle
                    if (CheckCircleCollision(circle, otherCircle))
                    {
                        // Apply the collision response (bounce)
                        Bounce(circle.objIndex, otherCircle.objIndex);

                        // Update circle data after the bounce
                        circleData[circleIndex] = circle;
                        circleData[otherCircleIndex] = otherCircle;
                    }
                }
            }
        }
    }

    // Get neighboring cells (including the current cell)
    IEnumerable<Vector2Int> GetNeighboringCells(Vector2Int cell)
    {
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                yield return new Vector2Int(cell.x + x, cell.y + y);
            }
        }
    }


    /*******************************************************************************
        Sweep and Prune (Optimized)
            Implementation of the Sweep and Prune Collision detection algorithm
    *******************************************************************************/
    [BurstCompile]
    public struct UpdateBoxCollisionJob : IJobParallelFor
    {
        public NativeArray<CircleData> Circles;
        public Vector2 BoxSize;
        public float DeltaTime;

        public void Execute(int index)
        {
            // Update position based on velocity
            CircleData circle = Circles[index]; // Create a copy of the struct
            circle.UpdatePosition(DeltaTime);

            // Resolve box collision
            if (circle.pos.x - circle.radius < 0 || circle.pos.x + circle.radius > BoxSize.x)
            {
                circle.velocity.x *= -1;
                circle.pos.x = Mathf.Clamp(circle.pos.x, circle.radius, BoxSize.x - circle.radius);
            }

            if (circle.pos.y - circle.radius < 0 || circle.pos.y + circle.radius > BoxSize.y)
            {
                circle.velocity.y *= -1;
                circle.pos.y = Mathf.Clamp(circle.pos.y, circle.radius, BoxSize.y - circle.radius);
            }

            Circles[index] = circle; // Update the array by setting the whole struct back
        }
    } 

    [BurstCompile]
    public struct UpdateEdgePositionsJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<CircleData> CirclePositions; // Positions of all objects
        public NativeArray<Edge> Edges; // Edges to update

        public void Execute(int i)
        {
            Edge edge = Edges[i]; // Get the current edge
            float circleXPos = CirclePositions[edge.ObjectIndex].pos.x; // Get the circle's x position
            edge.UpdateEdgePosX(circleXPos); // Update the edge's Xpos
            Edges[i] = edge; // Write back the updated edge
        }
    }

    [BurstCompile]
    public struct InsertionSortJob : IJob
    {
        public NativeArray<Edge> Edges;

        public void Execute()
        {
            for (int i = 1; i < Edges.Length; i++)
            {
                Edge key = Edges[i];
                int j = i - 1;

                while (j >= 0 && Edges[j].Xpos > key.Xpos)
                {
                    Edges[j + 1] = Edges[j];
                    j--;
                }
                Edges[j + 1] = key;
            }
        }
    }

    [BurstCompile]
    public struct CollisionDetectionSAPJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Edge> Edges;
        [ReadOnly] public NativeArray<CircleData> cData;
        public NativeList<OverlapResult>.ParallelWriter Overlaps; 
        [NativeDisableParallelForRestriction]
        public NativeArray<bool> TouchingFlags;

        public void Execute(int index)
        {
            var edge = Edges[index];
            if (edge.IsLeft)
            {
                // Check for overlaps with currently "touching" objects
                for (int i = 0; i < TouchingFlags.Length; i++)
                {
                    if (TouchingFlags[i] && i != edge.ObjectIndex) // If touching and not the same object
                    {
                        // Compute overlap using positions and radii
                        Vector2 pos1 = cData[edge.ObjectIndex].pos;
                        Vector2 pos2 = cData[i].pos;
                        float combinedRadius = cData[edge.ObjectIndex].radius +cData[i].radius;

                        if (Vector2.Distance(pos1, pos2) < combinedRadius)
                        {
                            Overlaps.AddNoResize(new OverlapResult
                            {
                                ObjectA = edge.ObjectIndex,
                                ObjectB = i
                            });
                        }
                    }
                }
                // Mark this object as "touching"
                TouchingFlags[edge.ObjectIndex] = true;
            }
            else
            {
                // Mark this object as "not touching"
                TouchingFlags[edge.ObjectIndex] = false;
            }
        }
    }

    /*******************************************************************************
        Bounce implementation 
            Implementation of "bounce" mechanic for circles
    *******************************************************************************/
    public void Bounce(int indexA, int indexB)
    {
        CircleData circle1 = circleData[indexA];
        CircleData circle2 = circleData[indexB];

        // Calculate the normal and relative velocity
        Vector2 normal = (circle1.pos - circle2.pos).normalized;
        Vector2 relativeVelocity = circle1.velocity - circle2.velocity;

        // Calculate the relative velocity along the normal
        float velocityAlongNormal = Vector2.Dot(relativeVelocity, normal);

        // Only apply a response if objects are moving towards each other
        if (velocityAlongNormal < 0)
        {
            float impulse = -(1 + bounceFactor) * velocityAlongNormal;
            impulse /= (1 / circle1.radius + 1 / circle2.radius);

            // Apply the impulse
            circle1.velocity += impulse * normal / circle1.radius;
            circle2.velocity -= impulse * normal / circle2.radius;
        }
        
        // Write the modified data back into the array
        circleData[indexA] = circle1;
        circleData[indexB] = circle2;
    }

    void OnDestroy(){
        circleData.Dispose();
        edgesArray.Dispose();
    }
}
