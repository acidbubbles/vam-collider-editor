using SimpleJSON;
using UnityEngine;

public class BoxColliderModel : ColliderModel<BoxCollider>
{
    private readonly Vector3 _initialSize;
    private Vector3 _size;
    private readonly Vector3 _initialCenter;
    private Vector3 _center;

    public BoxColliderModel(MVRScript parent, BoxCollider collider, ColliderPreviewConfig config)
        : base(parent, collider, config)
    {
        _initialSize = _size = collider.size;
        _initialCenter = _center = collider.center;
    }

    public override bool SyncOverrides()
    {
        if (!Modified) return false;
        bool changed = false;
        if (Collider.size != _size)
        {
            Collider.size = _size;
            changed = true;
        }
        if (Collider.center != _center)
        {
            Collider.center = _center;
            changed = true;
        }
        return changed;
    }

    protected override GameObject DoCreatePreview() => GameObject.CreatePrimitive(PrimitiveType.Cube);

    public override void SyncPreviews()
    {
        SyncPreview(Preview);
        SyncPreview(XRayPreview);
    }

    private void SyncPreview(GameObject preview)
    {
        if (preview == null) return;

        preview.transform.localScale = Collider.size;
        preview.transform.localPosition = Collider.center;
    }

    protected override void DoLoadJson(JSONClass jsonClass)
    {
        LoadJsonField(jsonClass, "size", val => Collider.size = _size = val);
        LoadJsonField(jsonClass, "center", val => Collider.center = _center = val);
    }

    protected override JSONClass DoGetJson()
    {
        var jsonClass = new JSONClass();

        jsonClass["sizeX"].AsFloat = _size.x;
        jsonClass["sizeY"].AsFloat = _size.y;
        jsonClass["sizeZ"].AsFloat = _size.z;

        jsonClass["centerX"].AsFloat = _center.x;
        jsonClass["centerY"].AsFloat = _center.y;
        jsonClass["centerZ"].AsFloat = _center.z;

        return jsonClass;
    }

    protected override void DoResetToInitial()
    {
        base.DoResetToInitial();
        Collider.size = _size = _initialSize;
        Collider.center = _center = _initialCenter;
    }

    public override void DoCreateControls()
    {
        RegisterControl(Script.CreateFloatSlider(RegisterStorable(new JSONStorableFloat("sizeX", Collider.size.x, value =>
        {
            var size = _size;
            size.x = value;
            Collider.size = _size = size;
            SetModified();
            SyncPreviews();
        }, -0.25f, 0.25f, false)).WithDefault(_initialSize.x), "Size.X"));

        RegisterControl(Script.CreateFloatSlider(RegisterStorable(new JSONStorableFloat("sizeY", Collider.size.y, value =>
        {
            var size = _size;
            size.y = value;
            Collider.size = _size = size;
            SetModified();
            SyncPreviews();
        }, -0.25f, 0.25f, false)).WithDefault(_initialSize.y), "Size.Y"));

        RegisterControl(Script.CreateFloatSlider(RegisterStorable(new JSONStorableFloat("sizeZ", Collider.size.z, value =>
        {
            var size = _size;
            size.z = value;
            Collider.size = _size = size;
            SetModified();
            SyncPreviews();
        }, -0.25f, 0.25f, false)).WithDefault(_initialSize.z), "Size.Z"));

        RegisterControl(Script.CreateFloatSlider(RegisterStorable(new JSONStorableFloat("centerX", Collider.center.x, value =>
        {
            var center = _center;
            center.x = value;
            Collider.center = _center = center;
            SetModified();
            SyncPreviews();
        }, MakeMinPosition(Collider.center.x), MakeMaxPosition(Collider.center.x), false)).WithDefault(_initialCenter.x), "Center.X"));

        RegisterControl(Script.CreateFloatSlider(RegisterStorable(new JSONStorableFloat("centerY", Collider.center.y, value =>
        {
            var center = _center;
            center.y = value;
            Collider.center = _center = center;
            SetModified();
            SyncPreviews();
        }, MakeMinPosition(Collider.center.y), MakeMaxPosition(Collider.center.y), false)).WithDefault(_initialCenter.y), "Center.Y"));

        RegisterControl(Script.CreateFloatSlider(RegisterStorable(new JSONStorableFloat("centerZ", Collider.center.z, value =>
        {
            var center = _center;
            center.z = value;
            Collider.center = _center = center;
            SetModified();
            SyncPreviews();
        }, MakeMinPosition(Collider.center.z), MakeMaxPosition(Collider.center.z), false)).WithDefault(_initialCenter.z), "Center.Z"));
    }
}
