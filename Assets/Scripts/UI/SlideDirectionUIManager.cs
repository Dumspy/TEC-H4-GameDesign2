using UnityEngine.UIElements;
using UnityEngine;

public class SlideDirectionUIManager
{
    private readonly Label slideDirectionLabel;
    private readonly MonoBehaviour invoker;

    public SlideDirectionUIManager(Label label, MonoBehaviour invoker)
    {
        slideDirectionLabel = label;
        this.invoker = invoker;
    }

    public void ShowSlideDirection(SlideDirection direction)
    {
        string dirText = direction switch
        {
            SlideDirection.Up => "Board slid UP!",
            SlideDirection.Down => "Board slid DOWN!",
            SlideDirection.Left => "Board slid LEFT!",
            SlideDirection.Right => "Board slid RIGHT!",
            _ => "Board slid!"
        };
        slideDirectionLabel.text = dirText;
        slideDirectionLabel.style.display = DisplayStyle.Flex;
        invoker.CancelInvoke(nameof(HideSlideDirection));
        invoker.Invoke(nameof(HideSlideDirection), 1f);
    }

    public void HideSlideDirection()
    {
        slideDirectionLabel.style.display = DisplayStyle.None;
    }
}
