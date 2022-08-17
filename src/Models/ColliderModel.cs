using System;
using SimpleJSON;
using UnityEngine;

using Object = UnityEngine.Object;

public abstract class ColliderModel<T> : ColliderModel where T : Collider
{
    protected new T Collider { get; }

    protected ColliderModel(MVRScript parent, T collider, ColliderPreviewConfig config)
        : base(parent, collider, config)
    {
        Collider = collider;
    }

    public override GameObject[] CreatePreview()
    {
        var preview = DoCreatePreview();

        foreach (var gameObject in preview)
        {
            gameObject.GetComponent<Renderer>().material = MaterialHelper.GetNextMaterial(Id.GetHashCode());
            foreach (var c in gameObject.GetComponents<Collider>())
            {
                c.enabled = false;
                Object.Destroy(c);
            }

            gameObject.transform.SetParent(Collider.transform, false);
        }

        return preview;
    }
}

public abstract class ColliderModel : ModelBase<Collider>, IModel
{
    private readonly ColliderPreviewConfig _config;
    private bool _highlighted;

    public string Type => "Collider";
    public Collider Collider { get; set; }
    public RigidbodyModel RigidbodyModel { get; set; }
    public GameObject[] ProtrusionPreview { get; protected set; }
    public GameObject[] XRayPreview { get; protected set; }
    public bool Shown { get; set; }

    public abstract bool SyncOverrides();

    public virtual void UpdatePreviewsFromConfig()
    {
        if (_config.PreviewsEnabled && Shown)
        {
            UpdateXRayPreviewFromConfig();
            UpdatePreviewProtrusionsFromConfig();

            SyncPreviews();
            RefreshHighlightedProtrusionPreview();
            RefreshHighlightedXRayPreview();
        }
        else
        {
            DestroyPreviews();
        }
    }

    public void UpdateXRayPreviewFromConfig()
    {
        if (_config.PreviewsXRayOpacity > 0 && !_highlighted || _config.SelectedPreviewsXRayOpacity > 0 && _highlighted)
        {
            if(XRayPreview == null)
            {
                XRayPreview = CreatePreview();
            }

            foreach (var gameObject in XRayPreview)
            {
                var previewRenderer = gameObject.GetComponent<Renderer>();
                var material = previewRenderer.material;
                var color = material.color;
                color.a = _highlighted
                    ?  _config.SelectedPreviewsXRayOpacity
                    : _config.PreviewsXRayOpacity;
                material.color = color;

                if (material.shader.name != "Battlehub/RTGizmos/Handles")
                {
                    material.shader = Shader.Find("Battlehub/RTGizmos/Handles");
                    material.SetFloat("_Offset", 1f);
                    material.SetFloat("_MinAlpha", 1f);
                }

                material.renderQueue = _config.PreviewsXRayRenderQueue;
            }
        }
        else
        {
            DestroyPreviewXRay();
        }
    }

    private void UpdatePreviewProtrusionsFromConfig()
    {
        if (_config.PreviewsXRayOpacity > 0 && !_highlighted || _config.SelectedPreviewsXRayOpacity > 0 && _highlighted)
        {
            if (ProtrusionPreview == null)
            {
                ProtrusionPreview = CreatePreview();
            }

            foreach (var gameObject in ProtrusionPreview)
            {
                var previewRenderer = gameObject.GetComponent<Renderer>();
                var material = previewRenderer.material;
                var color = material.color;
                color.a = _highlighted
                    ?  _config.SelectedPreviewsProtrusionsOpacity
                    : _config.PreviewsProtrusionsOpacity;
                material.color = color;

                if (material.shader.name != "Standard")
                {
                    material.shader = Shader.Find("Standard");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite", 0);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.EnableKeyword("_ALPHABLEND_ON");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                }

                material.renderQueue = _config.PreviewsProtrusionsRenderQueue;
            }
        }
        else
        {
            DestroyPreviewProtrusion();
        }
    }

    protected ColliderModel(MVRScript script, Collider collider, ColliderPreviewConfig config)
        : base(script, collider, CreateLabel(collider))
    {
        Collider = collider;
        _config = config;
    }

    private static string CreateLabel(Collider collider)
    {
        var parent = collider.attachedRigidbody != null ? collider.attachedRigidbody.name : collider.transform.parent.name;
        var label = parent == collider.name ? NameHelper.Simplify(collider.name) : $"{NameHelper.Simplify(parent)}/{NameHelper.Simplify(collider.name)}";
        return $"[co] {label}";
    }

    public static ColliderModel CreateTyped(MVRScript script, Collider collider, ColliderPreviewConfig config)
    {
        ColliderModel typed;

        if (collider is SphereCollider)
            typed = new SphereColliderModel(script, (SphereCollider)collider, config);
        else if (collider is BoxCollider)
            typed = new BoxColliderModel(script, (BoxCollider)collider, config);
        else if (collider is CapsuleCollider)
            typed = new CapsuleColliderModel(script, (CapsuleCollider)collider, config);
        else
            throw new InvalidOperationException("Unsupported collider type");

        return typed;
    }

    protected override void CreateControlsInternal()
    {
        if (RigidbodyModel != null)
        {
            var goToRigidbodyButton = Script.CreateButton("Go to Rigidbody", true);
            goToRigidbodyButton.button.onClick.AddListener(() =>
            {
                Script.SendMessage("SelectEditable", RigidbodyModel);
            });
            RegisterControl(goToRigidbodyButton);
        }

        DoCreateControls();
    }

    public abstract void DoCreateControls();

    public virtual void DestroyPreviews()
    {
        DestroyPreviewProtrusion();
        DestroyPreviewXRay();
    }

    private void DestroyPreviewProtrusion()
    {
        if (ProtrusionPreview == null) return;

        foreach(var gameObject in ProtrusionPreview)
        {
            Object.Destroy(gameObject);
        }
        ProtrusionPreview = null;
    }

    private void DestroyPreviewXRay()
    {
        if (XRayPreview == null) return;

        foreach(var gameObject in XRayPreview)
        {
            Object.Destroy(gameObject);
        }
        XRayPreview = null;
    }

    public abstract GameObject[] CreatePreview();

    protected abstract GameObject[] DoCreatePreview();

    public override void SetHighlighted(bool value)
    {
        if (_highlighted == value) return;

        _highlighted = value;
        RefreshHighlightedProtrusionPreview();
        RefreshHighlightedXRayPreview();
    }

    protected void RefreshHighlightedProtrusionPreview()
    {
        if (ProtrusionPreview == null) return;

        foreach (var gameObject in ProtrusionPreview)
        {
            var previewRenderer = gameObject.GetComponent<Renderer>();
            var color = previewRenderer.material.color;
            color.a = _highlighted ? _config.SelectedPreviewsProtrusionsOpacity : _config.PreviewsProtrusionsOpacity;
            previewRenderer.material.color = color;
        }
    }

    protected void RefreshHighlightedXRayPreview()
    {
        if (XRayPreview == null) return;

        foreach (var gameObject in XRayPreview)
        {
            var previewRenderer = gameObject.GetComponent<Renderer>();
            var color = previewRenderer.material.color;
            color.a = _highlighted ? _config.SelectedPreviewsXRayOpacity : _config.PreviewsXRayOpacity;
            previewRenderer.material.color = color;
        }
    }

    public override void LoadJson(JSONClass jsonClass)
    {
        base.LoadJson(jsonClass);
        SyncPreviews();
    }

    protected override void DoResetToInitial()
    {
        SyncPreviews();
    }

    public override string ToString() => Id;
}
