using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;


public class GameStateManager : NetworkBehaviour
{
    // Track which players have requested a restart
    public NetworkList<ulong> restartRequests;
    // Track who started last game (X or O)
    public NetworkVariable<int> lastStartingPlayer = new((int)PlayerSymbol.X);
    public NetworkList<int> boardState;
    public NetworkVariable<int> currentTurn = new((int)PlayerSymbol.X);
    public NetworkVariable<int> gameResult = new((int)GameResult.Ongoing);

    // Store last slide direction for UI/animation
    public SlideDirection lastSlideDirection = SlideDirection.Up;

    [Header("Prefabs & Materials")]
    public GameObject placeablePrefab;
    public Material xMaterial;
    public Material oMaterial;

    // You may want to set these in the Inspector
    public Transform[] cellPositions; // Array of cell transforms (set in editor or at runtime)

    // Track player symbols by clientId
    public Dictionary<ulong, int> playerSymbols = new();

    // Track slide usage per player
    private Dictionary<int, bool> slideUsedBySymbol = new();

    public void SlideBoard(SlideDirection direction)
    {
        BoardManager.SlideBoard(boardState, direction);
        lastSlideDirection = direction;
        PieceManager.UpdatePieceVisuals(boardState, cellPositions, placeablePrefab);
        ShowSlideDirectionClientRpc(direction);
    }

    // ServerRpc for sliding the board, matching move handling
    [ServerRpc(RequireOwnership = false)]
    public void SlideBoardServerRpc(SlideDirection direction, int playerSymbol)
    {
        if (!CanSlide(playerSymbol)) return;
        slideUsedBySymbol[playerSymbol] = true;
        var playerControllers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var pc in playerControllers)
        {
            if (pc.playerSymbol.Value == playerSymbol)
            {
                pc.hasUsedSlide.Value = true;
                break;
            }
        }
        SlideBoard(direction);
    }

    private bool CanSlide(int playerSymbol)
    {
        if (gameResult.Value != (int)GameResult.Ongoing) return false;
        if (!playerSymbols.ContainsValue(playerSymbol)) return false;
        if (slideUsedBySymbol.ContainsKey(playerSymbol) && slideUsedBySymbol[playerSymbol]) return false;
        return true;
    }


    public event System.Action<SlideDirection> OnSlideDirectionChanged;

    [ClientRpc]
    private void ShowSlideDirectionClientRpc(SlideDirection direction)
    {
        OnSlideDirectionChanged?.Invoke(direction);
    }
    
    public static GameStateManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        boardState = new NetworkList<int>();
        restartRequests = new NetworkList<ulong>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            if (boardState.Count == 0)
            {
                for (int i = 0; i < 9; i++)
                {
                    boardState.Add((int)PlayerSymbol.None);
                }
            }
            // Assign host symbol if not set
            ulong hostId = NetworkManager.Singleton.LocalClientId;
            PlayerManager.AssignPlayerSymbol(playerSymbols, hostId);
        }
    }

    private void Start()
    {
        // Reference GameModeManager directly (global static class)
        if (GameModeManager.SelectedMode == GameModeManager.GameMode.Singleplayer)
        {
            Debug.Log("GameStateManager: Singleplayer mode selected. AI should be enabled.");
            // TODO: Add AI logic here
        }
        else
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            }
            else
            {
                Debug.LogError("GameStateManager: NetworkManager.Singleton is null in Start!");
            }
        }

        // Defensive checks
        if (placeablePrefab == null)
            Debug.LogError("GameStateManager: placeablePrefab is not assigned!");
        if (xMaterial == null)
            Debug.LogError("GameStateManager: xMaterial is not assigned!");
        if (oMaterial == null)
            Debug.LogError("GameStateManager: oMaterial is not assigned!");
        if (cellPositions == null || cellPositions.Length != 9)
            Debug.LogError("GameStateManager: cellPositions must be assigned and have 9 elements!");
    }

    public override void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            
        base.OnDestroy();
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;
        PlayerManager.AssignPlayerSymbol(playerSymbols, clientId);
    }



    [ServerRpc(RequireOwnership = false)]
    public void MakeMoveServerRpc(int cellIndex, int playerSymbol)
    {
        if (!IsValidMove(cellIndex, playerSymbol)) {
            return;
        }
        if (!BoardManager.MakeMove(boardState, cellIndex, playerSymbol)) {
            return;
        }
        currentTurn.Value = (playerSymbol == (int)PlayerSymbol.X) ? (int)PlayerSymbol.O : (int)PlayerSymbol.X;
        SpawnPiece(cellIndex, playerSymbol);
        gameResult.Value = (int)BoardManager.CheckGameResult(boardState);
    }

    private bool IsValidMove(int cellIndex, int playerSymbol)
    {
        if (gameResult.Value != (int)GameResult.Ongoing) return false;
        if (cellIndex < 0 || cellIndex >= boardState.Count) return false;
        if (boardState[cellIndex] != (int)PlayerSymbol.None) return false;
        if (playerSymbol != currentTurn.Value) return false;
        return true;
    }


    private GameObject SpawnPiece(int cellIndex, int playerSymbol, Vector3? startPosition = null)
    {
        return PieceManager.SpawnPiece(cellIndex, playerSymbol, cellPositions, placeablePrefab, startPosition);
    }

    // Game reset logic
    [ServerRpc(RequireOwnership = false)]
    public void RequestRestartServerRpc(ulong clientId)
    {
        if (!restartRequests.Contains(clientId))
        {
            restartRequests.Add(clientId);
        }
        // Only restart if both players have requested
        if (restartRequests.Count >= 2)
        {
            ResetGameInternal();
        }
    }

    private void ResetGameInternal()
    {
        ResetBoard();
        ResetPlayers();
        ResetSlideUsage();
        ClearPieces();
        ClearRestartRequests();
    }

    private void ResetBoard()
    {
        BoardManager.ResetBoard(boardState);
        gameResult.Value = (int)GameResult.Ongoing;
    }

    private void ResetPlayers()
    {
        // Alternate starting player
        lastStartingPlayer.Value = (lastStartingPlayer.Value == (int)PlayerSymbol.X) ? (int)PlayerSymbol.O : (int)PlayerSymbol.X;
        currentTurn.Value = lastStartingPlayer.Value;
    }

    private void ResetSlideUsage()
    {
        var playerControllers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var pc in playerControllers)
        {
            pc.hasUsedSlide.Value = false;
        }
    }

    private void ClearPieces()
    {
        PieceManager.ClearAllPieces();
    }

    private void ClearRestartRequests()
    {
        restartRequests.Clear();
    }

    // Moved to PlayerManager.cs

}
