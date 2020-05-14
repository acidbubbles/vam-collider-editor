using System.Collections.Generic;
using System.Linq;
using SimpleJSON;

public class AutoColliderGroupModel : ColliderContainerModelBase<AutoColliderGroup>, IModel
{
    private readonly float _initialAutoRadiusMultiplier;
    private readonly List<AutoColliderModel> _autoColliders;

    protected override bool OwnsColliders => false;

    public string Type => "Auto Collider Group";
    public AutoColliderGroup AutoColliderGroup => Component;

    public AutoColliderGroupModel(MVRScript script, AutoColliderGroup autoColliderGroup, List<AutoColliderModel> autoColliders)
        : base(script, autoColliderGroup, $"[ag] {Simplify(autoColliderGroup.name)}")
    {
        _initialAutoRadiusMultiplier = autoColliderGroup.autoRadiusMultiplier;
        _autoColliders = autoColliders;
    }

    protected override void CreateControlsInternal()
    {
        foreach (var autoCollider in _autoColliders)
        {
            var targetAutoCollider = autoCollider;
            var goToAutoColliderButton = Script.CreateButton($"Go to autocollider {targetAutoCollider.Label}", true);
            goToAutoColliderButton.button.onClick.AddListener(() =>
            {
                Script.SendMessage("SelectEditable", targetAutoCollider);
            });
            RegisterControl(goToAutoColliderButton);
        }

        RegisterControl(
                Script.CreateFloatSlider(RegisterStorable(
                    new JSONStorableFloat("autoRadiusMultiplier", Component.autoRadiusMultiplier, value =>
                    {
                        Component.autoRadiusMultiplier = value;
                        foreach (var autoCollider in _autoColliders)
                            autoCollider.UpdateValuesFromActual();
                        SetModified();
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
        LoadJsonField(jsonClass, "autoRadiusMultiplier", val => Component.autoRadiusMultiplier = val);
    }

    protected override JSONClass DoGetJson()
    {
        var jsonClass = new JSONClass();
        jsonClass["autoRadiusMultiplier"].AsFloat = Component.autoRadiusMultiplier;
        return jsonClass;
    }

    protected override void DoResetToInitial()
    {
        Component.autoRadiusMultiplier = _initialAutoRadiusMultiplier;
    }

    public override IEnumerable<ColliderModel> GetColliders() => _autoColliders.SelectMany(ac => ac.GetColliders());
}
