using System.Collections.Generic;
using System.Linq;
using SimpleJSON;

public class AutoColliderGroupModel : ColliderContainerModelBase<AutoColliderGroup>, IModel
{
    private readonly float _initialAutoRadiusMultiplier;
    private float _autoRadiusMultiplier;
    private readonly List<AutoColliderModel> _autoColliders;

    private JSONStorableFloat _autoRadiusMultiplierJSON;

    protected override bool OwnsColliders => true;

    public string Type => "Auto Collider Group";
    public AutoColliderGroup AutoColliderGroup => Component;

    public AutoColliderGroupModel(MVRScript script, AutoColliderGroup autoColliderGroup, List<AutoColliderModel> autoColliders)
        : base(script, autoColliderGroup, $"[ag] {NameHelper.Simplify(autoColliderGroup.name)}")
    {
        _initialAutoRadiusMultiplier = _autoRadiusMultiplier = autoColliderGroup.autoRadiusMultiplier;

        _autoColliders = autoColliders;
    }

    public bool SyncOverrides()
    {
        if (!Modified) return false;
        bool changed = false;
        if (Component.autoRadiusMultiplier != _autoRadiusMultiplier)
        {
            Component.autoRadiusMultiplier = _autoRadiusMultiplier;
            foreach (var autoCollider in _autoColliders)
                autoCollider.ReapplyMultiplier();
            changed = true;
        }
        return changed;
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

        _autoRadiusMultiplierJSON = new JSONStorableFloat("autoRadiusMultiplier", Component.autoRadiusMultiplier, SetAutoRadiusMultiplier, 0.001f, 2f, false).WithDefault(_initialAutoRadiusMultiplier);
        RegisterControl(Script.CreateFloatSlider(RegisterStorable(_autoRadiusMultiplierJSON), "Auto Radius Multiplier"));
    }

    private void SetAutoRadiusMultiplier(float value)
    {
        if (_autoRadiusMultiplierJSON != null) _autoRadiusMultiplierJSON.valNoCallback = value;
        Component.autoRadiusMultiplier = _autoRadiusMultiplier = value;
        foreach (var autoCollider in _autoColliders)
        {
            autoCollider.ReapplyMultiplier();
            autoCollider.RefreshAutoCollider();
        }
        SetModified();
        SetMirror<AutoColliderGroupModel>(m => m.SetAutoRadiusMultiplier(value));
    }

    protected override void DoLoadJson(JSONClass jsonClass)
    {
        LoadJsonField(jsonClass, "autoRadiusMultiplier", val => Component.autoRadiusMultiplier = _autoRadiusMultiplier = val);
    }

    protected override JSONClass DoGetJson()
    {
        var jsonClass = new JSONClass();
        jsonClass["autoRadiusMultiplier"].AsFloat = _autoRadiusMultiplier;
        return jsonClass;
    }

    protected override void DoResetToInitial()
    {
        Component.autoRadiusMultiplier = _autoRadiusMultiplier = _initialAutoRadiusMultiplier;
    }

    public override IEnumerable<ColliderModel> GetColliders() => _autoColliders.SelectMany(ac => ac.GetColliders());
}
