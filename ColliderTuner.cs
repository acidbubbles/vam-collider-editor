using System;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using UnityEngine;

/// <summary>
/// Rigid Body Disabled
/// By Acidbubbles
/// Disable rigidbodis on an atom
/// Source: https://github.com/acidbubbles/vam-collider-tuner
/// </summary>
public class ColliderTuner : MVRScript
{
    private static readonly Color _selectedColor = new Color(0f, 1f, 0f, 0.05f);
    private static readonly Color _unselectedColor = new Color(1f, 0f, 0f, 0.05f);

    private readonly Dictionary<Rigidbody, GameObject> _rigidBodiesDisplay = new Dictionary<Rigidbody, GameObject>();
    private Dictionary<string, Rigidbody> _rigidbodiesNameMap;
    private Atom _containingAtom;
    private JSONStorableBool _displayJSON;
    private UIDynamicPopup _rbAdjustListUI;
    private JSONClass _state = new JSONClass();
    private Rigidbody _currentRb;
    private Dictionary<Rigidbody, List<Collider>> _rbCollidersMap;

    #region Lifecycle

    public override void Init()
    {
        try
        {
            var jc = new JSONClass();
            _containingAtom = containingAtom;

            _displayJSON = new JSONStorableBool("Display Rigidbodies", false, (bool val) =>
            {
                if (val) CreateRigidBodiesDisplay();
                else DestroyRigidBodiesDisplay();
            })
            { isStorable = false };
            CreateToggle(_displayJSON, false);

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

            var rbAdjustListJSON = new JSONStorableStringChooser("Collider Adjustment", _rigidbodiesNameMap.Keys.OrderBy(k => k).ToList(), "", "Collider Adjustment", (string val) => ShowColliderAdjustments(val));
            _rbAdjustListUI = CreateScrollablePopup(rbAdjustListJSON, false);
            _rbAdjustListUI.popupPanelHeight = 900f;
        }
        catch (Exception e)
        {
            SuperController.LogError($"{nameof(ColliderTuner)}.{nameof(Init)}: {e}");
        }
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

    private readonly List<JSONStorableParam> _adjustmentStorables = new List<JSONStorableParam>();
    private readonly List<UIDynamicButton> _adjustmentButtons = new List<UIDynamicButton>();

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

        GameObject rbDisplay;
        if (_currentRb != null && _rigidBodiesDisplay.TryGetValue(_currentRb, out rbDisplay))
        {
            rbDisplay.GetComponent<Renderer>().material.color = _unselectedColor;
        }
        _currentRb = null;

        if (name == "") return;
        var rb = _rigidbodiesNameMap[name];
        _currentRb = rb;

        if (_rigidBodiesDisplay.TryGetValue(rb, out rbDisplay))
        {
            rbDisplay.GetComponent<Renderer>().material.color = _selectedColor;
        }

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

        CreateBoolAdjustment(null, () => _state.GetOrCreate(rb.name), "enabled", rb.detectCollisions, val =>
        {
            rb.detectCollisions = val;

            if (rbDisplay != null)
                rbDisplay.SetActive(val);
        });

        var colliders = _rbCollidersMap[rb];
        for (var colliderIndex = 0; colliderIndex < colliders.Count; colliderIndex++)
        {
            var collider = colliders[colliderIndex];

            var colliderName = Simplify(collider.name);
            if (colliderName == null) throw new NullReferenceException($"Collider name of rigidbody {rb.name} is null");
            var colliderUniqueName = $"{collider.name}:{colliderIndex}";
            Func<string, float?> getInitial = (string prop) => _state[rb.name]?["colliders"]?[colliderUniqueName]?[prop]?.AsFloat;
            Func<JSONClass> getJsonNode = () => _state.GetOrCreate(rb.name).GetOrCreate("colliders").GetOrCreate(colliderUniqueName);

            if (collider is SphereCollider)
            {
                var sphereCollider = (SphereCollider)collider;
                CreateFloatAdjustment(colliderName, getJsonNode, "radius", getInitial("radius") ?? sphereCollider.radius, val => sphereCollider.radius = val);
            }
            else if (collider is CapsuleCollider)
            {
                var capsuleCollider = (CapsuleCollider)collider;
                CreateFloatAdjustment(colliderName, getJsonNode, "radius", getInitial("radius") ?? capsuleCollider.radius, val => capsuleCollider.radius = val);
                CreateFloatAdjustment(colliderName, getJsonNode, "height", getInitial("height") ?? capsuleCollider.height, val => capsuleCollider.height = val);
            }
            else if (collider is BoxCollider)
            {
                var boxCollider = (BoxCollider)collider;
                CreateFloatAdjustment(colliderName, getJsonNode, "x", getInitial("x") ?? boxCollider.size.x, val => new Vector3(val, boxCollider.size.y, boxCollider.size.z));
                CreateFloatAdjustment(colliderName, getJsonNode, "y", getInitial("y") ?? boxCollider.size.y, val => new Vector3(boxCollider.size.x, val, boxCollider.size.z));
                CreateFloatAdjustment(colliderName, getJsonNode, "z", getInitial("z") ?? boxCollider.size.z, val => new Vector3(boxCollider.size.x, boxCollider.size.y, val));
            }
            else
            {
                SuperController.LogError($"Unknown collider {rb.name}/{collider.name} type: {collider}");
            }
        }
    }

