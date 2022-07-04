using GPUTools.Physics.Scripts.Behaviours;
using SimpleJSON;
using UnityEngine;

public class SphereColliderModel : ColliderModel<SphereCollider>
{
    private readonly float _initialRadius;
    private float _radius;
    private readonly Vector3 _initialCenter;
    private readonly float _initialGpuFriction;
    private readonly bool _initialGpuEnabled;
    private readonly GpuSphereCollider _gpu;
    private Vector3 _center;
    private float _gpuFriction;
    private bool _gpuEnabled;

    public SphereColliderModel(MVRScript parent, SphereCollider collider, ColliderPreviewConfig config)
        : base(parent, collider, config)
    {
        _initialRadius = _radius = collider.radius;
        _initialCenter = _center = collider.center;
        _gpu = collider.gameObject.GetComponent<GpuSphereCollider>();
        if (_gpu != null)
        {
            _initialGpuFriction = _gpuFriction = _gpu.friction;
            _initialGpuEnabled = _gpuEnabled = _gpu.enabled;
        }
    }

    public override bool SyncOverrides()
    {
        if (!Modified) return false;
        bool changed = false;
        if (Collider.radius != _radius)
        {
            Collider.radius = _radius;
            changed = true;
        }
        if (Collider.center != _center)
        {
            Collider.center = _center;
            changed = true;
        }
        if (_gpu != null)
        {
            if (_gpu.friction != _gpuFriction)
            {
                _gpu.friction = _gpuFriction;
                changed = true;
            }
            if (_gpu.enabled != _gpuEnabled)
            {
                _gpu.enabled = _gpuEnabled;
                changed = true;
            }
        }
        return changed;
    }

    public override void DoCreateControls()
    {
        RegisterControl(Script.CreateFloatSlider(RegisterStorable(new JSONStorableFloat("radius", Collider.radius, value =>
        {
            Collider.radius = _radius = value;
            SetModified();
            SyncPreviews();
        }, 0f, _initialRadius * 4f, false)).WithDefault(_initialRadius), "Radius"));

        RegisterControl(Script.CreateFloatSlider(RegisterStorable(new JSONStorableFloat("centerX", Collider.center.x, value =>
        {
            var center = _center;
            center.x = value;
            Collider.center = _center = center;
            SetModified();
            SyncPreviews();
        }, MakeMinPosition(Collider.center.x), MakeMaxPosition(Collider.center.x), false)).WithDefault(_initialCenter.x), "Center.X"));

        RegisterControl(Script.CreateFloatSlider(RegisterStorable(new JSONStorableFloat("centerY", Collider.center.y, value =>
        {
            var center = _center;
            center.y = value;
            Collider.center = _center = center;
            SetModified();
            SyncPreviews();
        }, MakeMinPosition(Collider.center.y), MakeMaxPosition(Collider.center.y), false)).WithDefault(_initialCenter.y), "Center.Y"));

        RegisterControl(Script.CreateFloatSlider(RegisterStorable(new JSONStorableFloat("centerZ", Collider.center.z, value =>
        {
            var center = _center;
            center.z = value;
            Collider.center = _center = center;
            SetModified();
            SyncPreviews();
        }, MakeMinPosition(Collider.center.z), MakeMaxPosition(Collider.center.z), false)).WithDefault(_initialCenter.z), "Center.Z"));

        if (_gpu != null)
        {
            RegisterControl(Script.CreateFloatSlider(RegisterStorable(new JSONStorableFloat("gpu.friction", _gpu.friction, value =>
            {
                _gpu.friction = _gpuFriction = value;
                SetModified();
            }, 0f, Mathf.Ceil(_initialGpuFriction) * 2f, false)).WithDefault(_gpu.friction), "GPU Friction"));

            RegisterControl(Script.CreateToggle(RegisterStorable(new JSONStorableBool("gpu.enabled", _gpu.enabled, (bool value) =>
            {
                _gpu.enabled = _gpuEnabled = value;
                SetModified();
            }).WithDefault(_initialGpuEnabled)), "GPU Collider Enabled"));
        }
    }

    protected override GameObject DoCreatePreview() => GameObject.CreatePrimitive(PrimitiveType.Sphere);

    public override void SyncPreviews()
    {
        SyncPreview(Preview);
        SyncPreview(XRayPreview);
    }

    private void SyncPreview(GameObject preview)
    {
        if (preview == null) return;

        preview.transform.localScale = Vector3.one * (Collider.radius * 2);
        preview.transform.localPosition = Collider.center;
    }

    protected override void DoLoadJson(JSONClass jsonClass)
    {
        LoadJsonField(jsonClass, "radius", val => Collider.radius = _radius = val);
        LoadJsonField(jsonClass, "center", val => Collider.center = _center = val);
        if (_gpu != null)
        {
            LoadJsonField(jsonClass, "gpuFriction", val => _gpu.friction = _gpuFriction = val);
            LoadJsonField(jsonClass, "gpuEnabled", val => _gpu.enabled = _gpuEnabled = val);
        }
    }

    protected override JSONClass DoGetJson()
    {
        var jsonClass = new JSONClass();

        jsonClass["radius"].AsFloat = _radius;

        jsonClass["centerX"].AsFloat = _center.x;
        jsonClass["centerY"].AsFloat = _center.y;
        jsonClass["centerZ"].AsFloat = _center.z;
        if (_gpu != null)
        {
            jsonClass["gpuFriction"].AsFloat = _gpuFriction;
            jsonClass["gpuEnabled"].AsBool = _gpuEnabled;
        }

        return jsonClass;
    }

    protected override void DoResetToInitial()
    {
        base.DoResetToInitial();
        Collider.radius = _radius = _initialRadius;
        Collider.center = _center = _initialCenter;
        if (_gpu != null)
        {
            _gpu.friction = _gpuFriction = _initialGpuFriction;
            _gpu.enabled = _gpuEnabled = _initialGpuEnabled;
        }
    }
}
