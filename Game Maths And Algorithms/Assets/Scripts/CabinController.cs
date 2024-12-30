using UnityEngine;

public class CabinController : MonoBehaviour
{
    public GameObject Tower;
    private SoftParent sParent;

    private void Start() {
        sParent = GetComponent<SoftParent>();
    }

    // Update is called once per frame
    private void Update() {
        if(FerrisRotation.isSpinning == true){
            sParent.relativeRot = Quaternion.Inverse(sParent.parent.transform.rotation) * Tower.transform.rotation;
        }
    }
}
