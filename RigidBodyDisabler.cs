using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Rigid Body Disabled
/// By Acidbubbles
/// Disable rigidbodis on an atom
/// Source: https://github.com/acidbubbles/vam-rigidbody-disabler
/// </summary>
public class RigidBodyDisabler : MVRScript
{
    private readonly Dictionary<string, GameObject> _rbDisplay = new Dictionary<string, GameObject>();
    private JSONStorableBool _displayJSON;

    public override void Init()
    {
        try
        {
            _displayJSON = new JSONStorableBool("Display Rigidbodies", false, (bool val) =>
            {
                if (val) CreateDisplay();
                else DestroyDisplay();
            })
            { isStorable = false };

            foreach (var rb in containingAtom.rigidbodies)
            {
                var rbJSON = new JSONStorableBool(rb.name, rb.detectCollisions, (bool val) =>
                {
                    rb.detectCollisions = val;
                    GameObject rbDisplay;
                    if (_rbDisplay.TryGetValue(rb.name, out rbDisplay))
                        rbDisplay.SetActive(val);
                });
                CreateToggle(rbJSON, true);
            }

            OnEnable();
        }
        catch (Exception e)
        {
            SuperController.LogError($"{nameof(RigidBodyDisabler)}.{nameof(Init)}: {e}");
        }
    }

    public void OnEnable()
    {
        try
        {
            InitRigidBodyCollisions();

            if (_displayJSON.val && _rbDisplay.Count == 0)
                CreateDisplay();
        }
        catch (Exception e)
        {
            SuperController.LogError($"{nameof(RigidBodyDisabler)}.{nameof(OnEnable)}: {e}");
        }
    }

    public void OnDisable()
    {
        try
        {
            DestroyDisplay();
            ResetRigidBodyCollisions();
        }
        catch (Exception e)
        {
            SuperController.LogError($"{nameof(RigidBodyDisabler)}.{nameof(OnDisable)}: {e}");
        }
    }

    public void OnDestroy()
    {
        try
        {
            DestroyDisplay();
            ResetRigidBodyCollisions();
        }
        catch (Exception e)
        {
            SuperController.LogError($"{nameof(RigidBodyDisabler)}.{nameof(OnDestroy)}: {e}");
        }
    }

    private void InitRigidBodyCollisions()
    {
        foreach (var rb in containingAtom.rigidbodies)
        {
            rb.detectCollisions = GetBoolJSONParam(rb.name)?.val ?? true;
        }
    }

    private void ResetRigidBodyCollisions()
    {
        foreach (var rb in containingAtom.rigidbodies)
        {
            rb.detectCollisions = true;
        }
    }

    private void CreateDisplay()
    {
        DestroyDisplay();
        foreach (var rb in containingAtom.rigidbodies)
        {
            var rbDisplay = VisualCuesHelper.Cross();
            rbDisplay.transform.parent = rb.transform;
            rbDisplay.transform.localPosition = Vector3.zero;
            rbDisplay.transform.localRotation = Quaternion.identity;
            rbDisplay.SetActive(rb.detectCollisions);
            _rbDisplay.Add(rb.name, rbDisplay);
        }
    }

    private void DestroyDisplay()
    {
        foreach (var rbDisplay in _rbDisplay)
        {
            Destroy(rbDisplay.Value);
        }
        _rbDisplay.Clear();
    }

    private static class VisualCuesHelper
    {
        public static GameObject Cross()
        {
            var go = new GameObject();
            var size = 0.2f; var width = 0.005f;
            CreatePrimitive(go.transform, PrimitiveType.Cube, Color.red).transform.localScale = new Vector3(size, width, width);
            CreatePrimitive(go.transform, PrimitiveType.Cube, Color.green).transform.localScale = new Vector3(width, size, width);
            CreatePrimitive(go.transform, PrimitiveType.Cube, Color.blue).transform.localScale = new Vector3(width, width, size);
            foreach (var c in go.GetComponentsInChildren<Collider>()) Destroy(c);
            return go;
        }

        public static GameObject CreatePrimitive(Transform parent, PrimitiveType type, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            go.GetComponent<Renderer>().material = new Material(Shader.Find("Sprites/Default")) { color = color, renderQueue = 4000 };
            foreach (var c in go.GetComponents<Collider>()) { c.enabled = false; Destroy(c); }
            go.transform.parent = parent;
            return go;
        }
    }
}
