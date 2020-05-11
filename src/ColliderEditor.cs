using System;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using UnityEngine;


/// <summary>
/// Collider Editor
/// By Acidbubbles and ProjectCanyon
/// Configures and customizes collisions (rigidbodies and colliders)
/// Source: https://github.com/acidbubbles/vam-collider-editor
/// </summary>
public class ColliderEditor : MVRScript
{
    private const string _saveExt = "colliders";

    private string _lastBrowseDir = SuperController.singleton.savesDir;

    private JSONStorableStringChooser _editablesJson;

    private IModel _selected;

    private UIDynamicPopup _editablesList;

    private Dictionary<string, ColliderModel> _colliders;
    private Dictionary<string, IModel> _editables;

    public override void Init()
    {
        try
        {
            BuildModels();
            BuildUI();
        }
        catch (Exception e)
        {
            SuperController.LogError($"{nameof(ColliderEditor)}.{nameof(Init)}: {e}");
        }
    }

    private void BuildUI()
    {
        var showPreviews = new JSONStorableBool("showPreviews", false, value =>
        {
            foreach (var colliderPair in _colliders)
                colliderPair.Value.ShowPreview = value;
        });
        RegisterBool(showPreviews);

        var showPreviewsToggle = CreateToggle(showPreviews);
        showPreviewsToggle.label = "Show Previews";

        var xRayPreviews = new JSONStorableBool("xRayPreviews", true, value =>
        {
            foreach (var colliderPair in _colliders)
                colliderPair.Value.XRayPreview = value;
        });
        RegisterBool(xRayPreviews);

        var xRayPreviewsToggle = CreateToggle(xRayPreviews);
        xRayPreviewsToggle.label = "Use XRay Previews";

        JSONStorableFloat previewOpacity = new JSONStorableFloat("previewOpacity", 0.001f, value =>
                   {
                       var alpha = ExponentialScale(value, 0.2f, 1f);
                       foreach (var colliderPair in _colliders)
                           colliderPair.Value.PreviewOpacity = alpha;
                   }, 0f, 1f);
        RegisterFloat(previewOpacity);
        CreateSlider(previewOpacity).label = "Preview Opacity";

        JSONStorableFloat selectedPreviewOpacity = new JSONStorableFloat("selectedPreviewOpacity", 0.3f, value =>
                   {
                       var alpha = ExponentialScale(value, 0.2f, 1f);
                       foreach (var colliderPair in _colliders)
                           colliderPair.Value.SelectedPreviewOpacity = alpha;
                   }, 0f, 1f);
        RegisterFloat(selectedPreviewOpacity);
        CreateSlider(selectedPreviewOpacity).label = "Selected Preview Opacity";

        var loadPresetUI = CreateButton("Load Preset");
        loadPresetUI.button.onClick.AddListener(() =>
        {
            if (_lastBrowseDir != null) SuperController.singleton.NormalizeMediaPath(_lastBrowseDir);
            SuperController.singleton.GetMediaPathDialog(HandleLoadPreset, _saveExt);
        });

        var savePresetUI = CreateButton("Save Preset");
        savePresetUI.button.onClick.AddListener(() =>
        {
            SuperController.singleton.NormalizeMediaPath(_lastBrowseDir);
            SuperController.singleton.GetMediaPathDialog(HandleSavePreset, _saveExt);

            var browser = SuperController.singleton.mediaFileBrowserUI;
            browser.SetTextEntry(true);
            browser.fileEntryField.text = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds + "." + _saveExt;
            browser.ActivateFileNameField();
        });

        var resetAllUI = CreateButton("Reset All");
        resetAllUI.button.onClick.AddListener(() =>
        {
            foreach (var colliderPair in _colliders)
                colliderPair.Value.ResetToInitial();
        });

        _editablesJson = new JSONStorableStringChooser(
            "Edit",
            new List<string>(),
            new List<string>(),
            "",
            "Edit");

        _editablesList = CreateScrollablePopup(_editablesJson, true);
        _editablesList.popupPanelHeight = 400f;

        _editablesJson.setCallbackFunction = id =>
        {
            if (_selected != null) _selected.Selected = false;
            _editables.TryGetValue(id, out _selected);
            if (_selected != null) _selected.Selected = true;
            SyncPopups();
        };

        UpdateFilter();
    }

    private void SyncPopups()
    {
        _editablesList.popup.Toggle();
        _editablesList.popup.Toggle();
    }

