using UnityEngine;
using Unity.Netcode;

public class PlayerController : NetworkBehaviour
{

    private GameStateManager gameStateManager;
    private int playerSymbol;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            gameStateManager = FindFirstObjectByType<GameStateManager>();
            // Symbol will be assigned via ClientRpc from GameStateManager
        }
    }

    public void SetPlayerSymbol(int symbol)
    {
        playerSymbol = symbol;
    }

    // Call this from your cell click logic, passing the cell index
    public void TryMakeMove(int cellIndex)
    {
        if (!IsOwner) {
            return;
        }
        if (gameStateManager.gameResult.Value != (int)GameResult.Ongoing)
        {
            return;
        }
        if (gameStateManager.currentTurn.Value != playerSymbol) {
            return; // Not your turn
        }
        gameStateManager.MakeMoveServerRpc(cellIndex, playerSymbol);
    }
}
