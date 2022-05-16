using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public static class Presets
{
    public const string None = "";
    private const string _enableAllCollisions = "Enable All Collisions";
    private const string _disableAllCollisions = "Disable All Collisions";
    private const string _resetAll = "Reset All";

    public static readonly List<string> List = new List<string>
    {
        None,
        _enableAllCollisions,
        _disableAllCollisions,
        _resetAll
    };

    public static void Apply(string presetName, List<IModel> editables)
    {
        switch (presetName)
        {
            case _enableAllCollisions:
                ApplyCollisions(editables, true);
                break;
            case _disableAllCollisions:
                ApplyCollisions(editables, false);
                break;
            case _resetAll:
                ResetAll(editables);
                break;
            default:
                throw new NotSupportedException($"Preset '{presetName}' is not supported");
        }
    }

    private static void ApplyCollisions(ICollection editables, bool collisionEnabled)
    {
        foreach (var editable in editables.OfType<AutoColliderModel>())
        {
            editable.collisionEnabled = collisionEnabled;
        }

        foreach (var editable in editables.OfType<RigidbodyModel>())
        {
            editable.detectCollisions = collisionEnabled;
        }
    }

    private static void ResetAll(List<IModel> editables)
    {
        foreach (var editable in editables)
            editable.ResetToInitial();
    }
}
