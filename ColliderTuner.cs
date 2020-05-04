using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using SimpleJSON;
using UnityEngine;
using Random = System.Random;

/// <summary>
///     Collider Tuner
///     By Acidbubbles
///     Disables colliders or adjust them to your liking
///     Source: https://github.com/acidbubbles/vam-collider-tuner
/// </summary>
public class ColliderTuner : MVRScript
{
    private const string _saveExt = "collidersprofile";

    private static readonly List<KeyValuePair<string, Regex>> _rbGroupDefinitions = new List<KeyValuePair<string, Regex>>
    {
        GroupDefinition("All", @"^.+$"),
        GroupDefinition("Head / Ears", @"^(head|lowerJaw|tongue|neck)"),
        GroupDefinition("Left arm", @"^l(Shldr|ForeArm)"),
        GroupDefinition("Left hand", @"^l(Index|Mid|Ring|Pinky|Thumb|Carpal|Hand)[0-9]?$"),
        GroupDefinition("Right arm", @"^r(Shldr|ForeArm)"),
        GroupDefinition("Right hand", @"^r(Index|Mid|Ring|Pinky|Thumb|Carpal|Hand)[0-9]?$"),
        GroupDefinition("Chest", @"^(chest|AutoColliderFemaleAutoColliderschest)"),
        GroupDefinition("Left breast", @"l((Pectoral)|Nipple)"),
        GroupDefinition("Right breast", @"r((Pectoral)|Nipple)"),
        GroupDefinition("Abdomen / Belly / Back", @"^(AutoColliderFemaleAutoColliders)?abdomen"),
        GroupDefinition("Hip / Pelvis", @"^(AutoCollider)?(hip|pelvis)"),
        GroupDefinition("Glute", @"^(AutoColliderFemaleAutoColliders)?[LR]Glute"),
        GroupDefinition("Anus", @"^_JointA[rl]"),
        GroupDefinition("Vagina", @"^_Joint(Gr|Gl|B)"),
        GroupDefinition("Penis", @"^(Gen[1-3])|Testes"),
        GroupDefinition("Left leg", @"^(AutoCollider(FemaleAutoColliders)?)?l(Thigh|Shin)"),
        GroupDefinition("Left foot", @"^l(Foot|Toe|BigToe|SmallToe)"),
        GroupDefinition("Right leg", @"^(AutoCollider(FemaleAutoColliders)?)?r(Thigh|Shin)"),
        GroupDefinition("Right foot", @"^r(Foot|Toe|BigToe|SmallToe)"),
        GroupDefinition("Other", @"^(?!.*).*$")
    };

    private readonly List<JSONStorableParam> _adjustmentStorables = new List<JSONStorableParam>();
    private readonly Dictionary<string, Collider> _colliders = new Dictionary<string, Collider>();
    private readonly Dictionary<string, Rigidbody> _rigidBodies = new Dictionary<string, Rigidbody>();
    private readonly Dictionary<string, List<string>> _rigidBodyColliderMap = new Dictionary<string, List<string>>();
    private readonly Dictionary<Rigidbody, string> _rigidBodyNameMap = new Dictionary<Rigidbody, string>();
    private Dictionary<string, GameObject> _colliderPreviews = new Dictionary<string, GameObject>();
    private JSONStorableStringChooser _collidersJson;
    private Atom _containingAtom;
    //private Material _deselectMaterial;
    private bool _filterStale;
    private string _lastBrowseDir = SuperController.singleton.savesDir;
    private JSONStorableStringChooser _rigidbodiesJson;
    private Dictionary<string, List<string>> _rigidBodyCategories;
    private JSONStorableStringChooser _rigidbodyGroupsJson;
    private string _selectedColliderName;
    private string _selectedRigidBodyName;
    private string _selectedRigidCategory;
    private JSONClass _state = new JSONClass();

    private static KeyValuePair<string, Regex> GroupDefinition(string label, string value) => new KeyValuePair<string, Regex>(label, new Regex(value, RegexOptions.Compiled | RegexOptions.ExplicitCapture));

