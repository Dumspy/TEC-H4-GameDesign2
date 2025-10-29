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
        SceneManager.LoadScene("Game");
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
        // Do NOT load scene here; server will move us
    }
}
