using System.Collections.Generic;

using System.Collections.Generic;

public interface IGameAI
{
    int ChooseMove(IReadOnlyList<int> boardState, int aiSymbol, int humanSymbol);
    bool ShouldSlide(IReadOnlyList<int> boardState, int aiSymbol, int humanSymbol);
    SlideDirection ChooseSlideDirection(IReadOnlyList<int> boardState, int aiSymbol, int humanSymbol);
}
