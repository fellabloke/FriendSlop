// PickableAutoAdder.cs
// Place this file inside Assets/Editor/

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.IO;

namespace PandDS{
[InitializeOnLoad]
public static class PickableAutoAdder
{
    static int pickableLayer = -1;
    static double lastCheckTime = 0;
    static double throttleSeconds = 0.5; // limit checks to twice per second

    static PickableAutoAdder()
    {
        // Try to get the layer index now (may be -1 until project layers loaded)
        RefreshLayerIndex();

        // Subscribe to hierarchy changes
        EditorApplication.hierarchyChanged += OnHierarchyChanged;

        // Also listen to scene opened to run initial pass
        EditorSceneManager.sceneOpened += (scene, mode) => { ScheduleCheck(); };
    }

    static void RefreshLayerIndex()
    {
        pickableLayer = LayerMask.NameToLayer("Pickable");
    }

    static void ScheduleCheck()
    {
        lastCheckTime = EditorApplication.timeSinceStartup - throttleSeconds; // allow immediate
        OnHierarchyChanged();
    }

    static void OnHierarchyChanged()
    {
        // throttle to avoid running too often
        if (EditorApplication.timeSinceStartup - lastCheckTime < throttleSeconds) return;
        lastCheckTime = EditorApplication.timeSinceStartup;

        // don't operate during playmode or when about to enter playmode
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        // refresh layer
        RefreshLayerIndex();
        if (pickableLayer == -1) return; // no Pickable layer in project

        // iterate all root gameobjects in all open scenes
        int scenes = SceneManager.sceneCount;
        for (int i = 0; i < scenes; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;

            var roots = scene.GetRootGameObjects();
            foreach (var root in roots)
            {
                ProcessTransformRecursive(root.transform);
            }
        }
    }

    static void ProcessTransformRecursive(Transform t)
    {
        if (t == null) return;

        var go = t.gameObject;

        // If object's layer is Pickable and it doesn't have RuntimePickup, add it
        if (go.layer == pickableLayer)
        {
            // check only scene objects (not assets)
            if (IsInScene(go))
            {
                var existing = go.GetComponent<RuntimePickup>();
                if (existing == null)
                {
                    // register undo for safety
                    Undo.AddComponent<RuntimePickup>(go);
                    // mark scene dirty
                    EditorSceneManager.MarkSceneDirty(go.scene);
                    if (EditorUtility.IsPersistent(go) == false)
                    {
                        Debug.Log($"[PickableAutoAdder] Added RuntimePickup to '{GetGameObjectPath(go)}' (layer Pickable).");
                    }
                }
            }
        }

        // recurse children
        for (int i = 0; i < t.childCount; i++)
            ProcessTransformRecursive(t.GetChild(i));
    }

    static bool IsInScene(GameObject go)
    {
        return go.scene.IsValid();
    }

    static string GetGameObjectPath(GameObject go)
    {
        return go.transform.parent == null ? go.name : GetGameObjectPath(go.transform.parent.gameObject) + "/" + go.name;
    }

    // Optional menu command: scan all prefabs in project and add RuntimePickup to those with Pickable layer
    [MenuItem("Tools/Pickable Tools/Scan Project Prefabs and Add RuntimePickup")]
    public static void ScanPrefabsAndAdd()
    {
        RefreshLayerIndex();
        if (pickableLayer == -1)
        {
            EditorUtility.DisplayDialog("Pickable layer not found", "No layer named 'Pickable' exists. Create it first under Edit → Project Settings → Tags and Layers.", "OK");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        int added = 0;
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefabRoot == null) continue;

            // skip variants or nested prefabs? we handle root prefab asset
            // If the prefab root has layer Pickable or any child does, add RuntimePickup to the root prefab
            bool hasPickable = false;
            foreach (Transform child in prefabRoot.GetComponentsInChildren<Transform>(true))
            {
                if (child.gameObject.layer == pickableLayer) { hasPickable = true; break; }
            }

            if (hasPickable)
            {
                // edit prefab asset
                GameObject instance = PrefabUtility.InstantiatePrefab(prefabRoot) as GameObject;
                if (instance == null) continue;

                if (instance.GetComponent<RuntimePickup>() == null)
                {
                    Undo.AddComponent<RuntimePickup>(instance);
                    // apply back to prefab
                    PrefabUtility.ApplyPrefabInstance(instance, InteractionMode.UserAction);
                    added++;
                }
                // destroy temp instance
                Object.DestroyImmediate(instance);
            }
        }

        EditorUtility.DisplayDialog("Prefab scan complete", $"Added RuntimePickup to {added} prefab(s).", "OK");
    }
}}
