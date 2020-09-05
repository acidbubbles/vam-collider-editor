using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using MVR.FileManagementSecure;
using System.Text.RegularExpressions;


// from https://stackoverflow.com/questions/248603
public class NaturalStringComparer : IComparer<string>
{
    private static readonly Regex _re = new Regex(@"(?<=\D)(?=\d)|(?<=\d)(?=\D)", RegexOptions.Compiled);

    public int Compare(string x, string y)
    {
        x = x.ToLower();
        y = y.ToLower();
        if(string.Compare(x, 0, y, 0, Math.Min(x.Length, y.Length)) == 0)
        {
            if(x.Length == y.Length) return 0;
            return x.Length < y.Length ? -1 : 1;
        }
        var a = _re.Split(x);
        var b = _re.Split(y);
        int i = 0;
        while(true)
        {
            int r = PartCompare(a[i], b[i]);
            if(r != 0) return r;
            ++i;
        }
    }

    private static int PartCompare(string x, string y)
    {
        int a, b;
        if(int.TryParse(x, out a) && int.TryParse(y, out b))
            return a.CompareTo(b);
        return x.CompareTo(y);
    }
}


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
#if (!VAM_GT_1_20)
    private const string _searchDefault = "Search...";
#endif
    private const string _collidersSavePath = "Saves\\colliders";

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
            if (!insideRestore) showPreviews.val = true;
            var alpha = value.ExponentialScale(ColliderPreviewConfig.ExponentialScaleMiddle, 1f);
            _config.PreviewsOpacity = alpha;
            foreach (var editable in _editables.All)
                editable.UpdatePreviewFromConfig();
        }, 0f, 1f);
        RegisterFloat(previewOpacity);
        CreateSlider(previewOpacity).label = "Preview Opacity";

        JSONStorableFloat selectedPreviewOpacity = new JSONStorableFloat("selectedPreviewOpacity", ColliderPreviewConfig.DefaultSelectedPreviewOpacity, value =>
        {
            if (!insideRestore) showPreviews.val = true;
            var alpha = value.ExponentialScale(ColliderPreviewConfig.ExponentialScaleMiddle, 1f);
            _config.SelectedPreviewsOpacity = alpha;
            if (_selected != null)
                _selected.UpdatePreviewFromConfig();
        }, 0f, 1f);
        RegisterFloat(selectedPreviewOpacity);
        CreateSlider(selectedPreviewOpacity).label = "Selected Preview Opacity";

        var loadPresetUI = CreateButton("Load Preset");
        loadPresetUI.button.onClick.AddListener(() =>
        {
            FileManagerSecure.CreateDirectory(_collidersSavePath);
            var shortcuts = FileManagerSecure.GetShortCutsForDirectory(_collidersSavePath);
            SuperController.singleton.GetMediaPathDialog(HandleLoadPreset, _saveExt, _collidersSavePath, false, true, false, null, false, shortcuts);
        });

        var savePresetUI = CreateButton("Save Preset");
        savePresetUI.button.onClick.AddListener(() =>
        {
            FileManagerSecure.CreateDirectory(_collidersSavePath);
            var fileBrowserUI = SuperController.singleton.fileBrowserUI;
            fileBrowserUI.SetTitle("Save colliders preset");
            fileBrowserUI.fileRemovePrefix = null;
            fileBrowserUI.hideExtension = false;
            fileBrowserUI.keepOpen = false;
            fileBrowserUI.fileFormat = _saveExt;
            fileBrowserUI.defaultPath = _collidersSavePath;
            fileBrowserUI.showDirs = true;
            fileBrowserUI.shortCuts = null;
            fileBrowserUI.browseVarFilesAsDirectories = false;
            fileBrowserUI.SetTextEntry(true);
            fileBrowserUI.Show(HandleSavePreset);
            fileBrowserUI.fileEntryField.text = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds + "." + _saveExt;
            fileBrowserUI.ActivateFileNameField();
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
        var groupsList = CreatePopupAuto(_groupsJson, false);
        groupsList.popupPanelHeight = 400f;
        _popups.Add(groupsList);

        var types = new List<string> { _noSelectionLabel };
        types.AddRange(_editables.All.Select(e => e.Type).Distinct());
        types.Add(_allLabel);
        _typesJson = new JSONStorableStringChooser("Type", types, types[0], "Type");
        _typesJson.setCallbackFunction = _ => UpdateFilter();
        var typesList = CreatePopupAuto(_typesJson, false);
        typesList.popupPanelHeight = 400f;
        _popups.Add(typesList);

        _modifiedOnlyJson = new JSONStorableBool("Modified Only", false);
        _modifiedOnlyJson.setCallbackFunction = _ => UpdateFilter();
        CreateToggle(_modifiedOnlyJson, false);

#if (!VAM_GT_1_20)
        _textFilterJson = new JSONStorableString("Search", _searchDefault);
        _textFilterJson.setCallbackFunction = _ => UpdateFilter();
        CreateTextInput(_textFilterJson, false);
#endif

        _editablesJson = new JSONStorableStringChooser(
            "Edit",
            new List<string>(),
            new List<string>(),
            "",
            "Edit");
        var editablesList = CreatePopupAuto(_editablesJson, true);
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

    public UIDynamicPopup CreatePopupAuto(JSONStorableStringChooser jssc, bool rightSide = false)
    {
#if (VAM_GT_1_20)
        var popup = CreateFilterablePopup(jssc, rightSide);

        popup.popup.labelText.alignment = TextAnchor.UpperCenter;
        popup.popup.labelText.GetComponent<RectTransform>().anchorMax = new Vector2(0, 0.89f);

        {
            var btn = Instantiate(manager.configurableButtonPrefab);
            btn.SetParent(popup.transform, false);
            Destroy(btn.GetComponent<LayoutElement>());
            btn.GetComponent<UIDynamicButton>().label = "<";
            btn.GetComponent<UIDynamicButton>().button.onClick.AddListener(() =>
            {
                popup.popup.SetPreviousValue();
            });
            var prevBtnRect = btn.GetComponent<RectTransform>();
            prevBtnRect.pivot = new Vector2(0, 0);
            prevBtnRect.anchoredPosition = new Vector2(10f, 0);
            prevBtnRect.sizeDelta = new Vector2(0f, 0f);
            prevBtnRect.offsetMin = new Vector2(5f, 5f);
            prevBtnRect.offsetMax = new Vector2(80f, 70f);
            prevBtnRect.anchorMin = new Vector2(0f, 0f);
            prevBtnRect.anchorMax = new Vector2(0f, 0f);
        }

        {
            var btn = Instantiate(manager.configurableButtonPrefab);
            btn.SetParent(popup.transform, false);
            Destroy(btn.GetComponent<LayoutElement>());
            btn.GetComponent<UIDynamicButton>().label = ">";
            btn.GetComponent<UIDynamicButton>().button.onClick.AddListener(() =>
            {
                popup.popup.SetNextValue();
            });
            var prevBtnRect = btn.GetComponent<RectTransform>();
            prevBtnRect.pivot = new Vector2(0, 0);
            prevBtnRect.anchoredPosition = new Vector2(10f, 0);
            prevBtnRect.sizeDelta = new Vector2(0f, 0f);
            prevBtnRect.offsetMin = new Vector2(82f, 5f);
            prevBtnRect.offsetMax = new Vector2(157f, 70f);
            prevBtnRect.anchorMin = new Vector2(0f, 0f);
            prevBtnRect.anchorMax = new Vector2(0f, 0f);
        }

        return popup;
#else
        return CreateScrollablePopup(jssc, rightSide);
#endif
    }

    public void SelectEditable(IModel val)
    {
        if (_selected != null) _selected.Selected = false;
        if (val != null)
        {
            _selected = val;
            val.Selected = true;
            _editablesJson.valNoCallback = val.Id;
        }
        else
        {
            _editablesJson.valNoCallback = "";
        }
    }

    private void UpdateFilter()
    {
        try
        {
            HideCurrentFilteredEditables();

            IEnumerable<IModel> filtered = _editables.All;
#if (!VAM_GT_1_20)
            var hasSearchQuery = !string.IsNullOrEmpty(_textFilterJson.val) && _textFilterJson.val != _searchDefault;

            if (!hasSearchQuery && _groupsJson.val == _noSelectionLabel && _typesJson.val == _noSelectionLabel && !_modifiedOnlyJson.val)
            {
                _editablesJson.choices = new List<string>();
                _editablesJson.displayChoices = new List<string>();
                _editablesJson.val = "";
                return;
            }
#endif

            if (_groupsJson.val != _allLabel && _groupsJson.val != _noSelectionLabel)
                filtered = filtered.Where(e => e.Group?.Name == _groupsJson.val);

            if (_typesJson.val != _allLabel && _typesJson.val != _noSelectionLabel)
                filtered = filtered.Where(e => e.Type == _typesJson.val);

            if (_modifiedOnlyJson.val)
                filtered = filtered.Where(e => e.Modified);

#if (!VAM_GT_1_20)
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
#endif

            var result = filtered.ToList().OrderBy(x => x.Label, new NaturalStringComparer());

            _editablesJson.choices = result.Select(x => x.Id).ToList();
            _editablesJson.displayChoices = result.Select(x => x.Label).ToList();
            if (!_editablesJson.choices.Contains(_editablesJson.val) || string.IsNullOrEmpty(_editablesJson.val))
                _editablesJson.val = _editablesJson.choices.FirstOrDefault() ?? "";

            foreach (var e in result)
            {
                e.Shown = true;
                e.UpdatePreviewFromConfig();
            }
        }
        catch (Exception e)
        {
            LogError(nameof(UpdateFilter), e.ToString());
        }
    }

    private void HideCurrentFilteredEditables()
    {
        var previous = _editablesJson.choices.Where(x => _editables.ByUuid.ContainsKey(x)).Select(x => _editables.ByUuid[x]);
        foreach (var e in previous)
        {
            e.Shown = false;
            e.UpdatePreviewFromConfig();
        }
    }

    #region Presets

    private void HandleLoadPreset(string path)
    {
        if (string.IsNullOrEmpty(path))
            return;

        LoadFromJson((JSONClass)LoadJSON(path));
    }

    private void HandleSavePreset(string path)
    {
        SuperController.singleton.fileBrowserUI.fileFormat = null;
        if (string.IsNullOrEmpty(path)) return;
        if (!path.ToLower().EndsWith($".{_saveExt}")) path += $".{_saveExt}";

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
        try
        {
            var editablesJsonClass = jsonClass["editables"].AsObject;
            var errorsCounter = 0;
            var maxErrors = 100;
            foreach (var editableId in editablesJsonClass.Keys)
            {
                var migratedEditableId = MigrationHelper.Migrate(editableId);

                IModel editableModel;
                if (_editables.ByUuid.TryGetValue(migratedEditableId, out editableModel))
                {
                    editableModel.LoadJson(editablesJsonClass[editableId].AsObject);
                }
                else
                {
                    if (++errorsCounter < maxErrors)
                    {
                        if (migratedEditableId != editableId)
                            SuperController.LogError($"{nameof(ColliderEditor)}: Did not find '{migratedEditableId}' (originally '{editableId}') defined in save file.");
                        else
                            SuperController.LogError($"{nameof(ColliderEditor)}: Did not find '{editableId}' defined in save file.");
                    }
                }
            }
            if (errorsCounter >= maxErrors)
                SuperController.LogError($"{nameof(ColliderEditor)}: ... {errorsCounter} total missing items found.");
        }
        catch (Exception e)
        {
            LogError(nameof(LoadFromJson), e.ToString());
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
        if (_editables == null) return;
        try
        {
            if (Time.time > _nextUpdate)
            {
                foreach (var editable in _editables.All)
                {
                    if(editable.SyncOverrides())
                        editable.SyncPreview();
                }

                _nextUpdate = Time.time + 1f;
            }
        }
        catch (Exception e)
        {
            LogError(nameof(Update), $"{containingAtom.name}'s Collider Editor will be disabled.\r\n" + e.ToString());
            enabled = false;
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
