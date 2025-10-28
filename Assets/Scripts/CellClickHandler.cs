using UnityEngine;

public class CellClickHandler : MonoBehaviour
{
    public int cellIndex; // Set this in the Inspector or at runtime

    public void OnCellClicked()
    {
        // Find the local player controller
        var playerControllers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var pc in playerControllers)
        {
            if (pc.IsOwner)
            {
                pc.TryMakeMove(cellIndex);
                break;
            }
        }
    }

    // If using OnMouseDown for 3D objects:
    private void OnMouseDown()
    {
        OnCellClicked();
    }
}
