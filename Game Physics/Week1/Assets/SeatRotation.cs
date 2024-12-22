
using System;
using TMPro;
using UnityEngine;
using XCharts.Runtime;


public class swingerRotation : MonoBehaviour
{   
    public LineChart EnergyChart;
    public LineChart HeightChart;
    public TMP_Text text;

    public HingeJoint hingeJoint1;
    // public HingeJoint hingeJoint2;
    private LineRenderer lineRenderer;
    public GameObject topBar;
    public Rigidbody pusher;
    public Rigidbody swinger;
    
    [SerializeField] bool hasPusher = false;
    [SerializeField] private float baseForce = 150;
    [SerializeField] private float torqueAmount = 5f;
    [SerializeField] private float pushForce = 10f;
    private float TEmin;
    private float Fw;
    private bool DEBUG = false;
    private float PEmin;
    private float localVelX = 0;
    private float lastUpdatelocalVelX = 0;
    private float pusherMass;
    private float swingerMass;
    [SerializeField] private bool forceAllowed = false;
    private float forceStartingAngle;
    private bool swingMotionForward;
    private float baseHeight = .5f;
    private float radius = 2.5f;


    // Timer for updating the chart
    private float chartUpdateTimer = 0f;
    private const float chartUpdateInterval = 0.01f; // interval of 0.01 seconds
    
    
    // Potetntial Energy: m * g * h        
    static Func<float, float, float> PotentialEnergy = (m, h) => m *  Physics.gravity.magnitude * h;

    // Potetntial Energy: 0.5f * m * v^2        
    static Func<float, float, float> KineticEnergy = (m, v) => .5f * m * v*v;

    ///  Total Energy: Potential Energy + Kinetic
    static Func<float, float, float, float> TotalEnergy = (m, v, h) => PotentialEnergy(m,h) + KineticEnergy(m,v);

    void Start(){
        // reset Energy Chart
        EnergyChart.ClearData();
        HeightChart.ClearData();

        // the minimum energy we would need to get enough speed to get over
        // theoretically with cetripetal force intact
        TEmin = TotalEnergy(swinger.mass, Mathf.Sqrt(Physics.gravity.magnitude * radius), 2*radius);
        
        // Minimal energy needed to rotate around center of mass 
        PEmin = PotentialEnergy(swinger.mass,2*radius);



        // calculate theoretical work force to generate needed amount of kinetic force 
        float arcLength = (315-225) * Mathf.Deg2Rad * radius;
        // work force = KE / distance 
        float Fw =  TEmin / arcLength;

        EnergyChart.SetSerieActive(0,false);
        EnergyChart.SetSerieActive(2,false);
        EnergyChart.SetSerieActive(3,false);
        EnergyChart.SetSerieActive(4,false);

        HeightChart.SetSerieActive(0, false);


        pusherMass = pusher.mass;
        swingerMass = swinger.mass;
        lineRenderer = swinger.GetComponent<LineRenderer>();
        lineRenderer.positionCount = 4;

        // Get the simulaton going
        swinger.AddForce(Vector3.right * baseForce * 10, ForceMode.Force);


    }
    private float recordStartKE = 0;


