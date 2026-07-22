Physics Pickup & Throw System – Documentation

*****Quick Setup (5 Steps)*********************************************



-1-Add the Interactor script to your Player object (FPS Controller, Character Controller, or VR Hand).

-2-Assign a Hold Point (empty child transform where objects will be held).

-3-Tag Pickable Objects

-4-Set your objects’ Layer to Pickable.

-5-No script is needed on objects — the system auto-attaches at runtime.

-6-Assign Impact Sound (Optional)

-7-In your RuntimePickup component, drag in a sound effect to the Impact Clip field.

-8-Adjust volume, pitch variation, and thresholds in the Inspector.

-9-Press Play & Test

-10-Look at an object → Press E to pick it up.

  E (hold) = keep object.

  E (release) = drop object.

  Right Click / Throw Key = throw object with physics.

  Rotate with Q/E (or remap).

*******Inspector Reference*************************************

  RuntimePickup (auto-attached at runtime)

 General

 Base Throw Force → Strength of throws.

 Throw Upward Factor → Adds slight arc to throws.

 Follow (Smooth)

 Follow Smooth Time → How quickly object follows the hold point (lower = snappier).

 Mass Slow Factor → Heavier objects feel slower to move.

 Rotation / Recovery

 Preserve Rotation on Pickup → Keeps the same orientation when picked up.

 Recover Delay → Wait before stabilizing rotation after collision.

 Recover Duration → Smooth recovery speed.

 Collision Damping

 Collision Angular Damping → Reduces spin on impact.

 Max Angular Velocity While Held → Prevents crazy spins when colliding.

 Hold Angle Clamp

 Max Down Angle → Prevents holding objects under the player (fixes flying exploit).

 Min Distance → Minimum space between player and held object.

 Impact SFX

 Impact Clip → Sound when object hits a surface.

 Impact Volume → Overall sound loudness.

 Pitch Variation → Random variation for natural sound.

 Impact Thresholds → Minimum collision force to play sounds (different values for held vs. dropped).

  Controls (Default)

 E → Pick Up / Drop

 Right Click / Throw Key → Throw

 Q / E → Rotate Object

 Mouse / Controller → Affects throw direction

 (You can remap keys in Interactor.cs.)

  Best Practices

 Use light colliders & rigidbodies for best results (avoid huge mass).
 
 Always put pickable objects on the Pickable layer.

 Keep followSmoothTime between 0.04–0.1 for best physics stability.

 For VR, replace the camera hold point with the hand controller transform.

  FAQ

Q: My object flies or clips through walls.

Increase Follow Smooth Time (slower = more stable).

Use lighter mass (< 20).

Q: Object keeps spinning after dropping.

Adjust Collision Angular Damping.

Q: Can this work in VR?

Yes. Replace the camera hold point with a VR hand transform. Works with OpenXR or XR Interaction Toolkit.