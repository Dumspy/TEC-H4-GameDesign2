using System;
using Unity.Netcode;

public static class BoardManager
{
    private static int GetIndex(int row, int col)
    {
        return row * 3 + col;
    }

    public static void SlideBoard(NetworkList<int> boardState, SlideDirection direction)
    {
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
                        int idx = GetIndex(row, col);
                        int symbol = boardState[idx];
                        if (symbol != (int)PlayerSymbol.None)
                        {
                            int targetIdx = GetIndex(targetRow, col);
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
                        int idx = GetIndex(row, col);
                        int symbol = boardState[idx];
                        if (symbol != (int)PlayerSymbol.None)
                        {
                            int targetIdx = GetIndex(targetRow, col);
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
                        int idx = GetIndex(row, col);
                        int symbol = boardState[idx];
                        if (symbol != (int)PlayerSymbol.None)
                        {
                            int targetIdx = GetIndex(row, targetCol);
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
                        int idx = GetIndex(row, col);
                        int symbol = boardState[idx];
                        if (symbol != (int)PlayerSymbol.None)
                        {
                            int targetIdx = GetIndex(row, targetCol);
                            newBoard[targetIdx] = symbol;
                            targetCol--;
                        }
                    }
                }
                break;
        }
        for (int i = 0; i < 9; i++)
            boardState[i] = newBoard[i];
    }

    private static int[] GetBoardArray(NetworkList<int> boardState)
    {
        int[] arr = new int[boardState.Count];
        for (int i = 0; i < boardState.Count; i++)
        {
            arr[i] = boardState[i];
        }
        return arr;
    }

    public static GameResult CheckGameResult(NetworkList<int> boardState)
    {
        var board = GetBoardArray(boardState);

        int[][] winPatterns = new int[][]
        {
            new int[] {0,1,2}, new int[] {3,4,5}, new int[] {6,7,8}, // rows
            new int[] {0,3,6}, new int[] {1,4,7}, new int[] {2,5,8}, // cols
            new int[] {0,4,8}, new int[] {2,4,6} // diags
        };
        foreach (var pattern in winPatterns)
        {
            if (board[pattern[0]] != (int)PlayerSymbol.None &&
                board[pattern[0]] == board[pattern[1]] &&
                board[pattern[1]] == board[pattern[2]])
            {
                return board[pattern[0]] == (int)PlayerSymbol.X ? GameResult.XWins : GameResult.OWins;
            }
        }
        // Draw if all cells filled
        bool allFilled = true;
        for (int i = 0; i < board.Length; i++)
        {
            if (board[i] == (int)PlayerSymbol.None)
            {
                allFilled = false;
                break;
            }
        }
        if (allFilled) return GameResult.Draw;
        return GameResult.Ongoing;
    }

    public static void ResetBoard(NetworkList<int> boardState)
    {
        for (int i = 0; i < boardState.Count; i++)
        {
            boardState[i] = (int)PlayerSymbol.None;
        }
    }

    public static bool MakeMove(NetworkList<int> boardState, int cellIndex, int playerSymbol)
    {
        if (cellIndex < 0 || cellIndex >= boardState.Count)
        {
            return false;
        }
        if (boardState[cellIndex] != (int)PlayerSymbol.None)
        {
            return false; // Cell already occupied
        }
        boardState[cellIndex] = playerSymbol;
        return true;
    }
}