    private void FixedUpdate(){
        // we only care about forwards/backwards motion
        localVelX = swinger.transform.InverseTransformDirection(swinger.velocity).x;
        float swingerAngle = swinger.rotation.eulerAngles.z;

        // change in direction forwards -> backwards or vice verca
        if(lastUpdatelocalVelX < 0 && localVelX >= 0){
            forceStartingAngle = swingerAngle;
            swingMotionForward = true;
            forceAllowed = true;
        } 
        if (lastUpdatelocalVelX > 0 && localVelX <= 0){
            swingMotionForward = false;
            forceAllowed = false;
        }


        // Apply torque and force based on swing direction
        if(swingerAngle < 90 || swingerAngle > 270){
            if(swingMotionForward){
                swinger.AddTorque(Vector3.forward * torqueAmount, ForceMode.Force);
                swinger.AddForce(Vector3.right * pushForce, ForceMode.Force);
            } else {
                swinger.AddTorque(Vector3.back * torqueAmount, ForceMode.Force);
                swinger.AddForce(Vector3.left * pushForce, ForceMode.Force);
            }
        }

        // Apply a force to simulate someone pushing the swinger
        if(hasPusher == true){
            if(forceAllowed && swingerAngle > 225 && swingerAngle < 315)
            {
                float forceToApply = baseForce;

                if(swingerAngle >= 270 && swingerAngle < 315)
                {
                    swinger.mass = swingerMass;
                    forceToApply = baseForce;
                } 
                else if(swingerAngle >= 225 && swingerAngle < 270)
                {
                    swinger.mass = swingerMass + pusherMass*.25f;
                    forceToApply -= pusherMass * Physics.gravity.magnitude;
                } 
                else {
                    swinger.mass = swingerMass;
                }
                swinger.AddForce(Vector3.right * forceToApply, ForceMode.Force);

            } 

        }
        lastUpdatelocalVelX = swinger.transform.InverseTransformDirection(swinger.velocity).x;

        chartUpdateTimer += Time.fixedDeltaTime;
        if (chartUpdateTimer >= chartUpdateInterval) {
            UpdateChart();
            chartUpdateTimer = 0f; // Reset the timer
        }
        
        if(Input.GetKeyDown(KeyCode.R)){
            DEBUG= !DEBUG;
        }



        if(Input.GetKeyDown(KeyCode.E)){
            EnergyChart.SetSerieActive(0,true);
            EnergyChart.SetSerieActive(2,true);
            EnergyChart.SetSerieActive(3,true);
            EnergyChart.SetSerieActive(4,true);

        } else if(Input.GetKeyDown(KeyCode.D)){
            EnergyChart.SetSerieActive(0,false);
            EnergyChart.SetSerieActive(2,false);
            EnergyChart.SetSerieActive(3,false);
            EnergyChart.SetSerieActive(4,false);

        }
        // DrawLine();
    }


    private void UpdateChart(){
        float Ep = PotentialEnergy(swingerMass,swinger.transform.position.y-baseHeight);
        float Ek = KineticEnergy(swingerMass, lastUpdatelocalVelX);
        recordStartKE = Mathf.Max(Ek, recordStartKE);


        EnergyChart.AddData(2, Ep);
        EnergyChart.AddData(3, Ek);
        EnergyChart.AddData(1, Ep+Ek);
        EnergyChart.AddData(4, PEmin);
        EnergyChart.AddData(0, TEmin);

        HeightChart.AddData(0, 5f);
        HeightChart.AddData(1, swinger.transform.position.y-baseHeight);

        text.text = "Highest Recorded Kinetic Energy: " + recordStartKE;

        if(DEBUG){
            Debug.Log("Highest Ek: " + recordStartKE);
            Debug.Log("Current Ep: " + Ep);
            Debug.Log("Current Ek: " + Ek);
        }

        // // distance travelled before reaching bottom point where kinetic energy is at the highest
        // float arcLength = (360-forceStartingAngle) * Mathf.Deg2Rad * 2.5f;

        // // work force = KE / distance 
        // float usedForce =  Ek / arcLength;


        // Debug.Log("KE= " + KE + " and neccessary force during built up: " + usedForce);

        // // Plot the theoretical height against real hegith against each other
        // float maxHeight = topBar.transform.position.y-.5f*2;
        // float height = swinger.transform.position.y;
    }

    private void DrawLine(){
        lineRenderer.SetPosition(0, new Vector3(0, 0,hingeJoint1.anchor.z));
        lineRenderer.SetPosition(1, hingeJoint1.anchor);
        // lineRenderer.SetPosition(2, hingeJoint2.anchor);
        // lineRenderer.SetPosition(3, new Vector3(0, 0, hingeJoint2.anchor.z));
    }
}
