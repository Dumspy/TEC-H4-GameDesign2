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

    private SlideDirectionUIManager slideDirectionUIManager;
    private ResultOverlayManager resultOverlayManager;
    private GameUIButtonManager buttonManager;

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
        TrySubscribeToSlideDirectionEvent();
    }

    private bool slideDirectionSubscribed = false;
    private void TrySubscribeToSlideDirectionEvent()
    {
        if (!slideDirectionSubscribed && GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnSlideDirectionChanged += ShowSlideDirection;
            slideDirectionSubscribed = true;
        }
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

        slideDirectionUIManager = new SlideDirectionUIManager(slideDirectionLabel);
        resultOverlayManager = new ResultOverlayManager(resultOverlay, resultLabel);
        buttonManager = new GameUIButtonManager(restartButton, slideButton);

        // Subscribe to slide direction event
        TrySubscribeToSlideDirectionEvent();
    }

    void OnDisable()
    {
        if (GameStateManager.Instance != null && slideDirectionSubscribed)
        {
            GameStateManager.Instance.OnSlideDirectionChanged -= ShowSlideDirection;
            slideDirectionSubscribed = false;
        }
    }

    void Update()
    {
        TrySubscribeToSlideDirectionEvent();
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
            buttonManager.SetSlideButtonEnabled(false);
            return;
        }
        bool bothPlayersPresent = !NetworkManager.Singleton.IsHost || GameStateManager.Instance != null && GameStateManager.Instance.playerSymbols.Count == 2;
        bool canSlide = bothPlayersPresent && !localPlayerController.hasUsedSlide.Value && GameStateManager.Instance.gameResult.Value == (int)GameResult.Ongoing;
        buttonManager.UpdateSlideButton(bothPlayersPresent, canSlide);
    }

    private void UpdateOverlayAndTurnLabelVisibility(bool showOverlay)
    {
        resultOverlayManager.UpdateOverlayAndTurnLabelVisibility(showOverlay, turnLabel);
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
        buttonManager.UpdateRestartButton(GameStateManager.Instance.restartRequests);
    }

    private void UpdateResultLabel()
    {
        resultOverlayManager.UpdateResultLabel((GameResult)GameStateManager.Instance.gameResult.Value);
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
        buttonManager.SetSlideButtonEnabled(false);
        // UI indication now handled by ClientRpc
    }

    public void ShowSlideDirection(SlideDirection direction)
    {
        slideDirectionUIManager.ShowSlideDirection(direction);
        CancelInvoke(nameof(HideSlideDirection));
        Invoke(nameof(HideSlideDirection), 1f);
    }

    public void HideSlideDirection()
    {
        slideDirectionUIManager.HideSlideDirection();
    }
}
