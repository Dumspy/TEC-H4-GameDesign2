using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using Unity.Netcode;

[RequireComponent(typeof(UIDocument))]
public class MainMenuController : MonoBehaviour
{
    private Button singleplayerButton;
    private Button hostButton;
    private Button joinButton;

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        singleplayerButton = root.Q<Button>("SingleplayerButton");
        hostButton = root.Q<Button>("HostButton");
        joinButton = root.Q<Button>("JoinButton");

        singleplayerButton.clicked += OnSingleplayerClicked;
        hostButton.clicked += OnHostClicked;
        joinButton.clicked += OnJoinClicked;
    }

    void OnSingleplayerClicked()
    {
        GameModeManager.SelectedMode = GameModeManager.GameMode.Singleplayer;
        NetworkManager.Singleton.StartHost();
        NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);
        // Optionally: block external connections in singleplayer
        // Example: NetworkManager.Singleton.ConnectionApprovalCallback = (request, response) => { response.Approved = false; };
    }

    void OnHostClicked()
    {
        GameModeManager.SelectedMode = GameModeManager.GameMode.Host;
        NetworkManager.Singleton.StartHost();
        NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);
    }

    void OnJoinClicked()
    {
        GameModeManager.SelectedMode = GameModeManager.GameMode.Client;
        NetworkManager.Singleton.StartClient();
    }
}
