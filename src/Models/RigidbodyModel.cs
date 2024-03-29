using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;


public class RigidbodyModel : ColliderContainerModelBase<Rigidbody>, IModel
{
    private readonly bool _initialDetectCollisions;
    private bool _detectCollisions;

    private JSONStorableBool _detectCollisionsJSON;

    protected override bool OwnsColliders => false;

    public bool detectCollisions
    {
        get { return _detectCollisions; }
        set { Component.detectCollisions = _detectCollisions = value; SetModified(); }
    }

    public string Type => "Rigidbody";
    public List<ColliderModel> Colliders { get; set; } = new List<ColliderModel>();
    public Rigidbody Rigidbody => Component;

    public RigidbodyModel(MVRScript script, Rigidbody rigidbody)
        : base(script, rigidbody, $"[rb] {NameHelper.Simplify(rigidbody.name)}")
    {
        _initialDetectCollisions = _detectCollisions = rigidbody.detectCollisions;
    }

    public bool SyncOverrides()
    {
        if (!Modified) return false;
        bool changed = false;
        if (Component.detectCollisions != _detectCollisions)
        {
            Component.detectCollisions = _detectCollisions;
            changed = true;
        }
        return changed;
    }

    public override IEnumerable<ColliderModel> GetColliders() => Colliders;

    protected override void CreateControlsInternal()
    {
        if (Colliders.Count > 0)
        {
            foreach (var collider in Colliders)
            {
                var targetCollider = collider;
                var goToColliderButton = Script.CreateButton($"Go to collider {targetCollider.Label}", true);
                goToColliderButton.button.onClick.AddListener(() =>
                {
                    Script.SendMessage("SelectEditable", targetCollider);
                });
                RegisterControl(goToColliderButton);
            }
        }

        _detectCollisionsJSON = new JSONStorableBool("detectCollisions", Component.detectCollisions, SetDetectCollisions);
        RegisterStorable(_detectCollisionsJSON);
        var detectCollisionsToggle = Script.CreateToggle(_detectCollisionsJSON, true);
        detectCollisionsToggle.label = "Detect Collisions";
        RegisterControl(detectCollisionsToggle);
    }

    private void SetDetectCollisions(bool value)
    {
        if (_detectCollisionsJSON != null) _detectCollisionsJSON.valNoCallback = value;
        Component.detectCollisions = _detectCollisions = value;
        SetModified();
        SetMirror<RigidbodyModel>(m => m.SetDetectCollisions(value));
    }

    protected override void DoLoadJson(JSONClass jsonClass)
    {
        LoadJsonField(jsonClass, "detectCollisions", val => Component.detectCollisions = _detectCollisions = val);
    }

    protected override JSONClass DoGetJson()
    {
        var jsonClass = new JSONClass();
        jsonClass["detectCollisions"].AsBool = _detectCollisions;
        return jsonClass;
    }

    protected override void DoResetToInitial()
    {
        Component.detectCollisions = _detectCollisions = _initialDetectCollisions;
    }
}
