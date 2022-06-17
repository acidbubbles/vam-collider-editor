using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

public class AutoColliderModel : ColliderContainerModelBase<AutoCollider>, IModel
{
    private readonly bool _initialCollisionEnabled;
    private bool _collisionEnabled;
    private readonly float _initialColliderLength;
    private float _colliderLength;
    private readonly float _initialColliderRadius;
    private float _colliderRadius;
    private readonly float _initialHardColliderBuffer;
    private float _hardColliderBuffer;
    private readonly float _initialColliderLookOffset;
    private float _colliderLookOffset;
    private readonly float _initialColliderUpOffset;
    private float _colliderUpOffset;
    private readonly float _initialColliderRightOffset;
    private float _colliderRightOffset;
    private readonly float _initialAutoLengthBuffer;
    private float _autoLengthBuffer;
    private readonly float _initialAutoRadiusBuffer;
    private float _autoRadiusBuffer;
    private readonly float _initialAutoRadiusMultiplier;
    private float _autoRadiusMultiplier;
    private readonly List<ColliderModel> _ownedColliders = new List<ColliderModel>();

    protected override bool OwnsColliders => true;

    public bool collisionEnabled
    {
        get { return _collisionEnabled; }
        set { Component.collisionEnabled = _collisionEnabled = value; SetModified(); }
    }

    public string Type => "Auto Collider";
    public AutoCollider AutoCollider => Component;
    public AutoColliderGroupModel AutoColliderGroup { get; set; }

    public AutoColliderModel(MVRScript script, AutoCollider autoCollider, ColliderPreviewConfig config)
        : base(script, autoCollider, $"[au] {NameHelper.Simplify(autoCollider.name)}")
    {
        _initialCollisionEnabled = _collisionEnabled = autoCollider.collisionEnabled;
        _initialColliderLength = _colliderLength = autoCollider.colliderLength;
        _initialColliderRadius = _colliderRadius = autoCollider.colliderRadius;
        _initialHardColliderBuffer = _hardColliderBuffer = autoCollider.hardColliderBuffer;
        _initialColliderLookOffset = _colliderLookOffset = autoCollider.colliderLookOffset;
        _initialColliderUpOffset = _colliderUpOffset = autoCollider.colliderUpOffset;
        _initialColliderRightOffset = _colliderRightOffset = autoCollider.colliderRightOffset;
        _initialAutoLengthBuffer = _autoLengthBuffer = autoCollider.autoLengthBuffer;
        _initialAutoRadiusBuffer = _autoRadiusBuffer = autoCollider.autoRadiusBuffer;
        _initialAutoRadiusMultiplier = _autoRadiusMultiplier = autoCollider.autoRadiusMultiplier;
        if (Component.hardCollider != null) _ownedColliders.Add(ColliderModel.CreateTyped(script, autoCollider.hardCollider, config));
        if (Component.jointCollider != null) _ownedColliders.Add(ColliderModel.CreateTyped(script, Component.jointCollider, config));
    }

    public bool SyncOverrides()
    {
        if (!Modified) return false;
        bool changed = false;
        if (Component.collisionEnabled != _collisionEnabled)
        {
            Component.collisionEnabled = _collisionEnabled;
            changed = true;
        }
        if (Component.useAutoLength)
        {
            if (Component.autoLengthBuffer != _autoLengthBuffer)
            {
                Component.autoLengthBuffer = _autoLengthBuffer;
                changed = true;
            }
        }
        else
        {
            if (Component.colliderLength != _colliderLength)
            {
                Component.colliderLength = _colliderLength;
                changed = true;
            }
        }
        if (Component.useAutoRadius)
        {
            if (Component.autoRadiusBuffer != _autoRadiusBuffer)
            {
                Component.autoRadiusBuffer = _autoRadiusBuffer;
                changed = true;
            }
            if (Component.autoRadiusMultiplier != _autoRadiusMultiplier)
            {
                Component.autoRadiusMultiplier = _autoRadiusMultiplier;
                changed = true;
            }
        }
        else
        {
            if (Component.colliderRadius != _colliderRadius)
            {
                Component.colliderRadius = _colliderRadius;
                changed = true;
            }
            if (Component.hardColliderBuffer != _hardColliderBuffer)
            {
                Component.hardColliderBuffer = _hardColliderBuffer;
                changed = true;
            }
        }
        if (Component.colliderLookOffset != _colliderLookOffset)
        {
            Component.colliderLookOffset = _colliderLookOffset;
            changed = true;
        }
        if (Component.colliderUpOffset != _colliderUpOffset)
        {
            Component.colliderUpOffset = _colliderUpOffset;
            changed = true;
        }
        if (Component.colliderRightOffset != _colliderRightOffset)
        {
            Component.colliderRightOffset = _colliderRightOffset;
            changed = true;
        }
        return changed;
    }

