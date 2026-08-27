using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class VanController : NetworkBehaviour 
{
    [SerializeField] private InputActionReference driveAction;

    [Header("Collision & Physics")]
    public float crashSpeedPenalty = 15f; 
    public float recoverySpeed = 2f;
    private float currentZOffset = 0f;
    public float pushMultiplier;
    
    public float rearEndJoltCooldown = 3f;
    private float lastJoltTime = 0f; 

    [Header("Handling")]
    [SerializeField] private float accelerationRate = 12.5f;
    [SerializeField] private float brakingRate = 20f;
    public float minSpeed = 0f;
    public float maxSpeed = 60f;
    public float idleDecelerationRate = 4f;
    public float steerSpeed = 8f;
    public float maxLaneX = 8.5f;

    [Header("Torque Curve")]
    [Tooltip("X-Axis = Speed % (0 to 1). Y-Axis = Acceleration Multiplier")]
    public AnimationCurve torqueCurve = new AnimationCurve(
        new Keyframe(0f, 0.3f),
        new Keyframe(0.3f, 1f),
        new Keyframe(1f, 0.1f)
    );

    public static event Action<float> OnVanCrashed;
    public static event Action<VanController> OnVanSpawned;

    public NetworkVariable<bool> isBeingDriven = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        OnVanSpawned?.Invoke(this);
        if (driveAction != null) driveAction.action.Enable();
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner && driveAction != null) driveAction.action.Disable();
    }

    void Update()
    {
        if (currentZOffset < 0f)
        {
            currentZOffset = Mathf.MoveTowards(currentZOffset, 0f, recoverySpeed * Time.deltaTime);
        }

        if (!IsOwner || !isBeingDriven.Value || driveAction == null) 
        {
            ApplyPosition();
            return;
        }

        Vector2 input = driveAction.action.ReadValue<Vector2>();
        float steerInput = input.x;
        float speedInput = input.y;

        HandleSteering(steerInput);
        HandleAcceleration(speedInput);
    }

    private void HandleSteering(float steerInput)
    {
        transform.Translate(Vector3.right * steerInput * steerSpeed * Time.deltaTime, Space.World);
        ApplyPosition();

        float targetYaw = steerInput * 15f; 
        float targetRoll = steerInput * -4f; 

        Quaternion targetRotation = Quaternion.Euler(0f, targetYaw, targetRoll);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 8f * Time.deltaTime);
    }

    private void ApplyPosition()
    {
        Vector3 clampedPos = transform.position;
        clampedPos.x = Mathf.Clamp(clampedPos.x, -maxLaneX, maxLaneX);
        clampedPos.z = currentZOffset; 
        transform.position = clampedPos;
    }

    private void HandleAcceleration(float speedInput)
    {
        if (Mathf.Abs(speedInput) > 0.1f && HighwayManager.Instance != null)
        {
            float requestedSpeedChange = 0f;

            if (speedInput > 0f) 
            {
                float currentSpeed = HighwayManager.Instance.currentSpeed.Value;
                float speedPercent = Mathf.InverseLerp(minSpeed, maxSpeed, currentSpeed);
                float torqueMultiplier = torqueCurve.Evaluate(speedPercent);

                requestedSpeedChange = speedInput * accelerationRate * torqueMultiplier;
            }
            else if (speedInput < 0f) 
            {
                requestedSpeedChange = speedInput * brakingRate; 
            }

            HighwayManager.Instance.RequestSpeedChangeServerRPC(requestedSpeedChange);
        }
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;

        if (collision.gameObject.CompareTag("Obstacle"))
        {
            ContactPoint contact = collision.GetContact(0);
            
            float zImpact = Mathf.Abs(contact.normal.z);
            float xImpact = Mathf.Abs(contact.normal.x);
            
            float impactSpeed = HighwayManager.Instance != null ? HighwayManager.Instance.currentSpeed.Value : 20f;

            CivilianCar hitCar = collision.gameObject.GetComponent<CivilianCar>();

            bool isAlreadyCrashed = hitCar != null && hitCar.IsCrashed;

            if (zImpact > xImpact)
            {
                if (HighwayManager.Instance != null)
                {
                    float penaltyMultiplier = isAlreadyCrashed ? 0.2f : 1.0f;
                    HighwayManager.Instance.RequestSpeedChangeServerRPC(-(impactSpeed * crashSpeedPenalty * penaltyMultiplier)); 
                }

                if (!isAlreadyCrashed && Time.time >= lastJoltTime + rearEndJoltCooldown)
                {
                    HandleCrashClientRpc(impactSpeed, true);
                    lastJoltTime = Time.time; 
                }

                if (hitCar != null && !isAlreadyCrashed)
                {
                    float forwardPush = impactSpeed * pushMultiplier;
                    hitCar.TriggerRearEnd(forwardPush);
                }
            }
            else
            {
                if (!isAlreadyCrashed)
                {
                    HandleCrashClientRpc(impactSpeed, false);
                    
                    if (hitCar != null)
                    {
                        Vector3 pushDirection = new Vector3(-contact.normal.x, 0, 0); 
                        pushDirection.Normalize(); 
                        hitCar.TriggerSwerve(pushDirection, impactSpeed);
                    }
                }
            }
        }
    }
    private void OnCollisionStay(Collision collision)
    {
        if (!IsServer) return;

        if (collision.gameObject.CompareTag("Obstacle"))
        {
            CivilianCar hitCar = collision.gameObject.GetComponent<CivilianCar>();
            if (hitCar != null)
            {
                hitCar.RegisterPush();
            }
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void HandleCrashClientRpc(float impactSpeed, bool isRearEnd)
    {
        if (isRearEnd)
        {
            if(impactSpeed > 40)
            {
                currentZOffset = -3f; 
                transform.rotation = Quaternion.Euler(15f, 0f, transform.rotation.eulerAngles.z);
                OnVanCrashed?.Invoke(impactSpeed);
            }
        }
        else
        {
            transform.rotation = Quaternion.Euler(0f, 0f, 10f); 
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestTakeWheelServerRpc(ulong requestingClientId)
    {
        NetworkObject.ChangeOwnership(requestingClientId);
        isBeingDriven.Value = true;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestLeaveWheelServerRpc()
    {
        NetworkObject.RemoveOwnership();
        isBeingDriven.Value = false;
    }
}