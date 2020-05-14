using SimpleJSON;
using UnityEngine;

public class CapsuleColliderModel : ColliderModel<CapsuleCollider>
{
    private readonly float _initialRadius;
    private readonly float _initialHeight;
    private readonly Vector3 _initialCenter;

    public CapsuleColliderModel(MVRScript parent, CapsuleCollider collider, ColliderPreviewConfig config)
        : base(parent, collider, config)
    {
        _initialRadius = collider.radius;
        _initialHeight = collider.height;
        _initialCenter = collider.center;
    }

    public override void DoCreateControls()
    {
        RegisterControl(Script.CreateFloatSlider(RegisterStorable(new JSONStorableFloat("radius", Collider.radius, value =>
        {
            Collider.radius = value;
            SetModified();
            DoUpdatePreviewFromCollider();
        }, 0f, _initialRadius * 4f, false)).WithDefault(_initialRadius), "Radius"));

        RegisterControl(Script.CreateFloatSlider(RegisterStorable(new JSONStorableFloat("height", Collider.height, value =>
        {
            Collider.height = value;
            SetModified();
            DoUpdatePreviewFromCollider();
        }, 0f, _initialHeight * 4f, false)).WithDefault(_initialHeight), "Height"));

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

    protected override void DoLoadJson(JSONClass jsonClass)
    {
        LoadJsonField(jsonClass, "radius", val => Collider.radius = val);
        LoadJsonField(jsonClass, "height", val => Collider.height = val);
        LoadJsonField(jsonClass, "center", val => Collider.center = val);
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
        base.DoResetToInitial();
        Collider.radius = _initialRadius;
        Collider.height = _initialHeight;
        Collider.center = _initialCenter;
    }

    protected override bool DeviatesFromInitial() =>
        !Mathf.Approximately(_initialRadius, Collider.radius) ||
        !Mathf.Approximately(_initialHeight, Collider.height) ||
        _initialCenter != Collider.center; // Vector3 has built in epsilon equality checks

    protected override GameObject DoCreatePreview() => GameObject.CreatePrimitive(PrimitiveType.Capsule);

    protected override void DoUpdatePreviewFromCollider()
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