    public override void Init()
    {
        try
        {
            pluginLabelJSON.val = "Collider Tuner v2.0.0(alpha)";

            if (containingAtom.type != "Person")
            {
                SuperController.LogError($"This plugin is for use with 'Person' atom only, not '{containingAtom.type}'");
                return;
            }

            _containingAtom = containingAtom;

            BuildLookups();

            var loadPresetUI = CreateButton("Load Preset");
            loadPresetUI.button.onClick.AddListener(() =>
            {
                if (_lastBrowseDir != null) SuperController.singleton.NormalizeMediaPath(_lastBrowseDir);
                SuperController.singleton.GetMediaPathDialog(path =>
                {
                    HandleLoadPreset(path);
                    _filterStale = true;
                }, _saveExt);
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

            CreateRigidBodyDisplays();
            CreateColliderDisplays();
        }
        catch (Exception e)
        {
            SuperController.LogError($"{nameof(ColliderTuner)}.{nameof(Init)}: {e}");
        }
    }

    private void HandleLoadPreset(string aPath)
    {
        if (string.IsNullOrEmpty(aPath))
            return;
        _lastBrowseDir = aPath.Substring(0, aPath.LastIndexOfAny(new[] {'/', '\\'})) + @"\";
        _state = (JSONClass) LoadJSON(aPath);
        RestoreFromState(false);
    }

    public void RestoreFromState(bool initial)
    {
        var colliders = _state.GetIfExists("colliders");
        if (colliders != null)
        {
            foreach (KeyValuePair<string, JSONNode> colliderEntry in colliders)
                if (_colliders.ContainsKey(colliderEntry.Key))
                {
                    RestoreColliderFromState(_colliders[colliderEntry.Key], (JSONClass) colliderEntry.Value, initial);
                    AdjustDisplayFromCollider(colliderEntry.Key);
                }
        }

        var rigidbodies = _state.GetIfExists("rigidbodies");
        if (rigidbodies != null)
        {
            foreach (KeyValuePair<string, JSONNode> rbEntry in rigidbodies)
                if (_rigidBodies.ContainsKey(rbEntry.Key))
                    RestoreRigidBodyFromState(_rigidBodies[rbEntry.Key], (JSONClass) rbEntry.Value, initial);
        }
    }

    private void RestoreRigidBodyFromState(Rigidbody rigidBody, JSONClass rbEntryValue, bool initial)
    {
        var enabledKey = "enabled" + (initial ? "Initial" : "");
        if (rbEntryValue.HasKey(enabledKey)) rigidBody.detectCollisions = rbEntryValue[enabledKey].AsBool;
    }

    private void RestoreColliderFromState(Collider collider, JSONClass colliderEntry, bool initial)
    {
        var suffix = initial ? "Initial" : "";
        if (collider is SphereCollider)
        {
            var sphereCollider = (SphereCollider) collider;
            if (colliderEntry.HasKey($"radius{suffix}"))
                sphereCollider.radius = colliderEntry[$"radius{suffix}"].AsFloat;

            var center = sphereCollider.center;
            if (colliderEntry.HasKey($"center.x{suffix}"))
                center.x = colliderEntry[$"center.x{suffix}"].AsFloat;
            if (colliderEntry.HasKey($"center.y{suffix}"))
                center.y = colliderEntry[$"center.y{suffix}"].AsFloat;
            if (colliderEntry.HasKey($"center.z{suffix}"))
                center.z = colliderEntry[$"center.z{suffix}"].AsFloat;
            sphereCollider.center = center;
        }
        else if (collider is CapsuleCollider)
        {
            var capsuleCollider = (CapsuleCollider) collider;
            if (colliderEntry.HasKey($"radius{suffix}"))
                capsuleCollider.radius = colliderEntry[$"radius{suffix}"].AsFloat;
            if (colliderEntry.HasKey($"height{suffix}"))
                capsuleCollider.height = colliderEntry[$"height{suffix}"].AsFloat;

            var center = capsuleCollider.center;
            if (colliderEntry.HasKey($"center.x{suffix}"))
                center.x = colliderEntry[$"center.x{suffix}"].AsFloat;
            if (colliderEntry.HasKey($"center.y{suffix}"))
                center.y = colliderEntry[$"center.y{suffix}"].AsFloat;
            if (colliderEntry.HasKey($"center.z{suffix}"))
                center.z = colliderEntry[$"center.z{suffix}"].AsFloat;
            capsuleCollider.center = center;
        }
        else if (collider is BoxCollider)
        {
            var boxCollider = (BoxCollider) collider;
            var size = boxCollider.size;
            if (colliderEntry.HasKey($"x{suffix}"))
                size.x = colliderEntry[$"x{suffix}"].AsFloat;
            if (colliderEntry.HasKey($"y{suffix}"))
                size.y = colliderEntry[$"y{suffix}"].AsFloat;
            if (colliderEntry.HasKey($"z{suffix}"))
                size.z = colliderEntry[$"z{suffix}"].AsFloat;
            boxCollider.size = size;

            var center = boxCollider.center;
            if (colliderEntry.HasKey($"center.x{suffix}"))
                center.x = colliderEntry[$"center.x{suffix}"].AsFloat;
            if (colliderEntry.HasKey($"center.y{suffix}"))
                center.y = colliderEntry[$"center.y{suffix}"].AsFloat;
            if (colliderEntry.HasKey($"center.z{suffix}"))
                center.z = colliderEntry[$"center.z{suffix}"].AsFloat;
            boxCollider.center = center;
        }
        else
        {
            SuperController.LogError($"Unknown collider {collider.name} type: {collider}");
        }
    }

    private void HandleSavePreset(string aPath)
    {
        if (string.IsNullOrEmpty(aPath))
            return;
        _lastBrowseDir = aPath.Substring(0, aPath.LastIndexOfAny(new[] {'/', '\\'})) + @"\";

        if (!aPath.ToLower().EndsWith($".{_saveExt}")) aPath += $".{_saveExt}";
        SaveJSON(_state, aPath);
    }

    private void CreateColliderDisplays()
    {
        _colliderPreviews = new Dictionary<string, GameObject>();
        foreach (KeyValuePair<string, Collider> keyValuePair in _colliders)
            _colliderPreviews.Add(keyValuePair.Key, CreateDisplayGameObject(keyValuePair.Value));
    }

    [SuppressMessage("ReSharper", "AccessToModifiedClosure")]
    private void CreateRigidBodyDisplays()
    {
        _rigidbodyGroupsJson = new JSONStorableStringChooser("Rigidbody Groups", _rigidBodyCategories.Keys.ToList(), "All", "Rigidbody Groups");

        UIDynamicPopup rbGroupListUI = CreateScrollablePopup(_rigidbodyGroupsJson);
        rbGroupListUI.popupPanelHeight = 900f;

        _rigidbodiesJson = new JSONStorableStringChooser("Rigidbodies", new List<string>(), "All", "Rigidbodies");
        UIDynamicPopup ridigBodyList = CreateScrollablePopup(_rigidbodiesJson);
        ridigBodyList.popupPanelHeight = 900f;

        _collidersJson = new JSONStorableStringChooser("Collider", new List<string>(), "", "Collider");

        UIDynamicPopup rbListUI = CreateScrollablePopup(_collidersJson);
        rbListUI.popupPanelHeight = 900f;

        _filterStale = true;

        _rigidbodyGroupsJson.setCallbackFunction = category =>
        {
            _selectedRigidCategory = category;
            _filterStale = true;
        };

        _rigidbodiesJson.setCallbackFunction = rigidbodyName =>
        {
            _selectedRigidBodyName = rigidbodyName;
            _filterStale = true;
        };

        _collidersJson.setCallbackFunction = colliderName =>
        {
            _selectedColliderName = colliderName;
            _filterStale = true;
        };
    }

    private void ApplyRigidBodyFilter()
    {
        try
        {
            foreach (var adjustmentJson in _adjustmentStorables)
            {
                if (adjustmentJson is JSONStorableFloat)
                    RemoveSlider((JSONStorableFloat) adjustmentJson);
                else if (adjustmentJson is JSONStorableBool)
                    RemoveToggle((JSONStorableBool) adjustmentJson);
                else
                    SuperController.LogError($"Unknown ui type for {adjustmentJson.name}: {adjustmentJson.GetType()}");
            }

            _adjustmentStorables.Clear();

            IEnumerable<string> rigidBodyNames;
            IEnumerable<string> colliderNames = new List<string>();

            bool filterByRigidbodyCategory = !string.IsNullOrEmpty(_selectedRigidCategory) && !_selectedRigidCategory.Equals("All", StringComparison.CurrentCultureIgnoreCase);
            bool filterByRigidBody = !string.IsNullOrEmpty(_selectedRigidBodyName) && !_selectedRigidBodyName.Equals("All", StringComparison.CurrentCultureIgnoreCase);

            // Rigidbody filtering

            if (filterByRigidbodyCategory)
                rigidBodyNames = new[] {"All"}.Concat(_rigidBodyCategories[_selectedRigidCategory]);
            else
                rigidBodyNames = new[] {"All"}.Concat(_rigidBodyCategories.SelectMany(x => x.Value));

            // Collider filtering

            if (filterByRigidBody)
            {
                if (_rigidBodyColliderMap.ContainsKey(_selectedRigidBodyName))
                {
                    colliderNames = _colliders.Keys
                        .Where(_rigidBodyColliderMap[_selectedRigidBodyName].Contains);
                }

                if (_rigidBodies.ContainsKey(_selectedRigidBodyName))
                {
                    CreateSpacer().height = 24f;
                    ShowRigidbodyAdjustments(_selectedRigidBodyName);
                }
                else
                {
                    SuperController.LogError($"Selected Rigidbody {_selectedRigidBodyName} not found in _rigidbodiesDictionary!");
                }
            }
            else if (filterByRigidbodyCategory)
            {
                colliderNames = _colliders.Keys
                    .Where(colliderName => rigidBodyNames
                        .Any(rb => _rigidBodyColliderMap.ContainsKey(rb)
                                   && _rigidBodyColliderMap[rb].Contains(colliderName)));
            }
            else
            {
                colliderNames = _colliders.Keys;
            }

            _rigidbodiesJson.choices = rigidBodyNames.Distinct().ToList();
            _rigidbodiesJson.val = !_rigidbodiesJson.choices.Contains(_selectedRigidBodyName) ? "All" : _selectedRigidBodyName;

            _collidersJson.choices = colliderNames.Distinct().ToList();
            _collidersJson.val = !_collidersJson.choices.Contains(_selectedColliderName)
                ? _collidersJson.choices.FirstOrDefault() ?? string.Empty : _selectedColliderName;

            if (_lastSelected != null)
            {
                var lastSelectedRenderer = _lastSelected.GetComponent<Renderer>();
                var color = lastSelectedRenderer.material.color;
                color.a = 0.005f;
                lastSelectedRenderer.material.color = color;
            }

            if (!string.IsNullOrEmpty(_selectedColliderName) && colliderNames.Contains(_selectedColliderName) && _colliderPreviews.ContainsKey(_selectedColliderName))
            {
                var preview = _colliderPreviews[_selectedColliderName];
                var previewRenderer = preview.GetComponent<Renderer>();
                var color = previewRenderer.material.color;
                color.a = 0.3f;
                previewRenderer.material.color = color;
                _lastSelected = preview;
            }

            if (!string.IsNullOrEmpty(_selectedColliderName))
                ShowColliderAdjustments(_selectedColliderName);
        }
        catch (Exception e)
        {
            SuperController.LogError($"{nameof(ColliderTuner)}: {e}");
        }
        finally
        {
            _filterStale = false;
        }
    }

    private GameObject _lastSelected;

    private IEnumerable<KeyValuePair<string, KeyValuePair<string,  Rigidbody>>> MemberOfGroup(KeyValuePair<string, Rigidbody> rigidbody)
    {
        bool matched = false;

        foreach (var g in _rbGroupDefinitions
            .Where(g => g.Value.IsMatch(rigidbody.Value.name)))
        {
            matched = true;
            yield return new KeyValuePair<string, KeyValuePair<string, Rigidbody>>(g.Key, rigidbody);
        }

        if (!matched)
            yield return new KeyValuePair<string, KeyValuePair<string, Rigidbody>>("Unknown", rigidbody);

        yield return new KeyValuePair<string, KeyValuePair<string, Rigidbody>>("All", rigidbody);
    }

    private void BuildLookups()
    {
        // Rigid Bodies

        var rigidBodyGroups = _containingAtom.rigidbodies
            .Where(rb => !rb.isKinematic &&
                         rb.name != "control" &&
                         rb.name != "object" &&
                         !rb.name.EndsWith("Control") &&
                         !rb.name.StartsWith("hairTool") &&
                         !rb.name.EndsWith("Trigger") &&
                         !rb.name.EndsWith("UI"))
            .GroupBy(x => x.name);

        foreach (var rigidBodyGroup in rigidBodyGroups)
        {
            var rbGroupMember = rigidBodyGroup.ToList();
            foreach (Rigidbody rigidBody in rbGroupMember)
            {
                string rigidBodyUniqueName = rbGroupMember.Count > 1
                    ? $"{rigidBody.name}_{rbGroupMember.IndexOf(rigidBody)}"
                    : rigidBody.name;

                _rigidBodies.Add(rigidBodyUniqueName, rigidBody);
                _rigidBodyNameMap.Add(rigidBody, rigidBodyUniqueName);
            }
        }

        _rigidBodyCategories = _rigidBodies.ToList()
            .SelectMany(MemberOfGroup)
            .GroupBy(x => x.Key)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.Value.Key)
                    .OrderBy(n => n)
                    .ToList()
            );

        // Colliders

        var colliderGroups = _containingAtom.GetComponentsInChildren<Collider>(true)
            .Where(collider => collider.name != "control" &&
                               collider.name != "object" &&
                               !collider.name.Contains("Tool") &&
                               !collider.name.EndsWith("Control") &&
                               !collider.name.EndsWith("Link") &&
                               !collider.name.EndsWith("Trigger") &&
                               !collider.name.EndsWith("UI"))
            .GroupBy(x => x.name);

        foreach (var cGroup in colliderGroups)
        {
            var cGroupMembers = cGroup.ToList();
            foreach (Collider collider in cGroupMembers)
            {
                string colliderUniqueName = cGroupMembers.Count > 1
                    ? $"{collider.name}_{cGroupMembers.IndexOf(collider)}"
                    : collider.name;

                _colliders.Add(colliderUniqueName, collider);

                if (collider.attachedRigidbody != null
                    && _rigidBodyNameMap.ContainsKey(collider.attachedRigidbody))
                {
                    string rigidBodyUniquename = _rigidBodyNameMap[collider.attachedRigidbody];

                    if (_rigidBodyColliderMap.ContainsKey(rigidBodyUniquename))
                        _rigidBodyColliderMap[rigidBodyUniquename].Add(colliderUniqueName);
                    else
                        _rigidBodyColliderMap.Add(rigidBodyUniquename, new List<string> {colliderUniqueName});
                }
            }
        }
    }

