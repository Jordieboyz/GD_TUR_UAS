using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class WaterManager : MonoBehaviour
{
    private Mesh mesh;

    private void Awake()
    {
        mesh = GetComponent<MeshFilter>().mesh;
    }

    private void Update()
    {
        Vector3[] vertices = mesh.vertices;
        for(int i = 0; i < vertices.Length; i++)
        {
            vertices[i].y = WaveManager.GetWaweHeight(transform.position.x + vertices[i].x);
        }
        mesh.vertices = vertices;
        mesh.RecalculateNormals();
    }
}
