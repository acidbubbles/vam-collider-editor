using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

public class AutoColliderModel : ColliderContainerModelBase<AutoCollider>, IModel
{
    private readonly float _initialAutoLengthBuffer;
    private readonly float _initialAutoRadiusBuffer;
    private readonly float _initialAutoRadiusMultiplier;
    private readonly List<ColliderModel> _ownedColliders = new List<ColliderModel>();

    protected override bool OwnsColliders => true;

    public string Type => "Auto Collider";
    public AutoCollider AutoCollider => Component;

    public AutoColliderModel(MVRScript script, AutoCollider autoCollider)
        : base(script, autoCollider, $"[au] {Simplify(autoCollider.name)}")
    {
        _initialAutoLengthBuffer = autoCollider.autoLengthBuffer;
        _initialAutoRadiusBuffer = autoCollider.autoRadiusBuffer;
        _initialAutoRadiusMultiplier = autoCollider.autoRadiusMultiplier;
        if (Component.hardCollider != null) _ownedColliders.Add(ColliderModel.CreateTyped(script, autoCollider.hardCollider));
        if (Component.jointCollider != null) _ownedColliders.Add(ColliderModel.CreateTyped(script, Component.jointCollider));
    }

    protected override void CreateControlsInternals()
    {
        var resetUi = Script.CreateButton("Reset AutoCollider", true);
        resetUi.button.onClick.AddListener(ResetToInitial);
        RegisterControl(resetUi);

        RegisterControl(
                Script.CreateFloatSlider(RegisterStorable(
                    new JSONStorableFloat("autoLengthBuffer", Component.autoLengthBuffer, value =>
                    {
                        Component.autoLengthBuffer = value;
                    }, 0f, 0.25f, false)
                    .WithDefault(_initialAutoLengthBuffer)
                ), "Auto Length Buffer")
        );

        RegisterControl(
                Script.CreateFloatSlider(RegisterStorable(
                    new JSONStorableFloat("autoRadiusBuffer", Component.autoRadiusBuffer, value =>
                    {
                        Component.autoRadiusBuffer = value;
                    }, 0f, 0.25f, false)
                    .WithDefault(_initialAutoRadiusBuffer)
                ), "Auto Radius Buffer")
        );

        RegisterControl(
                Script.CreateFloatSlider(RegisterStorable(
                    new JSONStorableFloat("autoRadiusMultiplier", Component.autoRadiusMultiplier, value =>
                    {
                        Component.autoRadiusMultiplier = value;
                    }, 0.001f, 2f, false)
                    .WithDefault(_initialAutoRadiusMultiplier)
                ), "Auto Radius Multiplier")
        );
    }

    protected override void SetSelected(bool value)
    {
        // TODO: Track colliders to highlight them
        base.SetSelected(value);
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

    public override IEnumerable<ColliderModel> GetColliders() => _ownedColliders;

    public IEnumerable<Rigidbody> GetRigidbodies()
    {
        if (Component.jointRB != null) yield return Component.jointRB;
        if (Component.kinematicRB != null) yield return Component.kinematicRB;
    }
}