    private void ShowColliderAdjustments(string colliderName)
    {
        Collider collider = _colliders[colliderName];

        Func<string, float?> getInitial = prop =>
        {
            // TODO: Default does not work here.
            var jc = _state.GetIfExists("colliders")?.GetIfExists(colliderName);
            if (jc == null) return null;
            if (!jc.HasKey($"{prop}Initial")) return null;
            var val = jc[$"{prop}Initial"].AsFloat;
            return val;
        };

        Func<JSONClass> getJsonNode = () => _state.GetOrCreate("colliders").GetOrCreate(colliderName);

        var sphereCollider = collider as SphereCollider;
        if (sphereCollider != null)
        {
            CreateFloatAdjustment(colliderName, getJsonNode, "radius", getInitial("radius"), sphereCollider.radius, val => sphereCollider.radius = val);
            CreateFloatAdjustment(colliderName, getJsonNode, "center.x", getInitial("center.x"), sphereCollider.center.x, val => sphereCollider.center = new Vector3(val, sphereCollider.center.y, sphereCollider.center.z), true, -0.05f, 0.05f);
            CreateFloatAdjustment(colliderName, getJsonNode, "center.y", getInitial("center.y"), sphereCollider.center.y, val => sphereCollider.center = new Vector3(sphereCollider.center.x, val, sphereCollider.center.z), true, -0.05f, 0.05f);
            CreateFloatAdjustment(colliderName, getJsonNode, "center.z", getInitial("center.z"), sphereCollider.center.z, val => sphereCollider.center = new Vector3(sphereCollider.center.x, sphereCollider.center.y, val), true, -0.05f, 0.05f);
        }

        var capsuleCollider = collider as CapsuleCollider;
        if (capsuleCollider != null)
        {
            CreateFloatAdjustment(colliderName, getJsonNode, "radius", getInitial("radius"), capsuleCollider.radius, val => capsuleCollider.radius = val);
            CreateFloatAdjustment(colliderName, getJsonNode, "height", getInitial("height"), capsuleCollider.height, val => capsuleCollider.height = val);
            CreateFloatAdjustment(colliderName, getJsonNode, "center.x", getInitial("center.x"), capsuleCollider.center.x, val => capsuleCollider.center = new Vector3(val, capsuleCollider.center.y, capsuleCollider.center.z), true, -0.05f, 0.05f);
            CreateFloatAdjustment(colliderName, getJsonNode, "center.y", getInitial("center.y"), capsuleCollider.center.y, val => capsuleCollider.center = new Vector3(capsuleCollider.center.x, val, capsuleCollider.center.z), true, -0.05f, 0.05f);
            CreateFloatAdjustment(colliderName, getJsonNode, "center.z", getInitial("center.z"), capsuleCollider.center.z, val => capsuleCollider.center = new Vector3(capsuleCollider.center.x, capsuleCollider.center.y, val), true, -0.05f, 0.05f);
        }

        var boxCollider = collider as BoxCollider;
        if (boxCollider != null)
        {
            CreateFloatAdjustment(colliderName, getJsonNode, "size.x", getInitial("size.x"), boxCollider.size.x, val => boxCollider.size = new Vector3(val, boxCollider.size.y, boxCollider.size.z));
            CreateFloatAdjustment(colliderName, getJsonNode, "size.y", getInitial("size.y"), boxCollider.size.y, val => boxCollider.size = new Vector3(boxCollider.size.x, val, boxCollider.size.z));
            CreateFloatAdjustment(colliderName, getJsonNode, "size.z", getInitial("size.z"), boxCollider.size.z, val => boxCollider.size = new Vector3(boxCollider.size.x, boxCollider.size.y, val));
            CreateFloatAdjustment(colliderName, getJsonNode, "center.x", getInitial("center.x"), boxCollider.center.x, val => boxCollider.center = new Vector3(val, boxCollider.center.y, boxCollider.center.z), true, -0.05f, 0.05f);
            CreateFloatAdjustment(colliderName, getJsonNode, "center.y", getInitial("center.y"), boxCollider.center.y, val => boxCollider.center = new Vector3(boxCollider.center.x, val, boxCollider.center.z), true, -0.05f, 0.05f);
            CreateFloatAdjustment(colliderName, getJsonNode, "center.z", getInitial("center.z"), boxCollider.center.z, val => boxCollider.center = new Vector3(boxCollider.center.x, boxCollider.center.y, val), true, -0.05f, 0.05f);
        }
    }

