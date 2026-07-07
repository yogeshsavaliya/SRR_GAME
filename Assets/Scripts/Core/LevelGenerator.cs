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
            return Generate(width, height, arrowCount, seed, null);
        }

        /// <summary>
        /// Generate a solvable board. When <paramref name="mask"/> is non-null,
        /// arrows are only placed in cells where mask[x,y] is true, so the filled
        /// arrows form a shape/picture. Arrows still travel through any empty cell
        /// (inside or outside the mask) to leave the board.
        /// </summary>
        public static Board Generate(int width, int height, int arrowCount, int seed, bool[,] mask)
        {
            var rng = new Random(seed);
            var board = new Board(width, height);

            int maxCells = width * height;
            if (arrowCount > maxCells) arrowCount = maxCells;

            int placed = 0;
            int safety = maxCells * 400;
            int id = 0;

            while (placed < arrowCount && safety-- > 0)
            {
                int x = rng.Next(width);
                int y = rng.Next(height);
                if (board.IsOccupied(x, y)) continue;
                if (mask != null && !mask[x, y]) continue;

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

        /// <summary>Parse a shape from ASCII rows: '#' = cell in shape, anything else = outside.</summary>
        public static bool[,] ParseMask(string[] rows, out int width, out int height)
        {
            height = rows.Length;
            width = 0;
            for (int i = 0; i < rows.Length; i++)
                if (rows[i].Length > width) width = rows[i].Length;

            var mask = new bool[width, height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < rows[y].Length; x++)
                    mask[x, y] = rows[y][x] == '#';
            return mask;
        }

        public static int MaskCount(bool[,] mask)
        {
            int c = 0;
            foreach (bool b in mask) if (b) c++;
            return c;
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
