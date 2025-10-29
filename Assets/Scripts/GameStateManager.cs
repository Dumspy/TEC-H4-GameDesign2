using UnityEngine;
using Unity.Netcode;
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

public class GameStateManager : NetworkBehaviour
{
    // Track which players have requested a restart
    public NetworkList<ulong> restartRequests;
    // Track who started last game (X or O)
    public NetworkVariable<int> lastStartingPlayer = new NetworkVariable<int>((int)PlayerSymbol.X);
    public NetworkList<int> boardState;
    public NetworkVariable<int> currentTurn = new NetworkVariable<int>((int)PlayerSymbol.X);
    public NetworkVariable<int> gameResult = new NetworkVariable<int>((int)GameResult.Ongoing);

    [Header("Prefabs & Materials")]
    public GameObject placeablePrefab;
    public Material xMaterial;
    public Material oMaterial;

    // You may want to set these in the Inspector
    public Transform[] cellPositions; // Array of cell transforms (set in editor or at runtime)

    // Track player symbols by clientId
    private Dictionary<ulong, int> playerSymbols = new Dictionary<ulong, int>();

    private void Awake()
    {
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
        }
    }

    private void Start()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
        else
        {
            Debug.LogError("GameStateManager: NetworkManager.Singleton is null in Start!");
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
        
        if (!playerSymbols.ContainsKey(clientId))
        {
            int symbol = playerSymbols.Count == 0 ? (int)PlayerSymbol.X : (int)PlayerSymbol.O;
            playerSymbols[clientId] = symbol;
            var playerControllers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            foreach (var pc in playerControllers)
            {
                if (pc.OwnerClientId == clientId)
                {
                    pc.playerSymbol.Value = symbol;
                    break;
                }
            }
        }
    }



    public int GetLocalPlayerSymbol()
    {
        ulong localId = NetworkManager.Singleton.LocalClientId;
        // Use NetworkVariable on PlayerController for local symbol
        var playerControllers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var pc in playerControllers)
        {
            if (pc.IsOwner)
            {
                return pc.playerSymbol.Value;
            }
        }
        return (int)PlayerSymbol.None;
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

    private void SpawnPiece(int cellIndex, int playerSymbol)
    {
        if (cellPositions == null || cellPositions.Length <= cellIndex) {
            Debug.LogError($"GameStateManager: cellPositions is null or index {cellIndex} out of bounds");
            return;
        }
        Transform cellTransform = cellPositions[cellIndex];
        GameObject piece = Instantiate(placeablePrefab, cellTransform.position, Quaternion.identity);
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
            pieceSync.xMaterial = xMaterial;
            pieceSync.oMaterial = oMaterial;
            pieceSync.playerSymbol.Value = playerSymbol;
        }
        else
        {
            Debug.LogError($"GameStateManager: No PieceNetworkSync component found on piece prefab!");
        }
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
        
        var pieces = GameObject.FindGameObjectsWithTag("Piece");
        foreach (var piece in pieces)
        {
            var netObj = piece.GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsSpawned)
                netObj.Despawn();
            else
                GameObject.Destroy(piece);
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

