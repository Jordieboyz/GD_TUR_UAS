using System.Linq;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    public MeshFilter meshFilter; // Assign the MeshFilter containing your water mesh.
    public Vector3 testPos;

    private void Update(){
        Debug.Log(GetWaterHeight(testPos));
    }

    /// <summary>
    /// Gets the height of the water at a given world position.
    /// </summary>
    public float GetWaterHeight(Vector3 worldPosition)
    {
        Mesh mesh = meshFilter.mesh;

        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;

        // Transform world position to local mesh position
        Vector3 localPosition = meshFilter.transform.InverseTransformPoint(worldPosition);
        Debug.Log(triangles.Count());
        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 v0 = vertices[triangles[i]];
            Vector3 v1 = vertices[triangles[i + 1]];
            Vector3 v2 = vertices[triangles[i + 2]];

            // Check if the point is inside the triangle
            if (PointInTriangle(localPosition, v0, v1, v2))
            {
                // Compute the height using barycentric interpolation
                return InterpolateHeight(localPosition, v0, v1, v2);
            }
        }

        Debug.LogWarning("Position not found in mesh bounds.");
        return 0f; // Default value if not found
    }

    private bool PointInTriangle(Vector3 point, Vector3 v0, Vector3 v1, Vector3 v2)
    {
        Vector3 u = v1 - v0;
        Vector3 v = v2 - v0;
        Vector3 w = point - v0;

        float uu = Vector3.Dot(u, u);
        float uv = Vector3.Dot(u, v);
        float vv = Vector3.Dot(v, v);
        float wu = Vector3.Dot(w, u);
        float wv = Vector3.Dot(w, v);

        float denom = uv * uv - uu * vv;
        float s = (uv * wv - vv * wu) / denom;
        float t = (uv * wu - uu * wv) / denom;

        return s >= 0 && t >= 0 && (s + t) <= 1;
    }

    private float InterpolateHeight(Vector3 point, Vector3 v0, Vector3 v1, Vector3 v2)
    {
        Vector3 u = v1 - v0;
        Vector3 v = v2 - v0;
        Vector3 w = point - v0;

        float uu = Vector3.Dot(u, u);
        float uv = Vector3.Dot(u, v);
        float vv = Vector3.Dot(v, v);
        float wu = Vector3.Dot(w, u);
        float wv = Vector3.Dot(w, v);

        float denom = uv * uv - uu * vv;
        float s = (uv * wv - vv * wu) / denom;
        float t = (uv * wu - uu * wv) / denom;

        return v0.y + s * (v1.y - v0.y) + t * (v2.y - v0.y);
    }
}
