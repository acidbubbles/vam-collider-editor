using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

public class AutoColliderModel : ColliderContainerModelBase<AutoCollider>, IModel
{
    private bool _initialCollisionEnabled;
    private bool _collisionEnabled;
    private float _initialColliderLength;
    private float _colliderLength;
    private float _initialColliderRadius;
    private float _colliderRadius;
    private float _initialHardColliderBuffer;
    private float _hardColliderBuffer;
    private float _initialColliderLookOffset;
    private float _colliderLookOffset;
    private float _initialColliderUpOffset;
    private float _colliderUpOffset;
    private float _initialColliderRightOffset;
    private float _colliderRightOffset;
    private float _initialAutoLengthBuffer;
    private float _autoLengthBuffer;
    private float _initialAutoRadiusBuffer;
    private float _autoRadiusBuffer;
    private float _initialAutoRadiusMultiplier;
    private float _autoRadiusMultiplier;
    private readonly List<ColliderModel> _ownedColliders = new List<ColliderModel>();

    private JSONStorableBool _collisionEnabledJSON;
    private JSONStorableFloat _autoLengthBufferJSON;
    private JSONStorableFloat _colliderLengthJSON;
    private JSONStorableFloat _autoRadiusBufferJSON;
    private JSONStorableFloat _autoRadiusMultiplierJSON;
    private JSONStorableFloat _colliderRadiusJSON;
    private JSONStorableFloat _hardColliderBufferJSON;
    private JSONStorableFloat _colliderLookOffsetJSON;
    private JSONStorableFloat _colliderUpOffsetJSON;
    private JSONStorableFloat _colliderRightOffsetJSON;

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
        StoreInitialValues();
        if (Component.hardCollider != null) _ownedColliders.Add(ColliderModel.CreateTyped(script, autoCollider.hardCollider, config));
        if (Component.jointCollider != null) _ownedColliders.Add(ColliderModel.CreateTyped(script, Component.jointCollider, config));
    }

    private void StoreInitialValues()
    {
        _initialCollisionEnabled = _collisionEnabled = AutoCollider.collisionEnabled;
        _initialColliderLength = _colliderLength = AutoCollider.colliderLength;
        _initialColliderRadius = _colliderRadius = AutoCollider.colliderRadius;
        _initialHardColliderBuffer = _hardColliderBuffer = AutoCollider.hardColliderBuffer;
        _initialColliderLookOffset = _colliderLookOffset = AutoCollider.colliderLookOffset;
        _initialColliderUpOffset = _colliderUpOffset = AutoCollider.colliderUpOffset;
        _initialColliderRightOffset = _colliderRightOffset = AutoCollider.colliderRightOffset;
        _initialAutoLengthBuffer = _autoLengthBuffer = AutoCollider.autoLengthBuffer;
        _initialAutoRadiusBuffer = _autoRadiusBuffer = AutoCollider.autoRadiusBuffer;
        _initialAutoRadiusMultiplier = _autoRadiusMultiplier = AutoCollider.autoRadiusMultiplier;
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

        _collisionEnabledJSON = new JSONStorableBool("collisionEnabled", Component.collisionEnabled, SetCollisionEnabled).WithDefault(_initialCollisionEnabled);
        RegisterControl(Script.CreateToggle(RegisterStorable(_collisionEnabledJSON), "Collision Enabled"));

        if (Component.useAutoLength)
        {
            _autoLengthBufferJSON = new JSONStorableFloat("autoLengthBuffer", Component.autoLengthBuffer, SetAutoLengthBuffer, -0.25f, 0.25f, false).WithDefault(_initialAutoLengthBuffer);
            RegisterControl(Script.CreateFloatSlider(RegisterStorable(_autoLengthBufferJSON), "Auto Length Buffer"));
        }
        else
        {
            _colliderLengthJSON = new JSONStorableFloat("colliderLength", Component.colliderLength, SetColliderLength, 0f, 0.25f, false).WithDefault(_initialColliderLength);
            RegisterControl(Script.CreateFloatSlider(RegisterStorable(_colliderLengthJSON), "Length"));
        }

        if (Component.useAutoRadius)
        {
            _autoRadiusBufferJSON = new JSONStorableFloat("autoRadiusBuffer", Component.autoRadiusBuffer, SetAutoRadiusBuffer, -0.025f, 0.025f, false).WithDefault(_initialAutoRadiusBuffer);
            RegisterControl( Script.CreateFloatSlider(RegisterStorable( _autoRadiusBufferJSON  ), "Auto Radius Buffer") );

            _autoRadiusMultiplierJSON = new JSONStorableFloat("autoRadiusMultiplier", Component.autoRadiusMultiplier, SetAutoRadiusMultiplier, 0.001f, 2f, false).WithDefault(_initialAutoRadiusMultiplier);
            RegisterControl(Script.CreateFloatSlider(RegisterStorable(_autoRadiusMultiplierJSON), "Auto Radius Multiplier"));
        }
        else
        {
            _colliderRadiusJSON = new JSONStorableFloat("colliderRadius", Component.colliderRadius, SetColliderRadius, 0f, 0.25f, false).WithDefault(_initialColliderRadius);
            RegisterControl(Script.CreateFloatSlider(RegisterStorable(_colliderRadiusJSON), "Radius"));

            _hardColliderBufferJSON = new JSONStorableFloat("hardColliderBuffer", Component.hardColliderBuffer, SetHardColliderBuffer, 0f, 0.25f, false).WithDefault(_initialHardColliderBuffer);
            RegisterControl(Script.CreateFloatSlider(RegisterStorable(_hardColliderBufferJSON), "Hard Collider Buffer"));
        }

        _colliderLookOffsetJSON = new JSONStorableFloat("colliderLookOffset", Component.colliderLookOffset, SetColliderLookOffset, MakeMinOffset(Component.colliderLookOffset), MakeMaxOffset(Component.colliderLookOffset), false).WithDefault(_initialColliderLookOffset);
        RegisterControl(Script.CreateFloatSlider(RegisterStorable(_colliderLookOffsetJSON), "Look Offset"));

        _colliderUpOffsetJSON = new JSONStorableFloat("colliderUpOffset", Component.colliderUpOffset, SetColliderUpOffset, MakeMinOffset(Component.colliderUpOffset), MakeMaxOffset(Component.colliderUpOffset), false).WithDefault(_initialColliderUpOffset);
        RegisterControl(Script.CreateFloatSlider(RegisterStorable(_colliderUpOffsetJSON), "Up Offset"));

        _colliderRightOffsetJSON = new JSONStorableFloat("colliderRightOffset", Component.colliderRightOffset, SetColliderRightOffset, MakeMinOffset(Component.colliderRightOffset), MakeMaxOffset(Component.colliderRightOffset), false).WithDefault(_initialColliderRightOffset);
        RegisterControl(Script.CreateFloatSlider(RegisterStorable(_colliderRightOffsetJSON), "Right Offset"));
    }

    private void SetCollisionEnabled(bool value)
    {
        if (_collisionEnabledJSON != null) _collisionEnabledJSON.valNoCallback = value;
        Component.collisionEnabled = _collisionEnabled = value;
        RefreshAutoCollider();
        SetModified();
        SetMirror<AutoColliderModel>(m => m.SetCollisionEnabled(value));
    }

    private void SetAutoLengthBuffer(float value)
    {
        if (_autoLengthBufferJSON != null) _autoLengthBufferJSON.valNoCallback = value;
        Component.autoLengthBuffer = _autoLengthBuffer = value;
        RefreshAutoCollider();
        SetModified();
        SetMirror<AutoColliderModel>(m => m.SetAutoLengthBuffer(value));
    }

    private void SetColliderLength(float value)
    {
        if (_colliderLengthJSON != null) _colliderLengthJSON.valNoCallback = value;
        Component.colliderLength = _colliderLength = value;
        RefreshAutoCollider();
        SetModified();
        SetMirror<AutoColliderModel>(m => m.SetColliderLength(value));
    }

    private void SetAutoRadiusBuffer(float value)
    {
        if (_autoRadiusBufferJSON != null) _autoRadiusBufferJSON.valNoCallback = value;
        Component.autoRadiusBuffer = _autoRadiusBuffer = value;
        RefreshAutoCollider();
        SetModified();
        SetMirror<AutoColliderModel>(m => m.SetAutoRadiusBuffer(value));
    }

    private void SetAutoRadiusMultiplier(float value)
    {
        if (_autoRadiusMultiplierJSON != null) _autoRadiusMultiplierJSON.valNoCallback = value;
        Component.autoRadiusMultiplier = _autoRadiusMultiplier = value;
        RefreshAutoCollider();
        SetModified();
        SetMirror<AutoColliderModel>(m => m.SetAutoRadiusMultiplier(value));
    }

    private void SetColliderRadius(float value)
    {
        if (_colliderRadiusJSON != null) _colliderRadiusJSON.valNoCallback = value;
        Component.colliderRadius = _colliderRadius = value;
        RefreshAutoCollider();
        SetModified();
        SetMirror<AutoColliderModel>(m => m.SetColliderRadius(value));
    }

    private void SetHardColliderBuffer(float value)
    {
        if (_hardColliderBufferJSON != null) _hardColliderBufferJSON.valNoCallback = value;
        Component.hardColliderBuffer = _hardColliderBuffer = value;
        RefreshAutoCollider();
        SetModified();
        SetMirror<AutoColliderModel>(m => m.SetHardColliderBuffer(value));
    }

    private void SetColliderLookOffset(float value)
    {
        if (_colliderLookOffsetJSON != null) _colliderLookOffsetJSON.valNoCallback = value;
        Component.colliderLookOffset = _colliderLookOffset = value;
        RefreshAutoCollider();
        SetModified();
        SetMirror<AutoColliderModel>(m => m.SetColliderLookOffset(value));
    }

    private void SetColliderUpOffset(float value)
    {
        if (_colliderUpOffsetJSON != null) _colliderUpOffsetJSON.valNoCallback = value;
        Component.colliderUpOffset = _colliderUpOffset = value;
        RefreshAutoCollider();
        SetModified();
        SetMirror<AutoColliderModel>(m => m.SetColliderUpOffset(value));
    }

    private void SetColliderRightOffset(float value)
    {
        if (_colliderRightOffsetJSON != null) _colliderRightOffsetJSON.valNoCallback = value;
        Component.colliderRightOffset = _colliderRightOffset = value;
        RefreshAutoCollider();
        SetModified();
        SetMirror<AutoColliderModel>(m => m.SetColliderRightOffset(-value));
    }

    public void RefreshAutoCollider()
    {
        var previousResizeTrigger = AutoCollider.resizeTrigger;
        AutoCollider.resizeTrigger = AutoCollider.ResizeTrigger.Always;
        AutoCollider.AutoColliderSizeSet(true);
        AutoCollider.resizeTrigger = previousResizeTrigger;
        SyncPreviews();
    }

    public void ReapplyMultiplier()
    {
        if (!Modified) return;
        if (Component.autoRadiusMultiplier != _autoRadiusMultiplier)
            Component.autoRadiusMultiplier = _autoRadiusMultiplier;
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

        // Restore initial values
        Component.AutoColliderSizeSet(true);

        StoreInitialValues();

        RefreshAutoCollider();
    }

    public override IEnumerable<ColliderModel> GetColliders() => _ownedColliders;

    public IEnumerable<Rigidbody> GetRigidbodies()
    {
        if (Component.jointRB != null) yield return Component.jointRB;
        if (Component.kinematicRB != null) yield return Component.kinematicRB;
    }
}