    private void ShowRigidbodyAdjustments(string rigidbodyName)
    {
        Func<JSONClass> getJsonNode = () => _state.GetOrCreate("rigidbodies").GetOrCreate(rigidbodyName);

        var jsonNode = getJsonNode();
        var rigidBody = _rigidBodies[rigidbodyName];

        bool? initial = jsonNode.HasKey("enabled") ? (bool?) jsonNode["enabled"].AsBool : null;

        CreateBoolAdjustment(getJsonNode, "enabled", initial, rigidBody.detectCollisions, val => { rigidBody.detectCollisions = val; });
    }
    
    private void DestroyColliderPreviews()
    {
        foreach (var rbDisplay in _colliderPreviews) Destroy(rbDisplay.Value);
    }

    public GameObject CreateDisplayGameObject(Collider collider)
    {
        GameObject primitive = null;

        if (collider is SphereCollider)
            primitive = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        else if (collider is CapsuleCollider)
            primitive = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        else if (collider is BoxCollider) primitive = GameObject.CreatePrimitive(PrimitiveType.Cube);

        if (primitive == null)
            primitive = GameObject.CreatePrimitive(PrimitiveType.Capsule);

        try
        {
            primitive.GetComponent<Renderer>().material = GetNextMaterial();
            foreach (var c in primitive.GetComponents<Collider>())
            {
                c.enabled = false;
                Destroy(c);
            }

            primitive.transform.SetParent(collider.transform, false);
            AdjustDisplayFromCollider(collider, primitive);
        }
        catch (Exception)
        {
            Destroy(primitive);
            throw;
        }

        return primitive;
    }

