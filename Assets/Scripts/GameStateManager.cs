using UnityEngine;
using Unity.Netcode;
// Ensure GameModeManager is accessible

using System.Collections.Generic;

public enum PlayerSymbol
{
    None = 0,
    X = 1,
    O = 2
}

public enum GameResult
{
    Ongoing = 0,
    XWins = 1,
    OWins = 2,
    Draw = 3
}

public enum SlideDirection
{
    Up,
    Down,
    Left,
    Right
}

public class GameStateManager : NetworkBehaviour
{
    // Track which players have requested a restart
    public NetworkList<ulong> restartRequests;
    // Track who started last game (X or O)
    public NetworkVariable<int> lastStartingPlayer = new NetworkVariable<int>((int)PlayerSymbol.X);
    public NetworkList<int> boardState;
    public NetworkVariable<int> currentTurn = new NetworkVariable<int>((int)PlayerSymbol.X);
    public NetworkVariable<int> gameResult = new NetworkVariable<int>((int)GameResult.Ongoing);

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

    // Debug: Slide direction selector for editor
    public SlideDirection debugSlideDirection = SlideDirection.Up;

    // Helper: Convert (row, col) to board index
    private int GetIndex(int row, int col)
    {
        return row * 3 + col;
    }
    // Slide the board in a direction
    public void SlideBoard(SlideDirection direction)
    {
        int[] newBoard = new int[9];
        for (int i = 0; i < 9; i++)
            newBoard[i] = (int)PlayerSymbol.None;

        lastSlideDirection = direction;
        switch (direction)
        {
            case SlideDirection.Up:
                for (int col = 0; col < 3; col++)
                {
                    int targetRow = 0;
                    for (int row = 0; row < 3; row++)
                    {
                        int idx = GetIndex(row, col);
                        int symbol = boardState[idx];
                        if (symbol != (int)PlayerSymbol.None)
                        {
                            int targetIdx = GetIndex(targetRow, col);
                            newBoard[targetIdx] = symbol;
                            targetRow++;
                        }
                    }
                }
                break;
            case SlideDirection.Down:
                for (int col = 0; col < 3; col++)
                {
                    int targetRow = 2;
                    for (int row = 2; row >= 0; row--)
                    {
                        int idx = GetIndex(row, col);
                        int symbol = boardState[idx];
                        if (symbol != (int)PlayerSymbol.None)
                        {
                            int targetIdx = GetIndex(targetRow, col);
                            newBoard[targetIdx] = symbol;
                            targetRow--;
                        }
                    }
                }
                break;
            case SlideDirection.Left:
                for (int row = 0; row < 3; row++)
                {
                    int targetCol = 0;
                    for (int col = 0; col < 3; col++)
                    {
                        int idx = GetIndex(row, col);
                        int symbol = boardState[idx];
                        if (symbol != (int)PlayerSymbol.None)
                        {
                            int targetIdx = GetIndex(row, targetCol);
                            newBoard[targetIdx] = symbol;
                            targetCol++;
                        }
                    }
                }
                break;
            case SlideDirection.Right:
                for (int row = 0; row < 3; row++)
                {
                    int targetCol = 2;
                    for (int col = 2; col >= 0; col--)
                    {
                        int idx = GetIndex(row, col);
                        int symbol = boardState[idx];
                        if (symbol != (int)PlayerSymbol.None)
                        {
                            int targetIdx = GetIndex(row, targetCol);
                            newBoard[targetIdx] = symbol;
                            targetCol--;
                        }
                    }
                }
                break;
        }
        // Update boardState
        for (int i = 0; i < 9; i++)
            boardState[i] = newBoard[i];

        // Update piece visuals after sliding
        UpdatePieceVisuals();
    }

