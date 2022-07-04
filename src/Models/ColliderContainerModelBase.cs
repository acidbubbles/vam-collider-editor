using System.Collections.Generic;
using UnityEngine;

public abstract class ColliderContainerModelBase<T> : ModelBase<T> where T : Component
{
    private bool _shown;

    protected abstract bool OwnsColliders { get; }
    public bool Shown
    {
        get
        {
            return _shown;
        }
        set
        {
            _shown = value;
            if (OwnsColliders)
            {
                foreach (var c in GetColliders())
                {
                    c.Shown = value;
                }
            }
        }
    }

    protected ColliderContainerModelBase(MVRScript script, T component, string label)
        : base(script, component, label)
    {
    }

    public override void SetHighlighted(bool value)
    {
        foreach (var collider in GetColliders())
            collider.SetHighlighted(value);
    }

    public override void SyncPreviews()
    {
        if (!OwnsColliders) return;
        foreach (var colliderModel in GetColliders())
            colliderModel.SyncPreviews();
    }

    public virtual void UpdatePreviewsFromConfig()
    {
        if (!OwnsColliders) return;
        foreach (var colliderModel in GetColliders())
            colliderModel.UpdatePreviewsFromConfig();
    }

    public void DestroyPreviews()
    {
        foreach (var colliderModel in GetColliders())
            colliderModel.DestroyPreviews();
    }

    public abstract IEnumerable<ColliderModel> GetColliders();
}
