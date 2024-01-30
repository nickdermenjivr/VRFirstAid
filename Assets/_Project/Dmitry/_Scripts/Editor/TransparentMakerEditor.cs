using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TransparentMaker))]
public class TransparentMakerEditor : Editor
{
    private static readonly int Surface = Shader.PropertyToID("_Surface");
    private static readonly int AlphaClip = Shader.PropertyToID("_AlphaClip");
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var selectedObject = ((TransparentMaker)target).gameObject;

        if (GUILayout.Button($"Make Transparent"))
        {
            CreateDuplicateWithMaterial(selectedObject, false);
        }

        if (GUILayout.Button($"Make Transparent Emission"))
        {
            CreateDuplicateWithMaterial(selectedObject, true);
        }
    }

    private static void CreateDuplicateWithMaterial(GameObject selectedObject, bool emission)
    {
        if (selectedObject == null)
        {
            EditorUtility.DisplayDialog($"No Object Selected", $"Please select an object.", $"OK");
            return;
        }

        var duplicate = Instantiate(selectedObject, selectedObject.transform.parent);
        duplicate.name = selectedObject.name + (emission ? $"_Emissive" : $"_Transparent");

        var renderers = duplicate.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            var newMaterials = new Material[renderer.sharedMaterials.Length];
            for (var i = 0; i < renderer.sharedMaterials.Length; i++)
            {
                var oldMaterial = renderer.sharedMaterials[i];
                if (oldMaterial != null)
                {
                    newMaterials[i] = CreateNewMaterial(oldMaterial, emission);
                }
            }

            renderer.sharedMaterials = newMaterials;
        }

        Undo.RegisterCreatedObjectUndo(duplicate, $"Create Duplicate with {duplicate.name}");
    }

    private static Material CreateNewMaterial(Material oldMaterial, bool emission)
    {
        var oldMaterialPath = AssetDatabase.GetAssetPath(oldMaterial);
        var folderPath = System.IO.Path.GetDirectoryName(oldMaterialPath);

        var newMaterial = new Material(oldMaterial)
        {
            name = oldMaterial.name + (emission ? $"Emissive" : $"Transparent")
        };

        newMaterial.SetFloat(Surface, 1);
        newMaterial.SetFloat(AlphaClip, 0);
        newMaterial.EnableKeyword($"_SURFACE_TYPE_TRANSPARENT");
        newMaterial.DisableKeyword($"_SURFACE_TYPE_OPAQUE");
        newMaterial.renderQueue = 3000;

        if (emission)
        {
            newMaterial.EnableKeyword($"_EMISSION");
            newMaterial.SetColor(EmissionColor, new Color(27f / 255f, 38f / 255f, 29f / 255f, 0f));
            newMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

            var baseMapColor = oldMaterial.color;
            baseMapColor.r = 0f;
            baseMapColor.g = 255f / 255f;
            baseMapColor.b = 30f / 255f;
            baseMapColor.a = 100f / 255f;
            newMaterial.color = baseMapColor;
        }
        else
        {
            var color = oldMaterial.color;
            color.a = 100f / 255f;
            newMaterial.color = color;
        }

        AssetDatabase.CreateAsset(newMaterial, $"{folderPath}/{newMaterial.name}.mat");
        AssetDatabase.SaveAssets();

        return newMaterial;
    }
}