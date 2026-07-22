using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class Jump : NetworkBehaviour
{
    private Rigidbody rb;

    [Header("Jump Fine Tuning")]
    [SerializeField] private float jumpVelocity = 6f; 
    [SerializeField] private float upwardGravityMultiplier = 3f;
    [SerializeField] private float fallMultiplier = 4f;

    [Header("References")]
    [SerializeField] private InputActionReference jumpKey;
    [SerializeField] private GroundCheck groundCheck;

    public event System.Action Jumped;

    private bool jumpRequestedThisFrame;

    void Reset()
    {
        groundCheck = GetComponentInChildren<GroundCheck>();
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner && jumpKey != null) 
        {
            jumpKey.action.Enable();
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner && jumpKey != null) 
        {
            jumpKey.action.Disable();
        }
    }

    void Update()
    {
        if (!IsOwner || jumpKey == null) return;

        if (jumpKey.action.WasPressedThisFrame())
        {
            jumpRequestedThisFrame = true;
        }
    }

    void FixedUpdate()
    {
        if (!IsOwner) return;

        if (jumpRequestedThisFrame)
        {
            if (groundCheck == null || groundCheck.isGrounded)
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpVelocity, rb.linearVelocity.z);
                Jumped?.Invoke();
            }
            
            jumpRequestedThisFrame = false;
        }

        if (groundCheck != null && !groundCheck.isGrounded)
        {
            if (rb.linearVelocity.y > 0)
            {
                rb.linearVelocity += Vector3.up * Physics.gravity.y * (upwardGravityMultiplier - 1) * Time.fixedDeltaTime;
            }
            else if (rb.linearVelocity.y < 0)
            {
                rb.linearVelocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
            }
        }
    }
}