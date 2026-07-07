using System.Collections.Generic;
using System.Text;

namespace Arrows.Core
{
    /// <summary>
    /// Pure (engine-independent) model of an Arrows Puzzle Escape board.
    ///
    /// Rules implemented:
    ///  - Every occupied cell holds an arrow pointing in one cardinal direction.
    ///  - An arrow can leave the board only if every cell between it and the edge
    ///    (in the direction it points) is empty.
    ///  - Removing an arrow only ever empties cells, so a cleared path can never
    ///    become blocked again. This makes greedy removal a complete solver.
    ///
    /// This type has no UnityEngine dependency and is unit-tested with Mono.
    /// </summary>
    public sealed class Board
    {
        public const int Empty = -1;

        public int Width { get; private set; }
        public int Height { get; private set; }

        // cells[x, y] stores a Direction (0..3) or Empty (-1).
        private readonly int[,] _cells;
        // Stable per-arrow id so the view can track/animate a specific arrow.
        private readonly int[,] _ids;

        public int RemainingCount { get; private set; }

        public Board(int width, int height)
        {
            Width = width;
            Height = height;
            _cells = new int[width, height];
            _ids = new int[width, height];
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    _cells[x, y] = Empty;
                    _ids[x, y] = -1;
                }
        }

        public bool InBounds(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }

        public bool IsOccupied(int x, int y)
        {
            return InBounds(x, y) && _cells[x, y] != Empty;
        }

        public Direction DirectionAt(int x, int y)
        {
            return (Direction)_cells[x, y];
        }

        public int IdAt(int x, int y)
        {
            return _ids[x, y];
        }

        public void SetArrow(int x, int y, Direction dir, int id)
        {
            if (!InBounds(x, y)) return;
            if (_cells[x, y] == Empty) RemainingCount++;
            _cells[x, y] = (int)dir;
            _ids[x, y] = id;
        }

        /// <summary>True if the arrow at (x,y) has an unobstructed path off the board.</summary>
        public bool IsPathClear(int x, int y)
        {
            if (!IsOccupied(x, y)) return false;
            Direction dir = DirectionAt(x, y);
            int dx = DirectionUtil.Dx(dir);
            int dy = DirectionUtil.Dy(dir);
            int cx = x + dx;
            int cy = y + dy;
            while (InBounds(cx, cy))
            {
                if (_cells[cx, cy] != Empty) return false;
                cx += dx;
                cy += dy;
            }
            return true;
        }

        /// <summary>
        /// Attempt to fly the arrow off the board. Returns true if it was removed,
        /// false if the tap is invalid or the path is blocked (a "wasted" tap).
        /// </summary>
        public bool TryRemove(int x, int y)
        {
            if (!IsOccupied(x, y)) return false;
            if (!IsPathClear(x, y)) return false;
            _cells[x, y] = Empty;
            _ids[x, y] = -1;
            RemainingCount--;
            return true;
        }

        public bool IsSolved
        {
            get { return RemainingCount == 0; }
        }

        /// <summary>Coordinates of every arrow that can currently be removed.</summary>
        public List<int[]> RemovableCells()
        {
            var result = new List<int[]>();
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    if (IsOccupied(x, y) && IsPathClear(x, y))
                        result.Add(new[] { x, y });
            return result;
        }

        public bool HasAnyMove()
        {
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    if (IsOccupied(x, y) && IsPathClear(x, y))
                        return true;
            return false;
        }

        public Board Clone()
        {
            var b = new Board(Width, Height);
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                {
                    b._cells[x, y] = _cells[x, y];
                    b._ids[x, y] = _ids[x, y];
                }
            b.RemainingCount = RemainingCount;
            return b;
        }

        /// <summary>Compact one-line representation, rows separated by '/'.</summary>
        public string Encode()
        {
            var sb = new StringBuilder();
            for (int y = 0; y < Height; y++)
            {
                if (y > 0) sb.Append('/');
                for (int x = 0; x < Width; x++)
                    sb.Append(_cells[x, y] == Empty ? '.' : DirectionUtil.ToChar((Direction)_cells[x, y]));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Parse a layout string. Rows are separated by '/'. Each character is a
        /// cell: '.' empty, or U/R/D/L for an arrow direction. Ids are assigned
        /// in reading order.
        /// </summary>
        public static Board Parse(string layout)
        {
            string[] rows = layout.Split('/');
            int height = rows.Length;
            int width = 0;
            for (int i = 0; i < rows.Length; i++)
                if (rows[i].Length > width) width = rows[i].Length;

            var board = new Board(width, height);
            int nextId = 0;
            for (int y = 0; y < height; y++)
            {
                string row = rows[y];
                for (int x = 0; x < row.Length; x++)
                {
                    char c = row[x];
                    Direction d;
                    if (DirectionUtil.TryParse(c, out d))
                        board.SetArrow(x, y, d, nextId++);
                }
            }
            return board;
        }
    }
}
