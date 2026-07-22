using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

namespace PandDS{
public class Interactor : NetworkBehaviour
{
    [Header("References")]
    public Transform cameraTransform;   
    public Transform holdPoint;         
    public LayerMask pickupLayer = ~0;  
    public Text promptText;             

    [Header("Interaction")]
    public float rayMaxDistance = 4f;
    public float overlapRadius = 1.2f;
    public KeyCode interactKey = KeyCode.E;
    public float holdToPickTime = 0.45f;
    public float interactCooldown = 0.25f;
    private float nextInteractTime = 0f;

    [Header("Throwing")]
    public float throwForceMultiplierOnLeftClick = 1.8f;
    public float throwForceMultiplierOnE = 1f;

    [Header("Rotation while held")]
    public float rotateSpeed = 90f;
    public float scrollRotateStep = 15f;
    private Rigidbody playerRb;

    // runtime
    float holdTimer = 0f;
    RuntimePickup currentLook = null;
    RuntimePickup heldItem = null;
    int originalLayer; 

    bool isHolding => heldItem != null;

    void Start()
    {
        playerRb = GetComponent<Rigidbody>();
        int pickableIdx = LayerMask.NameToLayer("Pickable");
        if (pickableIdx != -1 && pickupLayer == (LayerMask)~0)
        {
            pickupLayer = 1 << pickableIdx;
        }
    }

    void Update()
    {
        if (!IsOwner) return;
        if (Time.time < nextInteractTime) return;
        
        if (isHolding)
        {
            HandleHeldItemControls();
            return;
        }

        currentLook = TryGetRuntimePickupFromLook();
        if (currentLook != null)
        {
            if (Input.GetKey(interactKey))
            {
                holdTimer += Time.deltaTime;
                SetPrompt($"Picking... {(int)(100f * Mathf.Clamp01(holdTimer / holdToPickTime))}%");
                if (holdTimer >= holdToPickTime)
                {
                    PickupCurrent();
                }
            }
            else
            {
                holdTimer = 0f;
                SetPrompt("Hold E to pick up");
            }
        }
        else
        {
            holdTimer = 0f;
            SetPrompt("");
        }
    }

    void HandleHeldItemControls()
    {
        float wheel = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(wheel) > 0.0001f)
        {
            float deg = wheel * scrollRotateStep * Mathf.Sign(wheel);
            heldItem.RotateHeld(deg, cameraTransform.forward);
        }

        if (Input.GetKey(KeyCode.Q))
            heldItem.RotateHeld(-rotateSpeed * Time.deltaTime, cameraTransform.up);
        if (Input.GetKey(KeyCode.E))
            heldItem.RotateHeld(rotateSpeed * Time.deltaTime, cameraTransform.up);

        if (Input.GetMouseButtonDown(0))
        {
            ThrowHeld(throwForceMultiplierOnLeftClick);
            return;
        }

        if (Input.GetMouseButtonDown(1))
        {
            DropHeld();
            return;
        }

        if (Input.GetKeyDown(interactKey))
        {
            ThrowHeld(throwForceMultiplierOnE);
            return;
        }

        SetPrompt("Holding: LMB throw, RMB drop, Scroll/Q/E rotate");
    }

    RuntimePickup TryGetRuntimePickupFromLook()
    {
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, rayMaxDistance, pickupLayer))
        {
            return EnsureRuntimePickupForCollider(hit.collider);
        }

        Vector3 center = cameraTransform.position + cameraTransform.forward * 0.5f;
        Collider[] cols = Physics.OverlapSphere(center, overlapRadius, pickupLayer);
        if (cols == null || cols.Length == 0) return null;

        Collider bestCol = cols.OrderBy(c => Vector3.Distance(cameraTransform.position, c.transform.position)).First();
        return EnsureRuntimePickupForCollider(bestCol);
    }

    RuntimePickup EnsureRuntimePickupForCollider(Collider col)
    {
        if (col == null) return null;

        var rp = col.GetComponentInParent<RuntimePickup>();
        if (rp != null) return rp;

        var rb = col.attachedRigidbody;
        GameObject target = rb != null ? rb.gameObject : col.gameObject;

        var existing = target.GetComponent<RuntimePickup>();
        if (existing != null) return existing;

        var newRp = target.AddComponent<RuntimePickup>();
        return newRp;
    }

    void PickupCurrent()
    {
        if (currentLook == null) return;
        if (currentLook.isNetworkHeld.Value) return;
        
        heldItem = currentLook;
        
        // FIX BUG 2: Pass 'transform' so the item knows EXACTLY which player is holding it to ignore collisions
        heldItem.OnPickedUp(holdPoint, transform); 
        heldItem.RequestGrabServerRpc(NetworkManager.Singleton.LocalClientId);

        SetPrompt("Holding: LMB throw, RMB drop, Scroll/Q/E rotate");
        holdTimer = 0f;
    }

    void ThrowHeld(float multiplier)
    {
        if (heldItem == null) return;

        // FIX BUG 1: Calculate vectors client-side, but pass them to the server to apply the physics
        Vector3 currentMomentum = playerRb != null ? playerRb.linearVelocity : Vector3.zero;
        Vector3 throwForce = (cameraTransform.forward * (heldItem.baseThrowForce * multiplier)) + (cameraTransform.up * heldItem.throwUpwardFactor);

        heldItem.RequestDropServerRpc(currentMomentum, throwForce);
        
        // Clean up local variables immediately for responsiveness
        heldItem.LocalDrop();
        nextInteractTime = Time.time + interactCooldown;
        heldItem = null;
       
        SetPrompt("");
    }

    void DropHeld()
    {
        if (heldItem == null) return;

        Vector3 currentMomentum = playerRb != null ? playerRb.linearVelocity : Vector3.zero;
        Vector3 dropForce = (cameraTransform.forward * (heldItem.baseThrowForce * 0.1f)) + (cameraTransform.up * heldItem.throwUpwardFactor);

        heldItem.RequestDropServerRpc(currentMomentum, dropForce); 
        
        heldItem.LocalDrop();
        nextInteractTime = Time.time + interactCooldown;
        heldItem = null;

        SetPrompt("");
    }

    void SetPrompt(string t)
    {
        if (promptText != null) promptText.text = t;
    }
}}