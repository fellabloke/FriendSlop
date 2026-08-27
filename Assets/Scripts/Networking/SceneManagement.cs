using UnityEngine.SceneManagement;
using Unity.Netcode;
using UnityEngine;

public class SceneManagement : NetworkBehaviour
{
    public static SceneManagement Instance { get; private set; }
    
    [Header("Scenes")]
    [SerializeField] private string menuSceneName = "Menu";
    [SerializeField] private string gameSceneName = "Game";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else Destroy(gameObject);
    }

    void Start()
    {
        SceneManager.LoadScene(menuSceneName, LoadSceneMode.Additive);
    }

    public override void OnNetworkSpawn()
    {
        NetworkManager.Singleton.SceneManager.OnSceneEvent += SceneManagerOnSceneEvent;
        base.OnNetworkSpawn();
    }

    public void LoadScene(string _sceneName)
    {
        if (IsServer && !string.IsNullOrEmpty(_sceneName))
        {
            var status = NetworkManager.Singleton.SceneManager.LoadScene(_sceneName, LoadSceneMode.Additive);
            CheckStatus(status, true);
        }
    }

    private void UnloadLocalMenu()
    {
        Scene menuScene = SceneManager.GetSceneByName(menuSceneName);
        if (menuScene.IsValid() && menuScene.isLoaded)
        {
            SceneManager.UnloadSceneAsync(menuScene);
        }
    }

    private void CheckStatus(SceneEventProgressStatus status, bool isLoading = true)
    {
        var sceneEventAction = isLoading ? "load" : "unload";
        if (status != SceneEventProgressStatus.Started)
        {
            Debug.LogWarning($"Failed to {sceneEventAction} with status: {status}");
        }
    }

    private void SceneManagerOnSceneEvent(SceneEvent sceneEvent)
    {
        var clientOrServer = sceneEvent.ClientId == NetworkManager.ServerClientId ? "server" : "client";

        switch (sceneEvent.SceneEventType)
        {
            case SceneEventType.LoadComplete:

                if (sceneEvent.ClientId == NetworkManager.Singleton.LocalClientId && sceneEvent.SceneName == gameSceneName)
                {
                    UnloadLocalMenu();
                }
                break;

            case SceneEventType.SynchronizeComplete:
                if (sceneEvent.ClientId == NetworkManager.Singleton.LocalClientId)
                {
                    UnloadLocalMenu();
                }
                break;

            case SceneEventType.UnloadComplete:
                break;

            case SceneEventType.LoadEventCompleted:
            case SceneEventType.UnloadEventCompleted:
                var loadUnload = sceneEvent.SceneEventType == SceneEventType.LoadEventCompleted ? "Load" : "Unload";
                break;
        }
    }
}