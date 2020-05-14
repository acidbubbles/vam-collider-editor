using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;


public class RigidbodyModel : ColliderContainerModelBase<Rigidbody>, IModel
{
    private readonly bool _initialDetectCollisions;

    protected override bool OwnsColliders => false;

    public string Type => "Rigidbody";
    public List<ColliderModel> Colliders { get; set; } = new List<ColliderModel>();
    public Rigidbody Rigidbody => Component;

    public RigidbodyModel(MVRScript script, Rigidbody rigidbody)
        : base(script, rigidbody, $"[rb] {NameHelper.Simplify(rigidbody.name)}")
    {
        _initialDetectCollisions = rigidbody.detectCollisions;
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

        var detectCollisionsJsf = new JSONStorableBool("detectCollisions", Component.detectCollisions, value =>
        {
            Component.detectCollisions = value;
            SetModified();
        });
        RegisterStorable(detectCollisionsJsf);
        var detectCollisionsToggle = Script.CreateToggle(detectCollisionsJsf, true);
        detectCollisionsToggle.label = "Detect Collisions";
        RegisterControl(detectCollisionsToggle);
    }

    protected override void DoLoadJson(JSONClass jsonClass)
    {
        LoadJsonField(jsonClass, "detectCollisions", val => Component.detectCollisions = val);
    }

    protected override JSONClass DoGetJson()
    {
        var jsonClass = new JSONClass();
        jsonClass["detectCollisions"].AsBool = Component.detectCollisions;
        return jsonClass;
    }

    protected override void DoResetToInitial()
    {
        Component.detectCollisions = _initialDetectCollisions;
    }

    protected bool DeviatesFromInitial() => Component.detectCollisions != _initialDetectCollisions;
}
