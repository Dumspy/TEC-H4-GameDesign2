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
    }

    void Update()
    {
        if (gameStateManager == null) return;
        // Show/hide overlay based on game result
        bool showOverlay = gameStateManager.gameResult.Value != (int)GameResult.Ongoing;
        resultOverlay.style.display = showOverlay ? DisplayStyle.Flex : DisplayStyle.None;
        turnLabel.visible = !showOverlay;

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
        if (gameStateManager != null && gameStateManager.IsServer)
        {
            gameStateManager.ResetGameServerRpc();
        }
        else if (gameStateManager != null)
        {
            // If client, request server to reset
            gameStateManager.ResetGameServerRpc();
        }
    }
}
