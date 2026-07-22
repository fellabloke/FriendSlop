using UnityEngine;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;

public class PlayerManager : NetworkBehaviour 
{
    public enum PlayerState
    {
        Active,
        inMenu,
        inLobby,
        inTesting,
        Driving
    }
    
    public NetworkVariable<PlayerState> CurrentState = new NetworkVariable<PlayerState>(
        PlayerState.inMenu,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    [Header("Player Scripts")]
    [SerializeField] FirstPersonLook mouseLook;
    [SerializeField] FirstPersonMovement playerMovement;
    [SerializeField] Jump jump;
    [SerializeField] VanController vanController;
    [SerializeField] EnterDriverState driverState;

    void Start()
    {
        if (!IsOwner) return;

        playerMovement = GetComponent<FirstPersonMovement>();
        mouseLook = GetComponentInChildren<FirstPersonLook>();
        jump = GetComponent<Jump>();
        driverState = GetComponent<EnterDriverState>();

        if (vanController == null)
        {
            vanController = FindFirstObjectByType<VanController>();
            if (vanController != null && driverState != null)
            {
                driverState.SetVanReference(vanController);
            }
        }
    }

    void OnEnable()
    {
        VanController.OnVanSpawned += HandleVanSpawned;
    }
    void OnDisable()
    {
        VanController.OnVanSpawned -= HandleVanSpawned;
    }
    private void HandleVanSpawned(VanController spawnedVan)
    {
        vanController = spawnedVan;
        if (driverState != null) driverState.SetVanReference(spawnedVan);
    }
    
    public override void OnNetworkSpawn()
    {
        CurrentState.OnValueChanged += OnPlayerStateChanged;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.CurrentState.OnValueChanged += OnGlobalStateChanged;
            OnGlobalStateChanged(GameManager.Instance.CurrentState.Value, GameManager.Instance.CurrentState.Value);
        }
    }

    public override void OnNetworkDespawn()
    {
        CurrentState.OnValueChanged -= OnPlayerStateChanged;    
    }

    public void ChangePlayerState(PlayerState newState)
    {
        if (!IsOwner) return;

        CurrentState.Value = newState;
    }

    private void OnPlayerStateChanged(PlayerState previousState, PlayerState newState)
    {
        if (!IsOwner) return;

        switch (newState)
        {
            case PlayerState.Active:
                EnableMovement();
                break;
            case PlayerState.inMenu:
                DisableMovement();
                break;
            case PlayerState.inLobby:
                DisableMovement();
                break;
            case PlayerState.inTesting:
                EnableMovement();
                break;
            case PlayerState.Driving:
                DrivingControls();
                break;

        }
    }
    
    private void OnGlobalStateChanged(GameManager.GameState previousState, GameManager.GameState newState)
    {
        if (!IsOwner) return;

        if (newState == GameManager.GameState.Lobby)
        {
            ChangePlayerState(PlayerState.inLobby);
        }
        else if (newState == GameManager.GameState.inGame)
        {
            ChangePlayerState(PlayerState.Active);
        }
        else if (newState == GameManager.GameState.Testing)
        {
            ChangePlayerState(PlayerState.inTesting);
        }
    }

    void DisableMovement()
    {
        playerMovement.KillMovement();
        playerMovement.NoUseGravity();
        mouseLook.UnlockCursor();

        mouseLook.enabled = false;
        playerMovement.enabled = false;
        jump.enabled = false;
    }

    void EnableMovement()
    {
        playerMovement.UseGravity();
        playerMovement.UnprepareDriver();
        mouseLook.LockCursor();
        

        mouseLook.enabled = true;
        playerMovement.enabled = true;
        jump.enabled = true;
    }

    void DrivingControls()
    {
        playerMovement.KillMovement();
        playerMovement.PrepareDriver();
        playerMovement.enabled = false;
        jump.enabled = false;

        mouseLook.enabled = true;
        
        if (vanController != null)
        {
            vanController.enabled = true;
        }
        else
        {
            Debug.LogWarning("PlayerManager tried to enable driving controls, but VanController is null!");
        }
    }
}