    protected override void CreateControlsInternal()
    {
        if (AutoColliderGroup != null)
        {
            var goToAutoColliderGroupButton = Script.CreateButton($"Go to autocollider group {AutoColliderGroup.Label}", true);
            goToAutoColliderGroupButton.button.onClick.AddListener(() =>
            {
                Script.SendMessage("SelectEditable", AutoColliderGroup);
            });
            RegisterControl(goToAutoColliderGroupButton);
        }

        RegisterControl(
                Script.CreateToggle(RegisterStorable(
                    new JSONStorableBool("collisionEnabled", Component.collisionEnabled, value =>
                    {
                        SetCollisionEnabled(value);
                    })
                    .WithDefault(_initialCollisionEnabled)
                ), "Collision Enabled")
        );

        if (Component.useAutoLength)
        {
            RegisterControl(
                    Script.CreateFloatSlider(RegisterStorable(
                        new JSONStorableFloat("autoLengthBuffer", Component.autoLengthBuffer, value =>
                        {
                            SetAutoLengthBuffer(value);
                        }, -0.25f, 0.25f, false)
                        .WithDefault(_initialAutoLengthBuffer)
                    ), "Auto Length Buffer")
            );
        }
        else
        {
            RegisterControl(
                    Script.CreateFloatSlider(RegisterStorable(
                        new JSONStorableFloat("colliderLength", Component.colliderLength, value =>
                        {
                            SetColliderLength(value);
                        }, 0f, 0.25f, false)
                        .WithDefault(_initialColliderLength)
                    ), "Length")
            );
        }

        if (Component.useAutoRadius)
        {
            RegisterControl(
                    Script.CreateFloatSlider(RegisterStorable(
                        new JSONStorableFloat("autoRadiusBuffer", Component.autoRadiusBuffer, value =>
                        {
                            SetAutoRadiusBuffer(value);
                        }, -0.025f, 0.025f, false)
                        .WithDefault(_initialAutoRadiusBuffer)
                    ), "Auto Radius Buffer")
            );

            RegisterControl(
                    Script.CreateFloatSlider(RegisterStorable(
                        new JSONStorableFloat("autoRadiusMultiplier", Component.autoRadiusMultiplier, value =>
                        {
                            SetAutoRadiusMultiplier(value);
                        }, 0.001f, 2f, false)
                        .WithDefault(_initialAutoRadiusMultiplier)
                    ), "Auto Radius Multiplier")
            );
        }
        else
        {
            RegisterControl(
                    Script.CreateFloatSlider(RegisterStorable(
                        new JSONStorableFloat("colliderRadius", Component.colliderRadius, value =>
                        {
                            SetColliderRadius(value);
                        }, 0f, 0.25f, false)
                        .WithDefault(_initialColliderRadius)
                    ), "Radius")
            );

            RegisterControl(
                    Script.CreateFloatSlider(RegisterStorable(
                        new JSONStorableFloat("hardColliderBuffer", Component.hardColliderBuffer, value =>
                        {
                            SetHardColliderBuffer(value);
                        }, 0f, 0.25f, false)
                        .WithDefault(_initialHardColliderBuffer)
                    ), "Hard Collider Buffer")
            );
        }

        RegisterControl(
                Script.CreateFloatSlider(RegisterStorable(
                    new JSONStorableFloat("colliderLookOffset", Component.colliderLookOffset, value =>
                    {
                        SetColliderLookOffset(value);
                    }, MakeMinOffset(Component.colliderLookOffset), MakeMaxOffset(Component.colliderLookOffset), false)
                    .WithDefault(_initialColliderLookOffset)
                ), "Look Offset")
        );

        RegisterControl(
                Script.CreateFloatSlider(RegisterStorable(
                    new JSONStorableFloat("colliderUpOffset", Component.colliderUpOffset, value =>
                    {
                        SetColliderUpOffset(value);
                    }, MakeMinOffset(Component.colliderUpOffset), MakeMaxOffset(Component.colliderUpOffset), false)
                    .WithDefault(_initialColliderUpOffset)
                ), "Up Offset")
        );

        RegisterControl(
                Script.CreateFloatSlider(RegisterStorable(
                    new JSONStorableFloat("colliderRightOffset", Component.colliderRightOffset, value =>
                    {
                        SetColliderRightOffset(value);
                    }, MakeMinOffset(Component.colliderRightOffset), MakeMaxOffset(Component.colliderRightOffset), false)
                    .WithDefault(_initialColliderRightOffset)
                ), "Right Offset")
        );
    }

