using UnityEngine;
using Unity.Netcode;

public class PlayerController : NetworkBehaviour
{
    public NetworkVariable<int> playerSymbol = new((int)PlayerSymbol.None, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> hasUsedSlide = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        Debug.Log($"PlayerController OnNetworkSpawn: clientId={NetworkManager.Singleton.LocalClientId}, OwnerClientId={OwnerClientId}, playerSymbol={playerSymbol.Value}");
        if (IsOwner && GameUIController.Instance != null)
        {
            GameUIController.Instance.RegisterLocalPlayer(this);
        }
    }

    void Update()
    {
        if (!IsOwner) return;
        var ui = GameUIController.Instance;
        if (ui != null && !ui.playerRegistered)
        {
            ui.RegisterLocalPlayer(this);
        }
    }

    // Checks if the player can act (move or slide), with optional slide usage check
    // checkTurn: true for move actions, false for slide actions (slide is not turn-based)
    // checkSlide: true to ensure the player hasn't used their slide yet
    private bool CanAct(bool checkTurn = true, bool checkSlide = false)
    {
        if (!IsOwner) {
            Debug.Log("CanAct: Not owner");
            return false;
        }
        if (GameStateManager.Instance == null) {
            Debug.Log("CanAct: GameStateManager.Instance is null");
            return false;
        }
        // In multiplayer, require 2 players to act (host only)
        if (GameModeManager.SelectedMode != GameModeManager.GameMode.Singleplayer) {
            if (NetworkManager.Singleton.IsHost && GameStateManager.Instance.playerSymbols.Count != 2) {
                Debug.Log($"CanAct: Not enough players (playerSymbols.Count={GameStateManager.Instance.playerSymbols.Count})");
                return false;
            }
        }
        if (GameStateManager.Instance.gameResult.Value != (int)GameResult.Ongoing) {
            Debug.Log($"CanAct: Game is not ongoing (gameResult={GameStateManager.Instance.gameResult.Value})");
            return false;
        }
        if (checkTurn && GameStateManager.Instance.currentTurn.Value != playerSymbol.Value) {
            Debug.Log($"CanAct: Not your turn (currentTurn={GameStateManager.Instance.currentTurn.Value}, playerSymbol={playerSymbol.Value})");
            return false;
        }
        if (checkSlide && hasUsedSlide.Value) {
            Debug.Log("CanAct: Already used slide");
            return false;
        }
        Debug.Log($"CanAct: Allowed (currentTurn={GameStateManager.Instance.currentTurn.Value}, playerSymbol={playerSymbol.Value})");
        return true;
    }

    // Attempts to make a move for the player at the specified cell index
    // Only proceeds if all move-related conditions are met
    public void TryMakeMove(int cellIndex)
    {
        if (!CanAct(checkTurn: true)) {
            Debug.Log($"TryMakeMove: Blocked move for cell {cellIndex}, playerSymbol={playerSymbol.Value}");
            return;
        }
        Debug.Log($"TryMakeMove: Attempting move for cell {cellIndex}, playerSymbol={playerSymbol.Value}");
        GameStateManager.Instance.MakeMoveServerRpc(cellIndex, playerSymbol.Value);
    }

    // Attempts to slide the board in the specified direction
    // Only proceeds if all slide-related conditions are met
    public void TrySlideBoard(SlideDirection direction)
    {
        if (!CanAct(checkTurn: false, checkSlide: true)) return;
        GameStateManager.Instance.SlideBoardServerRpc(direction, playerSymbol.Value);
    }
}