    private void BuildModels()
    {
        var groups = containingAtom.type == "Person"
                 ? new List<Group>
                 {
                    new Group("All", @"^.+$"),
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
        var groupsDict = groups.ToDictionary(x => x.Id);

        // AutoColliders

        var autoColliderDuplicates = new HashSet<string>();
        var autoColliders = containingAtom.GetComponentsInChildren<AutoCollider>()
            .Select(autoCollider => new AutoColliderModel(this, autoCollider))
            .Where(model => { if (!autoColliderDuplicates.Add(model.Id)) { model.IsDuplicate = true; return false; } else { return true; } })
            .ToList();

        var autoCollidersRigidBodies = new HashSet<Rigidbody>(autoColliders.SelectMany(x => x.GetRigidbodies()));
        var autoCollidersColliders = new HashSet<Collider>(autoColliders.SelectMany(x => x.GetColliders()));

        // Rigidbodies

        var rigidbodyDuplicates = new HashSet<string>();
        var rigidbodies = containingAtom.GetComponentsInChildren<Rigidbody>(true)
            .Where(rigidbody => !autoCollidersRigidBodies.Contains(rigidbody))
            .Where(rigidbody => IsRigidbodyIncluded(rigidbody))
            .Select(rigidbody => RigidbodyModel.Create(this, rigidbody, groups))
            .Where(model => { if (!rigidbodyDuplicates.Add(model.Id)) { model.IsDuplicate = true; return false; } else { return true; } })
            .ToList();
        var rigidbodiesDict = rigidbodies.ToDictionary(x => x.Id);

        // Colliders

        var colliderDuplicates = new HashSet<string>();
        var colliders = containingAtom.GetComponentsInChildren<Collider>(true)
            .Where(collider => !autoCollidersColliders.Contains(collider))
            .Where(collider => IsColliderIncluded(collider))
            .Select(collider => ColliderModel.CreateTyped(this, collider, rigidbodiesDict))
            .Where(model => { if (!colliderDuplicates.Add(model.Id)) { model.IsDuplicate = true; return false; } else { return true; } })
            .ToList();

        _colliders = colliders.ToDictionary(x => x.Id);

        // All Editables

        _editables = colliders.Cast<IModel>()
            .Concat(autoColliders.Cast<IModel>())
            .Concat(rigidbodies.Cast<IModel>())
            .ToDictionary(x => x.Id, x => x);
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

    private void UpdateFilter()
    {
        try
        {
            // TODO: Split updating filter and updating selection, no need to repopulate every time
            var editables = _editables.Values.OrderBy(e => e.Label).ToList();
            _editablesJson.choices = editables.Select(x => x.Id).ToList();
            _editablesJson.displayChoices = editables.Select(x => x.Label).ToList();

            SyncPopups();
        }
        catch (Exception e)
        {
            LogError(nameof(UpdateFilter), e.ToString());
        }
    }

    private void LogError(string method, string message) => SuperController.LogError($"{nameof(ColliderEditor)}.{method}: {message}");

    private void HandleLoadPreset(string path)
    {
        if (string.IsNullOrEmpty(path))
            return;
        _lastBrowseDir = path.Substring(0, path.LastIndexOfAny(new[] { '/', '\\' })) + @"\";

        LoadFromJson((JSONClass)LoadJSON(path));
    }

    private void LoadFromJson(JSONClass jsonClass)
    {
        var editablesJsonClass = jsonClass["editables"].AsObject;
        foreach (string editableId in editablesJsonClass.Keys)
        {
            IModel editableModel;
            if (_editables.TryGetValue(editableId, out editableModel))
                editableModel.LoadJson(editablesJsonClass[editableId].AsObject);
        }
    }

    private void HandleSavePreset(string path)
    {
        if (string.IsNullOrEmpty(path))
            return;

        _lastBrowseDir = path.Substring(0, path.LastIndexOfAny(new[] { '/', '\\' })) + @"\";

        if (!path.ToLower().EndsWith($".{_saveExt}"))
            path += $".{_saveExt}";

        var presetJsonClass = new JSONClass();
        AppendJson(presetJsonClass);
        SaveJSON(presetJsonClass, path);
    }

    public void OnDestroy()
    {
        if (_colliders == null) return;
        try
        {
            foreach (var colliderModelPair in _colliders)
                colliderModelPair.Value.DestroyPreview();
        }
        catch (Exception e)
        {
            LogError(nameof(OnDestroy), e.ToString());
        }
    }

    public override JSONClass GetJSON(bool includePhysical = true, bool includeAppearance = true, bool forceStore = false)
    {
        var jsonClass = base.GetJSON(includePhysical, includeAppearance, forceStore);

        needsStore = true;

        AppendJson(jsonClass);

        return jsonClass;
    }

    private void AppendJson(JSONClass jsonClass)
    {
        var editablesJsonClass = new JSONClass();
        foreach (var editablePair in _editables)
        {
            if (editablePair.Value.IsDuplicate) continue;
            editablePair.Value.AppendJson(editablesJsonClass);
        }
        jsonClass.Add("editables", editablesJsonClass);
    }

    private float ExponentialScale(float inputValue, float midValue, float maxValue)
    {
        var m = maxValue / midValue;
        var c = Mathf.Log(Mathf.Pow(m - 1, 2));
        var b = maxValue / (Mathf.Exp(c) - 1);
        var a = -1 * b;
        return a + b * Mathf.Exp(c * inputValue);
    }

    private void FixedUpdate()
    {
        foreach (var colliderPair in _colliders)
        {
            colliderPair.Value.UpdateControls();
            colliderPair.Value.UpdatePreview();
        }
    }

    public override void RestoreFromJSON(JSONClass jc, bool restorePhysical = true, bool restoreAppearance = true, JSONArray presetAtoms = null, bool setMissingToDefault = true)
    {
        base.RestoreFromJSON(jc, restorePhysical, restoreAppearance, presetAtoms, setMissingToDefault);

        try
        {
            LoadFromJson(jc);
        }
        catch (Exception exc)
        {
            LogError(nameof(RestoreFromJSON), exc.ToString());
        }
    }
}
