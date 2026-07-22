#if UNITY_EDITOR
using UnityEngine;
using Unity.Netcode;

public class NetworkDebugHelper : MonoBehaviour
{
    [Header("Debug Control")]
    [Tooltip("Automatically start a local host session the moment Play Mode begins.")]
    [SerializeField] private bool autoHostOnPlay = true;

    [Header("Fallback Setup")]
    [Tooltip("Drag your NetworkManager prefab here. If you hit Play directly from this scene, it will instantiate the missing managers instantly.")]
    [SerializeField] private GameObject networkManagerPrefab;

    void Awake()
    {
        // If we bypass the Bootstrap scene, NetworkManager won't exist. 
        // This dynamically safe-guards our testing environment by creating one.
        if (NetworkManager.Singleton == null && networkManagerPrefab != null)
        {
            InstancyDebugArchitecture();
        }
    }

    void Start()
    {
        // If auto-host is active and the network is asleep, force a local connection
        if (autoHostOnPlay && NetworkManager.Singleton != null && !NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.StartHost();
            
            // Bypass the main menu cursor lock rule and force the lock state locally
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void InstancyDebugArchitecture()
    {
        GameObject spawnedManager = Instantiate(networkManagerPrefab);
        spawnedManager.name = "[DEBUG] NetworkManager";
    }
}
#endif