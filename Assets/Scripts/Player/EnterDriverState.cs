using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class EnterDriverState : NetworkBehaviour
{
    [Header("Driving References")]
    [SerializeField] private LayerMask driverSeatLayer;
    [SerializeField] private InputActionReference enterSeatButton;
    [SerializeField] private InputActionReference exitSeatButton;
    private VanController vanController;
    private Transform driverSeat; 

    [Header("Player References")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform cameraTransform;   
    private PlayerManager playerManager;

    [Header("Interact Parameters")]
    public float rayMaxDistance = 4f;

    private bool isLocalPlayerDriving = false;

    void Start()
    {
        if (!IsOwner) return;

        playerManager = GetComponent<PlayerManager>();
        playerTransform = GetComponent<Transform>();
    }

    public void SetVanReference(VanController spawnedVan)
    {
        vanController = spawnedVan;
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner && enterSeatButton != null) enterSeatButton.action.Enable(); 
        if (IsOwner && exitSeatButton != null) exitSeatButton.action.Enable(); 
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner && enterSeatButton != null) enterSeatButton.action.Disable(); 
        if (IsOwner && exitSeatButton != null) exitSeatButton.action.Disable(); 
    }

    void Update()
    {
        if (!IsOwner || !playerManager) return;

        if (vanController == null)
        {
            vanController = FindFirstObjectByType<VanController>();
            if (vanController == null) return;
        }

        if (enterSeatButton.action.WasPressedThisFrame() && !isLocalPlayerDriving)
        {
            Transform foundSeat = TrySeatPlayer();

            if (foundSeat != null && vanController.isBeingDriven.Value == false)
            {
                driverSeat = foundSeat;
                SwitchToDrivingState();
            }
        }

        if (exitSeatButton.action.WasPressedThisFrame() && isLocalPlayerDriving)
        {
            ExitDrivingSeat();
        }

        if (isLocalPlayerDriving && driverSeat != null)
        {
            playerTransform.position = driverSeat.position;
        }
    }

    Transform TrySeatPlayer()
    {
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, rayMaxDistance, driverSeatLayer))
        {
            return hit.collider.transform;
        }
        return null;
    }

    void SwitchToDrivingState()
    {
        isLocalPlayerDriving = true; 
        
        playerManager.ChangePlayerState(PlayerManager.PlayerState.Driving);
        vanController.RequestTakeWheelServerRpc(NetworkManager.Singleton.LocalClientId);
    }

    void ExitDrivingSeat()
    {
        isLocalPlayerDriving = false; 
        
        playerTransform.position += Vector3.right * 1.5f;

        playerManager.ChangePlayerState(PlayerManager.PlayerState.Active);
        vanController.RequestLeaveWheelServerRpc();
        
        driverSeat = null; 
    }
}