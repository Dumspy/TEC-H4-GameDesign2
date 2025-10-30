using UnityEngine.UIElements;
using UnityEngine;

public class ResultOverlayManager
{
    private readonly VisualElement resultOverlay;
    private readonly Label resultLabel;

    public ResultOverlayManager(VisualElement overlay, Label label)
    {
        resultOverlay = overlay;
        resultLabel = label;
    }

    public void UpdateOverlayAndTurnLabelVisibility(bool showOverlay, Label turnLabel)
    {
        resultOverlay.style.display = showOverlay ? DisplayStyle.Flex : DisplayStyle.None;
        turnLabel.visible = !showOverlay;
    }

    public void UpdateResultLabel(GameResult result)
    {
        switch (result)
        {
            case GameResult.XWins:
                resultLabel.text = "X Wins!";
                resultLabel.style.color = new StyleColor(new Color(33f/255f, 150f/255f, 243f/255f)); // blue
                break;
            case GameResult.OWins:
                resultLabel.text = "O Wins!";
                resultLabel.style.color = new StyleColor(new Color(220f/255f, 20f/255f, 60f/255f)); // red
                break;
            case GameResult.Draw:
                resultLabel.text = "Draw!";
                resultLabel.style.color = new StyleColor(new Color(255f/255f, 193f/255f, 7f/255f)); // yellow
                break;
            default:
                resultLabel.text = "";
                resultLabel.style.color = new StyleColor(Color.clear);
                break;
        }
    }
}
