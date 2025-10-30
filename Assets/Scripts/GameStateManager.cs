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

    public void SlideBoard(SlideDirection direction)
    {
        BoardManager.SlideBoard(boardState, direction);
        lastSlideDirection = direction;
        PieceManager.UpdatePieceVisuals(boardState, cellPositions, placeablePrefab);
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
            AssignPlayerSymbol(playerSymbols, hostId);
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
        AssignPlayerSymbol(playerSymbols, clientId);
    }



    [ServerRpc(RequireOwnership = false)]
    public void MakeMoveServerRpc(int cellIndex, int playerSymbol)
    {
        if (gameResult.Value != (int)GameResult.Ongoing) {
            return;
        }
        if (cellIndex < 0 || cellIndex >= boardState.Count) {
            Debug.LogError($"GameStateManager: Invalid cellIndex {cellIndex}");
            return;
        }
        if (boardState[cellIndex] != (int)PlayerSymbol.None) {
            return; // Cell already occupied
        }
        if (playerSymbol != currentTurn.Value) {
            return; // Not player's turn
        }

        boardState[cellIndex] = playerSymbol;
        currentTurn.Value = (playerSymbol == (int)PlayerSymbol.X) ? (int)PlayerSymbol.O : (int)PlayerSymbol.X;

        // Networked instantiation of the placeable prefab
        SpawnPiece(cellIndex, playerSymbol);

        // Win/draw detection
        gameResult.Value = (int)BoardManager.CheckGameResult(boardState);
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
        for (int i = 0; i < boardState.Count; i++)
        {
            boardState[i] = (int)PlayerSymbol.None;
        }
        // Alternate starting player
        lastStartingPlayer.Value = (lastStartingPlayer.Value == (int)PlayerSymbol.X) ? (int)PlayerSymbol.O : (int)PlayerSymbol.X;
        currentTurn.Value = lastStartingPlayer.Value;
        gameResult.Value = (int)GameResult.Ongoing;
        
        // Reset slide usage for all players
        var playerControllers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var pc in playerControllers)
        {
            pc.hasUsedSlide.Value = false;
        }

        var pieces = GameObject.FindGameObjectsWithTag("Piece");
        foreach (var piece in pieces)
        {
            var netObj = piece.GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsSpawned)
                netObj.Despawn();
            else
                Destroy(piece);
        }
        restartRequests.Clear();
    }

    public static void AssignPlayerSymbol(Dictionary<ulong, int> playerSymbols, ulong clientId)
    {
        if (!playerSymbols.ContainsKey(clientId))
        {
            int symbol = playerSymbols.Count == 0 ? (int)PlayerSymbol.X : (int)PlayerSymbol.O;
            playerSymbols[clientId] = symbol;
            var playerControllers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            foreach (var pc in playerControllers)
            {
                if (pc.OwnerClientId == clientId && pc.IsSpawned)
                {
                    pc.playerSymbol.Value = symbol;
                    break;
                }
            }
        }
    }
}
