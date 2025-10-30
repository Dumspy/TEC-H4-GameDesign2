using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Renderer))]
public class PieceNetworkSync : NetworkBehaviour
{
    public int cellIndex; // Track which cell this piece is in
    public NetworkVariable<int> playerSymbol = new();
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
        renderer.material = playerSymbol.Value switch
        {
            (int)PlayerSymbol.X => xMaterial,
            (int)PlayerSymbol.O => oMaterial,
            _ => renderer.material
        };
    }
}