    private void AdjustDisplayFromCollider(string colliderName)
    {
        if (_colliderPreviews.ContainsKey(colliderName) && _colliders.ContainsKey(colliderName))
        {
            SuperController.LogMessage($"AdjustDisplayFromCollider({colliderName})");
            AdjustDisplayFromCollider(_colliders[colliderName], _colliderPreviews[colliderName]);
        }
    }

    private void AdjustDisplayFromCollider(Collider collider, GameObject go)
    {
        SphereCollider sphereCollider = collider as SphereCollider;
        if (sphereCollider != null)
        {
            go.transform.localScale = Vector3.one * (sphereCollider.radius * 2);
            go.transform.localPosition = sphereCollider.center;
            return;
        }

        var capsuleCollider = collider as CapsuleCollider;
        if (capsuleCollider != null)
        {
            float size = capsuleCollider.radius * 2;
            float height = capsuleCollider.height / 2;
            go.transform.localScale = new Vector3(size, height, size);
            if (capsuleCollider.direction == 0)
                go.transform.localRotation = Quaternion.AngleAxis(90, Vector3.forward);
            else if (capsuleCollider.direction == 2)
                go.transform.localRotation = Quaternion.AngleAxis(90, Vector3.right);
            go.transform.localPosition = capsuleCollider.center;
            return;
        }

        var boxCollider = collider as BoxCollider;
        if (boxCollider != null)
        {
            go.transform.localScale = boxCollider.size;
            go.transform.localPosition = boxCollider.center;
            return;
        }

        SuperController.LogError($"Unknown collider {collider.name} type: {collider}");
    }

