using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

public class CapsuleColliderModel : ColliderModel<CapsuleCollider>
{
    private JSONStorableFloat _centerXStorableFloat;
    private JSONStorableFloat _centerYStorableFloat;
    private JSONStorableFloat _centerZStorableFloat;
    private JSONStorableFloat _heightStorableFloat;
    private JSONStorableFloat _radiusStorableFloat;

    public float InitialRadius { get; set; }
    public float InitialHeight { get; set; }
    public Vector3 InitialCenter { get; set; }

    public CapsuleColliderModel(MVRScript parent, CapsuleCollider collider)
        : base(parent, collider, collider.name)
    {
        InitialRadius = collider.radius;
        InitialHeight = collider.height;
        InitialCenter = collider.center;
    }

    public override IEnumerable<UIDynamic> DoCreateControls()
    {
        yield return Script.CreateFloatSlider(_radiusStorableFloat = new JSONStorableFloat("radius", Collider.radius, value =>
        {
            Collider.radius = value;
            DoUpdatePreview();
        }, 0f, InitialRadius * 4f, false).WithDefault(InitialRadius), "Radius");

        yield return Script.CreateFloatSlider(_heightStorableFloat = new JSONStorableFloat("height", Collider.height, value =>
        {
            Collider.height = value;
            DoUpdatePreview();
        }, 0f, InitialHeight * 4f, false).WithDefault(InitialHeight), "Height");

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

    protected override void DoLoadJson(JSONClass jsonClass)
    {
        Collider.radius = jsonClass["radius"].AsFloat;
        Collider.height = jsonClass["height"].AsFloat;

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
        jsonClass["height"].AsFloat = Collider.height;
        jsonClass["centerX"].AsFloat = Collider.center.x;
        jsonClass["centerY"].AsFloat = Collider.center.y;
        jsonClass["centerZ"].AsFloat = Collider.center.z;
        return jsonClass;
    }

    protected override void DoResetToInitial()
    {
        Collider.radius = InitialRadius;
        Collider.height = InitialHeight;
        Collider.center = InitialCenter;
    }

    protected override bool DeviatesFromInitial() =>
        !Mathf.Approximately(InitialRadius, Collider.radius) ||
        !Mathf.Approximately(InitialHeight, Collider.height) ||
        InitialCenter != Collider.center; // Vector3 has built in epsilon equality checks

    protected override GameObject DoCreatePreview() => GameObject.CreatePrimitive(PrimitiveType.Capsule);

    protected override void DoUpdatePreview()
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

    protected override void DoUpdateControls()
    {
        if (_radiusStorableFloat != null)
            _radiusStorableFloat.valNoCallback = Collider.radius;
        if (_heightStorableFloat != null)
            _heightStorableFloat.valNoCallback = Collider.height;
        if (_centerXStorableFloat != null)
            _centerXStorableFloat.valNoCallback = Collider.center.x;
        if (_centerYStorableFloat != null)
            _centerYStorableFloat.valNoCallback = Collider.center.y;
        if (_centerZStorableFloat != null)
            _centerZStorableFloat.valNoCallback = Collider.center.z;
    }
}
