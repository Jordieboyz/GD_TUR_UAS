using System.Timers;
using TMPro;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using XCharts.Runtime;

public class SailboatForces : MonoBehaviour
{
    public LineChart linechart;
    public TMP_Text text;
    public Rigidbody hullBody;
    public Rigidbody keelBody;

    public GameObject rudder;
    public GameObject windObj;
    public GameObject headSail;
    public GameObject giek;
    public GameObject mainSail;

    public float windSpeed = 6;

    private float[] headSailAngleToLiftCoeficient;
    private float[] headSailAngleToDragCoeficient;
    private float[] mainSailAngleToLiftCoeficient;
    private float[] mainSailAngleToDragCoeficient;

    const float waterRho = 1030.0f; //salt water density
    const float airRho = 1.2f; //air density
    
    const float mainSailAreaM2 = 20.0f; // Width * Height / 2
    const float headSailAreaM2 = 16.0f; // Width * Height / 2
    const float underwaterSurface = 18.5f; //also known as Wetted surface
    const float underwaterVolume = 3.3f; //also known as Displacement
    const float shipLenght = 8.3f; // Water Line Length

    const int maxSailAngle = 80;

    public void RotateRudder(float angle){
        hullBody.AddForceAtPosition(transform.right * (angle >= 0 ? 100 : -100) * GetVelocity().magnitude , rudder.transform.position, ForceMode.Force);
    }

    public void RotateHeadSail(float angle){
        RotateSail(headSail, angle);
    }

    public void RotateMainSail(float angle){
        RotateSail(mainSail, angle);
        RotateSail(giek, angle);
    }

    public Vector3 GetVelocity(){
        return hullBody.velocity;
    }

    Vector3 GetApparentWindVector() {
        return windObj.transform.forward * windSpeed - hullBody.velocity;
    }

    void Start(){
        linechart.ClearComponentData();
    }
    void FixedUpdate() {
        Vector3 apparentWind = GetApparentWindVector();

        addSailForce(headSail, apparentWind, headSailAreaM2);
        addSailForce(mainSail, apparentWind, mainSailAreaM2);
        addSailForce(giek, apparentWind, mainSailAreaM2);

        hullBody.AddForce(-GetVelocity().normalized * calcualteFrictionalForce());
        hullBody.AddForce(-GetVelocity().normalized * calculateResidualForce());

        addHullDragForce();        
        addForceToKeel();

        Debug.Log(hullBody.GetComponent<Rigidbody>().velocity);
    
    }
    void RotateSail(GameObject sail, float angle) {
        JointLimits newlimits = sail.GetComponent<HingeJoint>().limits;

        if(angle < 0 && Mathf.Abs(newlimits.min) < Mathf.Abs(maxSailAngle)){
            newlimits.min += (int)angle;
            newlimits.max = (int)newlimits.min + 30;
        } else if (angle >= 0 && Mathf.Abs(newlimits.max) < Mathf.Abs(maxSailAngle)) {
            newlimits.max += (int)angle;
            newlimits.min = newlimits.max - 30;
        }
        sail.GetComponent<HingeJoint>().limits = newlimits;
    }

    void addSailForce(GameObject sail, Vector3 apparentWind, float sailAreaM2) {
        Vector3 sailVector = sail.transform.forward;
        float windVelocity = apparentWind.magnitude;
        
        int sailAppraentAngleGrad = (int)Vector3.Angle(sail.transform.forward, -apparentWind);
        float liftCoefficient = CalculateLiftCoefficient(sailAppraentAngleGrad);
        float dragCoefficient = CalculateDragCoefficient(sailAppraentAngleGrad);        
        Vector3 liftForceDirection = calculateLiftDirection(apparentWind, sailVector);
        Vector3 liftForce = liftForceDirection * calculateSailForce(liftCoefficient, windVelocity, sailAreaM2);
        Vector3 dragForce = apparentWind.normalized * calculateSailForce(dragCoefficient, windVelocity, sailAreaM2);
        Vector3 resultForce = liftForce + dragForce;
        
        Rigidbody sailRb = sail.GetComponent<Rigidbody>();       
        sailRb.AddForce(resultForce);
    }

