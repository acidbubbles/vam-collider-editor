using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

using Object = UnityEngine.Object;

public abstract class ColliderModel<T> : ColliderModel where T : Collider
{
    protected T Collider { get; }

    protected ColliderModel(MVRScript parent, T collider)
        : base(parent, collider)
    {
        Collider = collider;
    }

    public override void CreatePreview()
    {
        var preview = DoCreatePreview();

        preview.GetComponent<Renderer>().material = MaterialHelper.GetNextMaterial();
        foreach (var c in preview.GetComponents<Collider>())
        {
            c.enabled = false;
            Object.Destroy(c);
        }

        preview.transform.SetParent(Collider.transform, false);

        Preview = preview;

        DoUpdatePreview();
        SetSelected(Selected);
    }
}

public abstract class ColliderModel : ModelBase<Collider>, IModel
{
    private float _previewOpacity;
    private float _selectedPreviewOpacity;
    private JSONStorableBool _xRayStorable;

    private bool _showPreview;

    public RigidbodyModel Rididbody { get; set; }

    public GameObject Preview { get; protected set; }
    public List<UIDynamic> Controls { get; private set; }

    public float SelectedPreviewOpacity
    {
        get { return _selectedPreviewOpacity; }
        set
        {
            if (Mathf.Approximately(value, _selectedPreviewOpacity))
                return;

            _selectedPreviewOpacity = value;

            if (Preview != null && Selected)
            {
                var previewRenderer = Preview.GetComponent<Renderer>();
                var color = previewRenderer.material.color;
                color.a = _selectedPreviewOpacity;
                previewRenderer.material.color = color;
                previewRenderer.enabled = false;
                previewRenderer.enabled = true;
            }
        }
    }

    public float PreviewOpacity
    {
        get { return _previewOpacity; }
        set
        {
            if (Mathf.Approximately(value, _previewOpacity))
                return;

            _previewOpacity = value;

            if (Preview != null && !Selected)
            {
                var previewRenderer = Preview.GetComponent<Renderer>();
                var color = previewRenderer.material.color;
                color.a = _previewOpacity;
                previewRenderer.material.color = color;

            }
        }
    }

    public bool ShowPreview
    {
        get { return _showPreview; }
        set
        {
            _showPreview = value;

            if (_showPreview)
                CreatePreview();
            else
                DestroyPreview();
        }
    }

    private bool _xRayPreview;
    public bool XRayPreview
    {
        get { return _xRayPreview; }
        set
        {
            _xRayPreview = value;

            if (Preview != null)
            {
                var previewRenderer = Preview.GetComponent<Renderer>();
                var material = previewRenderer.material;

                if (_xRayPreview)
                {
                    material.shader = Shader.Find("Battlehub/RTGizmos/Handles");
                    material.SetFloat("_Offset", 1f);
                    material.SetFloat("_MinAlpha", 1f);
                }
                else
                {
                    material.shader = Shader.Find("Standard");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite", 0);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.EnableKeyword("_ALPHABLEND_ON");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = 3000;
                }

                previewRenderer.material = material;

                if (_xRayStorable != null)
                    _xRayStorable.valNoCallback = value;
            }
        }
    }

    protected ColliderModel(MVRScript script, Collider component)
        : base(script, component, $"[co] {Simplify(component.attachedRigidbody != null ? component.attachedRigidbody.name : component.gameObject.name)}/{Simplify(component.name)}")
    {
    }

    public static ColliderModel CreateTyped(MVRScript script, Collider collider, Dictionary<string, RigidbodyModel> rigidbodies)
    {
        ColliderModel typed;

        if (collider is SphereCollider)
            typed = new SphereColliderModel(script, (SphereCollider)collider);
        else if (collider is BoxCollider)
            typed = new BoxColliderModel(script, (BoxCollider)collider);
        else if (collider is CapsuleCollider)
            typed = new CapsuleColliderModel(script, (CapsuleCollider)collider);
        else
            throw new InvalidOperationException("Unsupported collider type");

        if (collider.attachedRigidbody != null)
        {
            RigidbodyModel rigidbodyModel;
            if (rigidbodies.TryGetValue(collider.attachedRigidbody.Uuid(), out rigidbodyModel))
            {
                typed.Rididbody = rigidbodyModel;
                if (rigidbodyModel.Colliders == null)
                    rigidbodyModel.Colliders = new List<ColliderModel> { typed };
                else
                    rigidbodyModel.Colliders.Add(typed);
            }
        }

        return typed;
    }

    protected override void CreateControls()
    {
        DestroyControls();

        var controls = new List<UIDynamic>();

        _xRayStorable = new JSONStorableBool("xRayPreview", true, (bool value) => { XRayPreview = value; });

        var xRayToggle = Script.CreateToggle(_xRayStorable, true);
        xRayToggle.label = "XRay Preview";

        var resetUi = Script.CreateButton("Reset Collider", true);
        resetUi.button.onClick.AddListener(ResetToInitial);

        controls.Add(xRayToggle);
        controls.Add(resetUi);
        controls.AddRange(DoCreateControls());

        Controls = controls;
    }

    public abstract IEnumerable<UIDynamic> DoCreateControls();

    protected override void DestroyControls()
    {
        if (Controls == null)
            return;

        foreach (var adjustmentJson in Controls)
            Object.Destroy(adjustmentJson.gameObject);

        Controls.Clear();
    }

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

    public void UpdatePreview()
    {
        if (_showPreview)
            DoUpdatePreview();
    }

    protected abstract void DoUpdatePreview();

    public void UpdateControls()
    {
        DoUpdateControls();
    }

    protected abstract void DoUpdateControls();

    protected override void SetSelected(bool value)
    {
        if (Preview != null)
        {
            var previewRenderer = Preview.GetComponent<Renderer>();
            var color = previewRenderer.material.color;
            color.a = value ? _selectedPreviewOpacity : _previewOpacity;
            previewRenderer.material.color = color;
        }

        base.SetSelected(value);
    }

    public override void LoadJson(JSONClass jsonClass)
    {
        base.LoadJson(jsonClass);
        DoUpdatePreview();
    }

    public void ResetToInitial()
    {
        DoResetToInitial();
        DoUpdatePreview();

        if (Selected)
        {
            DestroyControls();
            CreateControls();
        }
    }

    protected abstract void DoResetToInitial();
    protected abstract bool DeviatesFromInitial();

    public override string ToString() => Id;
}
