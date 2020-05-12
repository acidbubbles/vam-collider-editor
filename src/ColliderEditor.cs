using System;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;

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
    private UIDynamicPopup _editablesList;

    private IModel _selected;
    private EditablesList _editables;

    public override void Init()
    {
        try
        {
            _editables = EditablesList.Build(this);
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
            foreach (var editablePair in _editables.All)
                editablePair.Value.SetShowPreview(value);
        });
        RegisterBool(showPreviews);

        var showPreviewsToggle = CreateToggle(showPreviews);
        showPreviewsToggle.label = "Show Previews";

        var xRayPreviews = new JSONStorableBool("xRayPreviews", true, value =>
        {
            foreach (var editablePair in _editables.All)
                editablePair.Value.SetXRayPreview(value);
        });
        RegisterBool(xRayPreviews);

        var xRayPreviewsToggle = CreateToggle(xRayPreviews);
        xRayPreviewsToggle.label = "Use XRay Previews";

        JSONStorableFloat previewOpacity = new JSONStorableFloat("previewOpacity", 0.001f, value =>
        {
            var alpha = value.ExponentialScale(0.1f, 1f);
            foreach (var editablePair in _editables.All)
                editablePair.Value.SetPreviewOpacity(alpha);
        }, 0f, 1f);
        RegisterFloat(previewOpacity);
        CreateSlider(previewOpacity).label = "Preview Opacity";

        JSONStorableFloat selectedPreviewOpacity = new JSONStorableFloat("selectedPreviewOpacity", 0.3f, value =>
        {
            var alpha = value.ExponentialScale(0.1f, 1f);
            foreach (var editablePair in _editables.All)
                editablePair.Value.SetSelectedPreviewOpacity(alpha);
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
            foreach (var colliderPair in _editables.Colliders)
                colliderPair.Value.ResetToInitial();
        });

        _editablesJson = new JSONStorableStringChooser(
            "Edit",
            new List<string>(),
            new List<string>(),
            "",
            "Edit");

        _editablesList = CreateScrollablePopup(_editablesJson, true);
        _editablesList.popupPanelHeight = 1200f;

        _editablesJson.setCallbackFunction = id =>
        {
            if (_selected != null) _selected.Selected = false;
            _editables.All.TryGetValue(id, out _selected);
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

    private void UpdateFilter()
    {
        try
        {
            var editables = _editables.All.Values.OrderBy(e => e.Label).ToList();
            _editablesJson.choices = editables.Select(x => x.Id).ToList();
            _editablesJson.displayChoices = editables.Select(x => x.Label).ToList();

            SyncPopups();
        }
        catch (Exception e)
        {
            LogError(nameof(UpdateFilter), e.ToString());
        }
    }

    #region Presets

    private void HandleLoadPreset(string path)
    {
        if (string.IsNullOrEmpty(path))
            return;
        _lastBrowseDir = path.Substring(0, path.LastIndexOfAny(new[] { '/', '\\' })) + @"\";

        LoadFromJson((JSONClass)LoadJSON(path));
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

    #endregion

    #region Load / Save JSON

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

    private void LoadFromJson(JSONClass jsonClass)
    {
        var editablesJsonClass = jsonClass["editables"].AsObject;
        foreach (string editableId in editablesJsonClass.Keys)
        {
            IModel editableModel;
            if (_editables.All.TryGetValue(editableId, out editableModel))
                editableModel.LoadJson(editablesJsonClass[editableId].AsObject);
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
        foreach (var editablePair in _editables.All)
        {
            if (editablePair.Value.IsDuplicate) continue;
            editablePair.Value.AppendJson(editablesJsonClass);
        }
        jsonClass.Add("editables", editablesJsonClass);
    }

    #endregion

    #region Unity events

    public void OnDestroy()
    {
        if (_editables.All == null) return;
        try
        {
            foreach (var editablePair in _editables.All)
                editablePair.Value.DestroyPreview();
        }
        catch (Exception e)
        {
            LogError(nameof(OnDestroy), e.ToString());
        }
    }


    private void FixedUpdate()
    {
        // TODO: Validate whether this is really necessary. Running code multiple times per frame should be avoided.
        foreach (var colliderPair in _editables.Colliders)
        {
            colliderPair.Value.UpdateControls();
            colliderPair.Value.UpdatePreview();
        }
    }

    #endregion

    private void LogError(string method, string message) => SuperController.LogError($"{nameof(ColliderEditor)}.{method}: {message}");
}
