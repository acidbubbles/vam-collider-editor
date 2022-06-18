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

	public static float MakeMinOffset(float val)
	{
		return val - 0.025f;
	}

	public static float MakeMaxOffset(float val)
	{
		return val + 0.025f;
	}

	public static float MakeMinPosition(float val)
	{
		return val - 0.025f;
	}

	public static float MakeMaxPosition(float val)
	{
		return val + 0.025f;
	}

    public JSONStorableParam FindStorable(string name)
    {
        foreach (var p in _controlsStorables)
        {
            SuperController.LogError(p.name);
            if (p.name == name)
                return p;
        }

        return null;
    }

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

    public virtual void UpdatePreviewFromConfig() { }
    public abstract void SyncPreview();

    public void SetModified()
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
        }

        var hierarchyJson = new JSONStorableString("Identity", Component.Hierarchy());
        RegisterStorable(hierarchyJson);
        var hierarchyTextField = Script.CreateTextField(hierarchyJson, true);
        hierarchyTextField.height = 80f;
        hierarchyJson.dynamicText = hierarchyTextField;
        // var debugTextInputField = debugTextField.gameObject.AddComponent<UnityEngine.UI.InputField>();
        // debugTextInputField.textComponent = debugTextField.UItext;
        // uuidJson.inputField = debugTextInputField;
        RegisterControl(hierarchyTextField);
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


    private static bool ChangingLink = false;

    public virtual IModel Linked
    {
        get
        {
            return FindLinked() as IModel;
        }
    }

    public string QualifiedName
    {
        get
        {
            return
                Component.transform.parent.parent.name + "." +
                Component.transform.parent.name + "." +
                Component.name;
        }
    }

    public void SetLinked(Action<ModelBase<T>, float> set, float value)
    {
        if (ChangingLink)
            return;

        try
        {
            ChangingLink = true;
            SetLinkedImpl(set, value);
        }
        finally
        {
            ChangingLink = false;
        }
    }

    public void SetLinked(Action<ModelBase<T>, bool> set, bool value)
    {
        if (ChangingLink)
            return;

        try
        {
            ChangingLink = true;
            SetLinkedImpl(set, value);
        }
        finally
        {
            ChangingLink = false;
        }
    }

    private void SetLinkedImpl(Action<ModelBase<T>, float> set, float value)
    {
        var linked = FindLinked();
        if (linked == null)
        {
            SuperController.LogError($"no link for {QualifiedName}");
            return;
        }

        set(linked, value);
    }

    private void SetLinkedImpl(Action<ModelBase<T>, bool> set, bool value)
    {
        var linked = FindLinked();
        if (linked == null)
        {
            SuperController.LogError($"no link for {QualifiedName}");
            return;
        }

        set(linked, value);
    }

    private ModelBase<T> FindLinked()
    {
        var name = QualifiedName;

        SuperController.LogError("FindLinked: " + name);

        var ce = (ColliderEditor)Script;

        ModelBase<T> linked = null;

        linked = FindLinkedIn(name, ce.EditablesList.Colliders);
        if (linked != null)
            return linked;

        linked = FindLinkedIn(name, ce.EditablesList.AutoColliders);
        if (linked != null)
            return linked;

        linked = FindLinkedIn(name, ce.EditablesList.Rigidbodies);
        if (linked != null)
            return linked;

        return null;
    }

    private ModelBase<T> FindLinkedIn<U>(
        string name, List<U> list) where U : IModel
    {
        var links = Links.GetList();

        foreach (Links.Link ln in links)
        {
            if (ln.a == name)
            {
                SuperController.LogError($"ln.a is {name}, checking ln.b {ln.b}");
                foreach (var m in list)
                {
                    if (m.QualifiedName == ln.b)
                    {
                        SuperController.LogError($"m.QualifiedName {m.QualifiedName} matches");
                        return m as ModelBase<T>;
                    }
                    else
                    {
                        SuperController.LogError($"m.QualifiedName {m.QualifiedName} doesn't match");
                    }
                }
            }
            else if (ln.b == name)
            {
                SuperController.LogError($"ln.b is {name}, checking ln.a {ln.a}");
                foreach (var m in list)
                {
                    if (m.QualifiedName == ln.a)
                    {
                        SuperController.LogError($"m.QualifiedName {m.QualifiedName} matches");
                        return m as ModelBase<T>;
                    }
                    else
                    {
                        SuperController.LogError($"m.QualifiedName {m.QualifiedName} doesn't match");
                    }
                }
            }
        }

        return null;
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
        value.x = jsonClass[nameX].AsFloat;
        value.y = jsonClass[nameY].AsFloat;
        value.z = jsonClass[nameZ].AsFloat;
        setValue(value);
    }

    protected abstract void DoResetToInitial();
    protected abstract void DoLoadJson(JSONClass jsonClass);
    protected abstract JSONClass DoGetJson();
}