    private JSONStorableFloat CreateFloatAdjustment(string colliderName, Func<JSONClass> getJsonNode, string propertyName, float? initial, float current, Action<float> setValue, bool allowSmallerThanMinimum = false, float min = 0.0002f, float? max = null)
    {
        var collider = _colliders[colliderName];
        var defaultVal = initial ?? current;
        JSONStorableFloat storable = null;
        storable = new JSONStorableFloat(
            propertyName,
            defaultVal,
            val =>
            {
                if (!allowSmallerThanMinimum && val < min)
                {
                    val = min;
                    storable.valNoCallback = min;
                }

                var originalPropertyName = $"{propertyName}Initial";
                var jc = getJsonNode();
                if (!jc.HasKey(originalPropertyName)) jc[originalPropertyName].AsFloat = defaultVal;
                jc[propertyName].AsFloat = val;
                setValue(val);
                if (_colliderPreviews.ContainsKey(colliderName))
                {
                    var preview = _colliderPreviews[colliderName];
                    AdjustDisplayFromCollider(collider, preview);
                }
            },
            min,
            max ?? defaultVal * 2f,
            false)
        {
            valNoCallback = current
        };
        CreateFineSlider(storable, true);
        _adjustmentStorables.Add(storable);

        return storable;
    }

    public UIDynamicSlider CreateFineSlider(JSONStorableFloat jsf, bool rightSide)
    {
        var slider = CreateSlider(jsf, rightSide);
        slider.valueFormat = "F5";
        return slider;
    }

