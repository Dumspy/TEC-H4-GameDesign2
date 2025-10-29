using UnityEngine;
using Unity.Netcode;

public class PieceNetworkSync : NetworkBehaviour
{
    public int cellIndex; // Track which cell this piece is in

    public NetworkVariable<int> playerSymbol = new NetworkVariable<int>();

    public Material xMaterial;
    public Material oMaterial;

    public override void OnNetworkSpawn()
    {
        UpdateMaterial();
        playerSymbol.OnValueChanged += (oldVal, newVal) => UpdateMaterial();
    }

    private void UpdateMaterial()
    {
        var renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            if (playerSymbol.Value == (int)PlayerSymbol.X && xMaterial != null)
                renderer.material = xMaterial;
            else if (playerSymbol.Value == (int)PlayerSymbol.O && oMaterial != null)
                renderer.material = oMaterial;
        }
    }
}
