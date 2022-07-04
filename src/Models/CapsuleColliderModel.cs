using GPUTools.Physics.Scripts.Behaviours;
using SimpleJSON;
using UnityEngine;

public class CapsuleColliderModel : ColliderModel<CapsuleCollider>
{
    private readonly float _initialRadius;
    private float _radius;
    private readonly float _initialHeight;
    private float _height;
    private readonly Vector3 _initialCenter;
    private readonly float _initialGpuFriction;
    private readonly bool _initialGpuEnabled;
    private readonly CapsuleLineSphereCollider _gpu;
    private Vector3 _center;
    private float _gpuFriction;
    private bool _gpuEnabled;

    private JSONStorableFloat _radiusJSON;
    private JSONStorableFloat _heightJSON;
    private JSONStorableFloat _centerXJSON;
    private JSONStorableFloat _centerYJSON;
    private JSONStorableFloat _centerZJSON;
    private JSONStorableFloat _gpuFrictionJSON;
    private JSONStorableBool _gpuEnabledJSON;

    public CapsuleColliderModel(MVRScript parent, CapsuleCollider collider, ColliderPreviewConfig config)
        : base(parent, collider, config)
    {
        _initialRadius = _radius = collider.radius;
        _initialHeight = _height = collider.height;
        _initialCenter = _center = collider.center;
        _gpu = collider.gameObject.GetComponent<CapsuleLineSphereCollider>();
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
        if (Collider.height != _height)
        {
            Collider.height = _height;
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
            if (changed) _gpu.UpdateData();
        }
        return changed;
    }

    public override void DoCreateControls()
    {
        _radiusJSON = new JSONStorableFloat("radius", Collider.radius, SetRadius, 0f, _initialRadius * 4f, false).WithDefault(_initialRadius);
        RegisterControl(Script.CreateFloatSlider(RegisterStorable(_radiusJSON), "Radius"));

        _heightJSON = new JSONStorableFloat("height", Collider.height, SetHeight, 0f, _initialHeight * 4f, false).WithDefault(_initialHeight);
        RegisterControl(Script.CreateFloatSlider(RegisterStorable(_heightJSON), "Height"));

        _centerXJSON = new JSONStorableFloat("centerX", Collider.center.x, SetCenterX, MakeMinPosition(Collider.center.x), MakeMaxPosition(Collider.center.x), false).WithDefault(_initialCenter.x);
        RegisterControl(Script.CreateFloatSlider(RegisterStorable(_centerXJSON), "Center.X"));

        _centerYJSON = new JSONStorableFloat("centerY", Collider.center.y, SetCenterY, MakeMinPosition(Collider.center.y), MakeMaxPosition(Collider.center.y), false).WithDefault(_initialCenter.y);
        RegisterControl(Script.CreateFloatSlider(RegisterStorable(_centerYJSON), "Center.Y"));

        _centerZJSON = new JSONStorableFloat("centerZ", Collider.center.z, SetCenterZ, MakeMinPosition(Collider.center.z), MakeMaxPosition(Collider.center.z), false).WithDefault(_initialCenter.z);
        RegisterControl(Script.CreateFloatSlider(RegisterStorable(_centerZJSON), "Center.Z"));

        if (_gpu != null)
        {
            _gpuFrictionJSON = new JSONStorableFloat("gpu.friction", _gpu.friction, SetGpuFriction, 0f, Mathf.Ceil(_initialGpuFriction) * 2f, false).WithDefault(_gpu.friction);
            RegisterControl(Script.CreateFloatSlider(RegisterStorable(_gpuFrictionJSON), "GPU Friction"));

            _gpuEnabledJSON = new JSONStorableBool("gpu.enabled", _gpu.enabled, SetGpuEnabled).WithDefault(_initialGpuEnabled);
            RegisterControl(Script.CreateToggle(RegisterStorable(_gpuEnabledJSON), "GPU Collider Enabled"));
        }
    }

    private void SetRadius(float value)
    {
        if (_radiusJSON != null) _radiusJSON.valNoCallback = value;
        Collider.radius = _radius = value;
        _gpu?.UpdateData();
        SetModified();
        SyncPreviews();
        SetMirror<CapsuleColliderModel>(m => m.SetRadius(value));
    }

