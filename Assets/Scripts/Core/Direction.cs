namespace Arrows.Core
{
    /// <summary>
    /// The four cardinal directions an arrow can point / travel.
    /// Grid convention used throughout the game: x = column (increases to the
    /// right), y = row (increases downward, so row 0 is the top row).
    /// </summary>
    public enum Direction
    {
        Up = 0,
        Right = 1,
        Down = 2,
        Left = 3
    }

    public static class DirectionUtil
    {
        public static int Dx(Direction d)
        {
            if (d == Direction.Right) return 1;
            if (d == Direction.Left) return -1;
            return 0;
        }

        public static int Dy(Direction d)
        {
            if (d == Direction.Down) return 1;
            if (d == Direction.Up) return -1;
            return 0;
        }

        /// <summary>Clockwise Z rotation (degrees) for a sprite drawn pointing Up by default.</summary>
        public static float ZRotationDegrees(Direction d)
        {
            if (d == Direction.Up) return 0f;
            if (d == Direction.Right) return -90f;
            if (d == Direction.Down) return 180f;
            return 90f; // Left
        }

        public static char ToChar(Direction d)
        {
            if (d == Direction.Up) return 'U';
            if (d == Direction.Right) return 'R';
            if (d == Direction.Down) return 'D';
            return 'L';
        }

        public static bool TryParse(char c, out Direction d)
        {
            switch (c)
            {
                case 'U': case 'u': d = Direction.Up; return true;
                case 'R': case 'r': d = Direction.Right; return true;
                case 'D': case 'd': d = Direction.Down; return true;
                case 'L': case 'l': d = Direction.Left; return true;
                default: d = Direction.Up; return false;
            }
        }
    }
}
