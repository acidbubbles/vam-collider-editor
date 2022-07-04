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

    public override GameObject CreatePreview()
    {
        var preview = DoCreatePreview();

        preview.GetComponent<Renderer>().material = MaterialHelper.GetNextMaterial(Id.GetHashCode());
        foreach (var c in preview.GetComponents<Collider>())
        {
            c.enabled = false;
            Object.Destroy(c);
        }

        preview.transform.SetParent(Collider.transform, false);
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
    public GameObject Preview { get; protected set; }
    public GameObject XRayPreview { get; protected set; }
    public bool Shown { get; set; }

    public abstract bool SyncOverrides();

    public virtual void UpdatePreviewsFromConfig()
    {
        if (_config.PreviewsEnabled && Shown)
        {
            if(Preview == null)
            {
                Preview = CreatePreview();
            }

            var previewRenderer = Preview.GetComponent<Renderer>();
            var material = previewRenderer.material;

            if (!_highlighted)
            {
                var color = previewRenderer.material.color;
                color.a = _config.PreviewsOpacity;
                previewRenderer.material.color = color;
            }
            else
            {
                var color = previewRenderer.material.color;
                color.a = _config.SelectedPreviewsOpacity;
                previewRenderer.material.color = color;
                previewRenderer.enabled = false;
                previewRenderer.enabled = true;
            }

            if (material.shader.name != "Standard")
            {
                material.shader = Shader.Find("Standard");
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = 3000;
                previewRenderer.material = material;
            }

            UpdateXRayPreviewFromConfig();

            SyncPreviews();
            RefreshHighlightedPreview();
            RefreshHighlightedXRayPreview();
        }
        else
        {
            DestroyPreviews();
        }
    }

    public void UpdateXRayPreviewFromConfig()
    {
        if (_config.XRayPreviews)
        {
            if(XRayPreview == null)
            {
                XRayPreview = CreatePreview();
            }

            var previewRenderer = XRayPreview.GetComponent<Renderer>();
            var material = previewRenderer.material;

            if (!_highlighted)
            {
                var color = previewRenderer.material.color;
                color.a = _config.RelativeXRayOpacity * _config.PreviewsOpacity;
                previewRenderer.material.color = color;
            }
            else
            {
                var color = previewRenderer.material.color;
                color.a = _config.RelativeXRayOpacity * _config.SelectedPreviewsOpacity;
                previewRenderer.material.color = color;
                previewRenderer.enabled = false;
                previewRenderer.enabled = true;
            }

            if (material.shader.name != "Battlehub/RTGizmos/Handles")
            {
                material.shader = Shader.Find("Battlehub/RTGizmos/Handles");
                material.SetFloat("_Offset", 1f);
                material.SetFloat("_MinAlpha", 1f);
                previewRenderer.material = material;
            }
        }
        else
        {
            DestroyXRayPreview();
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
        DestroyPreview();
        DestroyXRayPreview();
    }

    private void DestroyPreview()
    {
        if (Preview != null)
        {
            Object.Destroy(Preview);
            Preview = null;
        }
    }

    private void DestroyXRayPreview()
    {
        if (XRayPreview != null)
        {
            Object.Destroy(XRayPreview);
            XRayPreview = null;
        }
    }

    public abstract GameObject CreatePreview();

    protected abstract GameObject DoCreatePreview();

    public override void SetHighlighted(bool value)
    {
        if (_highlighted == value) return;

        _highlighted = value;
        RefreshHighlightedPreview();
        RefreshHighlightedXRayPreview();
    }

    protected void RefreshHighlightedPreview()
    {
        if (Preview != null)
        {
            var previewRenderer = Preview.GetComponent<Renderer>();
            var color = previewRenderer.material.color;
            color.a = _highlighted ? _config.SelectedPreviewsOpacity : _config.PreviewsOpacity;
            previewRenderer.material.color = color;
        }
    }

    protected void RefreshHighlightedXRayPreview()
    {
        if (XRayPreview != null)
        {
            var previewRenderer = XRayPreview.GetComponent<Renderer>();
            var color = previewRenderer.material.color;
            var alpha = _highlighted ? _config.SelectedPreviewsOpacity : _config.PreviewsOpacity;
            color.a = _config.RelativeXRayOpacity * alpha;
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
