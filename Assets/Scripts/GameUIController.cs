using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

public class GameUIController : MonoBehaviour
{
    public UIDocument uiDocument;
    private Label turnLabel;
    private Label resultLabel;
    private Button restartButton;
    private Button slideButton;
    private Label slideDirectionLabel;
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
        slideButton = turnBar.Q<Button>("SlideButton");
        slideDirectionLabel = turnBar.Q<Label>("SlideDirectionLabel");
        resultLabel = root.Q<Label>("ResultLabel");
        restartButton = root.Q<Button>("RestartButton");
        resultOverlay = root.Q<VisualElement>("ResultOverlay");
        restartButton.clicked += OnRestartClicked;
        restartButton.text = "Restart";
        slideButton.clicked += OnSlideClicked;
        slideButton.text = "Slide";
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
            slideButton.SetEnabled(false);
            return;
        }

        UpdateTurnLabelText();
        UpdateSlideButton();
    }

    private void UpdateSlideButton()
    {
        if (localPlayerController == null || !localPlayerController.IsSpawned)
        {
            slideButton.SetEnabled(false);
            return;
        }
        bool bothPlayersPresent = !NetworkManager.Singleton.IsHost || GameStateManager.Instance != null && GameStateManager.Instance.playerSymbols.Count == 2;
        bool canSlide = bothPlayersPresent && !localPlayerController.hasUsedSlide.Value && GameStateManager.Instance.gameResult.Value == (int)GameResult.Ongoing;
        slideButton.SetEnabled(canSlide);
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
        ulong localId = NetworkManager.Singleton.LocalClientId;
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
            ulong localId = NetworkManager.Singleton.LocalClientId;
            GameStateManager.Instance.RequestRestartServerRpc(localId);
            restartButton.SetEnabled(false); // Disable button after click
        }
    }

    void OnSlideClicked()
    {
        if (localPlayerController == null) return;
        // Pick a random direction
        SlideDirection direction = (SlideDirection)Random.Range(0, 4);
        localPlayerController.TrySlideBoard(direction);
        slideButton.SetEnabled(false);
        ShowSlideDirection(direction);
    }

    private void ShowSlideDirection(SlideDirection direction)
    {
        string dirText = direction switch
        {
            SlideDirection.Up => "Board slid UP!",
            SlideDirection.Down => "Board slid DOWN!",
            SlideDirection.Left => "Board slid LEFT!",
            SlideDirection.Right => "Board slid RIGHT!",
            _ => "Board slid!"
        };
        slideDirectionLabel.text = dirText;
        slideDirectionLabel.style.display = DisplayStyle.Flex;
        CancelInvoke(nameof(HideSlideDirection));
        Invoke(nameof(HideSlideDirection), 1f);
    }

    private void HideSlideDirection()
    {
        slideDirectionLabel.style.display = DisplayStyle.None;
    }
}
