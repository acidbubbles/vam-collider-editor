using System;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using UnityEngine;

/// <summary>
/// Rigid Body Disabled
/// By Acidbubbles
/// Disable rigidbodis on an atom
/// Source: https://github.com/acidbubbles/vam-rigidbody-disabler
/// </summary>
public class RigidBodyDisabler : MVRScript
{
    private readonly List<JSONStorableBool> _rigidBodiesJSONs = new List<JSONStorableBool>();
    private readonly Dictionary<Rigidbody, GameObject> _rigidBodiesDisplay = new Dictionary<Rigidbody, GameObject>();
    private Atom _containingAtom;
    private JSONStorableBool _displayJSON;

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

            foreach (var rb in GetRigidBodies().OrderBy(rb => rb.name))
            {
                var rbJSON = new JSONStorableBool(rb.name, rb.detectCollisions, (bool val) =>
                {
                    rb.detectCollisions = val;
                    GameObject rbDisplay;
                    if (_rigidBodiesDisplay.TryGetValue(rb, out rbDisplay))
                        rbDisplay.SetActive(val);
                });
                CreateToggle(rbJSON, true);
                _rigidBodiesJSONs.Add(rbJSON);
            }
        }
        catch (Exception e)
        {
            SuperController.LogError($"{nameof(RigidBodyDisabler)}.{nameof(Init)}: {e}");
        }
    }

    public void OnEnable()
    {
        if (_containingAtom == null) return;
        try
        {
            InitRigidBodyCollisions();

            if (_displayJSON.val && _rigidBodiesDisplay.Count == 0)
                CreateRigidBodiesDisplay();
        }
        catch (Exception e)
        {
            SuperController.LogError($"{nameof(RigidBodyDisabler)}.{nameof(OnEnable)}: {e}");
        }
    }

    public void OnDisable()
    {
        if (_containingAtom == null) return;
        try
        {
            DestroyRigidBodiesDisplay();
            ResetRigidBodyCollisions();
        }
        catch (Exception e)
        {
            SuperController.LogError($"{nameof(RigidBodyDisabler)}.{nameof(OnDisable)}: {e}");
        }
    }

    public void OnDestroy()
    {
        if (_containingAtom == null) return;
        try
        {
            DestroyRigidBodiesDisplay();
            ResetRigidBodyCollisions();
        }
        catch (Exception e)
        {
            SuperController.LogError($"{nameof(RigidBodyDisabler)}.{nameof(OnDestroy)}: {e}");
        }
    }

    #endregion

    #region Load / Save

    public override JSONClass GetJSON(bool includePhysical = true, bool includeAppearance = true, bool forceStore = false)
    {
        var json = base.GetJSON(includePhysical, includeAppearance, forceStore);

        try
        {
            var disabledRigidbodies = new JSONArray();
            foreach (var rbJSON in _rigidBodiesJSONs.Where(rbJSON => !rbJSON.val))
                disabledRigidbodies.Add(rbJSON.name);
            json["disabledRigidbodies"] = disabledRigidbodies;
            needsStore = true;
        }
        catch (Exception exc)
        {
            SuperController.LogError($"{nameof(RigidBodyDisabler)}.{nameof(GetJSON)}:  {exc}");
        }

        return json;
    }

    public override void RestoreFromJSON(JSONClass jc, bool restorePhysical = true, bool restoreAppearance = true, JSONArray presetAtoms = null, bool setMissingToDefault = true)
    {
        base.RestoreFromJSON(jc, restorePhysical, restoreAppearance, presetAtoms, setMissingToDefault);

        try
        {
            var disabledRigidbodies = jc["disabledRigidbodies"] as JSONArray;
            if (disabledRigidbodies != null)
            {
                foreach (var rbName in disabledRigidbodies.Cast<JSONNode>())
                {
                    var rbJSON = _rigidBodiesJSONs.FirstOrDefault(r => r.name == rbName.Value);
                    if (rbJSON != null)
                        rbJSON.val = false;
                }
            }
        }
        catch (Exception exc)
        {
            SuperController.LogError($"{nameof(RigidBodyDisabler)}.{nameof(RestoreFromJSON)}: {exc}");
        }
    }

    #endregion

    #region Rigidbodies

    private void InitRigidBodyCollisions()
    {
        foreach (var rb in GetRigidBodies())
        {
            rb.detectCollisions = GetBoolJSONParam(rb.name)?.val ?? true;
        }
    }

    private void ResetRigidBodyCollisions()
    {
        foreach (var rb in GetRigidBodies())
        {
            if (rb == null) throw new NullReferenceException($"{nameof(rb)} is null");
            rb.detectCollisions = true;
        }
    }

    private IEnumerable<Rigidbody> GetRigidBodies()
    {
        foreach (var rb in _containingAtom.rigidbodies)
        {
            if (!rb.detectCollisions) continue;
            if (rb.name == "control") continue;
            if (rb.name == "object") continue;
            if (rb.name.EndsWith("Control")) continue;
            if (rb.name.StartsWith("hairTool")) continue;
            if (rb.name.EndsWith("Trigger")) continue;
            var rbCollider = rb.GetComponentInChildren<Collider>();
            if (rbCollider == null) continue;
            yield return rb;
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
}
