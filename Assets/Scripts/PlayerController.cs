using UnityEngine;
using Unity.Netcode;

public class PlayerController : NetworkBehaviour
{
    public NetworkVariable<int> playerSymbol = new((int)PlayerSymbol.None, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    void Update()
    {
        if (!IsOwner) return;
        var ui = GameUIController.Instance;
        if (ui != null && !ui.playerRegistered)
        {
            ui.RegisterLocalPlayer(this);
        }
    }

    public void TryMakeMove(int cellIndex)
    {
        if (!IsOwner)
        {
            return;
        }

        if (GameStateManager.Instance == null)
        {
            Debug.LogWarning("PlayerController: gameStateManager is null in TryMakeMove!");
            return;
        }
        
        if (GameStateManager.Instance.gameResult.Value != (int)GameResult.Ongoing)
        {
            return; // Game already over
        }

        if (GameStateManager.Instance.currentTurn.Value != playerSymbol.Value)
        {
            return; // Not your turn
        }

        GameStateManager.Instance.MakeMoveServerRpc(cellIndex, playerSymbol.Value);
    }
}
