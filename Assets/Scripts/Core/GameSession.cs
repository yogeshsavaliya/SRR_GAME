namespace Arrows.Core
{
    public enum TapResult
    {
        Invalid,   // tapped an empty cell
        Escaped,   // arrow flew off the board
        Blocked,   // path obstructed: arrow stays, a heart is lost
        Won,       // that tap cleared the final arrow
        GameOver   // that blocked tap consumed the last heart
    }

    /// <summary>
    /// Pure game-state wrapper around a <see cref="Board"/> that tracks hearts,
    /// move count and win/lose state. No UnityEngine dependency.
    /// </summary>
    public sealed class GameSession
    {
        public Board Board { get; private set; }
        public int Hearts { get; private set; }
        public int MaxHearts { get; private set; }
        public int Moves { get; private set; }
        public bool IsWon { get; private set; }
        public bool IsGameOver { get; private set; }

        public GameSession(Board board, int maxHearts)
        {
            Board = board;
            MaxHearts = maxHearts;
            Hearts = maxHearts;
        }

        public bool IsFinished
        {
            get { return IsWon || IsGameOver; }
        }

        /// <summary>Apply a tap at (x,y) and return what happened.</summary>
        public TapResult Tap(int x, int y)
        {
            if (IsFinished) return TapResult.Invalid;
            if (!Board.IsOccupied(x, y)) return TapResult.Invalid;

            if (Board.TryRemove(x, y))
            {
                Moves++;
                if (Board.IsSolved)
                {
                    IsWon = true;
                    return TapResult.Won;
                }
                return TapResult.Escaped;
            }

            // Blocked tap: costs a heart.
            Hearts--;
            if (Hearts <= 0)
            {
                Hearts = 0;
                IsGameOver = true;
                return TapResult.GameOver;
            }
            return TapResult.Blocked;
        }
    }
}
