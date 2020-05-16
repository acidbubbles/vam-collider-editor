using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

public abstract class ModelBase<T> where T : Component
{
    private bool _selected;
    private JSONStorableBool _modifiedJson;
    private readonly List<JSONStorableParam> _controlsStorables = new List<JSONStorableParam>();
    private readonly List<UIDynamic> _controlDynamics = new List<UIDynamic>();

    protected readonly MVRScript Script;

    public readonly T Component;
    public Group Group { get; set; }
    public ModelBase<T> Mirror { get; set; }
    public string Id { get; set; }
    public string Label { get; set; }
    public bool IsDuplicate { get; set; }
    public bool Modified { get; protected set; }

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

    public ModelBase(MVRScript script, T component, string label)
    {
        if (script == null) throw new ArgumentNullException(nameof(script));
        if (component == null) throw new ArgumentNullException(nameof(component));
        if (string.IsNullOrEmpty(label)) throw new ArgumentException("message", nameof(label));

        Script = script;
        Component = component;
        Id = component.Uuid();
        Label = label;
    }

    protected virtual void SetSelected(bool value)
    {
        if (value)
            CreateControls();
        else
            DestroyControls();
    }

    public abstract void SyncPreview();

    protected void SetModified()
    {
        Modified = true;
        if (_modifiedJson != null) _modifiedJson.valNoCallback = true;
    }

    public void AppendJson(JSONClass parent)
    {
        if (IsDuplicate) return;
        if (!Modified) return;
        parent.Add(Id, DoGetJson());
    }

    public virtual void LoadJson(JSONClass jsonClass)
    {
        if (!IsDuplicate)
        {
            DoLoadJson(jsonClass);
            if (Modified && _modifiedJson != null) _modifiedJson.valNoCallback = true;
        }

        if (Selected)
        {
            DestroyControls();
            CreateControls();
        }
    }

    public override string ToString() => Id;

    protected TControl RegisterControl<TControl>(TControl control)
        where TControl : UIDynamic
    {
        _controlDynamics.Add(control);
        return control;
    }

    protected TStorable RegisterStorable<TStorable>(TStorable storable)
        where TStorable : JSONStorableParam
    {
        _controlsStorables.Add(storable);
        return storable;
    }

    protected void CreateControls()
    {
        DestroyControls();
        if (IsDuplicate)
        {
            var jss = RegisterStorable(new JSONStorableString("Duplicate", "This item has duplicates and cannot be edited."));
            RegisterControl(Script.CreateTextField(jss));
        }
        else
        {
            _modifiedJson = new JSONStorableBool("This item has been modified", false);
            _modifiedJson.valNoCallback = Modified;
            _modifiedJson.setCallbackFunction = (bool val) =>
            {
                if (val)
                {
                    // You cannot just enable the Modified flag without actually modifying anything
                    _modifiedJson.valNoCallback = false;
                    return;
                }

                ResetToInitial();
            };
            var resetUi = Script.CreateToggle(_modifiedJson, true);
            RegisterControl(resetUi);

            if (Mirror != null)
            {
                var goToMirrorButton = Script.CreateButton("Go to mirror", true);
                goToMirrorButton.button.onClick.AddListener(() =>
                {
                    Script.SendMessage("SelectEditable", Mirror);
                });
                RegisterControl(goToMirrorButton);
            }

            CreateControlsInternal();

            /*
            var debugJson = new JSONStorableString("Debug", Id);
            RegisterStorable(debugJson);
            var debugTextField = Script.CreateTextField(debugJson, true);
            RegisterControl(debugTextField);
            */
        }
    }

    protected abstract void CreateControlsInternal();

    protected void DestroyControls()
    {
        foreach (var storable in _controlsStorables)
        {
            if (storable is JSONStorableFloat)
            {
                var jsf = (JSONStorableFloat)storable;
                Script.RemoveSlider(jsf);
            }
            else if (storable is JSONStorableBool)
            {
                var jsb = (JSONStorableBool)storable;
                Script.RemoveToggle(jsb);
            }
            else if (storable is JSONStorableString)
            {
                var jss = (JSONStorableString)storable;
                Script.RemoveTextField(jss);
            }
            else
            {
                SuperController.LogError($"Unknown storable type: {storable.GetType()}");
            }
        }

        _controlsStorables.Clear();

        foreach (var control in _controlDynamics)
        {
            if (control is UIDynamicSlider)
            {
                var slider = (UIDynamicSlider)control;
                Script.RemoveSlider(slider);
            }
            else if (control is UIDynamicToggle)
            {
                var toggle = (UIDynamicToggle)control;
                Script.RemoveToggle(toggle);
            }
            else if (control is UIDynamicTextField)
            {
                var textfield = (UIDynamicTextField)control;
                Script.RemoveTextField(textfield);
            }
            else if (control is UIDynamicButton)
            {
                var button = (UIDynamicButton)control;
                Script.RemoveButton(button);
            }
            else
            {
                SuperController.LogError($"Unknown control type: {control.GetType()}");
            }
        }

        _controlDynamics.Clear();

        _modifiedJson = null;
    }

    public void ResetToInitial()
    {
        DoResetToInitial();

        if (Selected)
        {
            DestroyControls();
            CreateControls();
        }

        Modified = false;
        if (_modifiedJson != null) _modifiedJson.valNoCallback = false;
    }

    protected void LoadJsonField(JSONClass jsonClass, string name, Action<bool> setValue)
    {
        if (!jsonClass.HasKey(name)) return;
        Modified = true;
        setValue(jsonClass[name].AsBool);
    }

    protected void LoadJsonField(JSONClass jsonClass, string name, Action<float> setValue)
    {
        if (!jsonClass.HasKey(name)) return;
        Modified = true;
        setValue(jsonClass[name].AsFloat);
    }

    protected void LoadJsonField(JSONClass jsonClass, string name, Action<Vector3> setValue)
    {
        var nameX = $"{name}X";
        var nameY = $"{name}Y";
        var nameZ = $"{name}Z";
        if (!jsonClass.HasKey(nameX)) return;
        Modified = true;
        Vector3 value;
        value.x = jsonClass["sizeX"].AsFloat;
        value.y = jsonClass["sizeY"].AsFloat;
        value.z = jsonClass["sizeZ"].AsFloat;
        setValue(value);
    }

    protected abstract void DoResetToInitial();
    protected abstract void DoLoadJson(JSONClass jsonClass);
    protected abstract JSONClass DoGetJson();
}
