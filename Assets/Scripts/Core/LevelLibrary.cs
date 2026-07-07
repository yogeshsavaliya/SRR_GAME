using System.Collections.Generic;

namespace Arrows.Core
{
    public sealed class LevelDefinition
    {
        public readonly string Name;
        public readonly string Layout;
        public readonly int Hearts;

        public LevelDefinition(string name, string layout, int hearts)
        {
            Name = name;
            Layout = layout;
            Hearts = hearts;
        }

        public Board CreateBoard()
        {
            return Board.Parse(Layout);
        }
    }

    /// <summary>
    /// Handcrafted, verified-solvable levels of increasing difficulty.
    /// Layout: rows separated by '/', cells are '.' (empty) or U/R/D/L arrows.
    /// </summary>
    public static class LevelLibrary
    {
        // Each level fills a distinct SHAPE with arrows. Level 1 is an easy
        // warm-up; the rest are generated + verified solvable by BoardSolver
        // (see tools/run-logic-tests.sh, "gen" mode). '.' cells are empty
        // background; arrows travel through them to leave the board.
        public static readonly LevelDefinition[] Levels = new[]
        {
            new LevelDefinition("Warm Up",     "UUU/L.R/DDD", 5),
            new LevelDefinition("Square",      "UURR/DDRR/LLRR/LLDD", 5),
            new LevelDefinition("Pyramid",     "..L../.UUR./LLLDR", 5),
            new LevelDefinition("Diamond",     "..R../.LUR./URUDU/.DDD./..R..", 4),
            new LevelDefinition("Plus",        "..L../..L../UUUDR/..L../..R..", 4),
            new LevelDefinition("Letter H",    "U...L/L...R/UUULR/U...L/D...D", 4),
            new LevelDefinition("Ring",        "UUUUR/R...R/L...R/L...L/LLDRD", 3),
            new LevelDefinition("Arrow",       "...R.../..DUU../.ULLRR./DUDUUDD/...L.../...D.../...R...", 3),
            new LevelDefinition("Big Diamond", "...R.../..LLL../.LLRDR./UULRDUR/.RURDR./..DDR../...D...", 3),
            new LevelDefinition("Heart",       ".UR.RR./DULLDUD/DLLLDUR/.LRDDR./..RRR../...L...", 3),
        };

        public static int Count { get { return Levels.Length; } }

        public static LevelDefinition Get(int index)
        {
            if (index < 0) index = 0;
            if (index >= Levels.Length) index = Levels.Length - 1;
            return Levels[index];
        }

        /// <summary>Only the layouts that pass the solver should ship; used by tests.</summary>
        public static List<string> AllLayouts()
        {
            var list = new List<string>();
            for (int i = 0; i < Levels.Length; i++) list.Add(Levels[i].Layout);
            return list;
        }
    }
}
