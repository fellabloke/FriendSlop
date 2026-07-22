// Assets/Editor/RuntimePickupHelperWindow.cs
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;
namespace PandDS{
public class RuntimePickupHelperWindow : EditorWindow
{
    enum PresetType { Light, Medium, Heavy, Custom }
    PresetType selectedPreset = PresetType.Medium;

    // Default presets
    Dictionary<PresetType, (float followSmoothTime, float massSlowFactor, float baseThrowForce)> presets =
        new Dictionary<PresetType, (float, float, float)>
        {
            { PresetType.Light, (0.04f, 0.08f, 8f) },
            { PresetType.Medium, (0.1f, 0.12f, 3f) },
            { PresetType.Heavy, (0.3f, 0.16f, 1f) },
            { PresetType.Custom, (0.06f, 0.12f, 6f) } // will be loaded from file
        };

    static string PresetSavePath => Path.Combine(Application.dataPath, "Editor", "RuntimePickupPreset.json");

    [MenuItem("Tools/Pickup System/RuntimePickup Helper")]
    public static void ShowWindow()
    {
        GetWindow<RuntimePickupHelperWindow>("Pickup Presets");
    }

    void OnEnable()
    {
        LoadCustomPreset();
    }

    void OnGUI()
    {
        GUILayout.Label("RuntimePickup Presets", EditorStyles.boldLabel);
        GUILayout.Space(5);

        selectedPreset = (PresetType)EditorGUILayout.EnumPopup("Preset", selectedPreset);

        if (selectedPreset == PresetType.Custom)
        {
            var val = presets[PresetType.Custom];
            val.followSmoothTime = EditorGUILayout.FloatField("Follow Smooth Time", val.followSmoothTime);
            val.massSlowFactor = EditorGUILayout.FloatField("Mass Slow Factor", val.massSlowFactor);
            val.baseThrowForce = EditorGUILayout.FloatField("Base Throw Force", val.baseThrowForce);
            presets[PresetType.Custom] = val;

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Save Custom Preset"))
                SaveCustomPreset();
            if (GUILayout.Button("Load Custom Preset"))
                LoadCustomPreset();
            GUILayout.EndHorizontal();
        }

        GUILayout.Space(10);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Apply to ALL in Scene", GUILayout.Height(30)))
        {
            ApplyPresetToTargets(includeAll: true);
        }
        if (GUILayout.Button("Apply to SELECTION", GUILayout.Height(30)))
        {
            ApplyPresetToTargets(includeAll: false);
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);
        if (GUILayout.Button("Reset to Default (Medium)", GUILayout.Height(25)))
        {
            ApplyPresetValues(presets[PresetType.Medium], FindObjectsOfType<RuntimePickup>(true));
        }

        GUILayout.Space(10);
        EditorGUILayout.HelpBox("Choose a preset, then apply it to all RuntimePickup components in the scene or only the selected objects. Custom presets are saved in Assets/Editor/RuntimePickupPreset.json.", MessageType.Info);
    }

    void ApplyPresetToTargets(bool includeAll)
    {
        var val = presets[selectedPreset];
        List<RuntimePickup> targets = new List<RuntimePickup>();

        if (includeAll)
        {
            targets.AddRange(FindObjectsOfType<RuntimePickup>(true));
        }
        else
        {
            foreach (var obj in Selection.gameObjects)
            {
                var rp = obj.GetComponent<RuntimePickup>();
                if (rp != null) targets.Add(rp);
            }
        }

        if (targets.Count == 0)
        {
            Debug.LogWarning("[RuntimePickupHelper] No RuntimePickup components found to apply preset.");
            return;
        }

        ApplyPresetValues(val, targets);
    }

    void ApplyPresetValues((float followSmoothTime, float massSlowFactor, float baseThrowForce) val, IEnumerable<RuntimePickup> pickups)
    {
        // convert to array of UnityEngine.Object for Undo.RecordObjects
        var pickupList = pickups.Where(p => p != null).ToArray();
        if (pickupList.Length == 0)
        {
            Debug.LogWarning("[RuntimePickupHelper] No valid RuntimePickup targets to apply.");
            return;
        }

        UnityEngine.Object[] undoObjects = pickupList.Cast<UnityEngine.Object>().ToArray();
        Undo.RecordObjects(undoObjects, "Apply Pickup Preset");

        int count = 0;
        foreach (var p in pickupList)
        {
            p.followSmoothTime = val.followSmoothTime;
            p.massSlowFactor = val.massSlowFactor;
            p.baseThrowForce = val.baseThrowForce;
            EditorUtility.SetDirty(p);
            count++;
        }

        Debug.Log($"[RuntimePickupHelper] Applied {selectedPreset} preset to {count} objects.");
    }

    void SaveCustomPreset()
    {
        var val = presets[PresetType.Custom];
        string json = JsonUtility.ToJson(new PresetData(val.followSmoothTime, val.massSlowFactor, val.baseThrowForce), true);
        Directory.CreateDirectory(Path.GetDirectoryName(PresetSavePath));
        File.WriteAllText(PresetSavePath, json);
        AssetDatabase.Refresh();
        Debug.Log("[RuntimePickupHelper] Custom preset saved.");
    }

    void LoadCustomPreset()
    {
        if (!File.Exists(PresetSavePath))
        {
            Debug.LogWarning("[RuntimePickupHelper] No custom preset found, using defaults.");
            return;
        }

        string json = File.ReadAllText(PresetSavePath);
        var data = JsonUtility.FromJson<PresetData>(json);
        presets[PresetType.Custom] = (data.followSmoothTime, data.massSlowFactor, data.baseThrowForce);
        Debug.Log("[RuntimePickupHelper] Custom preset loaded.");
    }

    [System.Serializable]
    public struct PresetData
    {
        public float followSmoothTime;
        public float massSlowFactor;
        public float baseThrowForce;

        public PresetData(float f, float m, float b)
        {
            followSmoothTime = f;
            massSlowFactor = m;
            baseThrowForce = b;
        }
    }
}}
