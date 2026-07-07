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
        // Levels 1-2 are hand-authored intros; 3-10 are generated and verified
        // solvable by BoardSolver (see tools/run-logic-tests.sh).
        public static readonly LevelDefinition[] Levels = new[]
        {
            new LevelDefinition("Warm Up",    "UUU/L.R/DDD", 5),
            new LevelDefinition("First Steps","URD/L.R/ULD", 5),
            new LevelDefinition("Crossroads", "LUUL/LDD./D.../.D.R", 5),
            new LevelDefinition("Gridlock",   "URUU/U.DU/LDDR/DR..", 4),
            new LevelDefinition("Pinwheel",   "L.R.R/LU.RR/..LRR/RD.../DD.R.", 4),
            new LevelDefinition("Tangle",     "RU.UU/.LULR/L..LU/LU..R/LDLRU", 4),
            new LevelDefinition("Knots",      "DLLUD/L..UR/LL.UR/DR..D/DLDDD", 3),
            new LevelDefinition("Labyrinth",  "LUR.U./U..U.R/U..U.R/LUU.RR/LLDRRR/LRD.DD", 3),
            new LevelDefinition("Big Knot",   "UUULUR/D.U.DR/LUU..D/L.U.RR/DULRDD/DDDDRD", 3),
            new LevelDefinition("Nightmare",  "DLUUUR/L.DLUR/LL..LR/LL.D.R/LL..RR/LDDDUR", 3),
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
