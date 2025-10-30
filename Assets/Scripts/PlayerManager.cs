using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public static class PlayerManager
{
    public static void AssignPlayerSymbol(Dictionary<ulong, int> playerSymbols, ulong clientId)
    {
        if (!playerSymbols.ContainsKey(clientId))
        {
            int symbol = playerSymbols.Count == 0 ? (int)PlayerSymbol.X : (int)PlayerSymbol.O;
            playerSymbols.Add(clientId, symbol);
            Debug.Log($"AssignPlayerSymbol: Assigning symbol {symbol} to clientId {clientId}");
            var playerControllers = Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            foreach (var pc in playerControllers)
            {
                if (pc.OwnerClientId == clientId && pc.IsSpawned)
                {
                    Debug.Log($"AssignPlayerSymbol: Setting playerSymbol.Value={symbol} for PlayerController with OwnerClientId={pc.OwnerClientId}");
                    pc.playerSymbol.Value = symbol;
                    break;
                }
            }
        }
    }
}
