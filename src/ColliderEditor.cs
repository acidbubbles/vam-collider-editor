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

    private Dictionary<string, RigidbodyGroupModel> _rigidbodyGroups;
    private Dictionary<string, ColliderModel> _colliders;
    private Dictionary<string, AutoColliderModel> _autoColliders;
    private Dictionary<string, RigidbodyModel> _rigidbodies;

    private JSONStorableStringChooser _rigidbodyGroupsJson;
    private JSONStorableStringChooser _targetJson;
    private JSONStorableStringChooser _autoColliderJson;
    private JSONStorableStringChooser _rigidbodiesJson;

    private ColliderModel _lastSelectedCollider;
    private RigidbodyModel _lastSelectedRigidbody;
    private AutoColliderModel _lastSelectedAutoCollider;

    private ColliderModel _selectedCollider;
    private RigidbodyGroupModel _selectedGroup;
    private RigidbodyModel _selectedRigidbody;
    private AutoColliderModel _selectedAutoCollider;

    private UIDynamicPopup _rbGroupListUI;
    private UIDynamicPopup _ridigBodyList;
    private UIDynamicPopup _rbListUI;
    private UIDynamicPopup _autoColliderListUI;

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

        _rigidbodyGroupsJson = new JSONStorableStringChooser(
            "Rigidbody Groups",
            _rigidbodyGroups.Keys.ToList(),
            _rigidbodyGroups.Select(x => x.Value.Name).ToList(),
            _rigidbodyGroups.Keys.First(),
            "Rigidbody Groups");

        _rbGroupListUI = CreateScrollablePopup(_rigidbodyGroupsJson);
        _rbGroupListUI.popupPanelHeight = 400f;

        _rigidbodiesJson = new JSONStorableStringChooser(
            "Rigidbodies",
            new List<string>(),
            new List<string>(),
            "All",
            "Rigidbodies");

        _ridigBodyList = CreateScrollablePopup(_rigidbodiesJson);
        _ridigBodyList.popupPanelHeight = 400f;

        _targetJson = new JSONStorableStringChooser(
            "Colliders",
            new List<string>(),
            new List<string>(),
            "",
            "Colliders");

        _rbListUI = CreateScrollablePopup(_targetJson, true);
        _rbListUI.popupPanelHeight = 400f;

        var autoColliderPairs = _autoColliders.OrderBy(kvp => kvp.Key).ToList();
        _autoColliderJson = new JSONStorableStringChooser(
            "Auto Colliders",
            autoColliderPairs.Select(kvp => kvp.Key).ToList(),
            autoColliderPairs.Select(kvp => kvp.Value.Label).ToList(),
            "", "Auto Colliders"
        );
        _autoColliderListUI = CreateScrollablePopup(_autoColliderJson);
        _autoColliderListUI.popupPanelHeight = 400f;

        _rigidbodyGroupsJson.setCallbackFunction = groupId =>
        {
            _rigidbodyGroups.TryGetValue(groupId, out _selectedGroup);
            UpdateFilter();
        };

        _rigidbodiesJson.setCallbackFunction = rigidbodyId =>
        {
            _selectedRigidbody?.DestroyControls();
            _rigidbodies.TryGetValue(rigidbodyId, out _selectedRigidbody);
            UpdateFilter();
        };

        _targetJson.setCallbackFunction = colliderId =>
        {
            _colliders.TryGetValue(colliderId, out _selectedCollider);
            UpdateFilter();
        };

        _autoColliderJson.setCallbackFunction = autoColliderId =>
        {
            _autoColliders.TryGetValue(autoColliderId, out _selectedAutoCollider);
            UpdateFilter();
        };

        _rigidbodyGroups.TryGetValue("Head / Ears", out _selectedGroup);

        UpdateFilter();
    }

    private void SyncPopups()
    {
        _rigidbodyGroupsJson.popup.Toggle();
        _rigidbodyGroupsJson.popup.Toggle();

        _rbListUI.popup.Toggle();
        _rbListUI.popup.Toggle();

        _ridigBodyList.popup.Toggle();
        _ridigBodyList.popup.Toggle();
    }

    private void BuildModels()
    {
        var rigidbodyGroups = containingAtom.type == "Person"
        ? new List<RigidbodyGroupModel>
        {
            new RigidbodyGroupModel("All", @"^.+$"),
            new RigidbodyGroupModel("Head / Ears", @"^(head|lowerJaw|tongue|neck)"),
            new RigidbodyGroupModel("Left arm", @"^l(Shldr|ForeArm)"),
            new RigidbodyGroupModel("Left hand", @"^l(Index|Mid|Ring|Pinky|Thumb|Carpal|Hand)[0-9]?$"),
            new RigidbodyGroupModel("Right arm", @"^r(Shldr|ForeArm)"),
            new RigidbodyGroupModel("Right hand", @"^r(Index|Mid|Ring|Pinky|Thumb|Carpal|Hand)[0-9]?$"),
            new RigidbodyGroupModel("Chest", @"^(chest|AutoColliderFemaleAutoColliderschest)"),
            new RigidbodyGroupModel("Left breast", @"l((Pectoral)|Nipple)"),
            new RigidbodyGroupModel("Right breast", @"r((Pectoral)|Nipple)"),
            new RigidbodyGroupModel("Abdomen / Belly / Back", @"^(AutoColliderFemaleAutoColliders)?abdomen"),
            new RigidbodyGroupModel("Hip / Pelvis", @"^(AutoCollider)?(hip|pelvis)"),
            new RigidbodyGroupModel("Glute", @"^(AutoColliderFemaleAutoColliders)?[LR]Glute"),
            new RigidbodyGroupModel("Anus", @"^_JointA[rl]"),
            new RigidbodyGroupModel("Vagina", @"^_Joint(Gr|Gl|B)"),
            new RigidbodyGroupModel("Penis", @"^(Gen[1-3])|Testes"),
            new RigidbodyGroupModel("Left leg", @"^(AutoCollider(FemaleAutoColliders)?)?l(Thigh|Shin)"),
            new RigidbodyGroupModel("Left foot", @"^l(Foot|Toe|BigToe|SmallToe)"),
            new RigidbodyGroupModel("Right leg", @"^(AutoCollider(FemaleAutoColliders)?)?r(Thigh|Shin)"),
            new RigidbodyGroupModel("Right foot", @"^r(Foot|Toe|BigToe|SmallToe)"),
            new RigidbodyGroupModel("Other", @"^(?!.*).*$")
        }
        : new List<RigidbodyGroupModel>
        {
            new RigidbodyGroupModel("All", @"^.+$"),
        };

        // AutoColliders

        _autoColliders = containingAtom.GetComponentsInChildren<AutoCollider>()
            .Select(autoCollider => AutoColliderModel.Create(this, autoCollider))
            .ToDictionary(autoColliderModel => autoColliderModel.Id);

        var autoCollidersRigidBodies = new HashSet<Rigidbody>(_autoColliders.Values.SelectMany(x => x.GetRigidbodies()));
        var autoCollidersColliders = new HashSet<Collider>(_autoColliders.Values.SelectMany(x => x.GetColliders()));

        // Rigidbodies

        _rigidbodyGroups = rigidbodyGroups.ToDictionary(x => x.Id);

        _rigidbodies = containingAtom.GetComponentsInChildren<Rigidbody>(true)
            .Where(rigidbody => !autoCollidersRigidBodies.Contains(rigidbody))
            .Where(rigidbody => IsRigidbodyIncluded(rigidbody))
            .Select(rigidbody => RigidbodyModel.Create(this, rigidbody, rigidbodyGroups))
            .ToDictionary(x => x.Id);

        // Colliders

        var colliderQuery = containingAtom.GetComponentsInChildren<Collider>(true)
            .Where(collider => !autoCollidersColliders.Contains(collider))
            .Where(collider => IsColliderIncluded(collider));


        _colliders = new Dictionary<string, ColliderModel>();

        foreach (Collider collider in colliderQuery)
        {
            var model = ColliderModel.CreateTyped(this, collider, _rigidbodies);

            if (_colliders.ContainsKey(model.Id))
            {
                SuperController.LogError($"Duplicate collider Id {model.Id}");
                continue;
            }

            _colliders.Add(model.Id, model);
        }
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
            IEnumerable<RigidbodyModel> rigidbodies;
            IEnumerable<ColliderModel> colliders;

            // Rigidbody filtering

            if (_selectedGroup != null)
            {
                rigidbodies = _rigidbodies.Values.Where(x => x.Groups.Contains(_selectedGroup));
                colliders = _colliders.Values.Where(collider => collider.Rididbody != null && collider.Rididbody.Groups.Contains(_selectedGroup));
            }
            else
            {
                rigidbodies = _rigidbodies.Values;
                colliders = _colliders.Values;
            }

            if (_selectedGroup != null && _rigidbodyGroupsJson.choices.Contains(_selectedGroup.Id))
            {
                _rigidbodyGroupsJson.valNoCallback = _selectedGroup.Id;
            }
            else
            {
                _rigidbodyGroupsJson.valNoCallback = "All";
                _selectedGroup = null;
            }

            _rigidbodiesJson.choices = new[] { "All" }.Concat(rigidbodies.Select(x => x.Id)).ToList();
            _rigidbodiesJson.displayChoices = new[] { "All" }.Concat(rigidbodies.Select(x => x.Label)).ToList();


            if (_selectedRigidbody != null && _rigidbodiesJson.choices.Contains(_selectedRigidbody.Id))
            {
                _rigidbodiesJson.valNoCallback = _selectedRigidbody.Id;
            }
            else
            {
                _rigidbodiesJson.valNoCallback = "All";
                _selectedRigidbody = null;
            }

            // Collider filtering

            if (_selectedRigidbody != null) colliders = _colliders.Values.Where(collider => collider.Rididbody != null && collider.Rididbody == _selectedRigidbody);

            _targetJson.choices = colliders.Select(x => x.Id).ToList();
            _targetJson.displayChoices = colliders.Select(x => x.Label).ToList();

            if (_selectedCollider != null && _targetJson.choices.Contains(_selectedCollider.Id))
            {
                _targetJson.valNoCallback = _selectedCollider.Id;
            }
            else
            {
                var firstAvailableId = _targetJson.choices.FirstOrDefault();
                _targetJson.valNoCallback = firstAvailableId ?? string.Empty;
                if (!string.IsNullOrEmpty(firstAvailableId))
                    _colliders.TryGetValue(firstAvailableId, out _selectedCollider);
                else
                    _selectedCollider = null;
            }

            UpdateSelectedRigidbody();
            UpdateSelectedCollider();
            UpdateSelectedAutoCollider();
            SyncPopups();

        }
        catch (Exception e)
        {
            LogError(nameof(UpdateFilter), e.ToString());
        }
    }

    private void UpdateSelectedCollider()
    {
        if (_lastSelectedCollider != null)
            _lastSelectedCollider.Selected = false;

        if (_selectedCollider != null)
        {
            _selectedCollider.Selected = true;
            _lastSelectedCollider = _selectedCollider;
        }
    }

    private void UpdateSelectedRigidbody()
    {
        if (_lastSelectedRigidbody != null)
            _lastSelectedRigidbody.Selected = false;

        if (_selectedRigidbody != null)
        {
            _selectedRigidbody.Selected = true;
            _lastSelectedRigidbody = _selectedRigidbody;
        }
    }

    private void UpdateSelectedAutoCollider()
    {
        if (_lastSelectedAutoCollider != null)
            _lastSelectedAutoCollider.Selected = false;

        if (_selectedAutoCollider != null)
        {
            _selectedAutoCollider.Selected = true;
            _lastSelectedAutoCollider = _selectedAutoCollider;
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
        var collidersJsonClass = jsonClass["colliders"].AsObject;
        foreach (string colliderId in collidersJsonClass.Keys)
        {
            ColliderModel colliderModel;
            if (_colliders.TryGetValue(colliderId, out colliderModel))
                colliderModel.LoadJson(collidersJsonClass[colliderId].AsObject);
        }

        var rigidbodiesJsonClass = jsonClass["rigidbodies"].AsObject;
        foreach (string rigidbodyId in rigidbodiesJsonClass.Keys)
        {
            RigidbodyModel rigidbodyModel;
            if (_rigidbodies.TryGetValue(rigidbodyId, out rigidbodyModel))
                rigidbodyModel.LoadJson(rigidbodiesJsonClass[rigidbodyId].AsObject);
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
        var colliders = new JSONClass();
        foreach (var colliderPairs in _colliders)
            colliderPairs.Value.AppendJson(colliders);
        jsonClass.Add("colliders", colliders);

        var rigidbodies = new JSONClass();
        foreach (var rigidbodyPair in _rigidbodies)
            rigidbodyPair.Value.AppendJson(rigidbodies);
        jsonClass.Add("rigidbodies", rigidbodies);
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
