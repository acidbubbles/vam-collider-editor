using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

using Object = UnityEngine.Object;
public class AutoColliderModel : ModelBase, IModel
{
    public static AutoColliderModel Create(MVRScript script, AutoCollider autoCollider)
    {
        return new AutoColliderModel(script, autoCollider, autoCollider.name);
    }

    private readonly float _initialAutoLengthBuffer;
    private readonly float _initialAutoRadiusBuffer;
    private readonly float _initialAutoRadiusMultiplier;

    private bool _selected;
    private readonly MVRScript _script;
    private readonly AutoCollider _autoCollider;

    public List<UIDynamic> Controls { get; private set; }

    public bool Selected
    {
        get { return _selected; }
        set
        {
            if (_selected != value)
            {
                SetSelected(value);
                _selected = value;
            }
        }
    }

    public AutoColliderModel(MVRScript script, AutoCollider autoCollider, string label)
    {
        _script = script;
        _autoCollider = autoCollider;
        _initialAutoLengthBuffer = autoCollider.autoLengthBuffer;
        _initialAutoRadiusBuffer = autoCollider.autoRadiusBuffer;
        _initialAutoRadiusMultiplier = autoCollider.autoRadiusMultiplier;
        Id = autoCollider.Uuid();
        if (label.StartsWith("AutoColliderAutoColliders"))
            Label = label.Substring("AutoColliderAutoColliders".Length);
        else if (label.StartsWith("AutoColliderFemaleAutoColliders"))
            Label = label.Substring("AutoColliderFemaleAutoColliders".Length);
        else if (label.StartsWith("AutoCollider"))
            Label = label.Substring("AutoCollider".Length);
        else
            Label = label;
    }

    protected virtual void SetSelected(bool value)
    {
        if (value)
            CreateControls();
        else
            DestroyControls();
    }

    public void CreateControls()
    {
        DestroyControls();

        var controls = new List<UIDynamic>();

        var resetUi = _script.CreateButton("Reset AutoCollider", true);
        resetUi.button.onClick.AddListener(ResetToInitial);

        controls.Add(resetUi);
        controls.AddRange(DoCreateControls());

        Controls = controls;
    }

    public IEnumerable<UIDynamic> DoCreateControls()
    {
        yield return _script.CreateFloatSlider(new JSONStorableFloat("autoLengthBuffer", _autoCollider.autoLengthBuffer, value =>
        {
            _autoCollider.autoLengthBuffer = value;
        }, 0f, _initialAutoLengthBuffer * 4f, false).WithDefault(_initialAutoLengthBuffer), "Auto Length Buffer");

        yield return _script.CreateFloatSlider(new JSONStorableFloat("autoRadiusBuffer", _autoCollider.autoRadiusBuffer, value =>
        {
            _autoCollider.autoRadiusBuffer = value;
        }, 0f, _initialAutoRadiusBuffer * 4f, false).WithDefault(_initialAutoRadiusBuffer), "Auto Radius Buffer");

        yield return _script.CreateFloatSlider(new JSONStorableFloat("autoRadiusMultiplier", _autoCollider.autoRadiusMultiplier, value =>
        {
            _autoCollider.autoRadiusMultiplier = value;
        }, 0f, _initialAutoRadiusMultiplier * 4f, false).WithDefault(_initialAutoRadiusMultiplier), "Auto Radius Multiplier");
    }

    public virtual void DestroyControls()
    {
        if (Controls == null)
            return;

        foreach (var adjustmentJson in Controls)
            Object.Destroy(adjustmentJson.gameObject);

        Controls.Clear();
    }

    public void AppendJson(JSONClass parent)
    {
        parent.Add(Id, DoGetJson());
    }

    public void LoadJson(JSONClass jsonClass)
    {
        DoLoadJson(jsonClass);

        if (Selected)
        {
            DestroyControls();
            CreateControls();
        }
    }

    private void DoLoadJson(JSONClass jsonClass)
    {
        _autoCollider.autoLengthBuffer = jsonClass["autoLengthBuffer"].AsFloat;
        _autoCollider.autoRadiusBuffer = jsonClass["autoRadiusBuffer"].AsFloat;
        _autoCollider.autoRadiusMultiplier = jsonClass["autoRadiusMultiplier"].AsFloat;
    }

    public JSONClass DoGetJson()
    {
        var jsonClass = new JSONClass();
        jsonClass["autoLengthBuffer"].AsFloat = _autoCollider.autoLengthBuffer;
        jsonClass["autoRadiusBuffer"].AsFloat = _autoCollider.autoRadiusBuffer;
        jsonClass["autoRadiusMultiplier"].AsFloat = _autoCollider.autoRadiusMultiplier;
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
        _autoCollider.autoRadiusBuffer = _initialAutoRadiusBuffer;
    }

    public IEnumerable<Collider> GetColliders()
    {
        if (_autoCollider.hardCollider != null) yield return _autoCollider.hardCollider;
        if (_autoCollider.jointCollider != null) yield return _autoCollider.jointCollider;
    }

    public IEnumerable<Rigidbody> GetRigidbodies()
    {
        if (_autoCollider.jointRB != null) yield return _autoCollider.jointRB;
        if (_autoCollider.kinematicRB != null) yield return _autoCollider.kinematicRB;
    }
}
