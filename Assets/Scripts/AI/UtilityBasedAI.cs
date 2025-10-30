using System.Collections.Generic;

public class UtilityBasedAI : IGameAI
{
    // Scoring weights (easy to adapt)
    private const int WIN_SCORE = 10000;
    private const int BLOCK_SCORE = 500;
    private const int SETUP_SCORE = 100;
    private const int CENTER_SCORE = 20;
    private const int CORNER_SCORE = 5;
    private const int OTHER_SCORE = 1;

    public int ChooseMove(IReadOnlyList<int> boardState, int aiSymbol, int humanSymbol)
    {
        // Only pick the best move on the board provided (already post-slide if a slide was performed)
        
        int bestMove = -1;
        int bestScore = int.MinValue;
        for (int i = 0; i < boardState.Count; i++)
        {
            if (boardState[i] != (int)PlayerSymbol.None) continue; // Only consider empty cells
            int score = ScoreMove(boardState, i, aiSymbol, humanSymbol);
            if (score > bestScore)
            {
                bestScore = score;
                bestMove = i;
            }
        }
        return bestMove;
    }

    public bool ShouldSlide(IReadOnlyList<int> boardState, int aiSymbol, int humanSymbol)
    {
        // Score best move without sliding
        int bestNoSlide = int.MinValue;
        for (int i = 0; i < boardState.Count; i++)
        {
            int score = ScoreMove(boardState, i, aiSymbol, humanSymbol);
            if (score > bestNoSlide)
                bestNoSlide = score;
        }

        // Score best move for each slide direction
        int bestWithSlide = int.MinValue;
        foreach (SlideDirection dir in System.Enum.GetValues(typeof(SlideDirection)))
        {
            var slidBoard = SimulateSlide(boardState, dir);
            for (int i = 0; i < slidBoard.Count; i++)
            {
                int score = ScoreMove(slidBoard, i, aiSymbol, humanSymbol);
                if (score > bestWithSlide)
                    bestWithSlide = score;
            }
        }

        // Slide if it improves the best possible move
        return bestWithSlide > bestNoSlide;
    }

