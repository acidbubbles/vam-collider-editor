using System;
using System.Collections.Generic;
using UnityEngine;

public static class ComponentExtensions
{
    public static string Uuid(this Component component)
    {
        var siblings = component.GetComponents<Component>().ToList();
        int siblingIndex = siblings.IndexOf(component);

        var paths = new Stack<string>(new[] { $"{component.name}[{siblingIndex}]" });
        var current = component.gameObject.transform;

        while (current != null && !current.name.Equals("geometry", StringComparison.InvariantCultureIgnoreCase)
                               && !current.name.Equals("Genesis2Female", StringComparison.InvariantCultureIgnoreCase)
                               && !current.name.Equals("Genesis2Male", StringComparison.InvariantCultureIgnoreCase))
        {
            paths.Push($"{current.name}[{current.GetSiblingIndex()}]");
            current = current.transform.parent;
        }

        return component.GetTypeName() + ":" + string.Join(".", paths.ToArray());
    }

    public static string GetTypeName(this Component component)
    {
        if (component is CapsuleCollider)
            return nameof(CapsuleCollider);
        if (component is SphereCollider)
            return nameof(SphereCollider);
        if (component is BoxCollider)
            return nameof(BoxCollider);
        if (component is AutoCollider)
            return nameof(AutoCollider);
        if (component is AutoColliderGroup)
            return nameof(AutoColliderGroup);
        if (component is Rigidbody)
            return nameof(Rigidbody);
        throw new InvalidOperationException($"Unknown component type: {component.GetType()}");
    }

    public static UIDynamic CreateFloatSlider(this MVRScript script, JSONStorableFloat jsf, string label, bool rightSide = true, string valueFormat = "F8")
    {
        var control = script.CreateSlider(jsf, rightSide);
        control.valueFormat = valueFormat;
        control.label = label;
        return control;
    }
}
