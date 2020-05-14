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

    public override void UpdatePreviewFromCollider()
    {
        if (!OwnsColliders) return;
        foreach (var colliderModel in GetColliders())
            colliderModel.UpdatePreviewFromCollider();
    }

    public void UpdatePreviewFromConfig()
    {
        if (!OwnsColliders) return;
        foreach (var colliderModel in GetColliders())
            colliderModel.UpdatePreviewFromConfig();
    }

    public void DestroyPreview()
    {
        foreach (var colliderModel in GetColliders())
            colliderModel.DestroyPreview();
    }

    public abstract IEnumerable<ColliderModel> GetColliders();
}
