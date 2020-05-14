using SimpleJSON;
using UnityEngine;

public class SphereColliderModel : ColliderModel<SphereCollider>
{
    private readonly float _initialRadius;
    private readonly Vector3 _initialCenter;

    public SphereColliderModel(MVRScript parent, SphereCollider collider, ColliderPreviewConfig config)
        : base(parent, collider, config)
    {
        _initialRadius = collider.radius;
        _initialCenter = collider.center;
    }

    protected override GameObject DoCreatePreview() => GameObject.CreatePrimitive(PrimitiveType.Sphere);

    protected override void DoUpdatePreviewFromCollider()
    {
        if (Preview == null) return;

        Preview.transform.localScale = Vector3.one * (Collider.radius * 2);
        Preview.transform.localPosition = Collider.center;
    }

    protected override void DoLoadJson(JSONClass jsonClass)
    {
        LoadJsonField(jsonClass, "radius", val => Collider.radius = val);
        LoadJsonField(jsonClass, "center", val => Collider.center = val);
    }

    protected override JSONClass DoGetJson()
    {
        var jsonClass = new JSONClass();

        jsonClass["radius"].AsFloat = Collider.radius;

        jsonClass["centerX"].AsFloat = Collider.center.x;
        jsonClass["centerY"].AsFloat = Collider.center.y;
        jsonClass["centerZ"].AsFloat = Collider.center.z;

        return jsonClass;
    }

    protected override void DoResetToInitial()
    {
        base.DoResetToInitial();
        Collider.radius = _initialRadius;
        Collider.center = _initialCenter;
    }

    public override void DoCreateControls()
    {
        RegisterControl(Script.CreateFloatSlider(RegisterStorable(new JSONStorableFloat("radius", Collider.radius, value =>
        {
            Collider.radius = value;
            SetModified();
            DoUpdatePreviewFromCollider();
        }, 0f, _initialRadius * 4f, false)).WithDefault(_initialRadius), "Radius"));

        RegisterControl(Script.CreateFloatSlider(RegisterStorable(new JSONStorableFloat("centerX", Collider.center.x, value =>
        {
            var center = Collider.center;
            center.x = value;
            Collider.center = center;
            SetModified();
            DoUpdatePreviewFromCollider();
        }, -0.25f, 0.25f, false)).WithDefault(_initialCenter.x), "Center.X"));

        RegisterControl(Script.CreateFloatSlider(RegisterStorable(new JSONStorableFloat("centerY", Collider.center.y, value =>
        {
            var center = Collider.center;
            center.y = value;
            Collider.center = center;
            SetModified();
            DoUpdatePreviewFromCollider();
        }, -0.25f, 0.25f, false)).WithDefault(_initialCenter.y), "Center.Y"));

        RegisterControl(Script.CreateFloatSlider(RegisterStorable(new JSONStorableFloat("centerZ", Collider.center.z, value =>
        {
            var center = Collider.center;
            center.z = value;
            Collider.center = center;
            SetModified();
            DoUpdatePreviewFromCollider();
        }, -0.25f, 0.25f, false)).WithDefault(_initialCenter.z), "Center.Z"));
    }

    protected override bool DeviatesFromInitial() =>
        !Mathf.Approximately(_initialRadius, Collider.radius) ||
        _initialCenter != Collider.center; // Vector3 has built in epsilon equality checks
}
