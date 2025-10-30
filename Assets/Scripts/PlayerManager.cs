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
            playerSymbols[clientId] = symbol;
            var playerControllers = Object.FindObjectsOfType<PlayerController>();
            foreach (var pc in playerControllers)
            {
                if (pc.OwnerClientId == clientId && pc.IsSpawned)
                {
                    pc.playerSymbol.Value = symbol;
                    break;
                }
            }
        }
    }
}