    public SlideDirection ChooseSlideDirection(IReadOnlyList<int> boardState, int aiSymbol, int humanSymbol)
    {
        SlideDirection bestDirection = SlideDirection.Up;
        int bestScore = int.MinValue;
        foreach (SlideDirection dir in System.Enum.GetValues(typeof(SlideDirection)))
        {
            var slidBoard = SimulateSlide(boardState, dir);
            for (int i = 0; i < slidBoard.Count; i++)
            {
                int score = ScoreMove(slidBoard, i, aiSymbol, humanSymbol);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestDirection = dir;
                }
            }
        }
        return bestDirection;
    }

    // Helper methods (modular and easy to adapt)
    private int ScoreMove(IReadOnlyList<int> boardState, int moveIndex, int aiSymbol, int humanSymbol)
    {
        if (boardState[moveIndex] != (int)PlayerSymbol.None)
            return int.MinValue; // Invalid move

        int score = 0;
        if (IsWinningMove(boardState, moveIndex, aiSymbol))
            score += WIN_SCORE;
        else if (IsBlockingMove(boardState, moveIndex, aiSymbol, humanSymbol))
            score += BLOCK_SCORE;
        else if (IsSetupMove(boardState, moveIndex, aiSymbol))
            score += SETUP_SCORE;
        if (IsCenter(moveIndex))
            score += CENTER_SCORE;
        else if (IsCorner(moveIndex))
            score += CORNER_SCORE;
        else
            score += OTHER_SCORE;

        return score;
    }

    private static readonly int[][] winPatterns = new int[][]
    {
        new int[] {0,1,2}, new int[] {3,4,5}, new int[] {6,7,8}, // rows
        new int[] {0,3,6}, new int[] {1,4,7}, new int[] {2,5,8}, // cols
        new int[] {0,4,8}, new int[] {2,4,6} // diags
    };

    private bool IsWinningMove(IReadOnlyList<int> boardState, int moveIndex, int symbol)
    {
        // Simulate placing symbol at moveIndex, check for win
        var tempBoard = new int[boardState.Count];
        for (int i = 0; i < boardState.Count; i++) tempBoard[i] = boardState[i];
        tempBoard[moveIndex] = symbol;
        foreach (var pattern in winPatterns)
        {
            if (tempBoard[pattern[0]] == symbol &&
                tempBoard[pattern[1]] == symbol &&
                tempBoard[pattern[2]] == symbol)
            {
                return true;
            }
        }
        return false;
    }

    private bool IsBlockingMove(IReadOnlyList<int> boardState, int moveIndex, int aiSymbol, int humanSymbol)
    {
        // Simulate placing aiSymbol at moveIndex, check if it blocks human win
        var tempBoard = new int[boardState.Count];
        for (int i = 0; i < boardState.Count; i++) tempBoard[i] = boardState[i];
        tempBoard[moveIndex] = aiSymbol;
        // Check if human could have won at this spot
        foreach (var pattern in winPatterns)
        {
            int humanCount = 0, emptyCount = 0, aiCount = 0;
            foreach (var idx in pattern)
            {
                if (tempBoard[idx] == humanSymbol) humanCount++;
                else if (tempBoard[idx] == aiSymbol) aiCount++;
                else if (tempBoard[idx] == (int)PlayerSymbol.None) emptyCount++;
            }
            // If before move, human had 2 and 1 empty, and after move, no win
            if (humanCount == 2 && aiCount == 1 && emptyCount == 0)
            {
                return true;
            }
        }
        return false;
    }

    private bool IsSetupMove(IReadOnlyList<int> boardState, int moveIndex, int symbol)
    {
        // Simulate placing symbol at moveIndex, check for two-in-a-row with open third
        var tempBoard = new int[boardState.Count];
        for (int i = 0; i < boardState.Count; i++) tempBoard[i] = boardState[i];
        tempBoard[moveIndex] = symbol;
        foreach (var pattern in winPatterns)
        {
            int symbolCount = 0, emptyCount = 0;
            foreach (var idx in pattern)
            {
                if (tempBoard[idx] == symbol) symbolCount++;
                else if (tempBoard[idx] == (int)PlayerSymbol.None) emptyCount++;
            }
            if (symbolCount == 2 && emptyCount == 1)
            {
                return true;
            }
        }
        return false;
    }

    private bool IsCenter(int moveIndex)
    {
        // Center cell is index 4
        return moveIndex == 4;
    }

    private bool IsCorner(int moveIndex)
    {
        // Corners are indices 0, 2, 6, 8
        return moveIndex == 0 || moveIndex == 2 || moveIndex == 6 || moveIndex == 8;
    }

    private IReadOnlyList<int> SimulateSlide(IReadOnlyList<int> boardState, SlideDirection direction)
    {
        // Simulate sliding the board in the given direction
        int[] newBoard = new int[9];
        for (int i = 0; i < 9; i++)
            newBoard[i] = (int)PlayerSymbol.None;

        switch (direction)
        {
            case SlideDirection.Up:
                for (int col = 0; col < 3; col++)
                {
                    int targetRow = 0;
                    for (int row = 0; row < 3; row++)
                    {
                        int idx = row * 3 + col;
                        int symbol = boardState[idx];
                        if (symbol != (int)PlayerSymbol.None)
                        {
                            int targetIdx = targetRow * 3 + col;
                            newBoard[targetIdx] = symbol;
                            targetRow++;
                        }
                    }
                }
                break;
            case SlideDirection.Down:
                for (int col = 0; col < 3; col++)
                {
                    int targetRow = 2;
                    for (int row = 2; row >= 0; row--)
                    {
                        int idx = row * 3 + col;
                        int symbol = boardState[idx];
                        if (symbol != (int)PlayerSymbol.None)
                        {
                            int targetIdx = targetRow * 3 + col;
                            newBoard[targetIdx] = symbol;
                            targetRow--;
                        }
                    }
                }
                break;
            case SlideDirection.Left:
                for (int row = 0; row < 3; row++)
                {
                    int targetCol = 0;
                    for (int col = 0; col < 3; col++)
                    {
                        int idx = row * 3 + col;
                        int symbol = boardState[idx];
                        if (symbol != (int)PlayerSymbol.None)
                        {
                            int targetIdx = row * 3 + targetCol;
                            newBoard[targetIdx] = symbol;
                            targetCol++;
                        }
                    }
                }
                break;
            case SlideDirection.Right:
                for (int row = 0; row < 3; row++)
                {
                    int targetCol = 2;
                    for (int col = 2; col >= 0; col--)
                    {
                        int idx = row * 3 + col;
                        int symbol = boardState[idx];
                        if (symbol != (int)PlayerSymbol.None)
                        {
                            int targetIdx = row * 3 + targetCol;
                            newBoard[targetIdx] = symbol;
                            targetCol--;
                        }
                    }
                }
                break;
        }
        return newBoard;
    }
}
