using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class TestingOverseer : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        StartCoroutine(WaitForGameManager());
    }

    private IEnumerator WaitForGameManager()
    {
        while (GameManager.Instance == null && IsSpawned)
        {
            yield return null; 
        }

        if (IsSpawned)
        {
            Debug.Log("Found GameManager");
            GameManager.Instance.ChangeState(GameManager.GameState.Testing);
        }
    }
}
