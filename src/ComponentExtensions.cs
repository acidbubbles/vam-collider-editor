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

        return string.Join(".", paths.ToArray());
    }

    public static UIDynamic CreateFloatSlider(this MVRScript script, JSONStorableFloat jsf, string label, bool rightSide = true, string valueFormat = "F8")
    {
        var control = script.CreateSlider(jsf, rightSide);
        control.valueFormat = valueFormat;
        control.label = label;
        return control;
    }
}
