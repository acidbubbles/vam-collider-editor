using System;
using SimpleJSON;
using UnityEngine;

public abstract class ModelBase<T> where T : Component
{
    private bool _selected;

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

    protected abstract void CreateControls();
    protected abstract void DestroyControls();

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
