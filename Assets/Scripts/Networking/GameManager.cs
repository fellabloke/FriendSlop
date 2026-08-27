using UnityEngine;
using Unity.Netcode;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }
    public enum GameState
    {
        Lobby,
        inGame,
        Testing
    }

    public NetworkVariable<GameState> CurrentState = new NetworkVariable<GameState>(
        GameState.Lobby,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else Destroy(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        CurrentState.OnValueChanged += OnGameStateChanged;
    }

    public override void OnNetworkDespawn()
    {
        CurrentState.OnValueChanged -= OnGameStateChanged;    
    }

    public void ChangeState(GameState newState)
    {
        if (!IsServer) return;

        CurrentState.Value = newState;
    }

    private void OnGameStateChanged(GameState previousState, GameState newState)
    {
        Debug.Log($"[GameManager] State changed from {previousState} to {newState}");

        switch (newState)
        {
            case GameState.Lobby:
                break;
            case GameState.inGame:
                break;
            case GameState.Testing:
                break;
        }
    }
}
