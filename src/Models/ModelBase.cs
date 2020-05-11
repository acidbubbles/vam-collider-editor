using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

public abstract class ModelBase<T> where T : Component
{
    private bool _selected;
    private readonly List<JSONStorableParam> _controlsStorables = new List<JSONStorableParam>();
    private readonly List<UIDynamic> _controlDynamics = new List<UIDynamic>();

    protected readonly MVRScript Script;
    protected readonly T Component;

    public string Id { get; set; }
    public string Label { get; set; }
    public bool IsDuplicate { get; set; }

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

    public void AppendJson(JSONClass parent)
    {
        parent.Add(Id, DoGetJson());
    }

    public virtual void LoadJson(JSONClass jsonClass)
    {
        DoLoadJson(jsonClass);

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

    protected abstract void CreateControls();
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
    }

    protected abstract void DoLoadJson(JSONClass jsonClass);
    protected abstract JSONClass DoGetJson();

    protected static string Simplify(string label)
    {
        if (label.StartsWith("AutoColliderAutoColliders"))
            return label.Substring("AutoColliderAutoColliders".Length);
        if (label.StartsWith("AutoColliderFemaleAutoColliders"))
            return label.Substring("AutoColliderFemaleAutoColliders".Length);
        if (label.StartsWith("AutoCollider"))
            return label.Substring("AutoCollider".Length);
        if (label.StartsWith("_"))
            return label.Substring(1);
        return label;
    }
}
