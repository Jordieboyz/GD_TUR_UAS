
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Structure to keep track of all the carriages.
public struct CabinManager {
    public GameObject[] carriages;
    public int minCabins; 
    public int maxCabins;
    public int nCabins;

    public CabinManager(int min, int max){
        carriages = new GameObject[max];
        minCabins = min;
        maxCabins = max;
        nCabins = minCabins;
    }
} 

public class StaticUtils {
    // Generic Scaler functions
    static public void SetScaleForOject(GameObject obj, float scale){
        obj.transform.localScale = new Vector3(scale, scale, scale);
    }

    static public void SetScaleForOject(GameObject[] objs, float scale){
        foreach(var ob in objs){
            if(ob != null){
                ob.transform.localScale = new Vector3(scale, scale, scale);
            }
        }
    }

    // Convert a polar coordinate to a caartesian coordinate.
    static public Vector3 ConvertPolarToCartesian(Vector2 r, float angleInDegrees){
        return new Vector3(r.x*Mathf.Cos(Mathf.Deg2Rad*angleInDegrees),  r.y*Mathf.Sin(Mathf.Deg2Rad*angleInDegrees), 0);
    }
}


public class UI_handler : MonoBehaviour {
    
    public Slider WheelScaleSlider;
    public Slider CabinScaleSlider;
    public Slider WheelHeightSlider;
    public TMP_InputField AmountOfCarriagesIField;
    public Button spinToggle;

    public GameObject wheel;
    public GameObject cabinPrefab;

    private CabinManager cabins  = new(1,20);

    private void Awake(){
        CabinScaleSlider.onValueChanged.AddListener(    delegate { SetCabinScale();     });
        WheelScaleSlider.onValueChanged.AddListener(         delegate { SetWheelScale();    });
        WheelHeightSlider.onValueChanged.AddListener(   delegate { SetWheelHeight(); });
        AmountOfCarriagesIField.onValueChanged.AddListener(     delegate { InstantiateCabins(); });
        spinToggle.onClick.AddListener(                 delegate { ToggleSpinning();    });
    }

    // Toggle the rotation of the ferris wheel
    private void ToggleSpinning(){
        FerrisRotation.isSpinning = !FerrisRotation.isSpinning;
        
        // Toggle the GUI to prevent changes during the ferrisWheel spin. 
        if(FerrisRotation.isSpinning){
            this.gameObject.SetActive(false);
            spinToggle.gameObject.SetActive(true);
        } else {
            this.gameObject.SetActive(true);
        }
    }

    private void SetWheelHeight(){
        // We need a minimum height to prevent touching the groun
        float minHeight = 15f;

        // Set the height of the wheel 
        wheel.transform.position = new Vector3(wheel.transform.position.x, Mathf.Abs(Mathf.Max(WheelHeightSlider.value, minHeight)), wheel.transform.position.z);
        
        // calulate and set scale relative to the delta of the wheel transformation on the Y axis for a better visual representation.
        float newScale = wheel.transform.position.y/minHeight;
        StaticUtils.SetScaleForOject(GameObject.Find("ferrisTower"), newScale);
        StaticUtils.SetScaleForOject(GameObject.Find("ground"), newScale);
         
    }

    private void SetWheelScale(){
        StaticUtils.SetScaleForOject(wheel, WheelScaleSlider.value/10);
    }

    // We never want to change only one cabin, so using a scaler for all of them is fine for now.
    private void SetCabinScale(){
        StaticUtils.SetScaleForOject(cabins.carriages, CabinScaleSlider.value/10);
    }

    // Instantiate the carriages on the wheel by calculating angles and using the half distance of the wheel
    private void InstantiateCabins(){        
        // Try to parse the amound of carriages and clamp the value between min and max amount of cabins, specified on top of this script.
        cabins.nCabins = !int.TryParse(AmountOfCarriagesIField.text, out int parsed) ? cabins.minCabins : Mathf.Clamp(parsed, cabins.minCabins, cabins.maxCabins);

        // retrieve the width of the wheel and caclulating a baseAngleOffset by calculating the angle based on the amount of carriages.
        // This only works, because we don't care about the Z axis in this project.
        Vector2 r = wheel.GetComponent<SphereCollider>().bounds.extents;
        float baseAngle = Mathf.PI*2*Mathf.Rad2Deg/cabins.nCabins;

        // Instantiate all the carriages witht heir cartesian coordinates converted from the polar coordinates we gradually calculat.
        for(int i = 0; i < cabins.maxCabins; i++){
            Destroy(cabins.carriages[i]);
            if(i < cabins.nCabins){
                cabins.carriages[i] = Instantiate(cabinPrefab, StaticUtils.ConvertPolarToCartesian(r, baseAngle*i)+wheel.transform.position, Quaternion.identity);
                cabins.carriages[i].SetActive(true);
            }
        }
        SetCabinScale();
    }
}

