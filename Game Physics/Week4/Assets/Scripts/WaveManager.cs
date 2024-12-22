using UnityEngine;

public class WaveManager : MonoBehaviour
{
    public static WaveManager instance;

    public float amplitude = 1f;
    public float lenght = 2f;
    public float speed = 1f;
    public float offset = 0f;

    private void Awake() {
        if (instance == null) {
            instance = this;
        } else if (instance != this) {
            Destroy(this);
        }
    }

    // Update is called once per frame
    void Update() {
        offset += Time.deltaTime * speed;
    }
    
    // create sin waves
    public static float GetWaweHeight(float _x) {
        return instance.amplitude * Mathf.Sin(_x / instance.lenght + instance.offset);
    }
}
