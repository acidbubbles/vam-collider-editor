using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SimpleJSON;
using UnityEngine;

using Object = UnityEngine.Object;
using Random = UnityEngine.Random;


/// <summary>
///     Collider Editor
///     Disables colliders or adjust them to your liking
///     This is a rewrite with permission from acidbubbles of his plugin 'ColliderTuner', the original plugin was written from the perspective of
///     manipulating RigidBodies. After a discussion on VaM Discord it turns out that Colliders should be the focus and the plugin was re-written
///     based on this. This fixed a host of issues around the identification of specific colliders so they can be saved and restored.
/// </summary>
public class ColliderEditor : MVRScript
{
    private const string _saveExt = "colliders";
    private Dictionary<string, ColliderModel> _colliders;
    private JSONStorableStringChooser _collidersJson;

    private string _lastBrowseDir = SuperController.singleton.savesDir;
    private ColliderModel _lastSelectedCollider;
    private RigidbodyModel _lastSelectedRigidbody;
    private Dictionary<string, RigidbodyModel> _rigidbodies;
    private JSONStorableStringChooser _rigidbodiesJson;

    private Dictionary<string, RigidbodyGroupModel> _rigidbodyGroups;

    private JSONStorableStringChooser _rigidbodyGroupsJson;
    private ColliderModel _selectedCollider;

    private RigidbodyGroupModel _selectedGroup;
    private RigidbodyModel _selectedRigidbody;

