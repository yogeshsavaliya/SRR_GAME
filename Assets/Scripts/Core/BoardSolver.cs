using System.Collections.Generic;

namespace Arrows.Core
{
    /// <summary>
    /// Solvability analysis for a board.
    ///
    /// Because removing an arrow only ever empties cells (a blocked path can
    /// become clear but a clear path can never become blocked), greedily
    /// removing any currently-removable arrow is a complete strategy: a board is
    /// solvable if and only if repeated greedy removal empties it.
    /// </summary>
    public static class BoardSolver
    {
        /// <summary>
        /// Returns an ordered list of {x,y} taps that clears the board, or null
        /// if the board cannot be solved.
        /// </summary>
        public static List<int[]> Solve(Board board)
        {
            Board work = board.Clone();
            var order = new List<int[]>();

            while (!work.IsSolved)
            {
                List<int[]> removable = work.RemovableCells();
                if (removable.Count == 0)
                    return null; // deadlock: no arrow can leave.

                foreach (int[] cell in removable)
                {
                    work.TryRemove(cell[0], cell[1]);
                    order.Add(cell);
                }
            }
            return order;
        }

        public static bool IsSolvable(Board board)
        {
            return Solve(board) != null;
        }
    }
}
