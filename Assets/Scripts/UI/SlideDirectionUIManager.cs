using UnityEngine.UIElements;
using UnityEngine;

public class SlideDirectionUIManager
{
    private readonly Label slideDirectionLabel;

    public SlideDirectionUIManager(Label label)
    {
        slideDirectionLabel = label;
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
    }

    public void HideSlideDirection()
    {
        slideDirectionLabel.style.display = DisplayStyle.None;
    }
}