    private void SetCollisionEnabled(bool value)
    {
        Component.collisionEnabled = _collisionEnabled = value;
        RefreshAutoCollider();
        SetModified();
        SetLinked((m, v) => (m as AutoColliderModel)?.SetCollisionEnabled(value), value);
    }

    private void SetAutoLengthBuffer(float value)
    {
        Component.autoLengthBuffer = _autoLengthBuffer = value;
        RefreshAutoCollider();
        SetModified();
        SetLinked((m, v) => (m as AutoColliderModel)?.SetAutoLengthBuffer(v), value);
    }

    private void SetColliderLength(float value)
    {
        Component.colliderLength = _colliderLength = value;
        RefreshAutoCollider();
        SetModified();
        SetLinked((m, v) => (m as AutoColliderModel)?.SetColliderLength(v), value);
    }

    private void SetAutoRadiusBuffer(float value)
    {
        Component.autoRadiusBuffer = _autoRadiusBuffer = value;
        RefreshAutoCollider();
        SetModified();
        SetLinked((m, v) => (m as AutoColliderModel)?.SetAutoRadiusBuffer(v), value);
    }

    private void SetAutoRadiusMultiplier(float value)
    {
        Component.autoRadiusMultiplier = _autoRadiusMultiplier = value;
        RefreshAutoCollider();
        SetModified();
        SetLinked((m, v) => (m as AutoColliderModel)?.SetAutoRadiusMultiplier(v), value);
    }

    private void SetColliderRadius(float value)
    {
        Component.colliderRadius = _colliderRadius = value;
        RefreshAutoCollider();
        SetModified();
        SetLinked((m, v) => (m as AutoColliderModel)?.SetColliderRadius(v), value);
    }

    private void SetHardColliderBuffer(float value)
    {
        Component.hardColliderBuffer = _hardColliderBuffer = value;
        RefreshAutoCollider();
        SetModified();
        SetLinked((m, v) => (m as AutoColliderModel)?.SetHardColliderBuffer(v), value);
    }

    private void SetColliderLookOffset(float value)
    {
        Component.colliderLookOffset = _colliderLookOffset = value;
        RefreshAutoCollider();
        SetModified();
        SetLinked((m, v) => (m as AutoColliderModel)?.SetColliderLookOffset(v), value);
    }

    private void SetColliderUpOffset(float value)
    {
        Component.colliderUpOffset = _colliderUpOffset = value;
        RefreshAutoCollider();
        SetModified();
        SetLinked((m, v) => (m as AutoColliderModel)?.SetColliderUpOffset(v), value);
    }

    private void SetColliderRightOffset(float value)
    {
        Component.colliderRightOffset = _colliderRightOffset = value;
        RefreshAutoCollider();
        SetModified();
        SetLinked((m, v) => (m as AutoColliderModel)?.SetColliderRightOffset(v), -value);
    }

    public void RefreshAutoCollider()
    {
        var previousResizeTrigger = AutoCollider.resizeTrigger;
        AutoCollider.resizeTrigger = AutoCollider.ResizeTrigger.Always;
        AutoCollider.AutoColliderSizeSet(true);
        AutoCollider.resizeTrigger = previousResizeTrigger;
        SyncPreview();
    }

    public void ReapplyMultiplier()
    {
        if (Modified)
        {
            if (Component.autoRadiusMultiplier != _autoRadiusMultiplier)
                Component.autoRadiusMultiplier = _autoRadiusMultiplier;
            return;
        }
    }

