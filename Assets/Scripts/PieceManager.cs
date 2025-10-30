using UnityEngine;
using Unity.Netcode;

public static class PieceManager
{
    public static void UpdatePieceVisuals(NetworkList<int> boardState, Transform[] cellPositions, GameObject placeablePrefab)
    {
        var pieces = GameObject.FindGameObjectsWithTag("Piece");
        var cellToPiece = new System.Collections.Generic.Dictionary<int, GameObject>();
        var oldPositions = new System.Collections.Generic.Dictionary<int, Vector3>();
        foreach (var piece in pieces)
        {
            var sync = piece.GetComponent<PieceNetworkSync>();
            if (sync != null)
            {
                cellToPiece[sync.cellIndex] = piece;
                oldPositions[sync.cellIndex] = piece.transform.position;
            }
        }
        for (int i = 0; i < 9; i++)
        {
            int symbol = boardState[i];
            if (symbol == (int)PlayerSymbol.None)
            {
                if (cellToPiece.ContainsKey(i))
                {
                    var netObj = cellToPiece[i].GetComponent<NetworkObject>();
                    if (netObj != null && netObj.IsSpawned)
                        netObj.Despawn();
                    else
                        Object.Destroy(cellToPiece[i]);
                }
            }
            else
            {
                if (cellToPiece.ContainsKey(i))
                {
                    var sync = cellToPiece[i].GetComponent<PieceNetworkSync>();
                    if (sync.playerSymbol.Value != symbol)
                    {
                        var netObj = cellToPiece[i].GetComponent<NetworkObject>();
                        if (netObj != null && netObj.IsSpawned)
                            netObj.Despawn();
                        else
                            Object.Destroy(cellToPiece[i]);
                        var newPiece = SpawnPiece(i, symbol, cellPositions, placeablePrefab);
                        newPiece.transform.position = cellPositions[i].position;
                        Debug.Log($"[Slide] Respawned piece snapped to {cellPositions[i].position} (cell {i})");
                    }
                    else
                    {
                        Vector3 newPos = cellPositions[i].position;
                        Vector3 oldPos = oldPositions.ContainsKey(i) ? oldPositions[i] : newPos;
                        if ((oldPos - newPos).sqrMagnitude > 0.001f)
                        {
                            cellToPiece[i].transform.position = newPos;
                            Debug.Log($"[Slide] Moving piece snapped to {newPos} (cell {i})");
                        }
                        sync.cellIndex = i;
                    }
                }
                else
                {
                    var newPiece = SpawnPiece(i, symbol, cellPositions, placeablePrefab);
                    Debug.Log($"[Slide] New piece spawned at {cellPositions[i].position} (cell {i})");
                }
            }
        }
    }

    public static GameObject SpawnPiece(int cellIndex, int playerSymbol, Transform[] cellPositions, GameObject placeablePrefab, Vector3? startPosition = null)
    {
        if (cellPositions == null || cellPositions.Length <= cellIndex)
        {
            Debug.LogError($"PieceManager: cellPositions is null or index {cellIndex} out of bounds");
            return null;
        }
        Transform cellTransform = cellPositions[cellIndex];
        Vector3 spawnPos = startPosition ?? cellTransform.position;
        GameObject piece = Object.Instantiate(placeablePrefab, spawnPos, Quaternion.identity);
        var netObj = piece.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.Spawn();
        }
        else
        {
            Debug.LogError($"PieceManager: No NetworkObject component found on piece prefab!");
        }
        var pieceSync = piece.GetComponent<PieceNetworkSync>();
        if (pieceSync != null)
        {
            pieceSync.playerSymbol.Value = playerSymbol;
            pieceSync.cellIndex = cellIndex;
        }
        else
        {
            Debug.LogError($"PieceManager: No PieceNetworkSync component found on piece prefab!");
        }
        return piece;
    }

    public static void ClearAllPieces()
    {
        var pieces = GameObject.FindGameObjectsWithTag("Piece");
        foreach (var piece in pieces)
        {
            var netObj = piece.GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsSpawned)
                netObj.Despawn();
            else
                Object.Destroy(piece);
        }
    }
}
