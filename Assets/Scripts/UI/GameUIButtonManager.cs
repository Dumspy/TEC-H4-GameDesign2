using UnityEngine.UIElements;
using Unity.Netcode;
using UnityEngine;

public class GameUIButtonManager
{
    private readonly Button restartButton;
    private readonly Button slideButton;

    public GameUIButtonManager(Button restart, Button slide)
    {
        restartButton = restart;
        slideButton = slide;
    }

    public void UpdateRestartButton(NetworkList<ulong> restartRequests)
    {
        int restartCount = restartRequests != null ? restartRequests.Count : 0;
        restartButton.text = $"Restart ({restartCount}/2)";
        ulong localId = NetworkManager.Singleton.LocalClientId;
        bool alreadyClicked = restartRequests != null && restartRequests.Contains(localId);
        restartButton.SetEnabled(!alreadyClicked);
    }

    public void UpdateSlideButton(bool bothPlayersPresent, bool canSlide)
    {
        slideButton.SetEnabled(bothPlayersPresent && canSlide);
    }

    public void SetSlideButtonEnabled(bool enabled)
    {
        slideButton.SetEnabled(enabled);
    }

    public void SetRestartButtonEnabled(bool enabled)
    {
        restartButton.SetEnabled(enabled);
    }
}
