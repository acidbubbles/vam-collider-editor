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

    public Vector3 InitialSize { get; set; }
    public Vector3 InitialCenter { get; set; }

    public BoxColliderModel(MVRScript parent, BoxCollider collider)
        : base(parent, collider)
    {
        InitialSize = collider.size;
        InitialCenter = collider.center;
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
        var size = Collider.size;
        size.x = jsonClass["sizeX"].AsFloat;
        size.y = jsonClass["sizeY"].AsFloat;
        size.z = jsonClass["sizeZ"].AsFloat;
        Collider.size = size;

        var center = Collider.center;
        center.x = jsonClass["centerX"].AsFloat;
        center.y = jsonClass["centerY"].AsFloat;
        center.z = jsonClass["centerZ"].AsFloat;
        Collider.center = center;
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
        Collider.size = InitialSize;
        Collider.center = InitialCenter;
    }

    public override IEnumerable<UIDynamic> DoCreateControls()
    {
        yield return Script.CreateFloatSlider(_sizeXStorableFloat = new JSONStorableFloat("sizeX", Collider.size.x, value =>
        {
            var size = Collider.size;
            size.x = value;
            Collider.size = size;
            DoUpdatePreview();
        }, -0.25f, 0.25f, false).WithDefault(InitialSize.x), "Size.X");

        yield return Script.CreateFloatSlider(_sizeYStorableFloat = new JSONStorableFloat("sizeY", Collider.size.y, value =>
        {
            var size = Collider.size;
            size.y = value;
            Collider.size = size;
            DoUpdatePreview();
        }, -0.25f, 0.25f, false).WithDefault(InitialSize.y), "Size.Y");

        yield return Script.CreateFloatSlider(_sizeZStorableFloat = new JSONStorableFloat("sizeZ", Collider.size.z, value =>
        {
            var size = Collider.size;
            size.z = value;
            Collider.size = size;
            DoUpdatePreview();
        }, -0.25f, 0.25f, false).WithDefault(InitialSize.z), "Size.Z");

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

    protected override bool DeviatesFromInitial() => InitialSize != Collider.size || InitialCenter != Collider.center; // Vector3 has built in epsilon equality checks
}