    // Update piece GameObjects to match boardState
    private void UpdatePieceVisuals()
    {
        // Find all piece GameObjects
        var pieces = GameObject.FindGameObjectsWithTag("Piece");
        // Map: cellIndex -> piece GameObject
        Dictionary<int, GameObject> cellToPiece = new Dictionary<int, GameObject>();
        Dictionary<int, Vector3> oldPositions = new Dictionary<int, Vector3>();
        foreach (var piece in pieces)
        {
            var sync = piece.GetComponent<PieceNetworkSync>();
            if (sync != null)
            {
                cellToPiece[sync.cellIndex] = piece;
                oldPositions[sync.cellIndex] = piece.transform.position;
            }
        }
        // For each cell, update or spawn/destroy as needed
        for (int i = 0; i < 9; i++)
        {
            int symbol = boardState[i];
            if (symbol == (int)PlayerSymbol.None)
            {
                // If a piece exists here, destroy it
                if (cellToPiece.ContainsKey(i))
                {
                    var netObj = cellToPiece[i].GetComponent<NetworkObject>();
                    if (netObj != null && netObj.IsSpawned)
                        netObj.Despawn();
                    else
                        Destroy(cellToPiece[i]);
                }
            }
            else
            {
                // Only respawn if symbol changed
                if (cellToPiece.ContainsKey(i))
                {
                    var sync = cellToPiece[i].GetComponent<PieceNetworkSync>();
                    if (sync.playerSymbol.Value != symbol)
                    {
                        var netObj = cellToPiece[i].GetComponent<NetworkObject>();
                        if (netObj != null && netObj.IsSpawned)
                            netObj.Despawn();
                        else
                            GameObject.Destroy(cellToPiece[i]);
                        var newPiece = SpawnPiece(i, symbol);
                        newPiece.transform.position = cellPositions[i].position;
                        Debug.Log($"[Slide] Respawned piece snapped to {cellPositions[i].position} (cell {i})");
                    }
                    else
                    {
                        // Animate piece to correct position if moved
                        Vector3 newPos = cellPositions[i].position;
                        Vector3 oldPos = oldPositions.ContainsKey(i) ? oldPositions[i] : newPos;
                        if ((oldPos - newPos).sqrMagnitude > 0.001f)
                        {
                            cellToPiece[i].transform.position = newPos;
                            Debug.Log($"[Slide] Moving piece snapped to {newPos} (cell {i})");
                        }
                        sync.cellIndex = i;
                    }
                }
                else
                {
                    // No piece exists, spawn it
                    var newPiece = SpawnPiece(i, symbol);
                    Debug.Log($"[Slide] New piece spawned at {cellPositions[i].position} (cell {i})");
                    // No animation needed for new pieces (they appear at their cell)
                }
            }
        }
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
            AssignPlayerSymbol(hostId);
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
        AssignPlayerSymbol(clientId);
    }

    public void AssignPlayerSymbol(ulong clientId)
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
        gameResult.Value = (int)CheckGameResult();
    }

    private GameObject SpawnPiece(int cellIndex, int playerSymbol, Vector3? startPosition = null)
    {
        if (cellPositions == null || cellPositions.Length <= cellIndex) {
            Debug.LogError($"GameStateManager: cellPositions is null or index {cellIndex} out of bounds");
            return null;
        }
        Transform cellTransform = cellPositions[cellIndex];
        Vector3 spawnPos = startPosition ?? cellTransform.position;
        GameObject piece = Instantiate(placeablePrefab, spawnPos, Quaternion.identity);
        var netObj = piece.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.Spawn(); // Networked spawn
        }
        else
        {
            Debug.LogError($"GameStateManager: No NetworkObject component found on piece prefab!");
        }
        // Assign material based on player
        var pieceSync = piece.GetComponent<PieceNetworkSync>();
        if (pieceSync != null)
        {
            pieceSync.playerSymbol.Value = playerSymbol;
            pieceSync.cellIndex = cellIndex;
        }
        else
        {
            Debug.LogError($"GameStateManager: No PieceNetworkSync component found on piece prefab!");
        }
        return piece;
    }

    // Utility: Get board as array
    public int[] GetBoardArray()
    {
        int[] arr = new int[boardState.Count];
        for (int i = 0; i < boardState.Count; i++)
        {
            arr[i] = boardState[i];
        }
        return arr;
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

    // Win/draw detection
    private GameResult CheckGameResult()
    {
        int[] b = GetBoardArray();
        int[][] winPatterns = new int[][]
        {
            new int[] {0,1,2}, new int[] {3,4,5}, new int[] {6,7,8}, // rows
            new int[] {0,3,6}, new int[] {1,4,7}, new int[] {2,5,8}, // cols
            new int[] {0,4,8}, new int[] {2,4,6} // diags
        };
        foreach (var pattern in winPatterns)
        {
            if (b[pattern[0]] != (int)PlayerSymbol.None &&
                b[pattern[0]] == b[pattern[1]] &&
                b[pattern[1]] == b[pattern[2]])
            {
                return b[pattern[0]] == (int)PlayerSymbol.X ? GameResult.XWins : GameResult.OWins;
            }
        }
        // Draw if all cells filled
        bool allFilled = true;
        for (int i = 0; i < b.Length; i++)
        {
            if (b[i] == (int)PlayerSymbol.None)
            {
                allFilled = false;
                break;
            }
        }
        if (allFilled) return GameResult.Draw;
        return GameResult.Ongoing;
    }
}

