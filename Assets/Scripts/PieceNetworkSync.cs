using UnityEngine;
using Unity.Netcode;

public class PieceNetworkSync : NetworkBehaviour
{
    public int cellIndex; // Track which cell this piece is in
    public NetworkVariable<int> playerSymbol = new();
    public GameObject crossPrefab;
    public GameObject oPrefab;

    public override void OnNetworkSpawn()
    {
        UpdateSymbolPrefab();
        playerSymbol.OnValueChanged += (oldVal, newVal) => UpdateSymbolPrefab();
    }

    private void UpdateSymbolPrefab()
    {
        // Destroy any existing child prefab
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        GameObject prefabToSpawn = null;
        if (playerSymbol.Value == (int)PlayerSymbol.X)
        {
            prefabToSpawn = crossPrefab;
        }
        else if (playerSymbol.Value == (int)PlayerSymbol.O)
        {
            prefabToSpawn = oPrefab;
        }

        if (prefabToSpawn != null)
        {
            var instance = Instantiate(prefabToSpawn, transform);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
        }
    }
}
