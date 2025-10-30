using UnityEngine;
using Unity.Netcode;

public class PlayerController : NetworkBehaviour
{
    public NetworkVariable<int> playerSymbol = new((int)PlayerSymbol.None, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> hasUsedSlide = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

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
        if (!IsOwner) return false;
        if (GameStateManager.Instance == null) return false;
        if (GameStateManager.Instance.playerSymbols.Count != 2 && NetworkManager.Singleton.IsHost) return false; // Only host can act before both players join
        if (GameStateManager.Instance.gameResult.Value != (int)GameResult.Ongoing) return false; // Game already over
        if (checkTurn && GameStateManager.Instance.currentTurn.Value != playerSymbol.Value) return false; // Not your turn
        if (checkSlide && hasUsedSlide.Value) return false;
        return true;
    }

    // Attempts to make a move for the player at the specified cell index
    // Only proceeds if all move-related conditions are met
    public void TryMakeMove(int cellIndex)
    {
        if (!CanAct(checkTurn: true)) return;
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