    private void CreateFloatAdjustment(string parentName, Func<JSONClass> getJsonNode, string propertyName, float initial, Action<float> setValue, float min = 0.00001f, float max = 0.2f)
    {
        var storable = new JSONStorableFloat(
            $"{parentName}/{propertyName}",
            initial,
            (float val) =>
            {
                var originalPropertyName = $"{propertyName}Initial";
                var jc = getJsonNode();
                if (!jc.HasKey(originalPropertyName)) jc[originalPropertyName].AsFloat = val;
                jc[propertyName].AsFloat = val;
                setValue(val);
            },
            min,
            max,
            false);
        CreateFineSlider(storable, true);
        _adjustmentStorables.Add(storable);
    }

    private void CreateBoolAdjustment(string parentName, Func<JSONClass> getJsonNode, string propertyName, bool initial, Action<bool> setValue)
    {
        var storable = new JSONStorableBool(
            $"{parentName}/{propertyName}",
            initial,
            (bool val) =>
            {
                var originalPropertyName = $"{propertyName}Initial";
                var jc = getJsonNode();
                if (!jc.HasKey(originalPropertyName)) jc[originalPropertyName].AsBool = val;
                jc[propertyName].AsBool = val;
                setValue(val);
            });
        CreateToggle(storable, true);
        _adjustmentStorables.Add(storable);
    }

    public void OnEnable()
    {
        if (_containingAtom == null) return;
        try
        {
            RestoreFromState(false);

            if (_displayJSON.val && _rigidBodiesDisplay.Count == 0)
                CreateRigidBodiesDisplay();
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
            RestoreFromState(true);
            DestroyRigidBodiesDisplay();
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
            RestoreFromState(true);
            DestroyRigidBodiesDisplay();
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
        SuperController.LogMessage("OK");
        RestoreFromState(false);
    }

    public void RestoreFromState(bool initial)
    {
        foreach (KeyValuePair<string, JSONNode> rbEntry in _state)
        {
            var rb = containingAtom.rigidbodies.FirstOrDefault(x => x.name == rbEntry.Key);
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
            // TODO
            SuperController.LogMessage($"{rb.name}.detectCollisions = {rb.detectCollisions}");
        }

        var colliders = _rbCollidersMap[rb];
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

    #endregion

    #region Display

    private void CreateRigidBodiesDisplay()
    {
        DestroyRigidBodiesDisplay();
        foreach (var rb in GetRigidBodies())
        {
            var rbDisplay = CreateDisplayGameObject(rb);
            if (rbDisplay == null) continue;
            rbDisplay.SetActive(rb.detectCollisions);
            try
            {
                _rigidBodiesDisplay.Add(rb, rbDisplay);
            }
            catch (ArgumentException exc)
            {
                SuperController.LogError($"Cannot add '{rb.name}': {exc.Message}");
            }
        }
    }

    private void DestroyRigidBodiesDisplay()
    {
        foreach (var rbDisplay in _rigidBodiesDisplay)
        {
            Destroy(rbDisplay.Value);
        }
        _rigidBodiesDisplay.Clear();
    }

    public GameObject CreateDisplayGameObject(Rigidbody rb)
    {
        var rbCollider = rb.GetComponentInChildren<Collider>();
        if (rbCollider == null) return null;
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        try
        {
            var material = new Material(Shader.Find("Standard")) { color = _currentRb == rb ? _selectedColor : _unselectedColor };
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
            go.GetComponent<Renderer>().material = material;
            foreach (var c in go.GetComponents<Collider>()) { c.enabled = false; Destroy(c); }
            go.transform.parent = rbCollider.transform.parent;
            go.transform.localPosition = rbCollider.transform.localPosition;
            go.transform.localRotation = rbCollider.transform.localRotation;
            go.transform.localScale = rbCollider.transform.localScale * 0.06f;
        }
        catch (Exception)
        {
            Destroy(go);
            throw;
        }
        return go;
    }

    #endregion

    #region UI

    private IEnumerable<Rigidbody> GetRigidBodies()
    {
        if (_rbCollidersMap == null)
        {
            _rbCollidersMap = new Dictionary<Rigidbody, List<Collider>>();
            foreach (var collider in _containingAtom.GetComponentsInChildren<Collider>())
            {
                if (collider.attachedRigidbody == null) continue;

                List<Collider> rbColliders;
                if (!_rbCollidersMap.TryGetValue(collider.attachedRigidbody, out rbColliders))
                {
                    rbColliders = new List<Collider>();
                    _rbCollidersMap.Add(collider.attachedRigidbody, rbColliders);
                }
                rbColliders.Add(collider);
            }
        }

        foreach (var rb in _containingAtom.rigidbodies)
        {
            if (rb.isKinematic) continue;
            if (rb.name == "control") continue;
            if (rb.name == "object") continue;
            if (!_rbCollidersMap.ContainsKey(rb)) continue;
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
        {
            return (JSONClass)jc[propertyName];
        }

        var child = new JSONClass();
        jc.Add(propertyName, child);
        return child;
    }
}
