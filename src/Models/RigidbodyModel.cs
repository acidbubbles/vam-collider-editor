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
        : base(script, rigidbody, $"[rb] {Simplify(rigidbody.name)}")
    {
        _initialDetectCollisions = rigidbody.detectCollisions;
    }

    public override IEnumerable<ColliderModel> GetColliders() => Colliders;

    protected override void CreateControlsInternals()
    {
        var resetUi = Script.CreateButton("Reset Rigidbody", true);
        resetUi.button.onClick.AddListener(ResetToInitial);
        RegisterControl(resetUi);

        var detectCollisionsJsf = new JSONStorableBool("detectCollisions", Component.detectCollisions, value => { Component.detectCollisions = value; });
        RegisterStorable(detectCollisionsJsf);
        var detectCollisionsToggle = Script.CreateToggle(detectCollisionsJsf, true);
        detectCollisionsToggle.label = "Detect Collisions";
        RegisterControl(detectCollisionsToggle);
    }


    protected override void DoLoadJson(JSONClass jsonClass)
    {
        Component.detectCollisions = jsonClass["detectCollisions"].AsBool;
    }

    protected override JSONClass DoGetJson()
    {
        var jsonClass = new JSONClass();
        jsonClass["detectCollisions"].AsBool = Component.detectCollisions;
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
        Component.detectCollisions = _initialDetectCollisions;
    }

    protected bool DeviatesFromInitial() => Component.detectCollisions != _initialDetectCollisions;
}
