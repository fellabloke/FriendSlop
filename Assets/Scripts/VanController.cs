using System;
using Mono.Cecil;
using Unity.Netcode;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.InputSystem;
public class VanController : NetworkBehaviour 
{
    public static event Action<VanController> OnVanSpawned;

    [SerializeField] private InputActionReference driveAction;

    public float steerSpeed = 8f;
    public float maxLaneX = 4.5f;

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
        if (!IsOwner || !isBeingDriven.Value || driveAction == null) return;

        Vector2 input = driveAction.action.ReadValue<Vector2>();
        float steerInput = input.x;
        float speedInput = input.y;

        HandleSteering(steerInput);
        HandleAcceleration(speedInput);
    }

    private void HandleSteering(float steerInput)
    {
        transform.Translate(Vector3.right * steerInput * steerSpeed * Time.deltaTime, Space.World);
        Vector3 clampedPos = transform.position;
        clampedPos.x = Mathf.Clamp(clampedPos.x, -maxLaneX, maxLaneX);

        clampedPos.z = 0f;
        clampedPos.y = 2f;

        transform.position = clampedPos;

        float tiltAngle = steerInput * -3f;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, 0, tiltAngle), 10f * Time.deltaTime);
    }

    private void HandleAcceleration(float speedInput)
    {
        if (Mathf.Abs(speedInput) > 0.1f && HighwayManager.Instance != null)
        {
            HighwayManager.Instance.RequestSpeedChangeServerRPC(speedInput);
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