    private void CreateBoolAdjustment(Func<JSONClass> getJsonNode, string propertyName, bool? initial, bool current, Action<bool> setValue)
    {
        var defaultVal = initial ?? current;
        var storable = new JSONStorableBool(
            $"{propertyName}",
            defaultVal,
            val =>
            {
                var originalPropertyName = $"{propertyName}Initial";
                var jc = getJsonNode();
                if (!jc.HasKey(originalPropertyName)) jc[originalPropertyName].AsBool = defaultVal;
                jc[propertyName].AsBool = val;
                setValue(val);
            })
        {
            valNoCallback = current
        };
        CreateToggle(storable, true);
        _adjustmentStorables.Add(storable);
    }

    public void OnEnable()
    {
        if (_containingAtom == null) return;
        try
        {
            RestoreFromState(false);
            _filterStale = true;
        }
        catch (Exception e)
        {
            SuperController.LogError($"{nameof(ColliderTuner)}.{nameof(OnEnable)}: {e}");
        }
    }

    public void OnDisable()
    {
        if (_containingAtom == null) return;
        try
        {
            DestroyColliderPreviews();
        }
        catch (Exception e)
        {
            SuperController.LogError($"{nameof(ColliderTuner)}.{nameof(OnDisable)}: {e}");
        }
    }

