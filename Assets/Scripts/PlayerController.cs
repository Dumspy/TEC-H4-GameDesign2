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

    public void TryMakeMove(int cellIndex)
    {
        if (!IsOwner) return;
        if (GameStateManager.Instance == null) return;
        if (GameStateManager.Instance.playerSymbols.Count != 2 && NetworkManager.Singleton.IsHost) return; // Only host can act before both players join
        if (GameStateManager.Instance.gameResult.Value != (int)GameResult.Ongoing) return; // Game already over
        if (GameStateManager.Instance.currentTurn.Value != playerSymbol.Value) return; // Not your turn

        GameStateManager.Instance.MakeMoveServerRpc(cellIndex, playerSymbol.Value);
    }

    public void TrySlideBoard(SlideDirection direction)
    {
        if (!IsOwner) return;
        if (hasUsedSlide.Value) return;
        if (GameStateManager.Instance == null) return;
        if (GameStateManager.Instance.playerSymbols.Count != 2 && NetworkManager.Singleton.IsHost) return; // Only host can act before both players join
        if (GameStateManager.Instance.gameResult.Value != (int)GameResult.Ongoing) return;

        SlideBoardServerRpc(direction);
    }

    [ServerRpc]
    private void SlideBoardServerRpc(SlideDirection direction)
    {
        if (hasUsedSlide.Value) return;
        hasUsedSlide.Value = true;
        GameStateManager.Instance.SlideBoard(direction);
    }
}