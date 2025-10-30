using System.Collections.Generic;
using UnityEngine;

public class BasicRandomAI : IGameAI
{
    public int ChooseMove(IReadOnlyList<int> boardState, int aiSymbol, int humanSymbol)
    {
        List<int> emptyCells = new List<int>();
        for (int i = 0; i < boardState.Count; i++)
        {
            if (boardState[i] == (int)PlayerSymbol.None)
                emptyCells.Add(i);
        }
        if (emptyCells.Count == 0)
            return -1;
        return emptyCells[Random.Range(0, emptyCells.Count)];
    }

    public bool ShouldSlide(IReadOnlyList<int> boardState, int aiSymbol, int humanSymbol)
    {
        // Randomly decide to slide 20% of the time if slide is available
        return Random.value < 0.2f;
    }

    public SlideDirection ChooseSlideDirection(IReadOnlyList<int> boardState, int aiSymbol, int humanSymbol)
    {
        // Pick a random direction
        return (SlideDirection)Random.Range(0, 4);
    }
}
