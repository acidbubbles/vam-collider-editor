using System.Collections.Generic;
using UnityEngine;

public abstract class ColliderContainerModelBase<T> : ModelBase<T> where T : Component
{
    protected abstract bool OwnsColliders { get; }

    protected ColliderContainerModelBase(MVRScript script, T component, string label)
        : base(script, component, label)
    {
    }

    protected override void SetSelected(bool value)
    {
        foreach (var collider in GetColliders())
            collider.SetHighlighted(value);
        base.SetSelected(value);
    }

    public void SetXRayPreview(bool value)
    {
        if (!OwnsColliders) return;
        foreach (var colliderModel in GetColliders())
            colliderModel.SetXRayPreview(value);
    }

    public void SetSelectedPreviewOpacity(float value)
    {
        if (!OwnsColliders) return;
        foreach (var colliderModel in GetColliders())
            colliderModel.SetSelectedPreviewOpacity(value);
    }

    public void SetPreviewOpacity(float value)
    {
        if (!OwnsColliders) return;
        foreach (var colliderModel in GetColliders())
            colliderModel.SetPreviewOpacity(value);
    }

    public void SetShowPreview(bool value)
    {
        if (!OwnsColliders) return;
        foreach (var colliderModel in GetColliders())
            colliderModel.SetShowPreview(value);
    }

    public void DestroyPreview()
    {
        foreach (var colliderModel in GetColliders())
            colliderModel.DestroyPreview();
    }

    public abstract IEnumerable<ColliderModel> GetColliders();
}