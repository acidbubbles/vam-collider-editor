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

    private JSONStorableFloat _radiusParam = null;
    private JSONStorableFloat _heightParam = null;
    private JSONStorableFloat _centerXParam = null;
    private JSONStorableFloat _centerYParam = null;
    private JSONStorableFloat _centerZParam = null;
    private JSONStorableFloat _gpuFrictionParam = null;
    private JSONStorableBool _gpuEnabledParam = null;

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
        _radiusParam = new JSONStorableFloat("radius", Collider.radius, value =>
        {
            SetRadius(value);
        }, 0f, _initialRadius * 4f, false);

        RegisterControl(Script.CreateFloatSlider(RegisterStorable(_radiusParam).WithDefault(_initialRadius), "Radius"));


        _heightParam = new JSONStorableFloat("height", Collider.height, value =>
        {
            SetHeight(value);
        }, 0f, _initialHeight * 4f, false);

        RegisterControl(Script.CreateFloatSlider(RegisterStorable(_heightParam).WithDefault(_initialHeight), "Height"));


        _centerXParam = new JSONStorableFloat("centerX", Collider.center.x, value =>
        {
            SetCenterX(value);
        }, MakeMinPosition(Collider.center.x), MakeMaxPosition(Collider.center.x), false);

        RegisterControl(Script.CreateFloatSlider(RegisterStorable(_centerXParam).WithDefault(_initialCenter.x), "Center.X"));


        _centerYParam = new JSONStorableFloat("centerY", Collider.center.y, value =>
        {
            SetCenterY(value);
        }, MakeMinPosition(Collider.center.y), MakeMaxPosition(Collider.center.y), false);

        RegisterControl(Script.CreateFloatSlider(RegisterStorable(_centerYParam).WithDefault(_initialCenter.y), "Center.Y"));


        _centerZParam = new JSONStorableFloat("centerZ", Collider.center.z, value =>
        {
            SetCenterZ(value);
        }, MakeMinPosition(Collider.center.z), MakeMaxPosition(Collider.center.z), false);

        RegisterControl(Script.CreateFloatSlider(RegisterStorable(_centerZParam).WithDefault(_initialCenter.z), "Center.Z"));


        if (_gpu != null)
        {
            _gpuFrictionParam = new JSONStorableFloat("gpu.friction", _gpu.friction, value =>
            {
                SetGpuFriction(value);
            }, 0f, Mathf.Ceil(_initialGpuFriction) * 2f, false);

            RegisterControl(Script.CreateFloatSlider(RegisterStorable(_gpuFrictionParam).WithDefault(_gpu.friction), "GPU Friction"));

            _gpuEnabledParam = new JSONStorableBool("gpu.enabled", _gpu.enabled, (bool value) =>
            {
                SetGpuEnabled(value);
            });

            RegisterControl(Script.CreateToggle(RegisterStorable(_gpuEnabledParam.WithDefault(_initialGpuEnabled)), "GPU Collider Enabled"));
        }
    }

    private void SetRadius(float value)
    {
        if (_radiusParam != null) _radiusParam.val = value;
        Collider.radius = _radius = value;
        _gpu?.UpdateData();
        SetModified();
        SyncPreview();
        SetOpposite((m, v) => (m as CapsuleColliderModel)?.SetRadius(v), value);
    }

    private void SetHeight(float value)
    {
        if (_heightParam != null) _heightParam.val = value;
        Collider.height = _height = value;
        _gpu?.UpdateData();
        SetModified();
        SyncPreview();
        SetOpposite((m, v) => (m as CapsuleColliderModel)?.SetHeight(v), value);
    }

    private void SetCenterX(float value)
    {
        if (_centerXParam != null) _centerXParam.val = value;
        var center = _center;
        center.x = value;
        Collider.center = _center = center;
        _gpu?.UpdateData();
        SetModified();
        SyncPreview();
        SetOpposite((m, v) => (m as CapsuleColliderModel)?.SetCenterX(v), -value);
    }

    private void SetCenterY(float value)
    {
        if (_centerYParam != null) _centerYParam.val = value;
        var center = _center;
        center.y = value;
        Collider.center = _center = center;
        _gpu?.UpdateData();
        SetModified();
        SyncPreview();
        SetOpposite((m, v) => (m as CapsuleColliderModel)?.SetCenterY(v), value);
    }

    private void SetCenterZ(float value)
    {
        if (_centerZParam != null) _centerZParam.val = value;
        var center = _center;
        center.z = value;
        Collider.center = _center = center;
        _gpu?.UpdateData();
        SetModified();
        SyncPreview();
        SetOpposite((m, v) => (m as CapsuleColliderModel)?.SetCenterZ(v), value);
    }

    private void SetGpuFriction(float value)
    {
        if (_gpuFrictionParam != null) _gpuFrictionParam.val = value;
        _gpu.friction = _gpuFriction = value;
        SetModified();
        SetOpposite((m, v) => (m as CapsuleColliderModel)?.SetGpuFriction(v), value);
    }

    private void SetGpuEnabled(bool value)
    {
        if (_gpuEnabledParam != null) _gpuEnabledParam.val = value;
        _gpu.enabled = _gpuEnabled = value;
        SetModified();
        SetOpposite((m, v) => (m as CapsuleColliderModel)?.SetGpuEnabled(v), value);
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

    public override void SyncPreview()
    {
        if (Preview == null) return;

        float size = Collider.radius * 2;
        float height = Collider.height / 2;
        Preview.transform.localScale = new Vector3(size, height, size);
        if (Collider.direction == 0)
            Preview.transform.localRotation = Quaternion.AngleAxis(90, Vector3.forward);
        else if (Collider.direction == 2)
            Preview.transform.localRotation = Quaternion.AngleAxis(90, Vector3.right);
        Preview.transform.localPosition = Collider.center;
    }
}
