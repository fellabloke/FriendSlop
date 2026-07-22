using UnityEngine;
using UnityEngine.SceneManagement;

public class ServerBootstrapper : MonoBehaviour
{
    [SerializeField] private string lobbySceneName = "Menu";
    void Start()
    {
        SceneManager.LoadScene(lobbySceneName, LoadSceneMode.Additive);
    }
}
