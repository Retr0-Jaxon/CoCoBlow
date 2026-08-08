using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class HairDryerRangeVisual : MonoBehaviour
{
    private const string RangeMaterialPath = "Assets/Materials/HairDryerRange.mat";

    [Header("Shape")]
    [SerializeField] private int segmentCount = 24;

    [Header("Look")]
    [SerializeField] private Color fillColor = new Color(0.2f, 0.75f, 1f, 0.22f);

    [Header("Display")]
    [SerializeField] private bool alwaysShowRange;
    [SerializeField] private float fadeDuration = 0.25f;

    [Header("References")]
    [SerializeField] private HairDryer hairDryer;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Material runtimeMaterial;
    private MaterialPropertyBlock materialPropertyBlock;
    [MaybeNull] private Mesh coneMesh;
    private bool ownsRuntimeMaterial;

    private float currentAlpha;
    private float fadeVelocity;
    private bool isVisible;
    private bool isFadingOut;

    private float cachedRange = -1f;
    private float cachedAngle = -1f;
    private int cachedSegments = -1;
    private bool isInitialized;

#if UNITY_EDITOR
    private bool editorRefreshQueued;
#endif

    private void Awake()
    {
        InitializeIfNeeded();
    }

    private void OnEnable()
    {
        InitializeIfNeeded();
        ForceEditorVisibleState();
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            QueueEditorRefresh();
            return;
        }
#endif

        InitializeIfNeeded();
        ForceEditorVisibleState();
    }

    private void OnDestroy()
    {
        if (coneMesh != null)
        {
            if (Application.isPlaying)
            {
                Destroy(coneMesh);
            }
            else
            {
                DestroyImmediate(coneMesh);
            }
        }

        if (ownsRuntimeMaterial && runtimeMaterial != null)
        {
            if (Application.isPlaying)
            {
                Destroy(runtimeMaterial);
            }
            else
            {
                DestroyImmediate(runtimeMaterial);
            }
        }
    }

    private void LateUpdate()
    {
        InitializeIfNeeded();

        if (hairDryer == null)
        {
            SetRendererEnabled(false);
            return;
        }

        bool shouldShow = !Application.isPlaying || alwaysShowRange || hairDryer.ShouldShowRangeVisual;
        UpdateVisibilityState(shouldShow);
        UpdateFade();

        if (!isVisible && !isFadingOut)
        {
            SetRendererEnabled(false);
            return;
        }

        RebuildMeshIfNeeded(hairDryer.WindRange, hairDryer.WindAngle);
        SetRendererEnabled(true);
        ApplyAlpha(currentAlpha);
    }

    public void Play()
    {
        isVisible = true;
        isFadingOut = false;
        fadeVelocity = 0f;
        currentAlpha = fillColor.a;
        SetRendererEnabled(true);
    }

    public void StopFade()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        isVisible = false;
        isFadingOut = true;
    }

    private void UpdateVisibilityState(bool shouldShow)
    {
        if (shouldShow)
        {
            if (!isVisible)
            {
                Play();
            }
        }
        else if (isVisible)
        {
            StopFade();
        }
    }

    private void UpdateFade()
    {
        if (!Application.isPlaying)
        {
            currentAlpha = fillColor.a;
            isVisible = true;
            isFadingOut = false;
            return;
        }

        if (!isFadingOut)
        {
            currentAlpha = fillColor.a;
            return;
        }

        currentAlpha = Mathf.SmoothDamp(currentAlpha, 0f, ref fadeVelocity, fadeDuration);
        if (currentAlpha <= 0.01f)
        {
            currentAlpha = 0f;
            isFadingOut = false;
            isVisible = false;
        }
    }

    private void RebuildMeshIfNeeded(float range, float angle)
    {
        if (Mathf.Approximately(cachedRange, range)
            && Mathf.Approximately(cachedAngle, angle)
            && cachedSegments == segmentCount)
        {
            return;
        }

        cachedRange = range;
        cachedAngle = angle;
        cachedSegments = segmentCount;

        BuildConeMesh(coneMesh, range, angle, segmentCount);
    }

    private static void BuildConeMesh(Mesh mesh, float range, float angleDegrees, int segments)
    {
        segments = Mathf.Max(3, segments);
        float radius = range * Mathf.Tan(angleDegrees * Mathf.Deg2Rad);

        int vertexCount = segments + 1;
        Vector3[] vertices = new Vector3[vertexCount];
        int[] triangles = new int[segments * 3];

        vertices[0] = Vector3.zero;
        for (int i = 0; i < segments; i++)
        {
            float t = i / (float)segments * Mathf.PI * 2f;
            vertices[i + 1] = new Vector3(Mathf.Cos(t) * radius, range, Mathf.Sin(t) * radius);
        }

        for (int i = 0; i < segments; i++)
        {
            int triIndex = i * 3;
            int next = i + 1;
            int nextWrapped = next == segments ? 1 : next + 1;
            triangles[triIndex] = 0;
            triangles[triIndex + 1] = next;
            triangles[triIndex + 2] = nextWrapped;
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    private void ConfigureRenderer()
    {
        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        meshRenderer.lightProbeUsage = LightProbeUsage.Off;
        meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
    }

    private void InitializeIfNeeded()
    {
        if (isInitialized)
        {
            return;
        }

        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        // if (hairDryer == null)
        // {
        //     hairDryer = GetComponentInParent<HairDryer>();
        // }

        ConfigureRenderer();
        EnsureMaterial();
        materialPropertyBlock = new MaterialPropertyBlock();

        if (coneMesh == null)
        {
            coneMesh = new Mesh { name = "HairDryerRangeCone" };
        }

        meshFilter.sharedMesh = coneMesh;
        currentAlpha = fillColor.a;
        isVisible = true;
        isFadingOut = false;
        isInitialized = true;
    }

    private void ForceEditorVisibleState()
    {
        if (Application.isPlaying)
        {
            return;
        }

        currentAlpha = fillColor.a;
        isVisible = true;
        isFadingOut = false;
        SetRendererEnabled(true);
    }

#if UNITY_EDITOR
    private void QueueEditorRefresh()
    {
        if (editorRefreshQueued)
        {
            return;
        }

        editorRefreshQueued = true;
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this == null)
            {
                return;
            }

            editorRefreshQueued = false;
            InitializeIfNeeded();
            ForceEditorVisibleState();

            if (hairDryer == null)
            {
                return;
            }

            cachedRange = -1f;
            cachedAngle = -1f;
            cachedSegments = -1;
            RebuildMeshIfNeeded(hairDryer.WindRange, hairDryer.WindAngle);
            ApplyAlpha(currentAlpha);
        };
    }
