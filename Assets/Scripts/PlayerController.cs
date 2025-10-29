using UnityEngine;
using Unity.Netcode;

public class PlayerController : NetworkBehaviour
{

    private GameStateManager gameStateManager;
    public NetworkVariable<int> playerSymbol = new NetworkVariable<int>(
        (int)PlayerSymbol.None, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            gameStateManager = FindFirstObjectByType<GameStateManager>();
            var ui = FindFirstObjectByType<GameUIController>();
            if (ui != null)
            {
                ui.RegisterLocalPlayer(this);
            }
        }
    }

    public void TryMakeMove(int cellIndex)
    {
        if (!IsOwner)
        {
            return;
        }
        
        if (gameStateManager.gameResult.Value != (int)GameResult.Ongoing)
        {
            return;
        }

        if (gameStateManager.currentTurn.Value != playerSymbol.Value) {
            return; // Not your turn
        }

        gameStateManager.MakeMoveServerRpc(cellIndex, playerSymbol.Value);
    }
}
