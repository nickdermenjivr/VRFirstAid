using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class Outline : MonoBehaviour
{
    private static readonly HashSet<Mesh> RegisteredMeshes = new();

    public enum Mode
    {
        OutlineAll,
        OutlineVisible,
        OutlineHidden,
        OutlineAndSilhouette,
        SilhouetteOnly
    }

    public Mode OutlineMode
    {
        get => outlineMode;
        set
        {
            outlineMode = value;
            _needsUpdate = true;
        }
    }

    public Color OutlineColor
    {
        get => outlineColor;
        set
        {
            outlineColor = value;
            _needsUpdate = true;
        }
    }

    public float OutlineWidth
    {
        get => outlineWidth;
        set
        {
            outlineWidth = value;
            _needsUpdate = true;
        }
    }

    [Serializable]
    private class ListVector3
    {
        public List<Vector3> data;
    }

    [Header("Outline Settings")] 
    [SerializeField] private Mode outlineMode = Mode.OutlineAll;
    [SerializeField] private Color outlineColor = Color.green;
    [SerializeField, Range(0f, 10f)] private float outlineWidth = 5f;
    [SerializeField] private bool precomputeOutline = true;

    [Space(10)] 
    
    [Header("Glow Settings")] 
    [SerializeField] private bool glow;
    [SerializeField, Range(0f, 10f)] private float minOutlineWidth = 4f;
    [SerializeField, Range(0f, 10f)] private float maxOutlineWidth = 8f;
    [SerializeField, Range(0f, 20f)] private float glowSpeed = 8f;

    [Space(10)] 
    
    [Header("Advanced Settings")] 
    [SerializeField, HideInInspector] private List<Mesh> bakeKeys = new();
    [SerializeField, HideInInspector] private List<ListVector3> bakeValues = new();

    private Renderer[] _renderers;
    private Material _outlineMaskMaterial;
    private Material _outlineFillMaterial;
    private bool _needsUpdate;
    private float _cachedOutlineWidth;

    private static readonly int OutlineColor1 = Shader.PropertyToID($"_OutlineColor");
    private static readonly int ZTest = Shader.PropertyToID($"_ZTest");
    private static readonly int Width = Shader.PropertyToID($"_OutlineWidth");

    private void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>();

        _outlineMaskMaterial = Instantiate(Resources.Load<Material>($@"Outline/Materials/OutlineMask"));
        _outlineFillMaterial = Instantiate(Resources.Load<Material>($@"Outline/Materials/OutlineFill"));

        _outlineMaskMaterial.name = $"OutlineMask (Instance)";
        _outlineFillMaterial.name = $"OutlineFill (Instance)";

        LoadSmoothNormals();

        _needsUpdate = true;
        _cachedOutlineWidth = outlineWidth;
    }

    private void OnEnable()
    {
        foreach (var renderTemp in _renderers)
        {
            var materials = renderTemp.sharedMaterials.ToList();

            materials.Add(_outlineMaskMaterial);
            materials.Add(_outlineFillMaterial);

            renderTemp.materials = materials.ToArray();
        }
    }

    private void OnValidate()
    {
        _needsUpdate = true;

        if (!precomputeOutline && bakeKeys.Count != 0 || bakeKeys.Count != bakeValues.Count)
        {
            bakeKeys.Clear();
            bakeValues.Clear();
        }

        if (precomputeOutline && bakeKeys.Count == 0)
        {
            Bake();
        }

        if (minOutlineWidth > maxOutlineWidth)
        {
            minOutlineWidth = maxOutlineWidth;
        }
    }

    private void Update()
    {
        switch (glow)
        {
            case true:
            {
                outlineWidth += glowSpeed * Time.deltaTime;
                if (outlineWidth > maxOutlineWidth || outlineWidth < minOutlineWidth)
                {
                    glowSpeed *= -1;
                    outlineWidth = Mathf.Clamp(outlineWidth, minOutlineWidth, maxOutlineWidth);
                }

                _needsUpdate = true;
                break;
            }
            case false:
                outlineWidth = _cachedOutlineWidth;
                break;
        }

        if (!_needsUpdate) return;

        _needsUpdate = false;

        UpdateMaterialProperties();
    }

    private void OnDisable()
    {
        foreach (var renderTemp in _renderers)
        {
            var materials = renderTemp.sharedMaterials.ToList();

            materials.Remove(_outlineMaskMaterial);
            materials.Remove(_outlineFillMaterial);

            renderTemp.materials = materials.ToArray();
        }
    }

    private void OnDestroy()
    {
        Destroy(_outlineMaskMaterial);
        Destroy(_outlineFillMaterial);
    }

    public void SetGlowEnabled(bool enableGlow)
    {
        glow = enableGlow;

        if (glow) return;

        outlineWidth = _cachedOutlineWidth;
    }

    private void Bake()
    {
        var bakedMeshes = new HashSet<Mesh>();

        foreach (var meshFilter in GetComponentsInChildren<MeshFilter>())
        {
            if (!bakedMeshes.Add(meshFilter.sharedMesh))
            {
                continue;
            }

            var smoothNormals = SmoothNormals(meshFilter.sharedMesh);

            bakeKeys.Add(meshFilter.sharedMesh);
            bakeValues.Add(new ListVector3() { data = smoothNormals });
        }
    }

    private void LoadSmoothNormals()
    {
        foreach (var meshFilter in GetComponentsInChildren<MeshFilter>())
        {
            if (!RegisteredMeshes.Add(meshFilter.sharedMesh))
            {
                continue;
            }

            var index = bakeKeys.IndexOf(meshFilter.sharedMesh);
            var smoothNormals = (index >= 0) ? bakeValues[index].data : SmoothNormals(meshFilter.sharedMesh);

            meshFilter.sharedMesh.SetUVs(3, smoothNormals);

            var rendererTemp = meshFilter.GetComponent<Renderer>();

            if (rendererTemp != null)
            {
                CombineSubMeshes(meshFilter.sharedMesh, rendererTemp.sharedMaterials);
            }
        }

        foreach (var skinnedMeshRenderer in GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            if (!RegisteredMeshes.Add(skinnedMeshRenderer.sharedMesh))
            {
                continue;
            }

            var sharedMesh = skinnedMeshRenderer.sharedMesh;
            sharedMesh.uv4 = new Vector2[sharedMesh.vertexCount];

            CombineSubMeshes(sharedMesh, skinnedMeshRenderer.sharedMaterials);
        }
    }

    private static List<Vector3> SmoothNormals(Mesh mesh)
    {
        var groups = mesh.vertices.Select((vertex, index) => new KeyValuePair<Vector3, int>(vertex, index))
            .GroupBy(pair => pair.Key);

        var smoothNormals = new List<Vector3>(mesh.normals);

        foreach (var group in groups)
        {
            if (group.Count() == 1)
            {
                continue;
            }

            var smoothNormal = group.Aggregate(Vector3.zero, (current, pair) => current + smoothNormals[pair.Value]);

            smoothNormal.Normalize();

            foreach (var pair in group)
            {
                smoothNormals[pair.Value] = smoothNormal;
            }
        }

        return smoothNormals;
    }

    private static void CombineSubMeshes(Mesh mesh, IReadOnlyCollection<Material> materials)
    {
        if (mesh.subMeshCount == 1)
        {
            return;
        }

        if (mesh.subMeshCount > materials.Count)
        {
            return;
        }

        mesh.subMeshCount++;
        mesh.SetTriangles(mesh.triangles, mesh.subMeshCount - 1);
    }

    private void UpdateMaterialProperties()
    {
        _outlineFillMaterial.SetColor(OutlineColor1, outlineColor);

        switch (outlineMode)
        {
            case Mode.OutlineAll:
                _outlineMaskMaterial.SetFloat(ZTest, (float)UnityEngine.Rendering.CompareFunction.Always);
                _outlineFillMaterial.SetFloat(ZTest, (float)UnityEngine.Rendering.CompareFunction.Always);
                _outlineFillMaterial.SetFloat(Width, outlineWidth);
                break;

            case Mode.OutlineVisible:
                _outlineMaskMaterial.SetFloat(ZTest, (float)UnityEngine.Rendering.CompareFunction.Always);
                _outlineFillMaterial.SetFloat(ZTest, (float)UnityEngine.Rendering.CompareFunction.LessEqual);
                _outlineFillMaterial.SetFloat(Width, outlineWidth);
                break;

            case Mode.OutlineHidden:
                _outlineMaskMaterial.SetFloat(ZTest, (float)UnityEngine.Rendering.CompareFunction.Always);
                _outlineFillMaterial.SetFloat(ZTest, (float)UnityEngine.Rendering.CompareFunction.Greater);
                _outlineFillMaterial.SetFloat(Width, outlineWidth);
                break;

            case Mode.OutlineAndSilhouette:
                _outlineMaskMaterial.SetFloat(ZTest, (float)UnityEngine.Rendering.CompareFunction.LessEqual);
                _outlineFillMaterial.SetFloat(ZTest, (float)UnityEngine.Rendering.CompareFunction.Always);
                _outlineFillMaterial.SetFloat(Width, outlineWidth);
                break;

            case Mode.SilhouetteOnly:
                _outlineMaskMaterial.SetFloat(ZTest, (float)UnityEngine.Rendering.CompareFunction.LessEqual);
                _outlineFillMaterial.SetFloat(ZTest, (float)UnityEngine.Rendering.CompareFunction.Greater);
                _outlineFillMaterial.SetFloat(Width, 0f);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}