using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

public class AutoColliderModel : ColliderContainerModelBase<AutoCollider>, IModel
{
    private readonly float _initialAutoLengthBuffer;
    private readonly float _initialAutoRadiusBuffer;
    private readonly float _initialAutoRadiusMultiplier;
    private readonly List<ColliderModel> _ownedColliders = new List<ColliderModel>();
    private float _lastAutoRadiusMultiplier;

    protected override bool OwnsColliders => true;

    public string Type => "Auto Collider";
    public AutoCollider AutoCollider => Component;
    public AutoColliderGroupModel AutoColliderGroup { get; set; }

    public AutoColliderModel(MVRScript script, AutoCollider autoCollider, ColliderPreviewConfig config)
        : base(script, autoCollider, $"[au] {Simplify(autoCollider.name)}")
    {
        _initialAutoLengthBuffer = autoCollider.autoLengthBuffer;
        _initialAutoRadiusBuffer = autoCollider.autoRadiusBuffer;
        _initialAutoRadiusMultiplier = _lastAutoRadiusMultiplier = autoCollider.autoRadiusMultiplier;
        if (Component.hardCollider != null) _ownedColliders.Add(ColliderModel.CreateTyped(script, autoCollider.hardCollider, config));
        if (Component.jointCollider != null) _ownedColliders.Add(ColliderModel.CreateTyped(script, Component.jointCollider, config));
    }

    protected override void CreateControlsInternal()
    {
        if (AutoColliderGroup != null)
        {
            var goToAutoColliderGroupButton = Script.CreateButton($"Go to autocollider group {AutoColliderGroup.Label}", true);
            goToAutoColliderGroupButton.button.onClick.AddListener(() =>
            {
                Script.SendMessage("SelectEditable", AutoColliderGroup);
            });
            RegisterControl(goToAutoColliderGroupButton);
        }

        RegisterControl(
                Script.CreateFloatSlider(RegisterStorable(
                    new JSONStorableFloat("autoLengthBuffer", Component.autoLengthBuffer, value =>
                    {
                        Component.autoLengthBuffer = value;
                        RecalculateAutoCollider();
                        SetModified();
                    }, 0f, 0.25f, false)
                    .WithDefault(_initialAutoLengthBuffer)
                ), "Auto Length Buffer")
        );

        RegisterControl(
                Script.CreateFloatSlider(RegisterStorable(
                    new JSONStorableFloat("autoRadiusBuffer", Component.autoRadiusBuffer, value =>
                    {
                        Component.autoRadiusBuffer = value;
                        RecalculateAutoCollider();
                        SetModified();
                    }, 0f, 0.25f, false)
                    .WithDefault(_initialAutoRadiusBuffer)
                ), "Auto Radius Buffer")
        );

        RegisterControl(
                Script.CreateFloatSlider(RegisterStorable(
                    new JSONStorableFloat("autoRadiusMultiplier", Component.autoRadiusMultiplier, value =>
                    {
                        Component.autoRadiusMultiplier = _lastAutoRadiusMultiplier = value;
                        RecalculateAutoCollider();
                        SetModified();
                    }, 0.001f, 2f, false)
                    .WithDefault(_initialAutoRadiusMultiplier)
                ), "Auto Radius Multiplier")
        );
    }

    private void RecalculateAutoCollider()
    {
        var previousResizeTrigger = AutoCollider.resizeTrigger;
        AutoCollider.resizeTrigger = AutoCollider.ResizeTrigger.Always;
        AutoCollider.AutoColliderSizeSet(true);
        AutoCollider.resizeTrigger = previousResizeTrigger;
        UpdatePreviewFromCollider();
    }

    public void UpdateValuesFromActual()
    {
        if (Modified)
        {
            if (Component.autoRadiusMultiplier != _lastAutoRadiusMultiplier)
                Component.autoRadiusMultiplier = _lastAutoRadiusMultiplier;
            return;
        }
    }

    protected override void SetSelected(bool value)
    {
        // TODO: Track colliders to highlight them
        base.SetSelected(value);
    }

    protected override void DoLoadJson(JSONClass jsonClass)
    {
        LoadJsonField(jsonClass, "autoLengthBuffer", val => Component.autoLengthBuffer = val);
        LoadJsonField(jsonClass, "autoRadiusBuffer", val => Component.autoRadiusBuffer = val);
        LoadJsonField(jsonClass, "autoRadiusMultiplier", val => Component.autoRadiusMultiplier = _lastAutoRadiusMultiplier = val);
    }

    protected override JSONClass DoGetJson()
    {
        var jsonClass = new JSONClass();
        jsonClass["autoLengthBuffer"].AsFloat = Component.autoLengthBuffer;
        jsonClass["autoRadiusBuffer"].AsFloat = Component.autoRadiusBuffer;
        jsonClass["autoRadiusMultiplier"].AsFloat = Component.autoRadiusMultiplier;
        return jsonClass;
    }

    protected override void DoResetToInitial()
    {
        Component.autoLengthBuffer = _initialAutoLengthBuffer;
        Component.autoRadiusBuffer = _initialAutoRadiusBuffer;
        Component.autoRadiusMultiplier = _lastAutoRadiusMultiplier = _initialAutoRadiusMultiplier;
    }

    public override IEnumerable<ColliderModel> GetColliders() => _ownedColliders;

    public IEnumerable<Rigidbody> GetRigidbodies()
    {
        if (Component.jointRB != null) yield return Component.jointRB;
        if (Component.kinematicRB != null) yield return Component.kinematicRB;
    }
}
