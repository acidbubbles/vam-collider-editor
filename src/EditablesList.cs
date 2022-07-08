using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EditablesList
{
    public static EditablesList Build(MVRScript script, ColliderPreviewConfig config)
    {
        var containingAtom = script.containingAtom;

        var groups = containingAtom.type == "Person"
                 ? new List<Group>
                 {
                    new Group("Head / Ears", @"^((AutoCollider(Female)?)?AutoColliders)?(s?[Hh]ead|lowerJaw|[Tt]ongue|neck|s?Face|_?Collider(Lip|Ear|Nose))"),
                    new Group("Left arm", @"^l(Shldr|ForeArm)"),
                    new Group("Left hand", @"^l(Index|Mid|Ring|Pinky|Thumb|Carpal|Hand)[0-9]?$"),
                    new Group("Right arm", @"^r(Shldr|ForeArm)"),
                    new Group("Right hand", @"^r(Index|Mid|Ring|Pinky|Thumb|Carpal|Hand)[0-9]?$"),
                    new Group("Chest", @"^(chest|(AutoCollider)?FemaleAutoColliderschest|MaleAutoColliderschest)"),
                    new Group("Left breast", @"l((Pectoral)|Nipple)"),
                    new Group("Right breast", @"r((Pectoral)|Nipple)"),
                    new Group("Abdomen / Belly / Back", @"^((AutoCollider)?FemaleAutoColliders)?abdomen"),
                    new Group("Hip / Pelvis", @"^((Female)?AutoColliders?|MaleAutoColliders)?(hip|pelvis)"),
                    new Group("Left glutes", @"^((AutoCollider)?FemaleAutoColliders)?LGlute"),
                    new Group("Right glutes", @"^((AutoCollider)?FemaleAutoColliders)?RGlute"),
                    new Group("Anus", @"^_JointA[rl]"),
                    new Group("Vagina", @"^_Joint(Gr|Gl|B)"),
                    new Group("Penis", @"^((AutoCollider)?Gen[1-3])|Testes"),
                    new Group("Left leg", @"^((AutoCollider)?(FemaleAutoColliders)?)?l(Thigh|Shin)"),
                    new Group("Left foot", @"^l(Foot|Toe|BigToe|SmallToe)"),
                    new Group("Right leg", @"^((AutoCollider)?(FemaleAutoColliders)?)?r(Thigh|Shin)"),
                    new Group("Right foot", @"^r(Foot|Toe|BigToe|SmallToe)"),
                    new Group("Physics mesh joints", @"^PhysicsMeshJoint.+$"),
                    new Group("Other", @"^.+$")
                 }
                 : new List<Group>
                 {
                    new Group("All", @"^.+$"),
                 };

        // AutoColliders

        var autoColliderDuplicates = new HashSet<string>();
        var autoColliders = containingAtom.GetComponentsInChildren<AutoCollider>()
            .Select(autoCollider => new AutoColliderModel(script, autoCollider, config))
            .Where(model => { if (!autoColliderDuplicates.Add(model.Id)) { model.IsDuplicate = true; return false; } else { return true; } })
            .ForEach(model => model.Group = groups.FirstOrDefault(g => g.Test(model.AutoCollider.name)))
            .ToList();

        var autoCollidersRigidBodies = new HashSet<Rigidbody>(autoColliders.SelectMany(x => x.GetRigidbodies()));
        var autoCollidersColliders = new HashSet<Collider>(autoColliders.SelectMany(x => x.GetColliders()).Select(x => x.Collider));
        var autoCollidersMap = autoColliders.ToDictionary(x => x.AutoCollider);

        // AutoColliderGroups

        var autoColliderGroupDuplicates = new HashSet<string>();
        var autoColliderGroups = containingAtom.GetComponentsInChildren<AutoColliderGroup>()
            .Select(autoColliderGroup =>
            {
                var childAutoColliders = autoColliderGroup.GetAutoColliders().Where(acg => autoCollidersMap.ContainsKey(acg)).Select(acg => autoCollidersMap[acg]).ToList();
                var model = new AutoColliderGroupModel(script, autoColliderGroup, childAutoColliders);
                childAutoColliders.ForEach(ac => ac.AutoColliderGroup = model);
                return model;
            })
            .Where(model => { if (!autoColliderGroupDuplicates.Add(model.Id)) { model.IsDuplicate = true; return false; } else { return true; } })
            .ForEach(model => model.Group = groups.FirstOrDefault(g => g.Test(model.AutoColliderGroup.name)))
            .ToList();

        // Rigidbodies

        var rigidbodyDuplicates = new HashSet<string>();
        var controllerRigidbodies = new HashSet<Rigidbody>(containingAtom.freeControllers.SelectMany(fc => fc.GetComponents<Rigidbody>()));
        var rigidbodies = containingAtom.GetComponentsInChildren<Rigidbody>(true)
            .Where(rigidbody => !autoCollidersRigidBodies.Contains(rigidbody))
            .Where(rigidbody => !controllerRigidbodies.Contains(rigidbody))
            .Where(rigidbody => IsRigidbodyIncluded(rigidbody))
            .Select(rigidbody => new RigidbodyModel(script, rigidbody))
            .Where(model => { if (!rigidbodyDuplicates.Add(model.Id)) { model.IsDuplicate = true; return false; } else { return true; } })
            .ForEach(model => model.Group = groups.FirstOrDefault(g => g.Test(model.Rigidbody.name)))
            .ToList();
        var rigidbodiesDict = rigidbodies.ToDictionary(x => x.Id);

        // Colliders

        var colliderDuplicates = new HashSet<string>();
        var colliders = containingAtom.GetComponentsInChildren<Collider>(true)
            .Where(collider => collider.gameObject.activeInHierarchy)
            .Where(collider => !autoCollidersColliders.Contains(collider))
            .Where(collider => collider.attachedRigidbody == null || IsRigidbodyIncluded(collider.attachedRigidbody))
            .Where(collider => IsColliderIncluded(collider))
            .Select(collider => ColliderModel.CreateTyped(script, collider, config))
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
                    SuperController.LogError($"Could not find a matching rigidbody for collider '{colliderModel.Id}', rigidbody '{colliderModel.Collider.attachedRigidbody.Uuid()}'.");
                    colliderModel.Group = groups.FirstOrDefault(g => g.Test(colliderModel.Collider.name));
                }
            }
            else
            {
                colliderModel.Group = groups.FirstOrDefault(g => g.Test(colliderModel.Collider.name));
            }
        }

        // Some rigidbodies have collisions even though they have no colliders...
        // rigidbodies.RemoveAll(model => model.Colliders.Count == 0);

        // All Editables

        return new EditablesList(
            groups,
            colliders,
            autoColliders,
            autoColliderGroups,
            rigidbodies
        );
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
        if (collider.name.EndsWith("Joint")) return false;
        if (collider is MeshCollider) return false;
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

    public List<Group> Groups { get; }
    public readonly List<ColliderModel> Colliders;
    public readonly List<AutoColliderModel> AutoColliders;
    public readonly List<AutoColliderGroupModel> AutoColliderGroups;
    public readonly List<RigidbodyModel> Rigidbodies;
    public List<IModel> All { get; }
    public Dictionary<string, IModel> ByUuid { get; }
    private bool _readyForUI;

    private EditablesList(List<Group> groups, List<ColliderModel> colliders, List<AutoColliderModel> autoColliders, List<AutoColliderGroupModel> autoColliderGroups, List<RigidbodyModel> rigidbodies)
    {
        Groups = groups;
        Colliders = colliders;
        AutoColliders = autoColliders;
        AutoColliderGroups = autoColliderGroups;
        Rigidbodies = rigidbodies;

        All = colliders.Cast<IModel>()
            .Concat(autoColliderGroups.Cast<IModel>())
            .Concat(autoColliders.Cast<IModel>())
            .Concat(rigidbodies.Cast<IModel>())
            .OrderBy(a => a.Label)
            .ToList();

        ByUuid = All.ToDictionary(x => x.Id, x => x);
    }

    public void PrepareForUI()
    {
        if (_readyForUI) return;
        _readyForUI = true;
        MatchMirror<AutoColliderModel, AutoCollider>(AutoColliders);
        MatchMirror<AutoColliderGroupModel, AutoColliderGroup>(AutoColliderGroups);
        MatchMirror<ColliderModel, Collider>(Colliders);
        MatchMirror<RigidbodyModel, Rigidbody>(Rigidbodies);
    }

    private static void MatchMirror<TModel, TComponent>(List<TModel> items)
        where TModel : ModelBase<TComponent>
        where TComponent : Component
    {
        var map = items.ToDictionary(i => i.Id, i => i);
        var skip = new HashSet<TModel>();
        foreach (var left in items)
        {
            if (skip.Contains(left))
            {
                continue;
            }
            var rightId = Mirrors.Find(left.Id);
            if (rightId == null)
            {
                continue;
            }
            TModel right;
            if (!map.TryGetValue(rightId, out right))
            {
                if (left.Id.Contains("Shin")) continue;
                // SuperController.LogError("NOT MATCHED:\n" + left.Id + "\n" + rightId);
                continue;
            }
            left.Mirror = right;
            right.Mirror = left;
            skip.Add(right);
        }
    }
}
