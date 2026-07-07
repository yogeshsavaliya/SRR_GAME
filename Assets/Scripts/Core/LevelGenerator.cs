using System;
using System.Collections.Generic;

namespace Arrows.Core
{
    /// <summary>
    /// Generates boards that are guaranteed solvable.
    ///
    /// Construction insight: if every arrow, at the moment it is placed, has a
    /// clear ray to the edge considering only the arrows placed *before* it, then
    /// removing the arrows in reverse placement order is always a valid solution
    /// (each arrow's only possible blockers were placed after it, and have already
    /// been removed by the time we reach it).
    /// </summary>
    public static class LevelGenerator
    {
        public static Board Generate(int width, int height, int arrowCount, int seed)
        {
            var rng = new Random(seed);
            var board = new Board(width, height);

            int maxCells = width * height;
            if (arrowCount > maxCells) arrowCount = maxCells;

            int placed = 0;
            int safety = maxCells * 200;
            int id = 0;

            while (placed < arrowCount && safety-- > 0)
            {
                int x = rng.Next(width);
                int y = rng.Next(height);
                if (board.IsOccupied(x, y)) continue;

                Direction[] dirs = ShuffledDirections(rng);
                for (int i = 0; i < dirs.Length; i++)
                {
                    if (RayIsClear(board, x, y, dirs[i]))
                    {
                        board.SetArrow(x, y, dirs[i], id++);
                        placed++;
                        break;
                    }
                }
            }
            return board;
        }

        private static bool RayIsClear(Board board, int x, int y, Direction dir)
        {
            int dx = DirectionUtil.Dx(dir);
            int dy = DirectionUtil.Dy(dir);
            int cx = x + dx;
            int cy = y + dy;
            while (board.InBounds(cx, cy))
            {
                if (board.IsOccupied(cx, cy)) return false;
                cx += dx;
                cy += dy;
            }
            return true;
        }

        private static Direction[] ShuffledDirections(Random rng)
        {
            var dirs = new List<Direction>
            {
                Direction.Up, Direction.Right, Direction.Down, Direction.Left
            };
            for (int i = dirs.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                Direction tmp = dirs[i];
                dirs[i] = dirs[j];
                dirs[j] = tmp;
            }
            return dirs.ToArray();
        }
    }
}
