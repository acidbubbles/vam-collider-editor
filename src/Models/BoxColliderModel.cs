using SimpleJSON;
using UnityEngine;

public class BoxColliderModel : ColliderModel<BoxCollider>
{
    private readonly Vector3 _initialSize;
    private readonly Vector3 _initialCenter;

    public BoxColliderModel(MVRScript parent, BoxCollider collider, ColliderPreviewConfig config)
        : base(parent, collider, config)
    {
        _initialSize = collider.size;
        _initialCenter = collider.center;
    }

    protected override GameObject DoCreatePreview() => GameObject.CreatePrimitive(PrimitiveType.Cube);

    public override void UpdatePreviewFromCollider()
    {
        if (Preview == null) return;

        Preview.transform.localScale = Collider.size;
        Preview.transform.localPosition = Collider.center;
    }

    protected override void DoLoadJson(JSONClass jsonClass)
    {
        LoadJsonField(jsonClass, "size", val => Collider.size = val);
        LoadJsonField(jsonClass, "center", val => Collider.center = val);
    }

    protected override JSONClass DoGetJson()
    {
        var jsonClass = new JSONClass();

        jsonClass["sizeX"].AsFloat = Collider.size.x;
        jsonClass["sizeY"].AsFloat = Collider.size.y;
        jsonClass["sizeZ"].AsFloat = Collider.size.z;

        jsonClass["centerX"].AsFloat = Collider.center.x;
        jsonClass["centerY"].AsFloat = Collider.center.y;
        jsonClass["centerZ"].AsFloat = Collider.center.z;

        return jsonClass;
    }

    protected override void DoResetToInitial()
    {
        base.DoResetToInitial();
        Collider.size = _initialSize;
        Collider.center = _initialCenter;
    }

    public override void DoCreateControls()
    {
        RegisterControl(Script.CreateFloatSlider(RegisterStorable(new JSONStorableFloat("sizeX", Collider.size.x, value =>
        {
            var size = Collider.size;
            size.x = value;
            Collider.size = size;
            SetModified();
            UpdatePreviewFromCollider();
        }, -0.25f, 0.25f, false)).WithDefault(_initialSize.x), "Size.X"));

        RegisterControl(Script.CreateFloatSlider(RegisterStorable(new JSONStorableFloat("sizeY", Collider.size.y, value =>
        {
            var size = Collider.size;
            size.y = value;
            Collider.size = size;
            SetModified();
            UpdatePreviewFromCollider();
        }, -0.25f, 0.25f, false)).WithDefault(_initialSize.y), "Size.Y"));

        RegisterControl(Script.CreateFloatSlider(RegisterStorable(new JSONStorableFloat("sizeZ", Collider.size.z, value =>
        {
            var size = Collider.size;
            size.z = value;
            Collider.size = size;
            SetModified();
            UpdatePreviewFromCollider();
        }, -0.25f, 0.25f, false)).WithDefault(_initialSize.z), "Size.Z"));

        RegisterControl(Script.CreateFloatSlider(RegisterStorable(new JSONStorableFloat("centerX", Collider.center.x, value =>
        {
            var center = Collider.center;
            center.x = value;
            Collider.center = center;
            SetModified();
            UpdatePreviewFromCollider();
        }, -0.25f, 0.25f, false)).WithDefault(_initialCenter.x), "Center.X"));

        RegisterControl(Script.CreateFloatSlider(RegisterStorable(new JSONStorableFloat("centerY", Collider.center.y, value =>
        {
            var center = Collider.center;
            center.y = value;
            Collider.center = center;
            SetModified();
            UpdatePreviewFromCollider();
        }, -0.25f, 0.25f, false)).WithDefault(_initialCenter.y), "Center.Y"));

        RegisterControl(Script.CreateFloatSlider(RegisterStorable(new JSONStorableFloat("centerZ", Collider.center.z, value =>
        {
            var center = Collider.center;
            center.z = value;
            Collider.center = center;
            SetModified();
            UpdatePreviewFromCollider();
        }, -0.25f, 0.25f, false)).WithDefault(_initialCenter.z), "Center.Z"));
    }

    protected override bool DeviatesFromInitial() => _initialSize != Collider.size || _initialCenter != Collider.center; // Vector3 has built in epsilon equality checks
}
