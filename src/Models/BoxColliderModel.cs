using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

public class BoxColliderModel : ColliderModel<BoxCollider>
{
    private JSONStorableFloat _centerXStorableFloat;
    private JSONStorableFloat _centerYStorableFloat;
    private JSONStorableFloat _centerZStorableFloat;
    private JSONStorableFloat _sizeXStorableFloat;
    private JSONStorableFloat _sizeYStorableFloat;
    private JSONStorableFloat _sizeZStorableFloat;

    private readonly Vector3 _initialSize;
    private readonly Vector3 _initialCenter;

    public BoxColliderModel(MVRScript parent, BoxCollider collider)
        : base(parent, collider)
    {
        _initialSize = collider.size;
        _initialCenter = collider.center;
    }

    protected override GameObject DoCreatePreview() => GameObject.CreatePrimitive(PrimitiveType.Cube);

    protected override void DoUpdatePreview()
    {
        if (Preview == null) return;

        Preview.transform.localScale = Collider.size;
        Preview.transform.localPosition = Collider.center;
    }

    protected override void DoUpdateControls()
    {
        if (_sizeXStorableFloat != null)
            _sizeXStorableFloat.valNoCallback = Collider.size.x;
        if (_sizeYStorableFloat != null)
            _sizeYStorableFloat.valNoCallback = Collider.size.y;
        if (_sizeZStorableFloat != null)
            _sizeZStorableFloat.valNoCallback = Collider.size.z;
        if (_centerXStorableFloat != null)
            _centerXStorableFloat.valNoCallback = Collider.center.x;
        if (_centerYStorableFloat != null)
            _centerYStorableFloat.valNoCallback = Collider.center.y;
        if (_centerZStorableFloat != null)
            _centerZStorableFloat.valNoCallback = Collider.center.z;
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
        RegisterControl(Script.CreateFloatSlider(RegisterStorable(_sizeXStorableFloat = new JSONStorableFloat("sizeX", Collider.size.x, value =>
        {
            var size = Collider.size;
            size.x = value;
            Collider.size = size;
            SetModified();
            DoUpdatePreview();
        }, -0.25f, 0.25f, false)).WithDefault(_initialSize.x), "Size.X"));

        RegisterControl(Script.CreateFloatSlider(RegisterStorable(_sizeYStorableFloat = new JSONStorableFloat("sizeY", Collider.size.y, value =>
        {
            var size = Collider.size;
            size.y = value;
            Collider.size = size;
            SetModified();
            DoUpdatePreview();
        }, -0.25f, 0.25f, false)).WithDefault(_initialSize.y), "Size.Y"));

        RegisterControl(Script.CreateFloatSlider(RegisterStorable(_sizeZStorableFloat = new JSONStorableFloat("sizeZ", Collider.size.z, value =>
        {
            var size = Collider.size;
            size.z = value;
            Collider.size = size;
            SetModified();
            DoUpdatePreview();
        }, -0.25f, 0.25f, false)).WithDefault(_initialSize.z), "Size.Z"));

        RegisterControl(Script.CreateFloatSlider(RegisterStorable(_centerXStorableFloat = new JSONStorableFloat("centerX", Collider.center.x, value =>
        {
            var center = Collider.center;
            center.x = value;
            Collider.center = center;
            SetModified();
            DoUpdatePreview();
        }, -0.25f, 0.25f, false)).WithDefault(_initialCenter.x), "Center.X"));

        RegisterControl(Script.CreateFloatSlider(RegisterStorable(_centerYStorableFloat = new JSONStorableFloat("centerY", Collider.center.y, value =>
        {
            var center = Collider.center;
            center.y = value;
            Collider.center = center;
            SetModified();
            DoUpdatePreview();
        }, -0.25f, 0.25f, false)).WithDefault(_initialCenter.y), "Center.Y"));

        RegisterControl(Script.CreateFloatSlider(RegisterStorable(_centerZStorableFloat = new JSONStorableFloat("centerZ", Collider.center.z, value =>
        {
            var center = Collider.center;
            center.z = value;
            Collider.center = center;
            SetModified();
            DoUpdatePreview();
        }, -0.25f, 0.25f, false)).WithDefault(_initialCenter.z), "Center.Z"));
    }

    protected override bool DeviatesFromInitial() => _initialSize != Collider.size || _initialCenter != Collider.center; // Vector3 has built in epsilon equality checks
}
