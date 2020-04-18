using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SimpleJSON;
using UnityEngine;

/// <summary>
/// Collider Tuner
/// By Acidbubbles
/// Disables colliders or adjust them to your liking
/// Source: https://github.com/acidbubbles/vam-collider-tuner
/// </summary>
public class ColliderTuner : MVRScript
{
    private const string _saveExt = "collidersprofile";
    private Dictionary<Collider, GameObject> _collidersDisplayMap;
    private Dictionary<string, Rigidbody> _rigidbodiesNameMap;
    private Dictionary<Rigidbody, List<Collider>> _rigidbodyCollidersMap;
    private Atom _containingAtom;
    private Material _selectedMaterial;
    private Material _deselectMaterial;
    private Rigidbody _selectedRigidbody;
    private JSONStorableBool _displayJSON;
    private JSONClass _state = new JSONClass();
    private string _lastBrowseDir;
    private readonly List<JSONStorableParam> _adjustmentStorables = new List<JSONStorableParam>();
    private readonly List<UIDynamicButton> _adjustmentButtons = new List<UIDynamicButton>();

    #region Lifecycle

    public override void Init()
    {
        try
        {
            var jc = new JSONClass();
            _containingAtom = containingAtom;

            _selectedMaterial = CreateMaterial(new Color(0f, 1f, 0f, 0.05f));
            _deselectMaterial = CreateMaterial(new Color(1f, 0f, 0f, 0.05f));

            _displayJSON = new JSONStorableBool("Display Rigidbodies", false, (bool val) =>
            {
                if (val) CreateColliderDisplays();
                else DestroyColliderDisplays();
            })
            { isStorable = false };
            CreateToggle(_displayJSON, false);

            CreateSpacer().height = 30f;

            _rigidbodiesNameMap = new Dictionary<string, Rigidbody>();
            foreach (var rb in GetRigidBodies())
            {
                var simplified = Simplify(rb.name);
                var name = simplified;
                var counter = 0;
                while (_rigidbodiesNameMap.ContainsKey(name))
                {
                    name = $"{simplified} ({++counter})";
                }
                _rigidbodiesNameMap.Add(name, rb);
            }

            var groups = _rigidbodiesNameMap.Keys.GroupBy(k => GroupOf(k)).ToDictionary(g => g.Key, g => g.OrderBy(n => n).ToList());
            groups.Add("", new List<string>());
            var rbListJSON = new JSONStorableStringChooser("Collider", new List<string>(), "", "Collider", (string val) => ShowColliderAdjustments(val));
            var rbGroupListJSON = new JSONStorableStringChooser("Colliders Groups", groups.Keys.OrderBy(k => k).ToList(), "", "Collider Groups", (string val) => { rbListJSON.choices = groups[val]; rbListJSON.val = ""; });

            var rbGroupListUI = CreateScrollablePopup(rbGroupListJSON, false);
            rbGroupListUI.popupPanelHeight = 900f;

            var rbListUI = CreateScrollablePopup(rbListJSON, false);
            rbGroupListUI.popupPanelHeight = 900f;

            CreateSpacer().height = 30f;

            var reapplyUI = CreateButton("Reapply", false);
            reapplyUI.button.onClick.AddListener(() =>
            {
                RestoreFromState(false);
                if (_collidersDisplayMap != null)
                {
                    DestroyColliderDisplays();
                    CreateColliderDisplays();
                }
            });

            var resetAllUI = CreateButton("Reset All", false);
            resetAllUI.button.onClick.AddListener(() =>
            {
                rbListJSON.val = "";
                rbGroupListJSON.val = "";
                RestoreFromState(true);
                if (_collidersDisplayMap != null)
                {
                    DestroyColliderDisplays();
                    CreateColliderDisplays();
                }
            });

            CreateSpacer().height = 30f;

            var loadPresetUI = CreateButton("Load Preset", false);
            loadPresetUI.button.onClick.AddListener(() =>
            {
                if (_lastBrowseDir != null) SuperController.singleton.NormalizeMediaPath(_lastBrowseDir);
                SuperController.singleton.GetMediaPathDialog((string path) =>
                {
                    HandleLoadPreset(path);
                    rbListJSON.val = "";
                    rbGroupListJSON.val = "";
                    if (_collidersDisplayMap != null)
                    {
                        DestroyColliderDisplays();
                        CreateColliderDisplays();
                    }
                }, _saveExt);
            });

            var savePresetUI = CreateButton("Save Preset", false);
            savePresetUI.button.onClick.AddListener(() =>
            {
                SuperController.singleton.NormalizeMediaPath(_lastBrowseDir);
                SuperController.singleton.GetMediaPathDialog(HandleSavePreset, _saveExt);

                var browser = SuperController.singleton.mediaFileBrowserUI;
                browser.SetTextEntry(true);
                browser.fileEntryField.text = string.Format("{0}.{1}", ((int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds).ToString(), _saveExt);
                browser.ActivateFileNameField();
            });
        }
        catch (Exception e)
        {
            SuperController.LogError($"{nameof(ColliderTuner)}.{nameof(Init)}: {e}");
        }
    }

    private readonly Regex _handNameRegex = new Regex(@"^(l|r)(Index|Mid|Ring|Pinky|Thumb|Carpal|Hand)[0-9]?$", RegexOptions.Compiled | RegexOptions.ExplicitCapture);

    private string GroupOf(string name)
    {
        if (name.StartsWith("tongue")) return "Tongue";
        if (_handNameRegex.IsMatch(name))
            return name.StartsWith("r") ? "Right Hand" : "Left Hand";

        return "Other";
    }

    private readonly string[] _prefixes = new[]
    {
        "AutoColliderFemaleAutoColliders",
        "AutoCollider"
    };

    private string Simplify(string name)
    {
        foreach (var prefix in _prefixes)
        {
            if (name.StartsWith(prefix))
            {
                name = name.Substring(prefix.Length);
                break;
            }
        }
        return name;
    }

    private void ShowColliderAdjustments(string name)
    {
        foreach (var adjustmentJSON in _adjustmentStorables)
        {
            if (adjustmentJSON is JSONStorableFloat)
                RemoveSlider((JSONStorableFloat)adjustmentJSON);
            else if (adjustmentJSON is JSONStorableBool)
                RemoveToggle((JSONStorableBool)adjustmentJSON);
            else
                SuperController.LogError($"Unknown ui type for {adjustmentJSON.name}: {adjustmentJSON.GetType()}");
        }
        _adjustmentStorables.Clear();

        foreach (var adjustmentButton in _adjustmentButtons)
        {
            RemoveButton(adjustmentButton);
        }
        _adjustmentButtons.Clear();

        if (_selectedRigidbody != null && _collidersDisplayMap != null)
        {
            foreach (var collider in _rigidbodyCollidersMap[_selectedRigidbody])
                _collidersDisplayMap[collider].GetComponent<Renderer>().material = _deselectMaterial;
        }
        _selectedRigidbody = null;

        if (name == "") return;
        var rb = _rigidbodiesNameMap[name];
        _selectedRigidbody = rb;

        var resetButton = CreateButton("Reset", true);
        resetButton.button.onClick.AddListener(() =>
        {
            foreach (var adjustmentJSON in _adjustmentStorables)
            {
                if (adjustmentJSON is JSONStorableFloat)
                    ((JSONStorableFloat)adjustmentJSON).SetValToDefault();
                else if (adjustmentJSON is JSONStorableBool)
                    ((JSONStorableBool)adjustmentJSON).SetValToDefault();
                else
                    SuperController.LogError($"Unknown ui type for {adjustmentJSON.name}: {adjustmentJSON.GetType()}");
            }
            _state.Remove(rb.name);
        });
        _adjustmentButtons.Add(resetButton);

        CreateBoolAdjustment(() => _state.GetOrCreate(rb.name), "enabled", (_state.HasKey(rb.name) && _state[rb.name].AsObject.HasKey("enabled")) ? (bool?)_state[rb.name]["enabled"].AsBool : null, rb.detectCollisions, val =>
        {
            rb.detectCollisions = val;

            if (_collidersDisplayMap != null)
            {
                foreach (var collider in _rigidbodyCollidersMap[_selectedRigidbody])
                    _collidersDisplayMap[collider].SetActive(val);
            }
        });

        var colliders = _rigidbodyCollidersMap[rb];
        for (var colliderIndex = 0; colliderIndex < colliders.Count; colliderIndex++)
        {
            var collider = colliders[colliderIndex];

            if (_collidersDisplayMap != null)
                _collidersDisplayMap[collider].GetComponent<Renderer>().material = _selectedMaterial;

            var colliderUniqueName = $"{collider.name}:{colliderIndex}";
            Func<string, float?> getInitial = (string prop) =>
            {
                // TODO: Default does not work here.
                var jc = _state.GetIfExists(rb.name)?.GetIfExists("colliders")?.GetIfExists(colliderUniqueName);
                if (jc == null) return null;
                if (!jc.HasKey($"{prop}Initial")) return null;
                var val = jc[$"{prop}Initial"].AsFloat;
                return val;
            };
            Func<JSONClass> getJsonNode = () => _state.GetOrCreate(rb.name).GetOrCreate("colliders").GetOrCreate(colliderUniqueName);

            if (collider is SphereCollider)
            {
                var sphereCollider = (SphereCollider)collider;
                CreateFloatAdjustment(collider, getJsonNode, "radius", getInitial("radius"), sphereCollider.radius, val => sphereCollider.radius = val);
            }
            else if (collider is CapsuleCollider)
            {
                var capsuleCollider = (CapsuleCollider)collider;
                CreateFloatAdjustment(collider, getJsonNode, "radius", getInitial("radius"), capsuleCollider.radius, val => capsuleCollider.radius = val);
                CreateFloatAdjustment(collider, getJsonNode, "height", getInitial("height"), capsuleCollider.height, val => capsuleCollider.height = val);
            }
            else if (collider is BoxCollider)
            {
                var boxCollider = (BoxCollider)collider;
                CreateFloatAdjustment(collider, getJsonNode, "x", getInitial("x"), boxCollider.size.x, val => new Vector3(val, boxCollider.size.y, boxCollider.size.z));
                CreateFloatAdjustment(collider, getJsonNode, "y", getInitial("y"), boxCollider.size.y, val => new Vector3(boxCollider.size.x, val, boxCollider.size.z));
                CreateFloatAdjustment(collider, getJsonNode, "z", getInitial("z"), boxCollider.size.z, val => new Vector3(boxCollider.size.x, boxCollider.size.y, val));
            }
            else
            {
                SuperController.LogError($"Unknown collider {rb.name}/{collider.name} type: {collider}");
            }
        }
    }

    private void CreateFloatAdjustment(Collider collider, Func<JSONClass> getJsonNode, string propertyName, float? initial, float current, Action<float> setValue, float min = 0.00001f, float max = 0.2f)
    {
        var colliderName = Simplify(collider.name);
        var defaultVal = initial ?? current;
        var storable = new JSONStorableFloat(
            $"{colliderName}/{propertyName}",
            defaultVal,
            (float val) =>
            {
                var originalPropertyName = $"{propertyName}Initial";
                var jc = getJsonNode();
                if (!jc.HasKey(originalPropertyName)) jc[originalPropertyName].AsFloat = defaultVal;
                jc[propertyName].AsFloat = val;
                setValue(val);
                if (_collidersDisplayMap != null)
                    AdjustDisplayFromCollider(collider, _collidersDisplayMap[collider]);
            },
            min,
            max,
            false)
        {
            valNoCallback = current
        };
        CreateFineSlider(storable, true);
        _adjustmentStorables.Add(storable);
    }

    private void CreateBoolAdjustment(Func<JSONClass> getJsonNode, string propertyName, bool? initial, bool current, Action<bool> setValue)
    {
        var defaultVal = initial ?? current;
        var storable = new JSONStorableBool(
            $"{propertyName}",
            defaultVal,
            (bool val) =>
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

            if (_displayJSON.val && _collidersDisplayMap == null)
                CreateColliderDisplays();
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
            DestroyColliderDisplays();
            RestoreFromState(true);
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
            DestroyColliderDisplays();
            RestoreFromState(true);
        }
        catch (Exception e)
        {
            SuperController.LogError($"{nameof(ColliderTuner)}.{nameof(OnDestroy)}: {e}");
        }
    }

    #endregion

    #region Load / Save

    public override JSONClass GetJSON(bool includePhysical = true, bool includeAppearance = true, bool forceStore = false)
    {
        var json = base.GetJSON(includePhysical, includeAppearance, forceStore);
        json["rigidbodies"] = _state;
        needsStore = true;
        return json;
    }

    public override void RestoreFromJSON(JSONClass jc, bool restorePhysical = true, bool restoreAppearance = true, JSONArray presetAtoms = null, bool setMissingToDefault = true)
    {
        base.RestoreFromJSON(jc, restorePhysical, restoreAppearance, presetAtoms, setMissingToDefault);

        try
        {
            _state = jc.HasKey("rigidbodies") ? (JSONClass)jc["rigidbodies"] : new JSONClass();
            RestoreFromState(false);
        }
        catch (Exception exc)
        {
            SuperController.LogError($"{nameof(ColliderTuner)}.{nameof(RestoreFromJSON)}: {exc}");
        }
    }

    public override void LateRestoreFromJSON(JSONClass jc, bool restorePhysical = true, bool restoreAppearance = true, bool setMissingToDefault = true)
     => RestoreFromJSON(jc, restorePhysical, restoreAppearance, null, setMissingToDefault);

    public override void PostRestore()
    {
        RestoreFromState(false);
    }

    public void RestoreFromState(bool initial)
    {
        foreach (KeyValuePair<string, JSONNode> rbEntry in _state)
        {
            var rb = _containingAtom.rigidbodies.FirstOrDefault(x => x.name == rbEntry.Key);
            if (rb == null)
            {
                SuperController.LogError($"Could not find rigidbody '{rbEntry.Key}' specified in save");
                continue;
            }

            var rbJC = (JSONClass)rbEntry.Value;
            RestoreRigidBodyFromState(rb, rbJC, initial);
        }
    }

    private void RestoreRigidBodyFromState(Rigidbody rb, JSONClass rbJC, bool initial)
    {
        var enabledKey = "enabled" + (initial ? "" : "Initial");
        if (rbJC.HasKey(enabledKey))
        {
            rb.detectCollisions = rbJC[enabledKey].AsBool;
        }

        var colliders = _rigidbodyCollidersMap[rb];
        foreach (KeyValuePair<string, JSONNode> colliderEntry in rbJC["colliders"].AsObject)
        {
            var colliderUniqueName = colliderEntry.Key.Split(':');
            if (colliderUniqueName.Length != 2)
            {
                SuperController.LogError($"Invalid collider unique name '{colliderEntry.Key}' of rigidbody '{rb.name}' specified in save");
                continue;
            }
            var colliderName = colliderUniqueName[0];
            var colliderIndex = int.Parse(colliderUniqueName[1]);
            if (colliders.Count <= colliderIndex)
            {
                SuperController.LogError($"Could not find collider '{colliderName}' at index {colliderIndex} of rigidbody '{rb.name}' specified in save (not enough colliders)");
                continue;
            }
            var collider = colliders[colliderIndex];
            if (collider.name != colliderName)
            {
                SuperController.LogError($"Could not find collider '{colliderName}' at index {colliderIndex} of rigidbody '{rb.name}' specified in save (found '{collider.name}' at this index)");
                continue;
            }

            RestoreColliderFromState(rb, collider, (JSONClass)colliderEntry.Value, initial);
        }
    }

    private void RestoreColliderFromState(Rigidbody rb, Collider collider, JSONClass jc, bool initial)
    {
        var suffix = initial ? "Initial" : "";
        if (collider is SphereCollider)
        {
            var sphereCollider = (SphereCollider)collider;
            if (jc.HasKey($"radius{suffix}"))
                sphereCollider.radius = jc[$"radius{suffix}"].AsFloat;
        }
        else if (collider is CapsuleCollider)
        {
            var capsuleCollider = (CapsuleCollider)collider;
            if (jc.HasKey($"radius{suffix}"))
                capsuleCollider.radius = jc[$"radius{suffix}"].AsFloat;
            if (jc.HasKey($"height{suffix}"))
                capsuleCollider.height = jc[$"height{suffix}"].AsFloat;
        }
        else if (collider is BoxCollider)
        {
            var boxCollider = (BoxCollider)collider;
            var size = boxCollider.size;
            if (jc.HasKey($"x{suffix}"))
                size.x = jc[$"x{suffix}"].AsFloat;
            if (jc.HasKey($"y{suffix}"))
                size.x = jc[$"y{suffix}"].AsFloat;
            if (jc.HasKey($"z{suffix}"))
                size.x = jc[$"z{suffix}"].AsFloat;
            boxCollider.size = size;
        }
        else
        {
            SuperController.LogError($"Unknown collider {rb.name}/{collider.name}/ type: {collider}");
        }
    }

    void HandleLoadPreset(string aPath)
    {
        if (string.IsNullOrEmpty(aPath))
            return;
        _lastBrowseDir = aPath.Substring(0, aPath.LastIndexOfAny(new char[] { '/', '\\' })) + @"\";
        RestoreFromState(true);
        _state = (JSONClass)LoadJSON(aPath);
        RestoreFromState(false);
    }

    void HandleSavePreset(string aPath)
    {
        if (string.IsNullOrEmpty(aPath))
            return;
        _lastBrowseDir = aPath.Substring(0, aPath.LastIndexOfAny(new char[] { '/', '\\' })) + @"\";

        if (!aPath.ToLower().EndsWith($".{_saveExt}"))
        {
            aPath += $".{_saveExt}";
        }
        SaveJSON(_state, aPath);
    }

    #endregion

    #region Display

    private static Material CreateMaterial(Color color)
    {
        var material = new Material(Shader.Find("Standard")) { color = color };
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 3000;
        return material;
    }

    private void CreateColliderDisplays()
    {
        DestroyColliderDisplays();
        _collidersDisplayMap = new Dictionary<Collider, GameObject>();
        foreach (var rb in GetRigidBodies())
        {
            foreach (var collider in _rigidbodyCollidersMap[rb])
            {
                var rbDisplay = CreateDisplayGameObject(collider, rb == _selectedRigidbody);
                rbDisplay.SetActive(rb.detectCollisions);
                try
                {
                    _collidersDisplayMap.Add(collider, rbDisplay);
                }
                catch (ArgumentException exc)
                {
                    SuperController.LogError($"Cannot add '{rb.name}': {exc.Message}");
                }
            }
        }
    }

    private void DestroyColliderDisplays()
    {
        if (_collidersDisplayMap == null) return;
        foreach (var rbDisplay in _collidersDisplayMap)
        {
            Destroy(rbDisplay.Value);
        }
        _collidersDisplayMap = null;
    }

    public GameObject CreateDisplayGameObject(Collider collider, bool selected)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        try
        {
            go.GetComponent<Renderer>().material = selected ? _selectedMaterial : _deselectMaterial;
            foreach (var c in go.GetComponents<Collider>()) { c.enabled = false; Destroy(c); }
            go.transform.SetParent(collider.transform, false);
            AdjustDisplayFromCollider(collider, go);
        }
        catch (Exception)
        {
            Destroy(go);
            throw;
        }
        return go;
    }

