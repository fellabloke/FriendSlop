using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace PandDS{
[DisallowMultipleComponent]
public class RuntimePickup : NetworkBehaviour
{
    [Header("General")]
    public float baseThrowForce = 6f;
    public float throwUpwardFactor = 0.28f;

    [Header("Follow (smooth)")]
    public float followSmoothTime = 0.06f;
    public float massSlowFactor = 0.12f;

    [Header("Rotation / Auto-Recover")]
    public bool preserveRotationOnPickup = true;
    public float recoverDelay = 0.45f;
    public float recoverDuration = 0.6f;

    [Header("Collision damping")]
    [Range(0f, 1f)] public float collisionAngularDamping = 0.15f;
    public float maxAngularVelocityWhileHeld = 4f;

    [Header("Hold angle clamp")]
    [Range(10f, 89f)]
    public float maxDownAngle = 60f;
    public float minDistance = 0.6f;

    [Header("Impact SFX (assign only)")]
    public AudioClip impactClip; 
    [Range(0f, 1f)] public float impactVolume = 1f;
    public float pitchVariation = 0.06f;

    [Header("Impact thresholds")]
    public float impactThresholdHeld = 0.35f;
    public float impactThresholdFree = 0.6f;

    // Deprecated in favor of direct injection via OnPickedUp
    // public Transform playerRoot;

    Rigidbody rb;
    Collider col;
    int originalLayer;
    bool originalLayerCaptured = false;

    bool isHeld = false;
    Transform holdPoint;
    Transform cameraTransform;
    Vector3 followVelocity;
    Quaternion pickedRotation;
    Quaternion rotationTarget;
    bool userRotated = false;
    bool collidedDuringHold = false;
    Coroutine recoverCoroutine;

    AudioPool audioPool;

    List<Collider> heldColliders = new List<Collider>();
    List<Collider> playerColliders = new List<Collider>();

    [Header("Soft Interaction Sandbox")]
    [SerializeField] private float interactionRadius = 0.6f;
    [SerializeField] private float knockbackMultiplier = 2f;
    
    private Collider[] interactionResults = new Collider[10];
    bool collisionsIgnoredWithPlayer = false;

    public NetworkVariable<bool> isNetworkHeld = new NetworkVariable<bool>(
        false, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    void Awake()
    {
        col = GetComponent<Collider>();
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();

        rb.mass = Mathf.Max(0.1f, rb.mass);
        rb.interpolation = RigidbodyInterpolation.None;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.maxAngularVelocity = Mathf.Max(rb.maxAngularVelocity, maxAngularVelocityWhileHeld);

        if (col != null && col.isTrigger) col.isTrigger = false;

        originalLayer = gameObject.layer;
        originalLayerCaptured = true;

        audioPool = FindFirstObjectByType<AudioPool>();
        if (audioPool == null)
        {
            var go = new GameObject("AudioPool_Auto");
            audioPool = go.AddComponent<AudioPool>();
            DontDestroyOnLoad(go);
        }
    }

    void FixedUpdate()
    {
        if (!IsOwner) return;
        
        if (!isHeld || holdPoint == null) return;

        float slowFactor = 1f + (rb.mass * massSlowFactor);
        float smoothTime = Mathf.Max(0.0001f, followSmoothTime * slowFactor);

        Vector3 desiredTarget = holdPoint.position;

        if (cameraTransform != null)
        {
            Vector3 camPos = cameraTransform.position;
            Vector3 dir = desiredTarget - camPos;
            float distance = Mathf.Max(dir.magnitude, minDistance);

            float angle = Vector3.Angle(cameraTransform.forward, dir);
            if (angle > maxDownAngle)
            {
                Vector3 allowedDir = Quaternion.AngleAxis(maxDownAngle, cameraTransform.right) * cameraTransform.forward;
                desiredTarget = camPos + allowedDir.normalized * distance;
            }

            Vector3 finalDir = desiredTarget - camPos;
            float finalDist = finalDir.magnitude;
            int ignoreMask = LayerMask.GetMask("Player", "HeldItem", "Ignore Raycast");
            int environmentMask = ~ignoreMask;

            if (Physics.SphereCast(camPos, 0.3f, finalDir.normalized, out RaycastHit hit, finalDist, environmentMask))
            {
                desiredTarget = hit.point + (hit.normal * 0.3f);
            }
        }

        Vector3 nextPos = Vector3.SmoothDamp(rb.position, desiredTarget, ref followVelocity, smoothTime, Mathf.Infinity, Time.fixedDeltaTime);
        
        rb.MovePosition(nextPos);

        if (collidedDuringHold)
        {
            rb.angularVelocity *= 1f - Mathf.Clamp01(collisionAngularDamping);
            if (rb.angularVelocity.sqrMagnitude > (maxAngularVelocityWhileHeld * maxAngularVelocityWhileHeld))
                rb.angularVelocity = rb.angularVelocity.normalized * maxAngularVelocityWhileHeld;
        }
        else
        {
            rb.angularVelocity = Vector3.zero;
            if (userRotated)
            {
                rb.MoveRotation(Quaternion.Slerp(rb.rotation, rotationTarget, 15f * Time.fixedDeltaTime));
            }
            else if (preserveRotationOnPickup)
            {
                rb.MoveRotation(Quaternion.Slerp(rb.rotation, pickedRotation, 25f * Time.fixedDeltaTime));
            }
        }

        // Make sure your soft interaction sandbox also reads from rb.position
        int hitCount = Physics.OverlapSphereNonAlloc(rb.position, interactionRadius, interactionResults, LayerMask.GetMask("Pickable"));

        for (int i = 0; i < hitCount; i++)
        {
            Rigidbody hitRb = interactionResults[i].attachedRigidbody;
            
            if (hitRb != null && hitRb != rb && !hitRb.isKinematic)
            {
                Vector3 pushDirection = (hitRb.position - rb.position).normalized;

                if (pushDirection.y < 0)
                {
                    pushDirection.y = 0f;
                }

                pushDirection += Vector3.up * 0.2f;

                float swingSpeed = Mathf.Clamp(followVelocity.magnitude, 1f, 12f);
                float finalForce = swingSpeed * knockbackMultiplier;

                hitRb.AddForce(pushDirection.normalized * finalForce, ForceMode.Force);
            }
        }
    }

    // Pass the actual interactor's root in so we ignore the correct colliders
    public void OnPickedUp(Transform holdPointTransform, Transform interactorRoot)
    {
        if (isHeld) return;

        originalLayer = gameObject.layer;
        originalLayerCaptured = true;

        isHeld = true;
        holdPoint = holdPointTransform;
        followVelocity = Vector3.zero;
        collidedDuringHold = false;
        userRotated = false;
        if (recoverCoroutine != null) { StopCoroutine(recoverCoroutine); recoverCoroutine = null; }

        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        pickedRotation = rb.rotation;
        rotationTarget = pickedRotation;

        var cam = holdPointTransform.GetComponentInParent<Camera>();
        cameraTransform = cam != null ? cam.transform : holdPointTransform.parent;

        int ignoreLayer = LayerMask.NameToLayer("Ignore Raycast");
        if (ignoreLayer >= 0) gameObject.layer = ignoreLayer;

        heldColliders.Clear();
        GetComponentsInChildren<Collider>(true, heldColliders);

        playerColliders.Clear();
        
        // Grab the explicit player's colliders instead of falling back to FindWithTag
        if (interactorRoot != null)
        {
            interactorRoot.GetComponentsInChildren<Collider>(true, playerColliders);
        }

        if (heldColliders.Count > 0 && playerColliders.Count > 0)
        {
            foreach (var pc in playerColliders)
            {
                if (pc == null) continue;
                foreach (var hc in heldColliders)
                {
                    if (hc == null) continue;
                    Physics.IgnoreCollision(hc, pc, true);
                }
            }
            collisionsIgnoredWithPlayer = true;
        }
    }

    // Consolidated client-side drop cleanup logic (replaces ThrowByCamera and previous drop methods)
    public void LocalDrop()
    {
        if (!isHeld) return;
        isHeld = false;
        holdPoint = null;
        userRotated = false;
        if (recoverCoroutine != null) { StopCoroutine(recoverCoroutine); recoverCoroutine = null; }
        followVelocity = Vector3.zero;

        if (collisionsIgnoredWithPlayer && playerColliders.Count > 0 && heldColliders.Count > 0)
        {
            foreach (var pc in playerColliders)
            {
                if (pc == null) continue;
                foreach (var hc in heldColliders)
                {
                    if (hc == null) continue;
                    Physics.IgnoreCollision(hc, pc, false);
                }
            }
            collisionsIgnoredWithPlayer = false;
        }

        heldColliders.Clear();
        playerColliders.Clear();

        if (originalLayerCaptured) gameObject.layer = originalLayer;

        if (rb)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }
    }

    public void RotateHeld(float deltaDegrees, Vector3 axis)
    {
        if (!isHeld) return;

        userRotated = true;
        rotationTarget = rotationTarget * Quaternion.AngleAxis(deltaDegrees, axis.normalized);
        rb.angularVelocity = Vector3.zero;
        rb.MoveRotation(rotationTarget);
    }

    void OnCollisionEnter(Collision collision)
    {
        float intensity = collision.relativeVelocity.magnitude;

        if (isHeld)
        {
            collidedDuringHold = true;
            rb.angularVelocity *= (1f - Mathf.Clamp01(collisionAngularDamping));
            if (recoverCoroutine != null) StopCoroutine(recoverCoroutine);
            recoverCoroutine = StartCoroutine(AutoRecoverRotation());

            if (impactClip != null && intensity >= impactThresholdHeld)
            {
                float vol = Mathf.Clamp01((intensity - impactThresholdHeld) / 3f) * impactVolume;
                PlayImpact(impactClip, collision.contacts[0].point, vol);
            }
        }
        else
        {
            if (impactClip != null && intensity >= impactThresholdFree)
            {
                float vol = Mathf.Clamp01((intensity - impactThresholdFree) / 6f) * impactVolume;
                PlayImpact(impactClip, collision.contacts[0].point, vol);
            }
        }
    }

    IEnumerator AutoRecoverRotation()
    {
        yield return new WaitForSeconds(recoverDelay);
        if (!isHeld) { recoverCoroutine = null; yield break; }

        float t = 0f;
        Quaternion start = rb.rotation;
        while (t < recoverDuration && isHeld && !userRotated)
        {
            t += Time.deltaTime;
            float f = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / recoverDuration));
            Quaternion target = Quaternion.Slerp(start, pickedRotation, f);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, target, 20f * Time.deltaTime));
            rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, Vector3.zero, 8f * Time.deltaTime);
            yield return null;
        }

        if (isHeld && !userRotated)
        {
            rb.MoveRotation(pickedRotation);
            collidedDuringHold = false;
        }

        recoverCoroutine = null;
    }

    public bool IsHeld() => isHeld;

    void PlayImpact(AudioClip clip, Vector3 pos, float volume = 1f)
    {
        if (clip == null) return;
        if (audioPool == null)
        {
            audioPool = FindFirstObjectByType<AudioPool>();
            if (audioPool == null)
            {
                var go = new GameObject("AudioPool_Auto");
                audioPool = go.AddComponent<AudioPool>();
                DontDestroyOnLoad(go);
            }
        }

        float pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        audioPool.Play(clip, pos, volume, pitch);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestGrabServerRpc(ulong requestingClientId)
    {
        if (!isNetworkHeld.Value)
        {
            NetworkObject.ChangeOwnership(requestingClientId);
            isNetworkHeld.Value = true;
            
            ConfirmGrabClientRpc();
        }
    }

    [ClientRpc]
    private void ConfirmGrabClientRpc()
    {
        // Don't trigger 'isHeld' state for non-owners locally to avoid FixedUpdate smooth damping conflicts
        if (!IsOwner)
        {
            if (rb) {
                rb.useGravity = false; 
                rb.isKinematic = true;
            }
        }
    }

    // RPC now accepts physics data
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestDropServerRpc(Vector3 inheritedVelocity, Vector3 throwForce)
    {
        NetworkObject.RemoveOwnership();
        isNetworkHeld.Value = false;

        // Apply the physical throw ON THE SERVER immediately so it syncs down
        if (rb)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.linearVelocity = inheritedVelocity;
            rb.AddForce(throwForce, ForceMode.VelocityChange);
        }

        ConfirmDropClientRpc();
    }

    [ClientRpc]
    private void ConfirmDropClientRpc()
    {
        LocalDrop();
    }
}}