    protected override void SetSelected(bool value)
    {
        // TODO: Track colliders to highlight them
        base.SetSelected(value);
    }

    protected override void DoLoadJson(JSONClass jsonClass)
    {
        LoadJsonField(jsonClass, "collisionEnabled", val => Component.collisionEnabled = _collisionEnabled = val);

        if (Component.useAutoLength)
        {
            LoadJsonField(jsonClass, "autoLengthBuffer", val => Component.autoLengthBuffer = _autoLengthBuffer = val);
        }
        else
        {
            LoadJsonField(jsonClass, "colliderLength", val => Component.colliderLength = _colliderLength = val);
        }
        if (Component.useAutoRadius)
        {
            LoadJsonField(jsonClass, "autoRadiusBuffer", val => Component.autoRadiusBuffer = _autoRadiusBuffer = val);
            LoadJsonField(jsonClass, "autoRadiusMultiplier", val => Component.autoRadiusMultiplier = _autoRadiusMultiplier = val);
        }
        else
        {
            LoadJsonField(jsonClass, "colliderRadius", val => Component.colliderRadius = _colliderRadius = val);
            LoadJsonField(jsonClass, "hardColliderBuffer", val => Component.hardColliderBuffer = _hardColliderBuffer = val);
        }

        LoadJsonField(jsonClass, "colliderLookOffset", val => Component.colliderLookOffset = _colliderLookOffset = val);
        LoadJsonField(jsonClass, "colliderUpOffset", val => Component.colliderUpOffset = _colliderUpOffset = val);
        LoadJsonField(jsonClass, "colliderRightOffset", val => Component.colliderRightOffset = _colliderRightOffset = val);
    }

    protected override JSONClass DoGetJson()
    {
        var jsonClass = new JSONClass();
        jsonClass["collisionEnabled"].AsBool = _collisionEnabled;
        if (Component.useAutoLength)
        {
            jsonClass["autoLengthBuffer"].AsFloat = _autoLengthBuffer;
        }
        else
        {
            jsonClass["colliderLength"].AsFloat = _colliderLength;
        }
        if (Component.useAutoRadius)
        {
            jsonClass["autoRadiusBuffer"].AsFloat = _autoRadiusBuffer;
            jsonClass["autoRadiusMultiplier"].AsFloat = _autoRadiusMultiplier;
        }
        else
        {
            jsonClass["colliderRadius"].AsFloat = _colliderRadius;
            jsonClass["hardColliderBuffer"].AsFloat = _hardColliderBuffer;
        }
        jsonClass["colliderLookOffset"].AsFloat = _colliderLookOffset;
        jsonClass["colliderUpOffset"].AsFloat = _colliderUpOffset;
        jsonClass["colliderRightOffset"].AsFloat = _colliderRightOffset;
        return jsonClass;
    }

    protected override void DoResetToInitial()
    {
        Component.collisionEnabled = _collisionEnabled = _initialCollisionEnabled;
        if (Component.useAutoLength)
        {
            Component.autoLengthBuffer = _autoLengthBuffer = _initialAutoLengthBuffer;
        }
        else
        {
            Component.colliderLength = _colliderLength = _initialColliderLength;
        }
        if (Component.useAutoRadius)
        {
            Component.autoRadiusBuffer = _autoRadiusBuffer = _initialAutoRadiusBuffer;
            Component.autoRadiusMultiplier = _autoRadiusMultiplier = _initialAutoRadiusMultiplier;
        }
        else
        {
            Component.colliderRadius = _colliderRadius = _initialColliderRadius;
            Component.hardColliderBuffer = _hardColliderBuffer = _initialHardColliderBuffer;
        }
        Component.colliderLookOffset = _colliderLookOffset = _initialColliderLookOffset;
        Component.colliderUpOffset = _colliderUpOffset = _initialColliderUpOffset;
        Component.colliderRightOffset = _colliderRightOffset = _initialColliderRightOffset;

        RefreshAutoCollider();
    }

    public override IEnumerable<ColliderModel> GetColliders() => _ownedColliders;

    public IEnumerable<Rigidbody> GetRigidbodies()
    {
        if (Component.jointRB != null) yield return Component.jointRB;
        if (Component.kinematicRB != null) yield return Component.kinematicRB;
    }
}
