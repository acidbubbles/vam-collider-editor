using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EditablesList
{
    public static EditablesList Build(MVRScript script)
    {
        var containingAtom = script.containingAtom;

        var groups = containingAtom.type == "Person"
                 ? new List<Group>
                 {
                    new Group("Head / Ears", @"^(head|lowerJaw|tongue|neck)"),
                    new Group("Left arm", @"^l(Shldr|ForeArm)"),
                    new Group("Left hand", @"^l(Index|Mid|Ring|Pinky|Thumb|Carpal|Hand)[0-9]?$"),
                    new Group("Right arm", @"^r(Shldr|ForeArm)"),
                    new Group("Right hand", @"^r(Index|Mid|Ring|Pinky|Thumb|Carpal|Hand)[0-9]?$"),
                    new Group("Chest", @"^(chest|AutoColliderFemaleAutoColliderschest)"),
                    new Group("Left breast", @"l((Pectoral)|Nipple)"),
                    new Group("Right breast", @"r((Pectoral)|Nipple)"),
                    new Group("Abdomen / Belly / Back", @"^(AutoColliderFemaleAutoColliders)?abdomen"),
                    new Group("Hip / Pelvis", @"^(AutoCollider)?(hip|pelvis)"),
                    new Group("Glute", @"^(AutoColliderFemaleAutoColliders)?[LR]Glute"),
                    new Group("Anus", @"^_JointA[rl]"),
                    new Group("Vagina", @"^_Joint(Gr|Gl|B)"),
                    new Group("Penis", @"^(Gen[1-3])|Testes"),
                    new Group("Left leg", @"^(AutoCollider(FemaleAutoColliders)?)?l(Thigh|Shin)"),
                    new Group("Left foot", @"^l(Foot|Toe|BigToe|SmallToe)"),
                    new Group("Right leg", @"^(AutoCollider(FemaleAutoColliders)?)?r(Thigh|Shin)"),
                    new Group("Right foot", @"^r(Foot|Toe|BigToe|SmallToe)"),
                    new Group("Other", @"^(?!.*).*$")
                 }
                 : new List<Group>
                 {
                    new Group("All", @"^.+$"),
                 };

        // AutoColliders

        var autoColliderDuplicates = new HashSet<string>();
        var autoColliders = containingAtom.GetComponentsInChildren<AutoCollider>()
            .Select(autoCollider => new AutoColliderModel(script, autoCollider))
            .Where(model => { if (!autoColliderDuplicates.Add(model.Id)) { model.IsDuplicate = true; return false; } else { return true; } })
            .ForEach(model => model.Group = groups.FirstOrDefault(g => g.Test(model.AutoCollider.name)))
            .ToList();

        var autoCollidersRigidBodies = new HashSet<Rigidbody>(autoColliders.SelectMany(x => x.GetRigidbodies()));
        var autoCollidersColliders = new HashSet<Collider>(autoColliders.SelectMany(x => x.GetColliders()).Select(x => x.Collider));

        // Rigidbodies

        var rigidbodyDuplicates = new HashSet<string>();
        var rigidbodies = containingAtom.GetComponentsInChildren<Rigidbody>(true)
            .Where(rigidbody => !autoCollidersRigidBodies.Contains(rigidbody))
            .Where(rigidbody => IsRigidbodyIncluded(rigidbody))
            .Select(rigidbody => new RigidbodyModel(script, rigidbody))
            .Where(model => { if (!rigidbodyDuplicates.Add(model.Id)) { model.IsDuplicate = true; return false; } else { return true; } })
            .ForEach(model => model.Group = groups.FirstOrDefault(g => g.Test(model.Rigidbody.name)))
            .ToList();
        var rigidbodiesDict = rigidbodies.ToDictionary(x => x.Id);

        // Colliders

        var colliderDuplicates = new HashSet<string>();
        var colliders = containingAtom.GetComponentsInChildren<Collider>(true)
            .Where(collider => !autoCollidersColliders.Contains(collider))
            .Where(collider => IsColliderIncluded(collider))
            .Select(collider => ColliderModel.CreateTyped(script, collider))
            .Where(model => { if (!colliderDuplicates.Add(model.Id)) { model.IsDuplicate = true; return false; } else { return true; } })
            .ToList();

        // Attach colliders to rigidbodies

        foreach (var colliderModel in colliders)
        {
            if (colliderModel.Collider.attachedRigidbody != null)
            {
                RigidbodyModel rigidbodyModel;
                if (rigidbodiesDict.TryGetValue(colliderModel.Collider.attachedRigidbody.Uuid(), out rigidbodyModel))
                {
                    colliderModel.RigidbodyModel = rigidbodyModel;
                    rigidbodyModel.Colliders.Add(colliderModel);
                    colliderModel.Group = rigidbodyModel.Group;
                }
                else
                {
                    colliderModel.Group = groups.FirstOrDefault(g => g.Test(colliderModel.Collider.name));
                }
            }
        }

        // All Editables

        var all = colliders.Cast<IModel>()
            .Concat(autoColliders.Cast<IModel>())
            .Concat(rigidbodies.Cast<IModel>())
            .ToList();

        return new EditablesList(groups, all, colliders);
    }

    private static bool IsColliderIncluded(Collider collider)
    {
        if (collider.name == "control") return false;
        if (collider.name == "object") return false;
        if (collider.name.Contains("Tool")) return false;
        if (collider.name.EndsWith("Control")) return false;
        if (collider.name.EndsWith("Link")) return false;
        if (collider.name.EndsWith("Trigger")) return false;
        if (collider.name.EndsWith("UI")) return false;
        if (collider.name.Contains("Ponytail")) return false;
        return true;
    }

    private static bool IsRigidbodyIncluded(Rigidbody rigidbody)
    {
        if (rigidbody.isKinematic) return false;
        if (rigidbody.name == "control") return false;
        if (rigidbody.name == "object") return false;
        if (rigidbody.name.EndsWith("Control")) return false;
        if (rigidbody.name.StartsWith("hairTool")) return false;
        if (rigidbody.name.EndsWith("Trigger")) return false;
        if (rigidbody.name.EndsWith("UI")) return false;
        if (rigidbody.name.Contains("Ponytail")) return false;
        return true;
    }

    public Dictionary<string, ColliderModel> Colliders { get; }
    public List<Group> Groups { get; }
    public Dictionary<string, IModel> ByUuid { get; }
    public List<IModel> All { get; }

    public EditablesList(List<Group> groups, List<IModel> all, List<ColliderModel> colliders)
    {
        Groups = groups;
        ByUuid = all.ToDictionary(x => x.Id, x => x);
        All = all.OrderBy(a => a.Label).ToList();
        Colliders = colliders.ToDictionary(x => x.Id);
    }
}
