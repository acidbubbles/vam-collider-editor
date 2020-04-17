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
    private readonly Dictionary<Rigidbody, GameObject> _rigidBodiesDisplay = new Dictionary<Rigidbody, GameObject>();
    private Dictionary<string, Rigidbody> _rigidbodiesNameMap;
    private Atom _containingAtom;
    private JSONStorableBool _displayJSON;
    private UIDynamicPopup _rbAdjustListUI;
    private JSONClass _state = new JSONClass();

    #region Lifecycle

    public override void Init()
    {
        try
        {
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

    private readonly List<JSONStorableParam> _adjustmentJSONs = new List<JSONStorableParam>();

    private void ShowColliderAdjustments(string name)
    {
        foreach (var adjustmentJSON in _adjustmentJSONs)
        {
            if (adjustmentJSON is JSONStorableFloat)
                RemoveSlider((JSONStorableFloat)adjustmentJSON);
            else if (adjustmentJSON is JSONStorableBool)
                RemoveToggle((JSONStorableBool)adjustmentJSON);
            else
                SuperController.LogError($"Unknown ui type for {adjustmentJSON.name}: {adjustmentJSON.GetType()}");
        }
        _adjustmentJSONs.Clear();

        if (name == "") return;
        var rb = _rigidbodiesNameMap[name];

        var rbJSON = new JSONStorableBool("Detect Collisions", rb.detectCollisions, (bool val) =>
        {
            rb.detectCollisions = val;
            _state[rb.name]["enabled"].AsBool = val;

            GameObject rbDisplay;
            if (_rigidBodiesDisplay.TryGetValue(rb, out rbDisplay))
                rbDisplay.SetActive(val);
        });
        var rbUI = CreateToggle(rbJSON, true);
        _adjustmentJSONs.Add(rbJSON);

        var colliders = rb.GetComponentsInChildren<Collider>().Where(c => c.attachedRigidbody == rb).ToList();
        for (var colliderIndex = 0; colliderIndex < colliders.Count; colliderIndex++)
        {
            var collider = colliders[colliderIndex];

            var colliderName = Simplify(collider.name);
            var colliderUniqueName = $"{collider.name}:{colliderIndex}";

            if (collider is SphereCollider)
            {
                var sphereCollider = (SphereCollider)collider;
                var rbRadiusStorableFloat = new JSONStorableFloat(
                    $"{colliderName}/radius",
                    sphereCollider.radius,
                    (float val) =>
                    {
                        sphereCollider.radius = val;
                        _state[rb.name]["colliders"][colliderUniqueName]["radius"].AsFloat = val;
                    },
                    0f,
                    0.2f,
                    false);
                CreateFineSlider(rbRadiusStorableFloat, true);
                _adjustmentJSONs.Add(rbRadiusStorableFloat);
            }
            else if (collider is CapsuleCollider)
            {
                var capsuleCollider = (CapsuleCollider)collider;
                var rbRadiusStorableFloat = new JSONStorableFloat(
                    $"{colliderName}/radius",
                    capsuleCollider.radius,
                    (float val) =>
                    {
                        capsuleCollider.radius = val;
                        _state[rb.name]["colliders"][colliderUniqueName]["radius"].AsFloat = val;
                    },
                    0f,
                    0.2f,
                    false);
                CreateFineSlider(rbRadiusStorableFloat, true);
                _adjustmentJSONs.Add(rbRadiusStorableFloat);
                var rbHeightStorableFloat = new JSONStorableFloat(
                    $"{colliderName}/height",
                    capsuleCollider.height,
                    (float val) =>
                    {
                        capsuleCollider.height = val;
                        _state[rb.name]["colliders"][colliderUniqueName]["height"].AsFloat = val;
                    },
                    0f,
                    0.2f,
                    false);
                CreateFineSlider(rbHeightStorableFloat, true);
                _adjustmentJSONs.Add(rbHeightStorableFloat);
            }
            else if (collider is BoxCollider)
            {
                var boxCollider = (BoxCollider)collider;
                var rbWidthStorableFloat = new JSONStorableFloat(
                    $"{colliderName}/width",
                    boxCollider.size.x,
                    (float val) =>
                    {
                        boxCollider.size = new Vector3(val, boxCollider.size.y, boxCollider.size.z);
                        _state[rb.name]["colliders"][colliderUniqueName]["x"].AsFloat = val;
                    },
                    0f,
                    0.2f,
                    false);
                CreateFineSlider(rbWidthStorableFloat, true);
                _adjustmentJSONs.Add(rbWidthStorableFloat);
                var rbHeightStorableFloat = new JSONStorableFloat(
                    $"{colliderName}/height",
                    boxCollider.size.y,
                    (float val) =>
                    {
                        boxCollider.size = new Vector3(boxCollider.size.x, val, boxCollider.size.z);
                        _state[rb.name]["colliders"][colliderUniqueName]["y"].AsFloat = val;
                    },
                    0f,
                    0.2f,
                    false);
                CreateFineSlider(rbHeightStorableFloat, true);
                _adjustmentJSONs.Add(rbHeightStorableFloat);
                var rbDepthStorableFloat = new JSONStorableFloat(
                    $"{colliderName}/depth",
                    boxCollider.size.z,
                    (float val) =>
                    {
                        boxCollider.size = new Vector3(boxCollider.size.x, boxCollider.size.y, val);
                        _state[rb.name]["colliders"][colliderUniqueName]["z"].AsFloat = val;
                    },
                    0f,
                    0.2f,
                    false);
                CreateFineSlider(rbDepthStorableFloat, true);
                _adjustmentJSONs.Add(rbDepthStorableFloat);
            }
            else
            {
                SuperController.LogError($"Unknown collider {rb.name}/{collider.name} type: {collider}");
            }
        }
    }

    public void OnEnable()
    {
        if (_containingAtom == null) return;
        try
        {
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
            RestoreFromState();
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
        RestoreFromState();
    }

    public void RestoreFromState()
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
            if (rbJC.HasKey("enabled")) rb.detectCollisions = rbJC["enabled"].AsBool;

            var colliders = rb.GetComponentsInChildren<Collider>().Where(c => c.attachedRigidbody == rb).ToList();
            foreach (KeyValuePair<string, JSONNode> colliderEntry in rbJC["colliders"].AsObject)
            {
                var colliderUniqueName = colliderEntry.Key.Split(':');
                if (colliderUniqueName.Length != 2)
                {
                    SuperController.LogError($"Invalid collider unique name '{colliderEntry.Key}' of rigidbody '{rbEntry.Key}' specified in save");
                    continue;
                }
                var colliderName = colliderUniqueName[0];
                var colliderIndex = int.Parse(colliderUniqueName[1]);
                if (colliders.Count <= colliderIndex)
                {
                    SuperController.LogError($"Could not find collider '{colliderName}' at index {colliderIndex} of rigidbody '{rbEntry.Key}' specified in save (not enough colliders)");
                    continue;
                }
                var collider = colliders[colliderIndex];
                if (collider.name != colliderName)
                {
                    SuperController.LogError($"Could not find collider '{colliderName}' at index {colliderIndex} of rigidbody '{rbEntry.Key}' specified in save (found '{collider.name}' at this index)");
                    continue;
                }

                RestoreColliderFromState(rb, collider, (JSONClass)colliderEntry.Value);
            }
        }
    }

    private void RestoreColliderFromState(Rigidbody rb, Collider collider, JSONClass jc)
    {
        if (collider is SphereCollider)
        {
            var sphereCollider = (SphereCollider)collider;
            if (jc.HasKey("radius"))
                sphereCollider.radius = jc["radius"].AsFloat;
        }
        else if (collider is CapsuleCollider)
        {
            var capsuleCollider = (CapsuleCollider)collider;
            if (jc.HasKey("radius"))
                capsuleCollider.radius = jc["radius"].AsFloat;
            if (jc.HasKey("height"))
                capsuleCollider.height = jc["height"].AsFloat;
        }
        else if (collider is BoxCollider)
        {
            var boxCollider = (BoxCollider)collider;
            var size = boxCollider.size;
            if (jc.HasKey("x"))
                size.x = jc["x"].AsFloat;
            if (jc.HasKey("y"))
                size.x = jc["y"].AsFloat;
            if (jc.HasKey("z"))
                size.x = jc["z"].AsFloat;
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
            var material = new Material(Shader.Find("Standard")) { color = new Color(1, 0, 0, 0.1f) };
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
            go.GetComponent<Renderer>().material = material;
            foreach (var c in go.GetComponents<Collider>()) { c.enabled = false; Destroy(c); }
            // if (rbCollider == null) throw new NullReferenceException("test");
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
        foreach (var rb in _containingAtom.rigidbodies)
        {
            if (rb.name == "control") continue;
            if (rb.name == "object") continue;
            if (rb.name.EndsWith("Control")) continue;
            if (rb.name.StartsWith("hairTool")) continue;
            if (rb.name.EndsWith("Trigger")) continue;
            if (rb.name.EndsWith("UI")) continue;
            var rbCollider = rb.GetComponentInChildren<Collider>();
            if (rbCollider == null) continue;
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