    private static void AdjustDisplayFromCollider(Collider collider, GameObject go)
    {
        if (collider is SphereCollider)
        {
            var sphereCollider = (SphereCollider)collider;
            go.transform.localScale = Vector3.one * (sphereCollider.radius * 2);
            go.transform.localPosition = sphereCollider.center;
        }
        else if (collider is CapsuleCollider)
        {
            var capsuleCollider = (CapsuleCollider)collider;
            float size = capsuleCollider.radius * 2;
            float height = capsuleCollider.height / 2;
            go.transform.localScale = new Vector3(size, height, size);
            if (capsuleCollider.direction == 0)
                go.transform.localRotation = Quaternion.AngleAxis(90, Vector3.forward);
            else if (capsuleCollider.direction == 2)
                go.transform.localRotation = Quaternion.AngleAxis(90, Vector3.right);
            go.transform.localPosition = capsuleCollider.center;
        }
        else if (collider is BoxCollider)
        {
            var boxCollider = (BoxCollider)collider;
            go.transform.localScale = boxCollider.size;
            go.transform.localPosition = boxCollider.center;
        }
        else
        {
            SuperController.LogError($"Unknown collider {collider.name} type: {collider}");
        }
    }

    #endregion

    #region UI

    private IEnumerable<Rigidbody> GetRigidBodies()
    {
        if (_rigidbodyCollidersMap == null)
        {
            _rigidbodyCollidersMap = new Dictionary<Rigidbody, List<Collider>>();
            foreach (var collider in _containingAtom.GetComponentsInChildren<Collider>())
            {
                if (collider.attachedRigidbody == null) continue;

                List<Collider> rbColliders;
                if (!_rigidbodyCollidersMap.TryGetValue(collider.attachedRigidbody, out rbColliders))
                {
                    rbColliders = new List<Collider>();
                    _rigidbodyCollidersMap.Add(collider.attachedRigidbody, rbColliders);
                }
                rbColliders.Add(collider);
            }
        }

        foreach (var rb in _containingAtom.rigidbodies)
        {
            if (rb.isKinematic) continue;
            if (rb.name == "control") continue;
            if (rb.name == "object") continue;
            if (!_rigidbodyCollidersMap.ContainsKey(rb)) continue;
            if (rb.name.EndsWith("Control")) continue;
            if (rb.name.StartsWith("hairTool")) continue;
            if (rb.name.EndsWith("Trigger")) continue;
            if (rb.name.EndsWith("UI")) continue;
            yield return rb;
        }
    }

    public UIDynamicSlider CreateFineSlider(JSONStorableFloat jsf, bool rightSide)
    {
        var slider = CreateSlider(jsf, rightSide);
        slider.valueFormat = "F5";
        return slider;
    }

    #endregion
}

public static class JSONNodeExtensions
{
    public static JSONClass GetOrCreate(this JSONClass jc, string propertyName)
    {
        if (jc.HasKey(propertyName))
            return (JSONClass)jc[propertyName];

        var child = new JSONClass();
        jc.Add(propertyName, child);
        return child;
    }

    public static JSONClass GetIfExists(this JSONClass jc, string propertyName)
    {
        if (!jc.HasKey(propertyName))
            return null;

        return (JSONClass)jc[propertyName];
    }
}