    Vector3 calculateLiftDirection(Vector3 apparentWind, Vector3 sailVector){
        float liftAngle = Vector3.SignedAngle(-apparentWind.normalized, sailVector.normalized, Vector3.up);
        if(Mathf.Abs(liftAngle) < 180){
            liftAngle = -90 * Mathf.Sign(liftAngle);
        } else {
            liftAngle = 90 * Mathf.Sign(liftAngle);
        }
        Vector3 liftForceDirection = rotateVectorByDegree(apparentWind, liftAngle);
        return liftForceDirection;
    }

    Vector3 rotateVectorByDegree(Vector3 origVector, float degree) {
        Vector3 rotatedVector = Quaternion.AngleAxis(degree, Vector3.up) * origVector.normalized;
        return rotatedVector;
    }

    //Keel force is opposite to boat side drag force caused by wind, but keel force is not the same as hull force
    //Beceause keel works kike a wing inder water
    //Keel force has also lift effect, thus part of lift has forward vector (because of small side drag)
    void addForceToKeel() {
        float sideSpeed = Vector3.Dot(GetVelocity(), transform.right);
        float rightAngle = -90 * Mathf.Sign(sideSpeed);
        Vector3 liftUnderwaterDirection = rotateVectorByDegree(GetVelocity(), rightAngle);
        float antiDargCoeficient = 2f; //TODO
        keelBody.AddForce(liftUnderwaterDirection * sideSpeed * sideSpeed * waterRho * antiDargCoeficient);
    }

    // TODO Hull has significant anti drag effect. But I don't have formaula for it yet.
    // It is similar to keel force but has no lift effect and influences whole yacht body
    void addHullDragForce() {
        float sideSpeed = Vector3.Dot(GetVelocity(), transform.right);
        float dargCoeficient = 0.25f; //TODO
        hullBody.AddForce(-GetVelocity().normalized * sideSpeed * sideSpeed * waterRho * dargCoeficient);
    }

    float calcualteFrictionalForce() {
        // Ffr = 1/2 * rho * V * V * S * Cf
        float Cf = 0.004f;
        // S = Cws * Sqrt( Vudw * Len )
        float S = 2.6f * Mathf.Sqrt(underwaterVolume * shipLenght);
        float Ffr = 0.5f * waterRho * Mathf.Pow(GetVelocity().magnitude, 2) * S * Cf;
        return Ffr;
    }

    float calculateResidualForce() {
        // Fr = 1/2 * rho * V * V * S * Cr
        float Cr = 2 * Mathf.Exp(-3);
        float Fr = 0.5f * waterRho * Mathf.Pow(GetVelocity().magnitude, 2) * Cr;
        return Fr;
    }

    float calculateSailForce(float coeficient, float velocity, float sailArea) {
        // Fs = 1/2 * rho * V * V * S * C
        float Fr = 0.5f * airRho * velocity * velocity * sailArea * coeficient;
        return Fr;
    }

    float CalculateLiftCoefficient(float angle)
    {
        // Example model: Lift coefficient peaks at 45° and drops symmetrically.
        float angleRad = Mathf.Deg2Rad * angle;
        return Mathf.Sin(2 * angleRad);
    }

    float CalculateDragCoefficient(float angle)
    {
        // Example model: Drag coefficient increases towards 90°.
        float angleRad = Mathf.Deg2Rad * angle;
        return Mathf.Abs(Mathf.Sin(angleRad));
    }

    float InterpolateCoefficient(float[] coefficients, float angle)
    {
        int lowerIndex = Mathf.FloorToInt(angle);
        int upperIndex = Mathf.CeilToInt(angle);

        if (lowerIndex < 0) lowerIndex = 0;
        if (upperIndex >= coefficients.Length) upperIndex = coefficients.Length - 1;

        return Mathf.Lerp(coefficients[lowerIndex], coefficients[upperIndex], angle - lowerIndex);
    }       

    public float getHeadSailLiftCoeficientAtAngle(int angle){
        return InterpolateCoefficient(headSailAngleToLiftCoeficient, angle);
    }
    public float getMainSailLiftCoeficientAtAngle(int angle){
        return InterpolateCoefficient(mainSailAngleToLiftCoeficient, angle);
    }
    public float getHeadSailDragCoeficientAtAngle(int angle){
        return InterpolateCoefficient(headSailAngleToDragCoeficient, angle);
    }
    public float getMainSailDragCoeficientAtAngle(int angle){
        return InterpolateCoefficient(mainSailAngleToDragCoeficient, angle);
    }
}
