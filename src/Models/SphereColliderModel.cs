using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

public class SphereColliderModel : ColliderModel<SphereCollider>
{
    private JSONStorableFloat _centerXStorableFloat;
    private JSONStorableFloat _centerYStorableFloat;
    private JSONStorableFloat _centerZStorableFloat;
    private JSONStorableFloat _radiusStorableFloat;

    public float InitialRadius { get; set; }
    public Vector3 InitialCenter { get; set; }

    public SphereColliderModel(MVRScript parent, SphereCollider collider)
        : base(parent, collider)
    {
        InitialRadius = collider.radius;
        InitialCenter = collider.center;
    }

    protected override GameObject DoCreatePreview() => GameObject.CreatePrimitive(PrimitiveType.Sphere);

    protected override void DoUpdatePreview()
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
        Collider.radius = jsonClass["radius"].AsFloat;

        var center = Collider.center;
        center.x = jsonClass["centerX"].AsFloat;
        center.y = jsonClass["centerY"].AsFloat;
        center.z = jsonClass["centerZ"].AsFloat;
        Collider.center = center;
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
        Collider.radius = InitialRadius;
        Collider.center = InitialCenter;
    }

    public override IEnumerable<UIDynamic> DoCreateControls()
    {
        yield return Script.CreateFloatSlider(_radiusStorableFloat = new JSONStorableFloat("radius", Collider.radius, value =>
        {
            Collider.radius = value;
            DoUpdatePreview();
        }, 0f, InitialRadius * 4f, false).WithDefault(InitialRadius), "Radius");

        yield return Script.CreateFloatSlider(_centerXStorableFloat = new JSONStorableFloat("centerX", Collider.center.x, value =>
        {
            var center = Collider.center;
            center.x = value;
            Collider.center = center;
            DoUpdatePreview();
        }, -0.25f, 0.25f, false).WithDefault(InitialCenter.x), "Center.X");

        yield return Script.CreateFloatSlider(_centerYStorableFloat = new JSONStorableFloat("centerY", Collider.center.y, value =>
        {
            var center = Collider.center;
            center.y = value;
            Collider.center = center;
            DoUpdatePreview();
        }, -0.25f, 0.25f, false).WithDefault(InitialCenter.y), "Center.Y");

        yield return Script.CreateFloatSlider(_centerZStorableFloat = new JSONStorableFloat("centerZ", Collider.center.z, value =>
        {
            var center = Collider.center;
            center.z = value;
            Collider.center = center;
            DoUpdatePreview();
        }, -0.25f, 0.25f, false).WithDefault(InitialCenter.z), "Center.Z");
    }

    protected override bool DeviatesFromInitial() =>
        !Mathf.Approximately(InitialRadius, Collider.radius) ||
        InitialCenter != Collider.center; // Vector3 has built in epsilon equality checks
}