    public override void Init()
    {
        try
        {
            pluginLabelJSON.val = "Collider Editor v1.0.0";

            if (containingAtom.type != "Person")
            {
                SuperController.LogError($"This plugin is for use with 'Person' atom only, not '{containingAtom.type}'");
                return;
            }

            BuildModels(includeAutoColliders: true);
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

        var showPreviewsToggle = CreateToggle(showPreviews);
        showPreviewsToggle.label = "Show Previews";

        CreateSlider(new JSONStorableFloat("previewOpacity", 0.001f, value =>
        {
            var alpha = ExponentialScale(value, 0.2f, 1f);
            foreach (var colliderPair in _colliders)
                colliderPair.Value.PreviewOpacity = alpha;
        }, 0f, 1f)).label = "Preview Opacity";

        CreateSlider(new JSONStorableFloat("selectedPreviewOpacity", 0.3f, value =>
        {
            var alpha = ExponentialScale(value, 0.2f, 1f);
            foreach (var colliderPair in _colliders)
                colliderPair.Value.SelectedPreviewOpacity = alpha;
        }, 0f, 1f)).label = "Selected Preview Opacity";

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
            browser.fileEntryField.text = (int) (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds + "." + _saveExt;
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
            "All",
            "Rigidbody Groups");

        UIDynamicPopup rbGroupListUI = CreateScrollablePopup(_rigidbodyGroupsJson);
        rbGroupListUI.popupPanelHeight = 400f;

        _rigidbodiesJson = new JSONStorableStringChooser(
            "Rigidbodies",
            _rigidbodies.Keys.ToList(),
            _rigidbodies.Select(x => x.Value.Label).ToList(),
            "All",
            "Rigidbodies");

        UIDynamicPopup ridigBodyList = CreateScrollablePopup(_rigidbodiesJson);
        ridigBodyList.popupPanelHeight = 400f;

        _collidersJson = new JSONStorableStringChooser(
            "Colliders",
            _colliders.Keys.ToList(),
            _colliders.Select(x => x.Value.Label).ToList(),
            "All",
            "Colliders");

        UIDynamicPopup rbListUI = CreateScrollablePopup(_collidersJson, true);
        rbListUI.popupPanelHeight = 400f;

        _rigidbodyGroupsJson.setCallbackFunction = groupId =>
        {
            _selectedGroup = _rigidbodyGroups.ContainsKey(groupId) ? _rigidbodyGroups[groupId] : null;
            UpdateFilter();
        };

        _rigidbodiesJson.setCallbackFunction = rigidbodyId =>
        {
            _selectedRigidbody?.DestroyControls();
            _selectedRigidbody = _rigidbodies.ContainsKey(rigidbodyId) ? _rigidbodies[rigidbodyId] : null;
            UpdateFilter();
        };

        _collidersJson.setCallbackFunction = colliderId =>
        {
            _selectedCollider = _colliders.ContainsKey(colliderId) ? _colliders[colliderId] : null;
            UpdateFilter();
        };

        UpdateFilter();
    }

    private void BuildModels(bool includeAutoColliders)
    {
        var rigidbodyGroups = new List<RigidbodyGroupModel>
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
        };

        _rigidbodyGroups = rigidbodyGroups.ToDictionary(x => x.Id);

        _rigidbodies = containingAtom.GetComponentsInChildren<Rigidbody>(true)
            .Where(rigidbody => !rigidbody.isKinematic && rigidbody.name != "control" && rigidbody.name != "object" &&
                                !rigidbody.name.EndsWith("Control") && !rigidbody.name.StartsWith("hairTool") && !rigidbody.name.EndsWith("Trigger") &&
                                !rigidbody.name.EndsWith("UI") &&
                                (includeAutoColliders || (!rigidbody.name.Contains("AutoCollider") && !rigidbody.name.Contains("PhysicsMesh"))) &&
                                !rigidbody.name.Contains("Ponytail")
            )
            .Select(rigidbody => RigidbodyModel.Create(this, rigidbody, rigidbodyGroups))
            .ToDictionary(x => x.Id);

        // Colliders

        var colliderQuery = containingAtom.GetComponentsInChildren<Collider>(true)
            .Where(collider => collider.name != "control" && collider.name != "object" && !collider.name.Contains("Tool") &&
                               !collider.name.EndsWith("Control") && !collider.name.EndsWith("Link") && !collider.name.EndsWith("Trigger") &&
                               !collider.name.EndsWith("UI") &&
                                (includeAutoColliders || (!collider.name.Contains("AutoCollider") && !collider.name.Contains("PhysicsMesh"))) &&
                               !collider.name.Contains("Ponytail")
            );


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

            _rigidbodiesJson.choices = new[] {"All"}.Concat(rigidbodies.Select(x => x.Id)).ToList();
            _rigidbodiesJson.displayChoices = new[] {"All"}.Concat(rigidbodies.Select(x => x.Label)).ToList();


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

            _collidersJson.choices = colliders.Select(x => x.Id).ToList();
            _collidersJson.displayChoices = colliders.Select(x => x.Label).ToList();

            if (_selectedCollider != null && _collidersJson.choices.Contains(_selectedCollider.Id))
            {
                _collidersJson.valNoCallback = _selectedCollider.Id;
            }
            else
            {
                var firstAvailableId = _collidersJson.choices.FirstOrDefault();
                _collidersJson.valNoCallback = firstAvailableId ?? string.Empty;
                if (!string.IsNullOrEmpty(firstAvailableId))
                    _selectedCollider = _colliders[firstAvailableId];
                else
                    _selectedCollider = null;
            }

            UpdateSelectedRigidbody();
            UpdateSelectedCollider();
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

    private void LogError(string method, string message) => SuperController.LogError($"ColliderTuner.{method}: {message}");

    private void HandleLoadPreset(string path)
    {
        if (string.IsNullOrEmpty(path))
            return;
        _lastBrowseDir = path.Substring(0, path.LastIndexOfAny(new[] {'/', '\\'})) + @"\";

        LoadFromJson((JSONClass) LoadJSON(path));
    }

    private void LoadFromJson(JSONClass jsonClass)
    {
        var collidersJsonClass = jsonClass["colliders"].AsObject;
        foreach (string colliderId in collidersJsonClass.Keys)
            if (_colliders.ContainsKey(colliderId))
                _colliders[colliderId]
                    .LoadJson(collidersJsonClass[colliderId].AsObject);

        var rigidbodiesJsonClass = jsonClass["rigidbodies"].AsObject;
        foreach (string rigidbodyId in rigidbodiesJsonClass.Keys)
            if (_rigidbodies.ContainsKey(rigidbodyId))
                _rigidbodies[rigidbodyId]
                    .LoadJson(rigidbodiesJsonClass[rigidbodyId].AsObject);
    }

    private void HandleSavePreset(string path)
    {
        if (string.IsNullOrEmpty(path))
            return;

        _lastBrowseDir = path.Substring(0, path.LastIndexOfAny(new[] {'/', '\\'})) + @"\";

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

public static class ColorExtensions
{
    public static Color ToColor(this string value)
    {
        Color color;
        ColorUtility.TryParseHtmlString(value, out color);
        color.a = 0.005f;
        return color;
    }
}

public static class MaterialHelper
{
    private static Queue<Material> _materials;

    public static Material GetNextMaterial()
    {
        if (_materials == null)
        {
            var materials = new List<Material>();
            materials.Add(CreateMaterial("#800000".ToColor()));
            materials.Add(CreateMaterial("#8B0000".ToColor()));
            materials.Add(CreateMaterial("#A52A2A".ToColor()));
            materials.Add(CreateMaterial("#B22222".ToColor()));
            materials.Add(CreateMaterial("#DC143C".ToColor()));
            materials.Add(CreateMaterial("#FF0000".ToColor()));
            materials.Add(CreateMaterial("#FF6347".ToColor()));
            materials.Add(CreateMaterial("#FF7F50".ToColor()));
            materials.Add(CreateMaterial("#CD5C5C".ToColor()));
            materials.Add(CreateMaterial("#F08080".ToColor()));
            materials.Add(CreateMaterial("#E9967A".ToColor()));
            materials.Add(CreateMaterial("#FA8072".ToColor()));
            materials.Add(CreateMaterial("#FFA07A".ToColor()));
            materials.Add(CreateMaterial("#FF4500".ToColor()));
            materials.Add(CreateMaterial("#FF8C00".ToColor()));
            materials.Add(CreateMaterial("#FFA500".ToColor()));
            materials.Add(CreateMaterial("#FFD700".ToColor()));
            materials.Add(CreateMaterial("#B8860B".ToColor()));
            materials.Add(CreateMaterial("#DAA520".ToColor()));
            materials.Add(CreateMaterial("#EEE8AA".ToColor()));
            materials.Add(CreateMaterial("#BDB76B".ToColor()));
            materials.Add(CreateMaterial("#F0E68C".ToColor()));
            materials.Add(CreateMaterial("#808000".ToColor()));
            materials.Add(CreateMaterial("#FFFF00".ToColor()));
            materials.Add(CreateMaterial("#9ACD32".ToColor()));
            materials.Add(CreateMaterial("#556B2F".ToColor()));
            materials.Add(CreateMaterial("#6B8E23".ToColor()));
            materials.Add(CreateMaterial("#7CFC00".ToColor()));
            materials.Add(CreateMaterial("#7FFF00".ToColor()));
            materials.Add(CreateMaterial("#ADFF2F".ToColor()));
            materials.Add(CreateMaterial("#006400".ToColor()));
            materials.Add(CreateMaterial("#008000".ToColor()));
            materials.Add(CreateMaterial("#228B22".ToColor()));
            materials.Add(CreateMaterial("#00FF00".ToColor()));
            materials.Add(CreateMaterial("#32CD32".ToColor()));
            materials.Add(CreateMaterial("#90EE90".ToColor()));
            materials.Add(CreateMaterial("#98FB98".ToColor()));
            materials.Add(CreateMaterial("#8FBC8F".ToColor()));
            materials.Add(CreateMaterial("#00FA9A".ToColor()));
            materials.Add(CreateMaterial("#00FF7F".ToColor()));
            materials.Add(CreateMaterial("#2E8B57".ToColor()));
            materials.Add(CreateMaterial("#66CDAA".ToColor()));
            materials.Add(CreateMaterial("#3CB371".ToColor()));
            materials.Add(CreateMaterial("#20B2AA".ToColor()));
            materials.Add(CreateMaterial("#2F4F4F".ToColor()));
            materials.Add(CreateMaterial("#008080".ToColor()));
            materials.Add(CreateMaterial("#008B8B".ToColor()));
            materials.Add(CreateMaterial("#00FFFF".ToColor()));
            materials.Add(CreateMaterial("#00FFFF".ToColor()));
            materials.Add(CreateMaterial("#E0FFFF".ToColor()));
            materials.Add(CreateMaterial("#00CED1".ToColor()));
            materials.Add(CreateMaterial("#40E0D0".ToColor()));
            materials.Add(CreateMaterial("#48D1CC".ToColor()));
            materials.Add(CreateMaterial("#AFEEEE".ToColor()));
            materials.Add(CreateMaterial("#7FFFD4".ToColor()));
            materials.Add(CreateMaterial("#B0E0E6".ToColor()));
            materials.Add(CreateMaterial("#5F9EA0".ToColor()));
            materials.Add(CreateMaterial("#4682B4".ToColor()));
            materials.Add(CreateMaterial("#6495ED".ToColor()));
            materials.Add(CreateMaterial("#00BFFF".ToColor()));
            materials.Add(CreateMaterial("#1E90FF".ToColor()));
            materials.Add(CreateMaterial("#ADD8E6".ToColor()));
            materials.Add(CreateMaterial("#87CEEB".ToColor()));
            materials.Add(CreateMaterial("#87CEFA".ToColor()));
            materials.Add(CreateMaterial("#191970".ToColor()));
            materials.Add(CreateMaterial("#000080".ToColor()));
            materials.Add(CreateMaterial("#00008B".ToColor()));
            materials.Add(CreateMaterial("#0000CD".ToColor()));
            materials.Add(CreateMaterial("#0000FF".ToColor()));
            materials.Add(CreateMaterial("#4169E1".ToColor()));
            materials.Add(CreateMaterial("#8A2BE2".ToColor()));
            materials.Add(CreateMaterial("#4B0082".ToColor()));
            materials.Add(CreateMaterial("#483D8B".ToColor()));
            materials.Add(CreateMaterial("#6A5ACD".ToColor()));
            materials.Add(CreateMaterial("#7B68EE".ToColor()));
            materials.Add(CreateMaterial("#9370DB".ToColor()));
            materials.Add(CreateMaterial("#8B008B".ToColor()));
            materials.Add(CreateMaterial("#9400D3".ToColor()));
            materials.Add(CreateMaterial("#9932CC".ToColor()));
            materials.Add(CreateMaterial("#BA55D3".ToColor()));
            materials.Add(CreateMaterial("#800080".ToColor()));
            materials.Add(CreateMaterial("#D8BFD8".ToColor()));
            materials.Add(CreateMaterial("#DDA0DD".ToColor()));
            materials.Add(CreateMaterial("#EE82EE".ToColor()));
            materials.Add(CreateMaterial("#FF00FF".ToColor()));
            materials.Add(CreateMaterial("#DA70D6".ToColor()));
            materials.Add(CreateMaterial("#C71585".ToColor()));
            materials.Add(CreateMaterial("#DB7093".ToColor()));
            materials.Add(CreateMaterial("#FF1493".ToColor()));
            materials.Add(CreateMaterial("#FF69B4".ToColor()));
            materials.Add(CreateMaterial("#FFB6C1".ToColor()));
            materials.Add(CreateMaterial("#FFC0CB".ToColor()));
            materials.Add(CreateMaterial("#FAEBD7".ToColor()));
            materials.Add(CreateMaterial("#F5F5DC".ToColor()));
            materials.Add(CreateMaterial("#FFE4C4".ToColor()));
            materials.Add(CreateMaterial("#FFEBCD".ToColor()));
            materials.Add(CreateMaterial("#F5DEB3".ToColor()));
            materials.Add(CreateMaterial("#FFF8DC".ToColor()));
            materials.Add(CreateMaterial("#FFFACD".ToColor()));
            materials.Add(CreateMaterial("#FAFAD2".ToColor()));
            materials.Add(CreateMaterial("#FFFFE0".ToColor()));
            materials.Add(CreateMaterial("#8B4513".ToColor()));
            materials.Add(CreateMaterial("#A0522D".ToColor()));
            materials.Add(CreateMaterial("#D2691E".ToColor()));
            materials.Add(CreateMaterial("#CD853F".ToColor()));
            materials.Add(CreateMaterial("#F4A460".ToColor()));
            materials.Add(CreateMaterial("#DEB887".ToColor()));
            materials.Add(CreateMaterial("#D2B48C".ToColor()));
            materials.Add(CreateMaterial("#BC8F8F".ToColor()));
            materials.Add(CreateMaterial("#FFE4B5".ToColor()));
            materials.Add(CreateMaterial("#FFDEAD".ToColor()));
            materials.Add(CreateMaterial("#FFDAB9".ToColor()));
            materials.Add(CreateMaterial("#FFE4E1".ToColor()));
            materials.Add(CreateMaterial("#FFF0F5".ToColor()));

            _materials = new Queue<Material>(materials.OrderBy(x => Random.Range(-1, 2)));
        }

        Material current;
        _materials.Enqueue(current = _materials.Dequeue());
        return current;
    }

    private static Material CreateMaterial(Color color)
    {
        var material = new Material(Shader.Find("Battlehub/RTGizmos/Handles")) {color = color};

        material.SetFloat("_Offset", 1f);
        material.SetFloat("_MinAlpha", 1f);

        return material;
    }
}

public abstract class ColliderModel<T> : ColliderModel where T : Collider
{
    protected T Collider { get; }

    protected ColliderModel(MVRScript parent, T collider, string label)
        : base(parent, collider.Uuid(), label)
    {
        Collider = collider;
    }

    public override void CreatePreview()
    {
        var preview = DoCreatePreview();

        preview.GetComponent<Renderer>().material = MaterialHelper.GetNextMaterial();
        foreach (var c in preview.GetComponents<Collider>())
        {
            c.enabled = false;
            Object.Destroy(c);
        }

        preview.transform.SetParent(Collider.transform, false);

        Preview = preview;

        DoUpdatePreview();
        SetSelected(Selected);
    }
}

public abstract class ColliderModel
{
    private float _previewOpacity;

    private bool _selected;

    private float _selectedPreviewOpacity;

    private bool _showPreview;
    protected MVRScript Parent { get; }

    public string Id { get; }
    public string Label { get; }
    public RigidbodyModel Rididbody { get; set; }

    public GameObject Preview { get; protected set; }
    public List<UIDynamic> Controls { get; private set; }

    public bool Selected
    {
        get { return _selected; }
        set
        {
            if (_selected != value)
            {
                SetSelected(value);
                _selected = value;
            }
        }
    }

    public float SelectedPreviewOpacity
    {
        get { return _selectedPreviewOpacity; }
        set
        {
            if (Mathf.Approximately(value, _selectedPreviewOpacity))
                return;

            _selectedPreviewOpacity = value;

            if (Preview != null && _selected)
            {
                var previewRenderer = Preview.GetComponent<Renderer>();
                var color = previewRenderer.material.color;
                color.a = _selectedPreviewOpacity;
                previewRenderer.material.color = color;
            }
        }
    }

    public float PreviewOpacity
    {
        get { return _previewOpacity; }
        set
        {
            if (Mathf.Approximately(value, _previewOpacity))
                return;

            _previewOpacity = value;

            if (Preview != null && !_selected)
            {
                var previewRenderer = Preview.GetComponent<Renderer>();
                var color = previewRenderer.material.color;
                color.a = _previewOpacity;
                previewRenderer.material.color = color;
            }
        }
    }

    public bool ShowPreview
    {
        get { return _showPreview; }
        set
        {
            _showPreview = value;

            if (_showPreview)
                CreatePreview();
            else
                DestroyPreview();
        }
    }

    protected ColliderModel(MVRScript parent, string id, string label)
    {
        Parent = parent;

        Id = id;
        Label = label;
    }

    public static ColliderModel CreateTyped(MVRScript parent, Collider collider, Dictionary<string, RigidbodyModel> rigidbodies)
    {
        ColliderModel typed;

        if (collider is SphereCollider)
            typed = new SphereColliderModel(parent, (SphereCollider) collider);
        else if (collider is BoxCollider)
            typed = new BoxColliderModel(parent, (BoxCollider) collider);
        else if (collider is CapsuleCollider)
            typed = new CapsuleColliderModel(parent, (CapsuleCollider) collider);
        else
            throw new ArgumentOutOfRangeException("Unsupported collider type");

        if (collider.attachedRigidbody != null)
        {
            var rigidbodyModel = rigidbodies[collider.attachedRigidbody.Uuid()];
            typed.Rididbody = rigidbodyModel;
            if (rigidbodyModel.Colliders == null)
                rigidbodyModel.Colliders = new List<ColliderModel> {typed};
            else
                rigidbodyModel.Colliders.Add(typed);
        }

        return typed;
    }

    public void CreateControls()
    {
        DestroyControls();

        var controls = new List<UIDynamic>();

        var resetUi = Parent.CreateButton("Reset Collider", true);
        resetUi.button.onClick.AddListener(ResetToInitial);

        controls.Add(resetUi);
        controls.AddRange(DoCreateControls());

        Controls = controls;
    }

    public abstract IEnumerable<UIDynamic> DoCreateControls();

    public virtual void DestroyControls()
    {
        if (Controls == null)
            return;

        foreach (var adjustmentJson in Controls)
            Object.Destroy(adjustmentJson.gameObject);

        Controls.Clear();
    }

    public virtual void DestroyPreview()
    {
        if (Preview != null)
        {
            Object.Destroy(Preview);
            Preview = null;
        }
    }

    public abstract void CreatePreview();

    protected abstract GameObject DoCreatePreview();

    public void UpdatePreview()
    {
        if (_showPreview)
            DoUpdatePreview();
    }

    protected abstract void DoUpdatePreview();

    public void UpdateControls()
    {
        DoUpdateControls();
    }

    protected abstract void DoUpdateControls();

    protected virtual void SetSelected(bool value)
    {
        if (Preview != null)
        {
            var previewRenderer = Preview.GetComponent<Renderer>();
            var color = previewRenderer.material.color;
            color.a = value ? _selectedPreviewOpacity : _previewOpacity;
            previewRenderer.material.color = color;
        }

        if (value)
            CreateControls();
        else
            DestroyControls();
    }

    public void AppendJson(JSONClass parent)
    {
        if (DeviatesFromInitial())
            parent.Add(Id, DoGetJson());
    }

    public void LoadJson(JSONClass jsonClass)
    {
        DoLoadJson(jsonClass);
        DoUpdatePreview();

        if (Selected)
        {
            DestroyControls();
            CreateControls();
        }
    }

    protected abstract void DoLoadJson(JSONClass jsonClass);

    public abstract JSONClass DoGetJson();

    public void ResetToInitial()
    {
        DoResetToInitial();
        DoUpdatePreview();

        if (Selected)
        {
            DestroyControls();
            CreateControls();
        }
    }

    protected abstract void DoResetToInitial();

    protected abstract bool DeviatesFromInitial();

    public override string ToString() => Id;
}

public class RigidbodyModel
{
    private readonly bool _initialEnabled;
    private readonly Rigidbody _rigidbody;

    private readonly MVRScript _script;

    private List<UIDynamic> _controls;

    private bool _selected;

    public string Id { get; set; }
    public string Name { get; set; }
    public string Label { get; set; }
    public List<RigidbodyGroupModel> Groups { get; set; }
    public List<ColliderModel> Colliders { get; set; }

    public bool Selected
    {
        get { return _selected; }
        set
        {
            if (_selected != value)
            {
                SetSelected(value);
                _selected = value;
            }
        }
    }

    public RigidbodyModel(MVRScript script, Rigidbody rigidbody, string label)
    {
        _script = script;
        _rigidbody = rigidbody;

        Id = rigidbody.Uuid();
        Name = rigidbody.name;
        Label = label;

        _initialEnabled = rigidbody.detectCollisions;
    }

    public static RigidbodyModel Create(MVRScript script, Rigidbody rigidbody, IEnumerable<RigidbodyGroupModel> groups)
    {
        var model = new RigidbodyModel(script, rigidbody, rigidbody.name);
        model.Groups = groups
            .Where(category => category.Pattern.IsMatch(rigidbody.name))
            .ToList();
        return model;
    }

    public override string ToString() => $"{Id}_{Name}";

    private void SetSelected(bool value)
    {
        if (value)
            CreateControls();
        else
            DestroyControls();
    }

    public void CreateControls()
    {
        DestroyControls();

        var controls = new List<UIDynamic>();

        var resetUi = _script.CreateButton("Reset Rigidbody");
        resetUi.button.onClick.AddListener(ResetToInitial);

        var enabledToggleJsf = new JSONStorableBool("enabled", _rigidbody.detectCollisions, value => { _rigidbody.detectCollisions = value; });
        var enabledToggle = _script.CreateToggle(enabledToggleJsf);
        enabledToggle.label = "Detect Collisions";

        controls.Add(resetUi);
        controls.Add(enabledToggle);

        _controls = controls;
    }

    public virtual void DestroyControls()
    {
        if (_controls == null)
            return;

        foreach (var control in _controls)
            Object.Destroy(control.gameObject);

        _controls.Clear();
    }

    public void AppendJson(JSONClass parent)
    {
        if (DeviatesFromInitial())
            parent.Add(Id, DoGetJson());
    }

    public void LoadJson(JSONClass jsonClass)
    {
        DoLoadJson(jsonClass);

        if (Selected)
        {
            DestroyControls();
            CreateControls();
        }
    }

    private void DoLoadJson(JSONClass jsonClass)
    {
        _rigidbody.detectCollisions = jsonClass["detectCollisions"].AsBool;
    }

    public JSONClass DoGetJson()
    {
        var jsonClass = new JSONClass();
        jsonClass["detectCollisions"].AsBool = _rigidbody.detectCollisions;
        return jsonClass;
    }

    public void ResetToInitial()
    {
        DoResetToInitial();

        if (Selected)
        {
            DestroyControls();
            CreateControls();
        }
    }

    protected void DoResetToInitial()
    {
        _rigidbody.detectCollisions = _initialEnabled;
    }

    protected bool DeviatesFromInitial() => _rigidbody.detectCollisions != _initialEnabled;
}

public class RigidbodyGroupModel
{
    public string Id { get; set; }
    public string Name { get; set; }
    public Regex Pattern { get; set; }

    public RigidbodyGroupModel(string name, string pattern)
    {
        Id = name;
        Name = name;
        Pattern = new Regex(pattern, RegexOptions.Compiled | RegexOptions.ExplicitCapture);
    }
}

public class CapsuleColliderModel : ColliderModel<CapsuleCollider>
{
    private JSONStorableFloat _centerXStorableFloat;
    private JSONStorableFloat _centerYStorableFloat;
    private JSONStorableFloat _centerZStorableFloat;
    private JSONStorableFloat _heightStorableFloat;
    private JSONStorableFloat _radiusStorableFloat;

    public float InitialRadius { get; set; }
    public float InitialHeight { get; set; }
    public Vector3 InitialCenter { get; set; }

    public CapsuleColliderModel(MVRScript parent, CapsuleCollider collider)
        : base(parent, collider, collider.name)
    {
        InitialRadius = collider.radius;
        InitialHeight = collider.height;
        InitialCenter = collider.center;
    }

    public override IEnumerable<UIDynamic> DoCreateControls()
    {
        yield return Parent.CreateFloatSlider(_radiusStorableFloat = new JSONStorableFloat("radius", Collider.radius, value =>
        {
            Collider.radius = value;
            DoUpdatePreview();
        }, 0f, 0.2f), "Radius");

        yield return Parent.CreateFloatSlider(_heightStorableFloat = new JSONStorableFloat("height", Collider.height, value =>
        {
            Collider.height = value;
            DoUpdatePreview();
        }, 0f, 0.2f), "Height");

        yield return Parent.CreateFloatSlider(_centerXStorableFloat = new JSONStorableFloat("centerX", Collider.center.x, value =>
        {
            var center = Collider.center;
            center.x = value;
            Collider.center = center;
            DoUpdatePreview();
        }, -0.05f, 0.05f), "Center.X");

        yield return Parent.CreateFloatSlider(_centerYStorableFloat = new JSONStorableFloat("centerY", Collider.center.y, value =>
        {
            var center = Collider.center;
            center.y = value;
            Collider.center = center;
            DoUpdatePreview();
        }, -0.05f, 0.05f), "Center.Y");

        yield return Parent.CreateFloatSlider(_centerZStorableFloat = new JSONStorableFloat("centerZ", Collider.center.z, value =>
        {
            var center = Collider.center;
            center.z = value;
            Collider.center = center;
            DoUpdatePreview();
        }, -0.05f, 0.05f), "Center.Z");
    }

    protected override void DoLoadJson(JSONClass jsonClass)
    {
        Collider.radius = jsonClass["radius"].AsFloat;
        Collider.height = jsonClass["height"].AsFloat;

        var center = Collider.center;
        center.x = jsonClass["centerX"].AsFloat;
        center.y = jsonClass["centerY"].AsFloat;
        center.z = jsonClass["centerZ"].AsFloat;
        Collider.center = center;
    }

    public override JSONClass DoGetJson()
    {
        var jsonClass = new JSONClass();
        jsonClass["radius"].AsFloat = Collider.radius;
        jsonClass["height"].AsFloat = Collider.height;
        jsonClass["centerX"].AsFloat = Collider.center.x;
        jsonClass["centerY"].AsFloat = Collider.center.y;
        jsonClass["centerZ"].AsFloat = Collider.center.z;
        return jsonClass;
    }

    protected override void DoResetToInitial()
    {
        Collider.radius = InitialRadius;
        Collider.height = InitialHeight;
        Collider.center = InitialCenter;
    }

    protected override bool DeviatesFromInitial() =>
        !Mathf.Approximately(InitialRadius, Collider.radius) ||
        !Mathf.Approximately(InitialHeight, Collider.height) ||
        InitialCenter != Collider.center; // Vector3 has built in epsilon equality checks

    protected override GameObject DoCreatePreview() => GameObject.CreatePrimitive(PrimitiveType.Capsule);

    protected override void DoUpdatePreview()
    {
        float size = Collider.radius * 2;
        float height = Collider.height / 2;
        Preview.transform.localScale = new Vector3(size, height, size);
        if (Collider.direction == 0)
            Preview.transform.localRotation = Quaternion.AngleAxis(90, Vector3.forward);
        else if (Collider.direction == 2)
            Preview.transform.localRotation = Quaternion.AngleAxis(90, Vector3.right);
        Preview.transform.localPosition = Collider.center;
    }

    protected override void DoUpdateControls()
    {
        if (_radiusStorableFloat != null)
            _radiusStorableFloat.valNoCallback = Collider.radius;
        if (_heightStorableFloat != null)
            _heightStorableFloat.valNoCallback = Collider.height;
        if (_centerXStorableFloat != null)
            _centerXStorableFloat.valNoCallback = Collider.center.x;
        if (_centerYStorableFloat != null)
            _centerYStorableFloat.valNoCallback = Collider.center.y;
        if (_centerZStorableFloat != null)
            _centerZStorableFloat.valNoCallback = Collider.center.z;
    }
}

public class SphereColliderModel : ColliderModel<SphereCollider>
{
    private JSONStorableFloat _centerXStorableFloat;
    private JSONStorableFloat _centerYStorableFloat;
    private JSONStorableFloat _centerZStorableFloat;
    private JSONStorableFloat _radiusStorableFloat;

    public float InitialRadius { get; set; }
    public Vector3 InitialCenter { get; set; }

    public SphereColliderModel(MVRScript parent, SphereCollider collider)
        : base(parent, collider, collider.name)
    {
        InitialRadius = collider.radius;
        InitialCenter = collider.center;
    }

    protected override GameObject DoCreatePreview() => GameObject.CreatePrimitive(PrimitiveType.Cube);

    protected override void DoUpdatePreview()
    {
        if (Preview == null)
            Preview = GameObject.CreatePrimitive(PrimitiveType.Sphere);

        Preview.transform.localScale = Vector3.one * (Collider.radius * 2);
        Preview.transform.localPosition = Collider.center;
    }

    protected override void DoUpdateControls()
    {
        if (_radiusStorableFloat != null)
            _radiusStorableFloat.valNoCallback = Collider.radius;
        if (_centerXStorableFloat != null)
            _centerXStorableFloat.valNoCallback = Collider.center.x;
        if (_centerYStorableFloat != null)
            _centerYStorableFloat.valNoCallback = Collider.center.y;
        if (_centerZStorableFloat != null)
            _centerZStorableFloat.valNoCallback = Collider.center.z;
    }

    protected override void DoLoadJson(JSONClass jsonClass)
    {
        Collider.radius = jsonClass["radius"].AsFloat;

        var center = Collider.center;
        center.x = jsonClass["centerX"].AsFloat;
        center.y = jsonClass["centerY"].AsFloat;
        center.z = jsonClass["centerZ"].AsFloat;
        Collider.center = center;
    }

    public override JSONClass DoGetJson()
    {
        var jsonClass = new JSONClass();

        jsonClass["radius"].AsFloat = Collider.radius;

        jsonClass["centerX"].AsFloat = Collider.center.x;
        jsonClass["centerY"].AsFloat = Collider.center.y;
        jsonClass["centerZ"].AsFloat = Collider.center.z;

        return jsonClass;
    }

    protected override void DoResetToInitial()
    {
        Collider.radius = InitialRadius;
        Collider.center = InitialCenter;
    }

    public override IEnumerable<UIDynamic> DoCreateControls()
    {
        yield return Parent.CreateFloatSlider(_radiusStorableFloat = new JSONStorableFloat("radius", Collider.radius, value =>
        {
            Collider.radius = value;
            DoUpdatePreview();
        }, 0f, 0.2f), "Radius");

        yield return Parent.CreateFloatSlider(_centerXStorableFloat = new JSONStorableFloat("centerX", Collider.center.x, value =>
        {
            var center = Collider.center;
            center.x = value;
            Collider.center = center;
            DoUpdatePreview();
        }, -0.05f, 0.05f), "Center.X");

        yield return Parent.CreateFloatSlider(_centerYStorableFloat = new JSONStorableFloat("centerY", Collider.center.y, value =>
        {
            var center = Collider.center;
            center.y = value;
            Collider.center = center;
            DoUpdatePreview();
        }, -0.05f, 0.05f), "Center.Y");

        yield return Parent.CreateFloatSlider(_centerZStorableFloat = new JSONStorableFloat("centerZ", Collider.center.z, value =>
        {
            var center = Collider.center;
            center.z = value;
            Collider.center = center;
            DoUpdatePreview();
        }, -0.05f, 0.05f), "Center.Z");
    }

    protected override bool DeviatesFromInitial() =>
        !Mathf.Approximately(InitialRadius, Collider.radius) ||
        InitialCenter != Collider.center; // Vector3 has built in epsilon equality checks
}

public class BoxColliderModel : ColliderModel<BoxCollider>
{
    private JSONStorableFloat _centerXStorableFloat;
    private JSONStorableFloat _centerYStorableFloat;
    private JSONStorableFloat _centerZStorableFloat;
    private JSONStorableFloat _sizeXStorableFloat;
    private JSONStorableFloat _sizeYStorableFloat;
    private JSONStorableFloat _sizeZStorableFloat;

    public Vector3 InitialSize { get; set; }
    public Vector3 InitialCenter { get; set; }

    public BoxColliderModel(MVRScript parent, BoxCollider collider)
        : base(parent, collider, collider.name)
    {
        InitialSize = collider.size;
        InitialCenter = collider.center;
    }

    protected override GameObject DoCreatePreview() => GameObject.CreatePrimitive(PrimitiveType.Cube);

    protected override void DoUpdatePreview()
    {
        Preview.transform.localScale = Collider.size;
        Preview.transform.localPosition = Collider.center;
    }

    protected override void DoUpdateControls()
    {
        if (_sizeXStorableFloat != null)
            _sizeXStorableFloat.valNoCallback = Collider.size.x;
        if (_sizeYStorableFloat != null)
            _sizeYStorableFloat.valNoCallback = Collider.size.y;
        if (_sizeZStorableFloat != null)
            _sizeZStorableFloat.valNoCallback = Collider.size.z;
        if (_centerXStorableFloat != null)
            _centerXStorableFloat.valNoCallback = Collider.center.x;
        if (_centerYStorableFloat != null)
            _centerYStorableFloat.valNoCallback = Collider.center.y;
        if (_centerZStorableFloat != null)
            _centerZStorableFloat.valNoCallback = Collider.center.z;
    }

    protected override void DoLoadJson(JSONClass jsonClass)
    {
        var size = Collider.size;
        size.x = jsonClass["sizeX"].AsFloat;
        size.y = jsonClass["sizeY"].AsFloat;
        size.z = jsonClass["sizeZ"].AsFloat;
        Collider.size = size;

        var center = Collider.center;
        center.x = jsonClass["centerX"].AsFloat;
        center.y = jsonClass["centerY"].AsFloat;
        center.z = jsonClass["centerZ"].AsFloat;
        Collider.center = center;
    }

    public override JSONClass DoGetJson()
    {
        var jsonClass = new JSONClass();

        jsonClass["sizeX"].AsFloat = Collider.size.x;
        jsonClass["sizeY"].AsFloat = Collider.size.y;
        jsonClass["sizeZ"].AsFloat = Collider.size.z;

        jsonClass["centerX"].AsFloat = Collider.center.x;
        jsonClass["centerY"].AsFloat = Collider.center.y;
        jsonClass["centerZ"].AsFloat = Collider.center.z;

        return jsonClass;
    }

    protected override void DoResetToInitial()
    {
        Collider.size = InitialSize;
        Collider.center = InitialCenter;
    }

    public override IEnumerable<UIDynamic> DoCreateControls()
    {
        yield return Parent.CreateFloatSlider(_sizeXStorableFloat = new JSONStorableFloat("sizeX", Collider.size.x, value =>
        {
            var size = Collider.size;
            size.x = value;
            Collider.size = size;
            DoUpdatePreview();
        }, -0.05f, 0.05f), "Size.X");

        yield return Parent.CreateFloatSlider(_sizeYStorableFloat = new JSONStorableFloat("sizeY", Collider.size.y, value =>
        {
            var size = Collider.size;
            size.y = value;
            Collider.size = size;
            DoUpdatePreview();
        }, -0.05f, 0.05f), "Size.Y");

        yield return Parent.CreateFloatSlider(_sizeZStorableFloat = new JSONStorableFloat("sizeZ", Collider.size.z, value =>
        {
            var size = Collider.size;
            size.z = value;
            Collider.size = size;
            DoUpdatePreview();
        }, -0.05f, 0.05f), "Size.Z");

        yield return Parent.CreateFloatSlider(_centerXStorableFloat = new JSONStorableFloat("centerX", Collider.center.x, value =>
        {
            var center = Collider.center;
            center.x = value;
            Collider.center = center;
            DoUpdatePreview();
        }, -0.05f, 0.05f), "Center.X");

        yield return Parent.CreateFloatSlider(_centerYStorableFloat = new JSONStorableFloat("centerY", Collider.center.y, value =>
        {
            var center = Collider.center;
            center.y = value;
            Collider.center = center;
            DoUpdatePreview();
        }, -0.05f, 0.05f), "Center.Y");

        yield return Parent.CreateFloatSlider(_centerZStorableFloat = new JSONStorableFloat("centerZ", Collider.center.z, value =>
        {
            var center = Collider.center;
            center.z = value;
            Collider.center = center;
            DoUpdatePreview();
        }, -0.05f, 0.05f), "Center.Z");
    }

    protected override bool DeviatesFromInitial() => InitialSize != Collider.size || InitialCenter != Collider.center; // Vector3 has built in epsilon equality checks
}

public static class ComponentExtensions
{
    public static string Uuid(this Component component)
    {
        var siblings = component.GetComponents<Component>().ToList();
        int siblingIndex = siblings.IndexOf(component);

        var paths = new Stack<string>(new[] {$"{component.name}[{siblingIndex}]"});
        var current = component.gameObject.transform;

        while (current != null && !current.name.Equals("geometry", StringComparison.InvariantCultureIgnoreCase)
                               && !current.name.Equals("Genesis2Female", StringComparison.InvariantCultureIgnoreCase)
                               && !current.name.Equals("Genesis2Male", StringComparison.InvariantCultureIgnoreCase))
        {
            paths.Push($"{current.name}[{current.GetSiblingIndex()}]");
            current = current.transform.parent;
        }

        return string.Join(".", paths.ToArray());
    }

    public static UIDynamic CreateFloatSlider(this MVRScript script, JSONStorableFloat jsf, string label, bool rightSide = true, string valueFormat = "F5")
    {
        var control = script.CreateSlider(jsf, rightSide);
        control.valueFormat = valueFormat;
        control.label = label;
        return control;
    }
}
