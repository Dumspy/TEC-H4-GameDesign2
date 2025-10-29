using UnityEngine;
using UnityEngine.UIElements;

public class GameUIController : MonoBehaviour
{
    public UIDocument uiDocument;
    private Label turnLabel;
    private Label resultLabel;
    private Button restartButton;
    private VisualElement resultOverlay;

    private GameStateManager gameStateManager;
    private PlayerController localPlayerController;

    public void RegisterLocalPlayer(PlayerController pc)
    {
        localPlayerController = pc;
    }

    void Awake()
    {
        gameStateManager = FindFirstObjectByType<GameStateManager>();
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
        if (gameStateManager == null) return;
        // Show/hide overlay based on game result
        bool showOverlay = gameStateManager.gameResult.Value != (int)GameResult.Ongoing;
        resultOverlay.style.display = showOverlay ? DisplayStyle.Flex : DisplayStyle.None;
        turnLabel.visible = !showOverlay;

        // Update turn label to "Your turn" or "Opponent's turn"
        if (!showOverlay)
        {
            // Find the local PlayerController and use its playerSymbol
            if (localPlayerController != null && localPlayerController.IsSpawned)
            {
                int currentTurn = gameStateManager.currentTurn.Value;
                if (localPlayerController.playerSymbol.Value == currentTurn)
                {
                    turnLabel.text = "Your turn";
                }
                else
                {
                    turnLabel.text = "Opponent's turn";
                }
            }
            else
            {
                turnLabel.text = "Waiting for network...";
            }
        }

        // Show restart progress (X/2)
        if (showOverlay)
        {
            int restartCount = gameStateManager.restartRequests != null ? gameStateManager.restartRequests.Count : 0;
            restartButton.text = $"Restart ({restartCount}/2)";
            // Enable button if not already clicked by this player
            ulong localId = Unity.Netcode.NetworkManager.Singleton.LocalClientId;
            bool alreadyClicked = gameStateManager.restartRequests != null && gameStateManager.restartRequests.Contains(localId);
            restartButton.SetEnabled(!alreadyClicked);
        }
        else
        {
            restartButton.text = "Restart";
            restartButton.SetEnabled(true);
        }

        // Update result label and color
        switch ((GameResult)gameStateManager.gameResult.Value)
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
        if (gameStateManager != null)
        {
            ulong localId = Unity.Netcode.NetworkManager.Singleton.LocalClientId;
            gameStateManager.RequestRestartServerRpc(localId);
            restartButton.SetEnabled(false); // Disable button after click
        }
    }
}
