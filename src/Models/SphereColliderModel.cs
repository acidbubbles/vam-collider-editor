using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

public class SphereColliderModel : ColliderModel<SphereCollider>
{
    private JSONStorableFloat _centerXStorableFloat;
    private JSONStorableFloat _centerYStorableFloat;
    private JSONStorableFloat _centerZStorableFloat;
    private JSONStorableFloat _radiusStorableFloat;

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

    protected override void DoUpdateControls()
    {
        if (_radiusStorableFloat != null)
            _radiusStorableFloat.valNoCallback = Collider.radius;
        if (_centerXStorableFloat != null)
            _centerXStorableFloat.valNoCallback = Collider.center.x;
        if (_centerYStorableFloat != null)
            _centerYStorableFloat.valNoCallback = Collider.center.y;
        if (_centerZStorableFloat != null)
            _centerZStorableFloat.valNoCallback = Collider.center.z;
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
        RegisterControl(Script.CreateFloatSlider(RegisterStorable(_radiusStorableFloat = new JSONStorableFloat("radius", Collider.radius, value =>
        {
            Collider.radius = value;
            SetModified();
            DoUpdatePreviewFromCollider();
        }, 0f, _initialRadius * 4f, false)).WithDefault(_initialRadius), "Radius"));

        RegisterControl(Script.CreateFloatSlider(RegisterStorable(_centerXStorableFloat = new JSONStorableFloat("centerX", Collider.center.x, value =>
        {
            var center = Collider.center;
            center.x = value;
            Collider.center = center;
            SetModified();
            DoUpdatePreviewFromCollider();
        }, -0.25f, 0.25f, false)).WithDefault(_initialCenter.x), "Center.X"));

        RegisterControl(Script.CreateFloatSlider(RegisterStorable(_centerYStorableFloat = new JSONStorableFloat("centerY", Collider.center.y, value =>
        {
            var center = Collider.center;
            center.y = value;
            Collider.center = center;
            SetModified();
            DoUpdatePreviewFromCollider();
        }, -0.25f, 0.25f, false)).WithDefault(_initialCenter.y), "Center.Y"));

        RegisterControl(Script.CreateFloatSlider(RegisterStorable(_centerZStorableFloat = new JSONStorableFloat("centerZ", Collider.center.z, value =>
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