    public void OnDestroy()
    {
        if (_containingAtom == null) return;
        try
        {
            DestroyColliderPreviews();
        }
        catch (Exception e)
        {
            SuperController.LogError($"{nameof(ColliderTuner)}.{nameof(OnDestroy)}: {e}");
        }
    }

    public override JSONClass GetJSON(bool includePhysical = true, bool includeAppearance = true, bool forceStore = false)
    {
        needsStore = true;
        return _state;
    }

    public override void RestoreFromJSON(JSONClass jc, bool restorePhysical = true, bool restoreAppearance = true, JSONArray presetAtoms = null, bool setMissingToDefault = true)
    {
        base.RestoreFromJSON(jc, restorePhysical, restoreAppearance, presetAtoms, setMissingToDefault);

        try
        {
            _state = jc;
            RestoreFromState(false);
        }
        catch (Exception exc)
        {
            SuperController.LogError($"{nameof(ColliderTuner)}.{nameof(RestoreFromJSON)}: {exc}");
        }
    }

    public override void LateRestoreFromJSON(JSONClass jc, bool restorePhysical = true, bool restoreAppearance = true, bool setMissingToDefault = true)
    {
        RestoreFromJSON(jc, restorePhysical, restoreAppearance, null, setMissingToDefault);
    }

    public override void PostRestore()
    {
        RestoreFromState(false);
    }

    private void Update()
    {
        if (_filterStale) ApplyRigidBodyFilter();
    }

    private Material CreateMaterial(Color color)
    {
        var material = new Material(Shader.Find("Battlehub/RTGizmos/Handles")) {color = color};

        material.SetFloat("_Offset", 1f);
        material.SetFloat("_MinAlpha", 1f);

        return material;
    }

    private Queue<Material> _materials;
    public Material GetNextMaterial()
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

            _materials = new Queue<Material>(materials.OrderBy(x => UnityEngine.Random.Range(-1, 2)));
        }

        Material current;
        _materials.Enqueue((current = _materials.Dequeue()));
        return current;
    }
}

public static class JSONNodeExtensions
{
    public static JSONClass GetOrCreate(this JSONClass jc, string propertyName)
    {
        if (jc.HasKey(propertyName))
            return (JSONClass) jc[propertyName];

        var child = new JSONClass();
        jc.Add(propertyName, child);
        return child;
    }

    public static JSONClass GetIfExists(this JSONClass jc, string propertyName)
    {
        if (!jc.HasKey(propertyName))
            return null;

        return (JSONClass) jc[propertyName];
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
