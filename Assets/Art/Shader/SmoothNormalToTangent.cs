using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// This script calculates "smoothed" normals (averaging normals of vertices at the same position)
// and stores them into the Mesh Tangents.

public class SmoothNormalBakerv2 : MonoBehaviour
{
    [Tooltip("If true, the calculation runs automatically when the game starts.")]
    public bool runOnAwake = true;

    [Tooltip("If true, the mesh is cloned before modification to prevent changing the original asset.")]
    public bool cloneMesh = true;

    void Awake()
    {
        if (runOnAwake)
        {
            CalculateAndStoreSmoothNormals();
        }
    }

    // Context Menu allows you to run this from the Editor by right-clicking the component
    [ContextMenu("Bake Smooth Normals to Tangents")]
    public void CalculateAndStoreSmoothNormals()
    {
        // Get all MeshFilters in the hierarchy
        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();
        
        if (meshFilters.Length == 0)
        {
            Debug.LogError("No MeshFilters found in hierarchy!");
            return;
        }

        foreach (var mf in meshFilters)
        {
            BakeMesh(mf);
        }

        Debug.Log($"<color=cyan>Smooth Normals Baked</color> for {meshFilters.Length} meshes in hierarchy: {name}");
    }

    private void BakeMesh(MeshFilter mf)
    {
        Mesh mesh;

        // Handle Mesh Instance vs Shared Mesh
        if (Application.isPlaying && cloneMesh)
        {
            // Clone the mesh to don't modify the source mesh
            mesh = mf.mesh; 
        }
        else
        {
            // In editor (or if we want to modify the asset directly), use sharedMesh
            mesh = mf.sharedMesh;
        }

        if (mesh == null)
        {
            return;
        }

        // 1. Get current data
        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;
        
        // 2. Group vertices by position
        // We use a dictionary to accumulate normals for all vertices that occupy the exact same point in space.
        // This merges hard edges (where vertices are duplicated) into a single smooth direction.
        Dictionary<Vector3, Vector3> averageNormals = new Dictionary<Vector3, Vector3>();

        for (int i = 0; i < vertices.Length; i++)
        {
            if (!averageNormals.ContainsKey(vertices[i]))
            {
                averageNormals.Add(vertices[i], normals[i]);
            }
            else
            {
                averageNormals[vertices[i]] += normals[i];
            }
        }

        // 3. Normalize the averaged vectors
        // We cannot modify the dictionary while iterating, so we can just normalize on the fly in step 4
        // or re-assign keys here. It's efficient enough to just normalize during assignment.

        // 4. Assign the smooth normal to the Tangent channel
        Vector4[] tangents = new Vector4[vertices.Length];

        for (int i = 0; i < vertices.Length; i++)
        {
            // Retrieve the averaged normal for this position
            Vector3 smoothNormal = averageNormals[vertices[i]].normalized;

            // Store in Tangent. Tangents are Vector4. 
            // We store the normal in XYZ. W is usually 1 or -1 for binormal, we set to 0 here.
            // If used with standard normal mapping shaders, w should be ±1
            tangents[i] = new Vector4(smoothNormal.x, smoothNormal.y, smoothNormal.z, 1f);
        }

        // 5. Apply back to mesh
        mesh.tangents = tangents;
    }
}