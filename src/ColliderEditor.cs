using UnityEngine;
using UnityEngine.UI;
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
    private const string _noSelectionLabel = "Select...";
    private const string _allLabel = "All";
    private const string _searchDefault = "Search...";
    private string _lastBrowseDir = SuperController.singleton.savesDir;

    private JSONStorableStringChooser _groupsJson;
    private JSONStorableStringChooser _typesJson;
    private JSONStorableBool _modifiedOnlyJson;
    private JSONStorableString _textFilterJson;
    private JSONStorableStringChooser _editablesJson;
    private readonly List<UIDynamicPopup> _popups = new List<UIDynamicPopup>();

    private readonly ColliderPreviewConfig _config = new ColliderPreviewConfig();
    private IModel _selected;
    private EditablesList _editables;
    private JSONClass _jsonWhenDisabled;

    public override void Init()
    {
        try
        {
            _editables = EditablesList.Build(this, _config);
            BuildUI();
        }
        catch (Exception e)
        {
            SuperController.LogError($"{nameof(ColliderEditor)}.{nameof(Init)}: {e}");
        }
    }

    private void BuildUI()
    {
        var showPreviews = new JSONStorableBool("showPreviews", ColliderPreviewConfig.DefaultPreviewsEnabled, value =>
        {
            _config.PreviewsEnabled = value;
            foreach (var editable in _editables.All)
                editable.UpdatePreviewFromConfig();
        });
        RegisterBool(showPreviews);
        var showPreviewsToggle = CreateToggle(showPreviews);
        showPreviewsToggle.label = "Show Previews";

        var xRayPreviews = new JSONStorableBool("xRayPreviews", ColliderPreviewConfig.DefaultXRayPreviews, value =>
        {
            _config.XRayPreviews = value;
            foreach (var editable in _editables.All)
                editable.UpdatePreviewFromConfig();
        });
        RegisterBool(xRayPreviews);
        var xRayPreviewsToggle = CreateToggle(xRayPreviews);
        xRayPreviewsToggle.label = "Use XRay Previews";

        JSONStorableFloat previewOpacity = new JSONStorableFloat("previewOpacity", ColliderPreviewConfig.DefaultPreviewsOpacity, value =>
        {
            if (!showPreviews.val) showPreviews.val = true;
            var alpha = value.ExponentialScale(0.1f, 1f);
            _config.PreviewsOpacity = alpha;
            foreach (var editable in _editables.All)
                editable.UpdatePreviewFromConfig();
        }, 0f, 1f);
        RegisterFloat(previewOpacity);
        CreateSlider(previewOpacity).label = "Preview Opacity";

        JSONStorableFloat selectedPreviewOpacity = new JSONStorableFloat("selectedPreviewOpacity", ColliderPreviewConfig.DefaultSelectedPreviewOpacity, value =>
        {
            if (!showPreviews.val) showPreviews.val = true;
            var alpha = value.ExponentialScale(0.1f, 1f);
            _config.SelectedPreviewsOpacity = alpha;
            if (_selected != null)
                _selected.UpdatePreviewFromConfig();
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
            foreach (var editable in _editables.All)
                editable.ResetToInitial();
        });

        var groups = new List<string> { _noSelectionLabel };
        groups.AddRange(_editables.Groups.Select(e => e.Name).Distinct());
        groups.Add(_allLabel);
        _groupsJson = new JSONStorableStringChooser("Group", groups, groups[0], "Group");
        _groupsJson.setCallbackFunction = _ => UpdateFilter();
        var groupsList = CreateScrollablePopup(_groupsJson, false);
        groupsList.popupPanelHeight = 400f;
        _popups.Add(groupsList);

        var types = new List<string> { _noSelectionLabel };
        types.AddRange(_editables.All.Select(e => e.Type).Distinct());
        types.Add(_allLabel);
        _typesJson = new JSONStorableStringChooser("Type", types, types[0], "Type");
        _typesJson.setCallbackFunction = _ => UpdateFilter();
        var typesList = CreateScrollablePopup(_typesJson, false);
        typesList.popupPanelHeight = 400f;
        _popups.Add(typesList);

        _modifiedOnlyJson = new JSONStorableBool("Modified Only", false);
        _modifiedOnlyJson.setCallbackFunction = _ => UpdateFilter();
        CreateToggle(_modifiedOnlyJson, false);

        _textFilterJson = new JSONStorableString("Search", _searchDefault);
        _textFilterJson.setCallbackFunction = _ => UpdateFilter();
        CreateTextInput(_textFilterJson, false);

        _editablesJson = new JSONStorableStringChooser(
            "Edit",
            new List<string>(),
            new List<string>(),
            "",
            "Edit");
        var editablesList = CreateScrollablePopup(_editablesJson, true);
        editablesList.popupPanelHeight = 1000f;
        _popups.Add(editablesList);
        _editablesJson.setCallbackFunction = id =>
        {
            IModel val;
            if (_editables.ByUuid.TryGetValue(id, out val))
                SelectEditable(val);
            else
                SelectEditable(null);
        };

        UpdateFilter();
    }

    public void SelectEditable(IModel val)
    {
        if (_selected != null) _selected.Selected = false;
        if (val != null)
        {
            _selected = val;
            val.Selected = true;
            _editablesJson.valNoCallback = val.Label;
        }
        else
        {
            _editablesJson.valNoCallback = "";
        }
        SyncPopups();
    }

    private void SyncPopups()
    {
        foreach (var popup in _popups)
        {
            popup.popup.Toggle();
            popup.popup.Toggle();
        }
    }

    private void UpdateFilter()
    {
        try
        {
            IEnumerable<IModel> filtered = _editables.All;
            var hasSearchQuery = !string.IsNullOrEmpty(_textFilterJson.val) && _textFilterJson.val != _searchDefault;

            if (!hasSearchQuery && _groupsJson.val == _noSelectionLabel && _typesJson.val == _noSelectionLabel && !_modifiedOnlyJson.val)
            {
                _editablesJson.choices = new List<string>();
                _editablesJson.displayChoices = new List<string>();
                _editablesJson.val = "";
                SyncPopups();
                return;
            }

            if (_groupsJson.val != _allLabel && _groupsJson.val != _noSelectionLabel)
                filtered = filtered.Where(e => e.Group?.Name == _groupsJson.val);

            if (_typesJson.val != _allLabel && _typesJson.val != _noSelectionLabel)
                filtered = filtered.Where(e => e.Type == _typesJson.val);

            if (_modifiedOnlyJson.val)
                filtered = filtered.Where(e => e.Modified);

            if (hasSearchQuery)
            {
                var tokens = _textFilterJson.val.Split(' ').Select(t => t.Trim());
                foreach (var token in tokens)
                {
                    filtered = filtered.Where(e =>
                        e.Type.IndexOf(token, StringComparison.InvariantCultureIgnoreCase) > -1 ||
                        e.Label.IndexOf(token, StringComparison.InvariantCultureIgnoreCase) > -1
                    );
                }
            }

            var result = filtered.ToList();

            _editablesJson.choices = filtered.Select(x => x.Id).ToList();
            _editablesJson.displayChoices = filtered.Select(x => x.Label).ToList();
            if (!_editablesJson.choices.Contains(_editablesJson.val) || string.IsNullOrEmpty(_editablesJson.val))
                _editablesJson.val = _editablesJson.choices.FirstOrDefault() ?? "";

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
            if (_editables.ByUuid.TryGetValue(editableId, out editableModel))
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
        foreach (var editable in _editables.All)
        {
            editable.AppendJson(editablesJsonClass);
        }
        jsonClass.Add("editables", editablesJsonClass);
    }

    #endregion

    #region Unity events

    public void OnEnable()
    {
        if (_editables?.All == null) return;
        try
        {
            if (_jsonWhenDisabled != null)
            {
                LoadFromJson(_jsonWhenDisabled);
                _jsonWhenDisabled = null;
            }
        }
        catch (Exception e)
        {
            LogError(nameof(OnEnable), e.ToString());
        }
    }

    public void OnDisable()
    {
        if (_editables?.All == null) return;
        try
        {
            _jsonWhenDisabled = new JSONClass();
            AppendJson(_jsonWhenDisabled);
            foreach (var editable in _editables.All)
            {
                editable.DestroyPreview();
                editable.ResetToInitial();
            }
        }
        catch (Exception e)
        {
            LogError(nameof(OnDisable), e.ToString());
        }
    }

    public void OnDestroy()
    {
        if (_editables?.All == null) return;
        try
        {
            _jsonWhenDisabled = null;
        }
        catch (Exception e)
        {
            LogError(nameof(OnDisable), e.ToString());
        }
    }

    private float _nextUpdate = Time.time;
    private void Update()
    {
        if (_config.PreviewsEnabled && Time.time > _nextUpdate)
        {
            foreach (var colliderPair in _editables.Colliders)
            {
                colliderPair.Value.UpdatePreviewFromCollider();
            }

            _nextUpdate = Time.time + 1f;
        }
    }

    #endregion

    private void LogError(string method, string message) => SuperController.LogError($"{nameof(ColliderEditor)}.{method}: {message}");

    public UIDynamicTextField CreateTextInput(JSONStorableString jss, bool rightSide = false)
    {
        var textfield = CreateTextField(jss, rightSide);
        textfield.height = 20f;
        textfield.backgroundColor = Color.white;
        var input = textfield.gameObject.AddComponent<InputField>();
        var rect = input.GetComponent<RectTransform>().sizeDelta = new Vector2(1f, 0.4f);
        input.textComponent = textfield.UItext;
        jss.inputField = input;
        return textfield;
    }
}
