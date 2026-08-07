#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

internal static class HairDryerRangeVisualBuilder
{
    private const string MaterialPath = "Assets/Materials/HairDryerRange.mat";
    private const string PrefabPath = "Assets/Prefabs/HairDryerRangeVisual.prefab";

    [MenuItem("CoCoBlow/Create Hair Dryer Range Visual Prefab")]
    public static void CreateRangeVisualAssets()
    {
        EnsureFolder("Assets/Materials");
        EnsureFolder("Assets/Prefabs");

        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                Debug.LogError("HairDryerRangeVisualBuilder: URP Unlit shader not found.");
                return;
            }

            material = new Material(shader)
            {
                name = "HairDryerRange"
            };
            material.SetColor("_BaseColor", new Color(0.2f, 0.75f, 1f, 0.22f));
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_Cull", 2f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
            AssetDatabase.CreateAsset(material, MaterialPath);
        }

        GameObject rangeVisualObject = new GameObject("RangeVisual");
        try
        {
            rangeVisualObject.AddComponent<MeshFilter>();
            MeshRenderer renderer = rangeVisualObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            rangeVisualObject.AddComponent<HairDryerRangeVisual>();

            PrefabUtility.SaveAsPrefabAsset(rangeVisualObject, PrefabPath);
            Debug.Log($"Range visual prefab created at {PrefabPath}. Parent it under HairDryer/Nozzle and assign to HairDryer.rangeVisual.");
        }
        finally
        {
            Object.DestroyImmediate(rangeVisualObject);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string parent = System.IO.Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
        string folderName = System.IO.Path.GetFileName(folderPath);
        if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(folderName))
        {
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
#endif
