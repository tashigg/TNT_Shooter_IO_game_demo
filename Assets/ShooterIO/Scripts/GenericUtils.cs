﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public static class GenericUtils
{
    public const int IgnoreRaycastLayer = 2;
    public const int WaterLayer = 4;

    private static List<InputField> inputFields;
#if USE_TEXT_MESH_PRO
    private static List<TMP_InputField> textMeshInputFields;
#endif
    private static bool isSetOnActiveSceneChanged_ResetInputField;

    public static bool IsFocusInputField()
    {
        GameObject[] rootObjects;
#if USE_TEXT_MESH_PRO
        if (inputFields == null || textMeshInputFields == null)
        {
            inputFields = new List<InputField>();
            textMeshInputFields = new List<TMP_InputField>();
            rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (GameObject rootObject in rootObjects)
            {
                inputFields.AddRange(rootObject.GetComponentsInChildren<InputField>(true));
                textMeshInputFields.AddRange(rootObject.GetComponentsInChildren<TMP_InputField>(true));
            }
        }
#else
        if (inputFields == null)
        {
            inputFields = new List<InputField>();
            rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (GameObject rootObject in rootObjects)
            {
                inputFields.AddRange(rootObject.GetComponentsInChildren<InputField>(true));
            }
        }
#endif

        foreach (InputField inputField in inputFields)
        {
            if (inputField.isFocused)
                return true;
        }
#if USE_TEXT_MESH_PRO
        foreach (TMP_InputField inputField in textMeshInputFields)
        {
            if (inputField.isFocused)
                return true;
        }
#endif
        if (!isSetOnActiveSceneChanged_ResetInputField)
        {
            SceneManager.activeSceneChanged += OnActiveSceneChanged_ResetInputField;
            isSetOnActiveSceneChanged_ResetInputField = true;
        }
        return false;
    }

    public static void OnActiveSceneChanged_ResetInputField(Scene scene1, Scene scene2)
    {
        inputFields = null;
#if USE_TEXT_MESH_PRO
        textMeshInputFields = null;
#endif
    }

    public static void SetLayerRecursively(this GameObject gameObject, int layerIndex, bool includeInactive)
    {
        Transform[] childrenTransforms = gameObject.GetComponentsInChildren<Transform>(includeInactive);
        foreach (Transform childTransform in childrenTransforms)
        {
            childTransform.gameObject.layer = layerIndex;
        }
    }

    public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
    {
        T result = gameObject.GetComponent<T>();
        if (result == null)
            result = gameObject.AddComponent<T>();
        return result;
    }

    public static void RemoveChildren(this Transform transform)
    {
        for (int i = transform.childCount - 1; i >= 0; --i)
        {
            Transform lastChild = transform.GetChild(i);
            Object.Destroy(lastChild.gameObject);
        }
    }

    public static void SetChildrenActive(this Transform transform, bool isActive)
    {
        for (int i = 0; i < transform.childCount; ++i)
        {
            transform.GetChild(i).gameObject.SetActive(isActive);
        }
    }

    public static void RemoveObjectsByComponentInChildren<T>(this GameObject gameObject, bool includeInactive) where T : Component
    {
        T[] components = gameObject.GetComponentsInChildren<T>(includeInactive);
        foreach (T component in components)
        {
            Object.DestroyImmediate(component.gameObject);
        }
    }

    public static void RemoveObjectsByComponentInParent<T>(this GameObject gameObject, bool includeInactive) where T : Component
    {
        T[] components = gameObject.GetComponentsInParent<T>(includeInactive);
        foreach (T component in components)
        {
            Object.DestroyImmediate(component.gameObject);
        }
    }

    public static void RemoveComponents<T>(this GameObject gameObject) where T : Component
    {
        T[] components = gameObject.GetComponents<T>();
        foreach (T component in components)
        {
            Object.DestroyImmediate(component);
        }
    }

    public static void RemoveComponentsInChildren<T>(this GameObject gameObject, bool includeInactive) where T : Component
    {
        T[] components = gameObject.GetComponentsInChildren<T>(includeInactive);
        foreach (T component in components)
        {
            Object.DestroyImmediate(component);
        }
    }

    public static void RemoveComponentsInParent<T>(this GameObject gameObject, bool includeInactive) where T : Component
    {
        T[] components = gameObject.GetComponentsInParent<T>(includeInactive);
        foreach (T component in components)
        {
            Object.DestroyImmediate(component);
        }
    }

    public static int GetNegativePositive()
    {
        return Random.value > 0.5f ? 1 : -1;
    }

    public static void SetAndStretchToParentSize(this RectTransform rect, RectTransform parentRect)
    {
        rect.SetParent(parentRect);
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = parentRect.rect.size;
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = Vector2.one;
    }

    public static Color SetAlpha(this Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    public static Vector3 GetXZ(this Vector3 position)
    {
        return new Vector3(position.x, 0, position.z);
    }
}