    private void SetHeight(float value)
    {
        if (_heightJSON != null) _heightJSON.valNoCallback = value;
        Collider.height = _height = value;
        _gpu?.UpdateData();
        SetModified();
        SyncPreviews();
        SetMirror<CapsuleColliderModel>(m => m.SetHeight(value));
    }

    private void SetCenterX(float value)
    {
        if (_centerXJSON != null) _centerXJSON.valNoCallback = value;
        var center = _center;
        center.x = value;
        Collider.center = _center = center;
        _gpu?.UpdateData();
        SetModified();
        SyncPreviews();
        SetMirror<CapsuleColliderModel>(m => m.SetCenterX(-value));
    }

    private void SetCenterY(float value)
    {
        if (_centerYJSON != null) _centerYJSON.valNoCallback = value;
        var center = _center;
        center.y = value;
        Collider.center = _center = center;
        _gpu?.UpdateData();
        SetModified();
        SyncPreviews();
        SetMirror<CapsuleColliderModel>(m => m.SetCenterY(value));
    }

    private void SetCenterZ(float value)
    {
        if (_centerZJSON != null) _centerZJSON.valNoCallback = value;
        var center = _center;
        center.z = value;
        Collider.center = _center = center;
        _gpu?.UpdateData();
        SetModified();
        SyncPreviews();
        SetMirror<CapsuleColliderModel>(m => m.SetCenterZ(value));
    }

    private void SetGpuFriction(float value)
    {
        if (_gpuFrictionJSON != null) _gpuFrictionJSON.valNoCallback = value;
        _gpu.friction = _gpuFriction = value;
        SetModified();
        SetMirror<CapsuleColliderModel>(m => m.SetGpuFriction(value));
    }

    private void SetGpuEnabled(bool value)
    {
        if (_gpuEnabledJSON != null) _gpuEnabledJSON.valNoCallback = value;
        _gpu.enabled = _gpuEnabled = value;
        SetModified();
        SetMirror<CapsuleColliderModel>(m => m.SetGpuEnabled(value));
    }

    protected override void DoLoadJson(JSONClass jsonClass)
    {
        LoadJsonField(jsonClass, "radius", val => Collider.radius = _radius = val);
        LoadJsonField(jsonClass, "height", val => Collider.height = _height = val);
        LoadJsonField(jsonClass, "center", val => Collider.center = _center = val);
        if (_gpu != null)
        {
            LoadJsonField(jsonClass, "gpuFriction", val => _gpu.friction = _gpuFriction = val);
            LoadJsonField(jsonClass, "gpuEnabled", val => _gpu.enabled = _gpuEnabled = val);
        }
        _gpu?.UpdateData();
    }

    protected override JSONClass DoGetJson()
    {
        var jsonClass = new JSONClass();
        jsonClass["radius"].AsFloat = _radius;
        jsonClass["height"].AsFloat = _height;
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
        Collider.height = _height = _initialHeight;
        Collider.center = _center = _initialCenter;
        if (_gpu != null)
        {
            _gpu.friction = _gpuFriction = _initialGpuFriction;
            _gpu.enabled = _gpuEnabled = _initialGpuEnabled;
            _gpu.UpdateData();
        }
    }

    protected override GameObject DoCreatePreview() => GameObject.CreatePrimitive(PrimitiveType.Capsule);

    public override void SyncPreviews()
    {
        SyncPreview(Preview);
        SyncPreview(XRayPreview);
    }

    private void SyncPreview(GameObject preview)
    {
        if (preview == null) return;

        float size = Collider.radius * 2;
        float height = Collider.height / 2;
        preview.transform.localScale = new Vector3(size, height, size);
        if (Collider.direction == 0)
            preview.transform.localRotation = Quaternion.AngleAxis(90, Vector3.forward);
        else if (Collider.direction == 2)
            preview.transform.localRotation = Quaternion.AngleAxis(90, Vector3.right);
        preview.transform.localPosition = Collider.center;
    }
}
