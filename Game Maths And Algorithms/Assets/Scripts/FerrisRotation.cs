using UnityEngine;

public class FerrisRotation : MonoBehaviour
{
    public static bool isSpinning = false;

    private SoftParent sParent;
    private float rotSpeed = 20f;

    void Start(){
        sParent = GetComponent<SoftParent>();
    }

    void Update()
    {   
        if(isSpinning){
            sParent.relativeRot *= Quaternion.Euler(0,0,rotSpeed*Time.deltaTime);
        }
    }
}