#endif

    private void EnsureMaterial()
    {
        Material sharedMaterial = Resources.Load<Material>(RangeMaterialPath);
#if UNITY_EDITOR
        if (sharedMaterial == null)
        {
            sharedMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(RangeMaterialPath);
        }
#endif

        if (sharedMaterial != null)
        {
            runtimeMaterial = sharedMaterial;
            ownsRuntimeMaterial = false;
            meshRenderer.sharedMaterial = runtimeMaterial;
            return;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        runtimeMaterial = new Material(shader)
        {
            name = "HairDryerRangeRuntime"
        };
        ownsRuntimeMaterial = true;
        runtimeMaterial.SetFloat("_Surface", 1f);
        runtimeMaterial.SetFloat("_Blend", 0f);
        runtimeMaterial.SetFloat("_Cull", 2f);
        runtimeMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        runtimeMaterial.renderQueue = (int)RenderQueue.Transparent;
        meshRenderer.sharedMaterial = runtimeMaterial;
    }

    private void ApplyAlpha(float alpha)
    {
        if (meshRenderer == null || materialPropertyBlock == null)
        {
            return;
        }

        Color color = fillColor;
        color.a = fillColor.a * alpha;

        meshRenderer.GetPropertyBlock(materialPropertyBlock);
        materialPropertyBlock.SetColor("_BaseColor", color);
        materialPropertyBlock.SetColor("_Color", color);
        meshRenderer.SetPropertyBlock(materialPropertyBlock);
    }

    private void SetRendererEnabled(bool enabled)
    {
        if (meshRenderer != null)
        {
            meshRenderer.enabled = enabled;
        }
    }
}
