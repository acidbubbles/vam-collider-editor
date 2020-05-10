using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using UnityEngine;

using Object = UnityEngine.Object;

public class RigidbodyModel : IModel
{
    private readonly bool _initialEnabled;
    private readonly Rigidbody _rigidbody;

    private readonly MVRScript _script;

    private List<UIDynamic> _controls;

    private bool _selected;

    public string Id { get; set; }
    public string Name { get; set; }
    public string Label { get; set; }
    public List<RigidbodyGroupModel> Groups { get; set; }
    public List<ColliderModel> Colliders { get; set; }

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

    public RigidbodyModel(MVRScript script, Rigidbody rigidbody, string label)
    {
        _script = script;
        _rigidbody = rigidbody;

        Id = rigidbody.Uuid();
        Name = rigidbody.name;
        Label = label;

        _initialEnabled = rigidbody.detectCollisions;
    }

    public static RigidbodyModel Create(MVRScript script, Rigidbody rigidbody, IEnumerable<RigidbodyGroupModel> groups)
    {
        var model = new RigidbodyModel(script, rigidbody, rigidbody.name);
        model.Groups = groups
            .Where(category => category.Pattern.IsMatch(rigidbody.name))
            .ToList();
        return model;
    }

    public override string ToString() => $"{Id}_{Name}";

    private void SetSelected(bool value)
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

        var resetUi = _script.CreateButton("Reset Rigidbody");
        resetUi.button.onClick.AddListener(ResetToInitial);

        var enabledToggleJsf = new JSONStorableBool("enabled", _rigidbody.detectCollisions, value => { _rigidbody.detectCollisions = value; });
        var enabledToggle = _script.CreateToggle(enabledToggleJsf);
        enabledToggle.label = "Detect Collisions";

        controls.Add(resetUi);
        controls.Add(enabledToggle);

        _controls = controls;
    }

    public virtual void DestroyControls()
    {
        if (_controls == null)
            return;

        foreach (var control in _controls)
            Object.Destroy(control.gameObject);

        _controls.Clear();
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
        _rigidbody.detectCollisions = jsonClass["detectCollisions"].AsBool;
    }

    public JSONClass DoGetJson()
    {
        var jsonClass = new JSONClass();
        jsonClass["detectCollisions"].AsBool = _rigidbody.detectCollisions;
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
        _rigidbody.detectCollisions = _initialEnabled;
    }

    protected bool DeviatesFromInitial() => _rigidbody.detectCollisions != _initialEnabled;
}
