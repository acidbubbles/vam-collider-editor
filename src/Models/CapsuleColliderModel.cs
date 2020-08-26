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
    private readonly float _initialFriction;
    private readonly CapsuleLineSphereCollider _gpu;
    private Vector3 _center;
    private float _friction;

    public CapsuleColliderModel(MVRScript parent, CapsuleCollider collider, ColliderPreviewConfig config)
        : base(parent, collider, config)
    {
        _initialRadius = _radius = collider.radius;
        _initialHeight = _height = collider.height;
        _initialCenter = _center = collider.center;
        _gpu = collider.gameObject.GetComponent<CapsuleLineSphereCollider>();
        if (_gpu != null)
        {
            _initialFriction = _friction = _gpu.friction;
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
        if (_gpu?.friction != _friction)
        {
            _gpu.friction = _friction;
            changed = true;
        }
        if (changed) _gpu?.UpdateData();
        return changed;
    }

    public override void DoCreateControls()
    {
        RegisterControl(Script.CreateFloatSlider(RegisterStorable(new JSONStorableFloat("radius", Collider.radius, value =>
        {
            Collider.radius = _radius = value;
            _gpu?.UpdateData();
            SetModified();
            SyncPreview();
        }, 0f, _initialRadius * 4f, false)).WithDefault(_initialRadius), "Radius"));

        RegisterControl(Script.CreateFloatSlider(RegisterStorable(new JSONStorableFloat("height", Collider.height, value =>
        {
            Collider.height = _height = value;
            _gpu?.UpdateData();
            SetModified();
            SyncPreview();
        }, 0f, _initialHeight * 4f, false)).WithDefault(_initialHeight), "Height"));

        RegisterControl(Script.CreateFloatSlider(RegisterStorable(new JSONStorableFloat("centerX", Collider.center.x, value =>
        {
            var center = _center;
            center.x = value;
            Collider.center = _center = center;
            _gpu?.UpdateData();
            SetModified();
            SyncPreview();
        }, -0.25f, 0.25f, false)).WithDefault(_initialCenter.x), "Center.X"));

        RegisterControl(Script.CreateFloatSlider(RegisterStorable(new JSONStorableFloat("centerY", Collider.center.y, value =>
        {
            var center = _center;
            center.y = value;
            Collider.center = _center = center;
            _gpu?.UpdateData();
            SetModified();
            SyncPreview();
        }, -0.25f, 0.25f, false)).WithDefault(_initialCenter.y), "Center.Y"));

        RegisterControl(Script.CreateFloatSlider(RegisterStorable(new JSONStorableFloat("centerZ", Collider.center.z, value =>
        {
            var center = _center;
            center.z = value;
            Collider.center = _center = center;
            _gpu?.UpdateData();
            SetModified();
            SyncPreview();
        }, -0.25f, 0.25f, false)).WithDefault(_initialCenter.z), "Center.Z"));

        if (_gpu != null)
        {
            RegisterControl(Script.CreateFloatSlider(RegisterStorable(new JSONStorableFloat("friction", _gpu.friction, value =>
            {
                _gpu.friction = _friction = value;
                SetModified();
            }, 0f, Mathf.Ceil(_initialFriction) * 2f, false)).WithDefault(_gpu.friction), "Friction"));
        }
    }

    protected override void DoLoadJson(JSONClass jsonClass)
    {
        LoadJsonField(jsonClass, "radius", val => Collider.radius = _radius = val);
        LoadJsonField(jsonClass, "height", val => Collider.height = _height = val);
        LoadJsonField(jsonClass, "center", val => Collider.center = _center = val);
        if (_gpu != null)
        {
            LoadJsonField(jsonClass, "friction", val => _gpu.friction = _friction = val);
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
            jsonClass["friction"].AsFloat = _friction;
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
            _gpu.friction = _friction = _initialFriction;
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
