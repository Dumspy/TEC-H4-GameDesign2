using UnityEngine;
using UnityEngine.UIElements;

public class GameUIController : MonoBehaviour
{
    public UIDocument uiDocument;
    private Label turnLabel;
    private Label resultLabel;
    private Button restartButton;
    private VisualElement resultOverlay;
    private PlayerController localPlayerController;
    public bool playerRegistered = false;

    public void RegisterLocalPlayer(PlayerController pc)
    {
        localPlayerController = pc;
        playerRegistered = true;
    }

    public static GameUIController Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void OnEnable()
    {
        var root = uiDocument.rootVisualElement;
        var turnBar = root.Q<VisualElement>("TurnBar");
        turnLabel = turnBar.Q<Label>("TurnLabel");
        resultLabel = root.Q<Label>("ResultLabel");
        restartButton = root.Q<Button>("RestartButton");
        resultOverlay = root.Q<VisualElement>("ResultOverlay");
        restartButton.clicked += OnRestartClicked;
        restartButton.text = "Restart";
    }

    void Update()
    {
        if (GameStateManager.Instance == null) return;
        bool showOverlay = GameStateManager.Instance.gameResult.Value != (int)GameResult.Ongoing;
        UpdateOverlayAndTurnLabelVisibility(showOverlay);
        
        if (showOverlay)
        {
            UpdateResultLabel();
            UpdateRestartButton();
            
            return;
        }

        UpdateTurnLabelText();
    }

    private void UpdateOverlayAndTurnLabelVisibility(bool showOverlay)
    {
        resultOverlay.style.display = showOverlay ? DisplayStyle.Flex : DisplayStyle.None;
        turnLabel.visible = !showOverlay;
    }

    private void UpdateTurnLabelText()
    {
        // Show 'Waiting for opponent...' if only one player is present
        if (GameStateManager.Instance != null && GameStateManager.Instance.playerSymbols.Count == 1)
        {
            turnLabel.text = "Waiting for opponent...";
            return;
        }
        
        if (localPlayerController != null && localPlayerController.IsSpawned)
        {
            int currentTurn = GameStateManager.Instance.currentTurn.Value;
            turnLabel.text = localPlayerController.playerSymbol.Value == currentTurn ? "Your turn" : "Opponent's turn";
            return;
        }
        
        turnLabel.text = "";
    }

    private void UpdateRestartButton()
    {
        int restartCount = GameStateManager.Instance.restartRequests != null ? GameStateManager.Instance.restartRequests.Count : 0;
        restartButton.text = $"Restart ({restartCount}/2)";
        ulong localId = Unity.Netcode.NetworkManager.Singleton.LocalClientId;
        bool alreadyClicked = GameStateManager.Instance.restartRequests != null && GameStateManager.Instance.restartRequests.Contains(localId);
        restartButton.SetEnabled(!alreadyClicked);
    }

    private void UpdateResultLabel()
    {
        switch ((GameResult)GameStateManager.Instance.gameResult.Value)
        {
            case GameResult.XWins:
                resultLabel.text = "X Wins!";
                resultLabel.style.color = new StyleColor(new Color(33f/255f, 150f/255f, 243f/255f)); // blue
                break;
            case GameResult.OWins:
                resultLabel.text = "O Wins!";
                resultLabel.style.color = new StyleColor(new Color(220f/255f, 20f/255f, 60f/255f)); // red
                break;
            case GameResult.Draw:
                resultLabel.text = "Draw!";
                resultLabel.style.color = new StyleColor(new Color(255f/255f, 193f/255f, 7f/255f)); // yellow
                break;
            default:
                resultLabel.text = "";
                resultLabel.style.color = new StyleColor(Color.clear);
                break;
        }
    }

    void OnRestartClicked()
    {
        if (GameStateManager.Instance != null)
        {
            ulong localId = Unity.Netcode.NetworkManager.Singleton.LocalClientId;
            GameStateManager.Instance.RequestRestartServerRpc(localId);
            restartButton.SetEnabled(false); // Disable button after click
        }
    }
}
