using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class FirstPersonMovement : NetworkBehaviour
{
    [Header("Input Sockets")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference runAction;
    [SerializeField] private InputActionReference jumpAction;

    [Header("Movement Speeds")]
    public float walkSpeed = 5f;
    public float runSpeed = 9f;
    public bool canRun = true;

    [Header("Jump Variables")]
    public float jumpHeight = 10f;

    public bool IsRunning { get; private set; }
    
    private Rigidbody rb;
    private Collider col;
    
    public List<System.Func<float>> speedOverrides = new List<System.Func<float>>();

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            if (moveAction != null) moveAction.action.Enable();
            if (runAction != null) runAction.action.Enable();
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            if (moveAction != null) moveAction.action.Disable();
            if (runAction != null) runAction.action.Disable();
        }
    }

    void FixedUpdate()
    {
        if (!IsOwner || moveAction == null) return;

        MovePlayer();
    }

    private void MovePlayer()
    {
        IsRunning = canRun && runAction != null && runAction.action.IsPressed();

        float currentTargetSpeed = IsRunning ? runSpeed : walkSpeed;
        if (speedOverrides.Count > 0)
        {
            currentTargetSpeed = speedOverrides[speedOverrides.Count - 1]();
        }

        Vector2 inputDirection = moveAction.action.ReadValue<Vector2>();

        Vector2 targetVelocity = inputDirection * currentTargetSpeed;

        Vector3 worldVelocity = transform.rotation * new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.y);

        rb.linearVelocity = worldVelocity;
    }

    public void KillMovement()
    {
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
    }

    public void UseGravity()
    {
        rb.useGravity = true;
    }

    public void NoUseGravity()
    {
        rb.useGravity = false;
    }


    public void PrepareDriver()
    {
        rb.isKinematic = true;
        col.enabled = false;
    }

    public void UnprepareDriver()
    {
        rb.isKinematic = false;
        col.enabled = true;
    }
}