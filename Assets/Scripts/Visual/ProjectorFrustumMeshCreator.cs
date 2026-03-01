// File: Scripts/Editor/ProjectorFrustumMeshCreator.cs
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class ProjectorFrustumMeshCreator
{
    [MenuItem("Tools/Projector/Create Frustum Mesh")]
    public static void CreateFrustumMesh()
    {
        Mesh mesh = new Mesh { name = "ProjectorFrustum" };

        // Параметры frustum (будут масштабироваться через transform)
        float near = 0f;     // Начинается от проектора
        float far = 1f;     // Нормализованная длина
        float halfAngle = 0.5f; // Будет управляться через scale

        Vector3[] vertices = new Vector3[8];

        // Near plane (маленький квад у проектора)
        float nS = near * halfAngle;
        vertices[0] = new Vector3(-nS, -nS, near); // near bottom-left
        vertices[1] = new Vector3(nS, -nS, near); // near bottom-right
        vertices[2] = new Vector3(nS, nS, near); // near top-right
        vertices[3] = new Vector3(-nS, nS, near); // near top-left

        // Far plane (большой квад на стене)
        float fS = far * halfAngle;
        vertices[4] = new Vector3(-fS, -fS, far);
        vertices[5] = new Vector3(fS, -fS, far);
        vertices[6] = new Vector3(fS, fS, far);
        vertices[7] = new Vector3(-fS, fS, far);

        // 12 треугольников (6 граней × 2 tri)
        int[] triangles = new int[]
        {
            // Front (far plane)
            4, 6, 5,  4, 7, 6,
            // Back (near plane)
            0, 1, 2,  0, 2, 3,
            // Left
            0, 3, 7,  0, 7, 4,
            // Right
            1, 5, 6,  1, 6, 2,
            // Top
            3, 2, 6,  3, 6, 7,
            // Bottom
            0, 4, 5,  0, 5, 1
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        // Сохраняем как asset
        string path = "Assets/Meshes/ProjectorFrustum.asset";
        System.IO.Directory.CreateDirectory("Assets/Meshes");
        AssetDatabase.CreateAsset(mesh, path);
        AssetDatabase.SaveAssets();

        Debug.Log($"Frustum mesh created at: {path}");
        EditorGUIUtility.PingObject(mesh);
    }
}
#endif