using SimpleJSON;
using UnityEngine;

public class SphereColliderModel : ColliderModel<SphereCollider>
{
    private readonly float _initialRadius;
    private float _radius;
    private readonly Vector3 _initialCenter;
    private Vector3 _center;

    public SphereColliderModel(MVRScript parent, SphereCollider collider, ColliderPreviewConfig config)
        : base(parent, collider, config)
    {
        _initialRadius = _radius = collider.radius;
        _initialCenter = _center = collider.center;
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
        return changed;
    }

    protected override GameObject DoCreatePreview() => GameObject.CreatePrimitive(PrimitiveType.Sphere);

    public override void SyncPreview()
    {
        if (Preview == null) return;

        Preview.transform.localScale = Vector3.one * (Collider.radius * 2);
        Preview.transform.localPosition = Collider.center;
    }

    protected override void DoLoadJson(JSONClass jsonClass)
    {
        LoadJsonField(jsonClass, "radius", val => Collider.radius = _radius = val);
        LoadJsonField(jsonClass, "center", val => Collider.center = _center = val);
    }

    protected override JSONClass DoGetJson()
    {
        var jsonClass = new JSONClass();

        jsonClass["radius"].AsFloat = _radius;

        jsonClass["centerX"].AsFloat = _center.x;
        jsonClass["centerY"].AsFloat = _center.y;
        jsonClass["centerZ"].AsFloat = _center.z;

        return jsonClass;
    }

    protected override void DoResetToInitial()
    {
        base.DoResetToInitial();
        Collider.radius = _radius = _initialRadius;
        Collider.center = _center = _initialCenter;
    }

    public override void DoCreateControls()
    {
        RegisterControl(Script.CreateFloatSlider(RegisterStorable(new JSONStorableFloat("radius", Collider.radius, value =>
        {
            Collider.radius = _radius = value;
            SetModified();
            SyncPreview();
        }, 0f, _initialRadius * 4f, false)).WithDefault(_initialRadius), "Radius"));

        RegisterControl(Script.CreateFloatSlider(RegisterStorable(new JSONStorableFloat("centerX", Collider.center.x, value =>
        {
            var center = _center;
            center.x = value;
            Collider.center = _center = center;
            SetModified();
            SyncPreview();
        }, -0.25f, 0.25f, false)).WithDefault(_initialCenter.x), "Center.X"));

        RegisterControl(Script.CreateFloatSlider(RegisterStorable(new JSONStorableFloat("centerY", Collider.center.y, value =>
        {
            var center = _center;
            center.y = value;
            Collider.center = _center = center;
            SetModified();
            SyncPreview();
        }, -0.25f, 0.25f, false)).WithDefault(_initialCenter.y), "Center.Y"));

        RegisterControl(Script.CreateFloatSlider(RegisterStorable(new JSONStorableFloat("centerZ", Collider.center.z, value =>
        {
            var center = _center;
            center.z = value;
            Collider.center = _center = center;
            SetModified();
            SyncPreview();
        }, -0.25f, 0.25f, false)).WithDefault(_initialCenter.z), "Center.Z"));
    }

    protected override bool DeviatesFromInitial() =>
        !Mathf.Approximately(_initialRadius, Collider.radius) ||
        _initialCenter != Collider.center; // Vector3 has built in epsilon equality checks
}
