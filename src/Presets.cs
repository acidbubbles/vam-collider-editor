using System;
using System.Collections.Generic;
using UnityEngine;

public static class Presets
{
    public const string None = "";
    public const string DisableAllCollisions = "Disable All Collisions";
    public const string Debug = "Debug";

    public static readonly List<string> List = new List<string>
    {
        None,
        DisableAllCollisions,
        Debug
    };

    public static void Apply(string presetName, Atom containingAtom, EditablesList editables)
    {
        switch (presetName)
        {
            case DisableAllCollisions:
                ApplyDisableAllCollisions(containingAtom, editables);
                break;
            case Debug:
                ApplyDebug(containingAtom, editables);
                break;
            default:
                throw new NotSupportedException($"Preset '{presetName}' is not supported");
        }
    }

    private static void ApplyDisableAllCollisions(Atom containingAtom, EditablesList editablesList)
    {
        throw new NotImplementedException();
    }

    private static void ApplyDebug(Atom containingAtom, EditablesList editablesList)
    {
        foreach (var rb in containingAtom.GetComponentsInChildren<Rigidbody>())
        {
            if(rb.detectCollisions)
                SuperController.LogMessage($"- {rb.Uuid()}");
        }
    }
}
