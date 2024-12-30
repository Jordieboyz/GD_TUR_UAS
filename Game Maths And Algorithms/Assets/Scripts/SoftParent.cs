using UnityEngine;

public class SoftParent : MonoBehaviour
{
    public GameObject parent;
    public Vector3 relativePos;
    public Quaternion relativeRot;

    private void Start() {
        relativePos = parent.transform.InverseTransformPoint(transform.position);
        relativeRot = Quaternion.Inverse(parent.transform.rotation) * transform.rotation;
    }

    private void Update() {
        transform.SetPositionAndRotation(parent.transform.TransformPoint(relativePos), parent.transform.rotation * relativeRot);
    }
}
