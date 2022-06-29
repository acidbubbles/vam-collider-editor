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

    public override void CreatePreview()
    {
        if (Preview != null) return;

        Preview = DoCreatePreview();

        Preview.GetComponent<Renderer>().material = MaterialHelper.GetNextMaterial(Id.GetHashCode());
        foreach (var c in Preview.GetComponents<Collider>())
        {
            c.enabled = false;
            Object.Destroy(c);
        }

        Preview.transform.SetParent(Collider.transform, false);

        SyncPreview();
        RefreshHighlighted();
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
    public bool Shown { get; set; }

    public abstract bool SyncOverrides();

    public virtual void UpdatePreviewFromConfig()
    {
        if (_config.PreviewsEnabled && Shown)
        {
            CreatePreview();

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

            if (_config.XRayPreviews && material.shader.name != "Battlehub/RTGizmos/Handles")
            {
                material.shader = Shader.Find("Battlehub/RTGizmos/Handles");
                material.SetFloat("_Offset", 1f);
                material.SetFloat("_MinAlpha", 1f);
                previewRenderer.material = material;
            }
            else if (!_config.XRayPreviews & material.shader.name != "Standard")
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
        }
        else
        {
            DestroyPreview();
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

    public virtual void DestroyPreview()
    {
        if (Preview != null)
        {
            Object.Destroy(Preview);
            Preview = null;
        }
    }

    public abstract void CreatePreview();

    protected abstract GameObject DoCreatePreview();

    public override void SetHighlighted(bool value)
    {
        if (_highlighted == value) return;

        _highlighted = value;
        RefreshHighlighted();
    }

    protected void RefreshHighlighted()
    {
        if (Preview != null)
        {
            var previewRenderer = Preview.GetComponent<Renderer>();
            var color = previewRenderer.material.color;
            color.a = _highlighted ? _config.SelectedPreviewsOpacity : _config.PreviewsOpacity;
            previewRenderer.material.color = color;
        }
    }

    public override void LoadJson(JSONClass jsonClass)
    {
        base.LoadJson(jsonClass);
        SyncPreview();
    }

    protected override void DoResetToInitial()
    {
        SyncPreview();
    }

    public override string ToString() => Id;
}
