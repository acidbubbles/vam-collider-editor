using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using MVR.FileManagementSecure;
using System.Collections;

/// <summary>
/// Collider Editor
/// By Acidbubbles, ProjectCanyon and via5
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
    private const string _collidersSavePath = "Saves\\PluginData\\ColliderEditor";

    private JSONStorableBool _showPreviewsJSON;
    private JSONStorableStringChooser _presetsJson;
    private JSONStorableStringChooser _groupsJson;
    private JSONStorableStringChooser _typesJson;
    private JSONStorableStringChooser _filterJson;
    private JSONStorableString _textFilterJson;
    private JSONStorableStringChooser _editablesJson;

    private UIListener _uiListener;
    private IModel _selected, _selectedMirror;
    private JSONClass _jsonWhenDisabled;
    private bool _restored;
    private bool _ready;

    public EditablesList EditablesList { get; private set; }

    public ColliderPreviewConfig Config { get; } = new ColliderPreviewConfig();

    public override void Init()
    {
        try
        {
            EditablesList = EditablesList.Build(this, Config);
            BuildUI();
            SuperController.singleton.StartCoroutine(DeferredInit());
            SuperController.singleton.BroadcastMessage("OnActionsProviderAvailable", this, SendMessageOptions.DontRequireReceiver);
        }
        catch (Exception e)
        {
            SuperController.LogError($"{nameof(ColliderEditor)}.{nameof(Init)}: {e}");
        }
    }

    public override void InitUI()
    {
        base.InitUI();
        if (UITransform == null || _uiListener != null) return;
        _uiListener = UITransform.gameObject.AddComponent<UIListener>();
        if (_uiListener == null) return;
        _uiListener.onDisabled.AddListener(OnBlur);
        _uiListener.onClick.AddListener(OnBlur);
    }

    private IEnumerator DeferredInit()
    {
        yield return new WaitForEndOfFrame();
        if (_restored)
        {
            _ready = true;
            yield break;
        }
        try
        {
            containingAtom.RestoreFromLast(this);
            _ready = true;
        }
        catch (Exception e)
        {
            SuperController.LogError($"{nameof(ColliderEditor)}.{nameof(DeferredInit)}: {e}");
        }
    }

    private void BuildUI()
    {
        _showPreviewsJSON = new JSONStorableBool("showPreviews", ColliderPreviewConfig.DefaultPreviewsEnabled, value =>
        {
            Config.PreviewsEnabled = value;
            foreach (var editable in EditablesList.All)
                editable.UpdatePreviewsFromConfig();
        })
        {
            isStorable = false,
            isRestorable = false
        };
        RegisterBool(_showPreviewsJSON);
        var showPreviewsToggle = CreateToggle(_showPreviewsJSON);
        showPreviewsToggle.label = "Show Previews";

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

        var groups = new List<string> { _noSelectionLabel };
        groups.AddRange(EditablesList.Groups.Select(e => e.Name).Distinct());
        groups.Add(_allLabel);
        _groupsJson = new JSONStorableStringChooser("Group", groups, groups[0], "Group")
        {
            setCallbackFunction = _ => UpdateFilter(),
            isStorable = false,
            isRestorable = false
        };
        var groupsPopup = CreatePopupAuto(_groupsJson, false, 400f);
        groupsPopup.popup.onOpenPopupHandlers += () => OnBlur(_groupsJson);

        var types = new List<string> { _noSelectionLabel };
        types.AddRange(EditablesList.All.Select(e => e.Type).Distinct());
        types.Add(_allLabel);
        _typesJson = new JSONStorableStringChooser("Type", types, types[0], "Type")
        {
            setCallbackFunction = _ => UpdateFilter(),
            isStorable = false,
            isRestorable = false
        };
        var typesPopup = CreatePopupAuto(_typesJson, false, 360f);
        typesPopup.popup.onOpenPopupHandlers += () => OnBlur(_typesJson);

        _filterJson = new JSONStorableStringChooser("Filter", Filters.List, Filters.None, "Filter")
        {
            setCallbackFunction = _ => UpdateFilter(),
            isStorable = false,
            isRestorable = false
        };
        var filterPopup = CreatePopupAuto(_filterJson, false, 400, true);
        filterPopup.popup.onOpenPopupHandlers += () => OnBlur(_filterJson);

        _presetsJson = new JSONStorableStringChooser("Apply", Presets.List, Presets.None, "Apply to...")
        {
            setCallbackFunction = v =>
            {
                _presetsJson.valNoCallback = Presets.None;
                Presets.Apply(v, _filteredEditables);
                var selected = _selected;
                SelectEditable(null);
                SelectEditable(selected);
            },
            isStorable = false,
            isRestorable = false
        };
        var presetPopup = CreatePopupAuto(_presetsJson, false, 500, true);
        presetPopup.popup.onOpenPopupHandlers += () => OnBlur(_presetsJson);

#if (!VAM_GT_1_20)
        _textFilterJson = new JSONStorableString("Search", _searchDefault)
        {
            setCallbackFunction = _ => UpdateFilter(),
            isStorable = false,
            isRestorable = false
        };
        CreateTextInput(_textFilterJson, false);
#endif

                var previewXRayOpacityJSON = new JSONStorableFloat("previewOpacityXRay", ColliderPreviewConfig.DefaultPreviewXRayOpacity, value =>
        {
            if (!insideRestore) _showPreviewsJSON.val = true;
            var alpha = value.ExponentialScale(ColliderPreviewConfig.ExponentialScaleMiddle, 1f);
            Config.PreviewsXRayOpacity = alpha;
            foreach (var editable in EditablesList.All)
                editable.UpdatePreviewsFromConfig();
        }, 0f, 1f);
        RegisterFloat(previewXRayOpacityJSON);
        CreateSlider(previewXRayOpacityJSON).label = "Preview Opacity (XRay)";

        var previewProtrusionsOpacityJSON = new JSONStorableFloat("previewOpacityProtrusions", ColliderPreviewConfig.DefaultPreviewProtrusionsOpacity, value =>
        {
            if (!insideRestore) _showPreviewsJSON.val = true;
            var alpha = value.ExponentialScale(ColliderPreviewConfig.ExponentialScaleMiddle, 1f);
            Config.PreviewsProtrusionsOpacity = alpha;
            foreach (var editable in EditablesList.All)
                editable.UpdatePreviewsFromConfig();
        }, 0f, 1f);
        RegisterFloat(previewProtrusionsOpacityJSON);
        CreateSlider(previewProtrusionsOpacityJSON).label = "Preview Opacity (Protrusions)";

        /*
        var previewXRayRenderQueueJSON = new JSONStorableFloat("previewRenderQueueXRay", 3001, (float value) =>
        {
            if (!insideRestore) _showPreviewsJSON.val = true;
            Config.PreviewsXRayRenderQueue = (int)value;
            foreach (var editable in EditablesList.All)
                editable.UpdatePreviewsFromConfig();
        }, 0, 5000);
        RegisterFloat(previewXRayRenderQueueJSON);
        CreateSlider(previewXRayRenderQueueJSON).label = "Preview RenderQueue (XRay)";

        var previewProtrusionsRenderQueueJSON = new JSONStorableFloat("previewRenderQueueProtrusions", 3002, (float value) =>
        {
            if (!insideRestore) _showPreviewsJSON.val = true;
            Config.PreviewsProtrusionsRenderQueue = (int)value;
            foreach (var editable in EditablesList.All)
                editable.UpdatePreviewsFromConfig();
        }, 0f, 5000);
        RegisterFloat(previewProtrusionsRenderQueueJSON);
        CreateSlider(previewProtrusionsRenderQueueJSON).label = "Preview RenderQueue (Protrusions)";
        */

        var selectedPreviewXRayOpacityJSON = new JSONStorableFloat("selectedPreviewOpacity", ColliderPreviewConfig.DefaultSelectedPreviewsXRayOpacity, value =>
        {
            if (!insideRestore) _showPreviewsJSON.val = true;
            var alpha = value.ExponentialScale(ColliderPreviewConfig.ExponentialScaleMiddle, 1f);
            Config.SelectedPreviewsXRayOpacity = alpha;
            if (_selected != null)
                _selected.UpdatePreviewsFromConfig();
            if (_selectedMirror != null)
                _selectedMirror.UpdatePreviewsFromConfig();
        }, 0f, 1f);
        RegisterFloat(selectedPreviewXRayOpacityJSON);
        CreateSlider(selectedPreviewXRayOpacityJSON).label = "Selected Preview Opacity (XRay)";

        var selectedPreviewProtrusionsOpacityJSON = new JSONStorableFloat("selectedPreviewOpacity", ColliderPreviewConfig.DefaultSelectedPreviewsProtrusionsOpacity, value =>
        {
            if (!insideRestore) _showPreviewsJSON.val = true;
            var alpha = value.ExponentialScale(ColliderPreviewConfig.ExponentialScaleMiddle, 1f);
            Config.SelectedPreviewsProtrusionsOpacity = alpha;
            if (_selected != null)
                _selected.UpdatePreviewsFromConfig();
            if (_selectedMirror != null)
                _selectedMirror.UpdatePreviewsFromConfig();
        }, 0f, 1f);
        RegisterFloat(selectedPreviewProtrusionsOpacityJSON);
        CreateSlider(selectedPreviewProtrusionsOpacityJSON).label = "Selected Preview Opacity (Protrusions)";

        var syncSymmetryJSON = new JSONStorableBool("autoSyncSymmetry", ColliderPreviewConfig.DefaultSyncSymmetry, value =>
        {
            Config.SyncSymmetry = value;
            foreach (var editable in EditablesList.All)
                editable.SyncWithMirror = value;
            SelectEditable(_selected);
        });
        RegisterBool(syncSymmetryJSON);
        var syncSymmetryToggle = CreateToggle(syncSymmetryJSON, true);
        syncSymmetryToggle.label = "Auto Sync Symmetry";

        _editablesJson = new JSONStorableStringChooser(
            "Edit",
            new List<string>(),
            new List<string>(),
            "",
            "Edit")
        {
            isStorable = false,
            isRestorable = false
        };
        var editablesList = CreatePopupAuto(_editablesJson, true);
        editablesList.popupPanelHeight = 1000f;
        editablesList.popup.onOpenPopupHandlers += () => OnBlur(_editablesJson);
        _editablesJson.setCallbackFunction = id =>
        {
            IModel val;
            if (!_ready) return;
            if (EditablesList.ByUuid.TryGetValue(id, out val))
                SelectEditable(val);
            else
                SelectEditable(null);
        };

        UpdateFilter();
    }

    public UIDynamicPopup CreatePopupAuto(JSONStorableStringChooser jssc, bool rightSide = false, float popupPanelHeight = 0f, bool upwards = false)
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
                popup.popup.visible = false;
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
                popup.popup.visible = false;
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

        if(popupPanelHeight > 0f)
        {
            popup.popupPanelHeight = popupPanelHeight;
        }

        if(upwards)
        {
            popup.popup.popupPanel.offsetMin += new Vector2(0, popup.popupPanelHeight + 60);
            popup.popup.popupPanel.offsetMax += new Vector2(0, popup.popupPanelHeight + 60);
        }

        return popup;
