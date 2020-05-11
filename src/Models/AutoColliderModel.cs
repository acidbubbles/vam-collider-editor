using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

using Object = UnityEngine.Object;
public class AutoColliderModel : ModelBase<AutoCollider>, IModel
{
    private readonly float _initialAutoLengthBuffer;
    private readonly float _initialAutoRadiusBuffer;
    private readonly float _initialAutoRadiusMultiplier;

    public List<UIDynamic> Controls { get; private set; }

    public AutoColliderModel(MVRScript script, AutoCollider autoCollider)
        : base(script, autoCollider, GetLabel(autoCollider))
    {
        _initialAutoLengthBuffer = autoCollider.autoLengthBuffer;
        _initialAutoRadiusBuffer = autoCollider.autoRadiusBuffer;
        _initialAutoRadiusMultiplier = autoCollider.autoRadiusMultiplier;
    }

    private static string GetLabel(AutoCollider autoCollider)
    {
        var label = autoCollider.name;
        if (label.StartsWith("AutoColliderAutoColliders"))
            return label.Substring("AutoColliderAutoColliders".Length);
        else if (label.StartsWith("AutoColliderFemaleAutoColliders"))
            return label.Substring("AutoColliderFemaleAutoColliders".Length);
        else if (label.StartsWith("AutoCollider"))
            return label.Substring("AutoCollider".Length);
        else
            return label;
    }

    protected override void CreateControls()
    {
        DestroyControls();

        var controls = new List<UIDynamic>();

        var resetUi = Script.CreateButton("Reset AutoCollider", true);
        resetUi.button.onClick.AddListener(ResetToInitial);

        controls.Add(resetUi);
        controls.AddRange(DoCreateControls());

        Controls = controls;
    }

    public IEnumerable<UIDynamic> DoCreateControls()
    {
        yield return Script.CreateFloatSlider(new JSONStorableFloat("autoLengthBuffer", Component.autoLengthBuffer, value =>
        {
            Component.autoLengthBuffer = value;
        }, 0f, _initialAutoLengthBuffer * 4f, false).WithDefault(_initialAutoLengthBuffer), "Auto Length Buffer");

        yield return Script.CreateFloatSlider(new JSONStorableFloat("autoRadiusBuffer", Component.autoRadiusBuffer, value =>
        {
            Component.autoRadiusBuffer = value;
        }, 0f, _initialAutoRadiusBuffer * 4f, false).WithDefault(_initialAutoRadiusBuffer), "Auto Radius Buffer");

        yield return Script.CreateFloatSlider(new JSONStorableFloat("autoRadiusMultiplier", Component.autoRadiusMultiplier, value =>
        {
            Component.autoRadiusMultiplier = value;
        }, 0f, _initialAutoRadiusMultiplier * 4f, false).WithDefault(_initialAutoRadiusMultiplier), "Auto Radius Multiplier");
    }

    protected override void DestroyControls()
    {
        if (Controls == null)
            return;

        foreach (var adjustmentJson in Controls)
            Object.Destroy(adjustmentJson.gameObject);

        Controls.Clear();
    }

    protected override void DoLoadJson(JSONClass jsonClass)
    {
        Component.autoLengthBuffer = jsonClass["autoLengthBuffer"].AsFloat;
        Component.autoRadiusBuffer = jsonClass["autoRadiusBuffer"].AsFloat;
        Component.autoRadiusMultiplier = jsonClass["autoRadiusMultiplier"].AsFloat;
    }

    protected override JSONClass DoGetJson()
    {
        var jsonClass = new JSONClass();
        jsonClass["autoLengthBuffer"].AsFloat = Component.autoLengthBuffer;
        jsonClass["autoRadiusBuffer"].AsFloat = Component.autoRadiusBuffer;
        jsonClass["autoRadiusMultiplier"].AsFloat = Component.autoRadiusMultiplier;
        return jsonClass;
    }

    public void ResetToInitial()
    {
        DoResetToInitial();

        if (Selected)
        {
            DestroyControls();
            CreateControls();
        }
    }

    protected void DoResetToInitial()
    {
        Component.autoRadiusBuffer = _initialAutoRadiusBuffer;
    }

    public IEnumerable<Collider> GetColliders()
    {
        // TODO: How can this be null? Delete and check.
        if (Component == null) yield break;
        if (Component.hardCollider != null) yield return Component.hardCollider;
        if (Component.jointCollider != null) yield return Component.jointCollider;
    }

    public IEnumerable<Rigidbody> GetRigidbodies()
    {
        // TODO: How can this be null? Delete and check.
        if (Component == null) yield break;
        if (Component.jointRB != null) yield return Component.jointRB;
        if (Component.kinematicRB != null) yield return Component.kinematicRB;
    }
}
