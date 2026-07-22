using UnityEngine;
using UnityEditor;
namespace PandDS{
public class PickupSystemDocsWindow : EditorWindow
{
    Vector2 scroll;

    [MenuItem("Tools/Pickup System/Documentation", false, 100)]
    public static void ShowWindow()
    {
        PickupSystemDocsWindow window = GetWindow<PickupSystemDocsWindow>("Pickup System Docs");
        window.minSize = new Vector2(600, 500);
    }

    void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        GUILayout.Label(" Physics Pickup & Throw System", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        GUILayout.Label(" Quick Setup", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "1. Import the package\n" +
            "2. Add the Interactor script to your Player (FPS Controller / Character / VR Hand)\n" +
            "3. Create an empty child Transform on your Player as Hold Point\n" +
            "4. Set objects' Layer to 'Pickable'\n" +
            "5. Press Play → Look at object → Press E to pick up, Right Click to throw",
            MessageType.Info
        );

        EditorGUILayout.Space();
        GUILayout.Label(" RuntimePickup Inspector Reference", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("• Base Throw Force – Strength of throws");
        EditorGUILayout.LabelField("• Throw Upward Factor – Adds slight arc to throws");
        EditorGUILayout.LabelField("• Follow Smooth Time – How quickly object follows (lower = snappier)");
        EditorGUILayout.LabelField("• Mass Slow Factor – Heavier objects move slower");
        EditorGUILayout.LabelField("• Recover Delay – Delay before rotation stabilizes after collision");
        EditorGUILayout.LabelField("• Recover Duration – Smooth recovery speed");
        EditorGUILayout.LabelField("• Collision Angular Damping – Reduces spin on impact");
        EditorGUILayout.LabelField("• Max Down Angle – Prevents holding under player (fixes flying exploit)");
        EditorGUILayout.LabelField("• Impact Clip – Audio played when object hits ground/other objects");
        EditorGUILayout.Space();

        GUILayout.Label(" Controls (Default)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "E → Pick Up / Drop\n" +
            "Right Click → Throw\n" +
            "Q / E → Rotate object\n" +
            "Mouse / Controller → Aim / Throw direction",
            MessageType.None
        );

        EditorGUILayout.Space();
        GUILayout.Label(" Best Practices", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "- Use light rigidbodies (< 20 mass) for best results\n" +
            "- Always assign Pickable layer to objects you want interactable\n" +
            "- Adjust Follow Smooth Time between 0.04 – 0.1 for stable holding\n" +
            "- For VR, use the controller transform as Hold Point instead of the camera",
            MessageType.None
        );

        EditorGUILayout.Space();
        GUILayout.Label(" FAQ", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Q: Objects fly/clipping?");
        EditorGUILayout.LabelField("A: Increase Follow Smooth Time, lower object mass.");
        EditorGUILayout.LabelField("Q: Objects spin after drop?");
        EditorGUILayout.LabelField("A: Increase Collision Angular Damping.");
        EditorGUILayout.LabelField("Q: VR Support?");
        EditorGUILayout.LabelField("A: Yes, replace Camera hold point with VR hand transform.");

        EditorGUILayout.EndScrollView();
    }
}}