#else
        return CreateScrollablePopup(jssc, rightSide);
#endif
    }

    public void SelectEditable(IModel val)
    {
        Deselect(ref _selected);
        Deselect(ref _selectedMirror);

        if (val == null)
        {
            _editablesJson.valNoCallback = "";
            return;
        }

        _editablesJson.valNoCallback = val.Id;
        EditablesList.PrepareForUI();

        Select(ref _selected, val, true);
        if (Config.SyncSymmetry && _selected.MirrorModel != null)
            Select(ref _selectedMirror, _selected.MirrorModel, false);
    }

    private void Deselect(ref IModel selected)
    {
        if (selected == null) return;
        selected.Selected = false;
        selected.Highlighted = false;
        selected.Shown = _filteredEditables.Contains(selected);
        selected.UpdatePreviewsFromConfig();
        selected = null;
    }

    // ReSharper disable once RedundantAssignment
    private static void Select(ref IModel selected, IModel val, bool showUI)
    {
        selected = val;
        selected.Shown = true;
        selected.Highlighted = true;
        selected.Selected = showUI;
        selected.UpdatePreviewsFromConfig();
    }

    private void UpdateFilter()
    {
        try
        {
            HideCurrentFilteredEditables();

            IEnumerable<IModel> filtered = EditablesList.All;
#if (!VAM_GT_1_20)
            var hasSearchQuery = !string.IsNullOrEmpty(_textFilterJson.val) && _textFilterJson.val != _searchDefault;

            if (!hasSearchQuery && _groupsJson.val == _noSelectionLabel && _typesJson.val == _noSelectionLabel && _filterJson.val == Filters.None)
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

            if (_filterJson.val == Filters.ModifiedOnly)
                filtered = filtered.Where(e => e.Modified);
            else if (_filterJson.val == Filters.NotModifiedOnly)
                filtered = filtered.Where(e => !e.Modified);

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

            _filteredEditables = filtered.OrderBy(x => x.Label, new NaturalStringComparer()).ToList();

            _editablesJson.choices = _filteredEditables.Select(x => x.Id).ToList();
            _editablesJson.displayChoices = _filteredEditables.Select(x => x.Label).ToList();

            foreach (var e in _filteredEditables)
            {
                e.Shown = true;
                e.UpdatePreviewsFromConfig();
            }

            if (_ready)
            {
                if (!_editablesJson.choices.Contains(_editablesJson.val) || string.IsNullOrEmpty(_editablesJson.val))
                    _editablesJson.val = _editablesJson.choices.FirstOrDefault() ?? "";
            }
        }
        catch (Exception e)
        {
            LogError(nameof(UpdateFilter), e.ToString());
        }
    }

    private void HideCurrentFilteredEditables()
    {
        var previous = _editablesJson.choices.Where(x => EditablesList.ByUuid.ContainsKey(x)).Select(x => EditablesList.ByUuid[x]);
        foreach (var e in previous)
        {
            e.Shown = false;
            e.UpdatePreviewsFromConfig();
        }

        SelectEditable(_selected);
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
            _restored = true;
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
                if (EditablesList.ByUuid.TryGetValue(migratedEditableId, out editableModel))
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
        foreach (var editable in EditablesList.All)
        {
            editable.AppendJson(editablesJsonClass);
        }
        jsonClass.Add("editables", editablesJsonClass);
    }

    #endregion

    #region Unity events

    public void OnEnable()
    {
        if (EditablesList?.All == null) return;
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
        if (EditablesList?.All == null) return;
        try
        {
            _jsonWhenDisabled = new JSONClass();
            AppendJson(_jsonWhenDisabled);
            foreach (var editable in EditablesList.All)
            {
                editable.DestroyPreviews();
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
        SuperController.singleton.BroadcastMessage("OnActionsProviderDestroyed", this, SendMessageOptions.DontRequireReceiver);
        if (EditablesList?.All == null) return;
        if (_uiListener != null)
            DestroyImmediate(_uiListener);

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
    private List<IModel> _filteredEditables;

    private void Update()
    {
        if (EditablesList == null) return;
        if (!(Time.time > _nextUpdate)) return;
        try
        {
            foreach (var editable in EditablesList.All)
            {
                if (editable.SyncOverrides())
                    editable.SyncPreviews();
            }

            _nextUpdate = Time.time + 1f;
        }
        catch (Exception e)
        {
            LogError(nameof(Update), $"{containingAtom.name}'s Collider Editor will be disabled.\r\n{e}");
            enabled = false;
        }
    }

    private void OnBlur()
    {
        OnBlur(null);
    }

    private void OnBlur(JSONStorableStringChooser except)
    {
        if (except != _groupsJson) _groupsJson.popup.visible = false;
        if (except != _typesJson) _typesJson.popup.visible = false;
        if (except != _filterJson) _filterJson.popup.visible = false;
        if (except != _presetsJson) _presetsJson.popup.visible = false;
        if (except != _editablesJson) _editablesJson.popup.visible = false;
    }

    #endregion

    public void OnBindingsListRequested(List<object> bindings)
    {
        bindings.Add(new JSONStorableAction("Toggle_ShowPreviews", () => _showPreviewsJSON.val = !_showPreviewsJSON.val));
    }

    private void LogError(string method, string message) => SuperController.LogError($"{nameof(ColliderEditor)}.{method}: {message}");

    public UIDynamicTextField CreateTextInput(JSONStorableString jss, bool rightSide = false)
    {
        var textfield = CreateTextField(jss, rightSide);
        textfield.height = 20f;
        textfield.backgroundColor = Color.white;
        var input = textfield.gameObject.AddComponent<InputField>();
        input.GetComponent<RectTransform>().sizeDelta = new Vector2(1f, 0.4f);
        input.textComponent = textfield.UItext;
        jss.inputField = input;
        return textfield;
    }
}
