using System;
using System.Collections.Generic;
using Arrows.Core;

// Standalone (Mono/.NET) test harness for the engine-independent game logic.
// Compiled with the Unity-bundled Mono compiler; see tools/run-logic-tests.sh.
// This does NOT require a Unity license and validates the puzzle mechanics.
internal static class Program
{
    private static int _passed;
    private static int _failed;

    private static void Check(string name, bool condition)
    {
        if (condition) { _passed++; Console.WriteLine("  [PASS] " + name); }
        else { _failed++; Console.WriteLine("  [FAIL] " + name); }
    }

    private static int Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "gen")
        {
            GenerateLevels();
            return 0;
        }

        Console.WriteLine("=== Arrows Puzzle Escape - core logic tests ===\n");

        MechanicsTests();
        HeartsTests();
        LevelSolvabilityTests();
        DemoPlaythrough();

        Console.WriteLine();
        Console.WriteLine("=== RESULT: " + _passed + " passed, " + _failed + " failed ===");
        return _failed == 0 ? 0 : 1;
    }

    private static void GenerateLevels()
    {
        // {width, height, arrowCount, seed} tuned for rising difficulty.
        int[][] specs = new int[][]
        {
            new[] {4, 4, 10, 101},
            new[] {4, 4, 13, 202},
            new[] {5, 5, 15, 303},
            new[] {5, 5, 19, 404},
            new[] {5, 5, 22, 505},
            new[] {6, 6, 26, 606},
            new[] {6, 6, 30, 707},
            new[] {6, 6, 33, 808},
        };
        foreach (int[] s in specs)
        {
            Board b = LevelGenerator.Generate(s[0], s[1], s[2], s[3]);
            bool solvable = BoardSolver.IsSolvable(b);
            Console.WriteLine(b.Encode() + "    # " + s[0] + "x" + s[1] + " arrows=" +
                              b.RemainingCount + " solvable=" + solvable);
        }
    }

    private static void MechanicsTests()
    {
        Console.WriteLine("Mechanics:");

        // Arrow pointing up at the top row escapes immediately.
        Board b = Board.Parse("U");
        Check("single up arrow has clear path", b.IsPathClear(0, 0));
        Check("single up arrow removes", b.TryRemove(0, 0));
        Check("board solved after removal", b.IsSolved);

        // Two arrows pointing at each other deadlock.
        Board d = Board.Parse("RL");
        Check("right arrow blocked by neighbour", !d.IsPathClear(0, 0));
        Check("left arrow blocked by neighbour", !d.IsPathClear(1, 0));
        Check("deadlock is unsolvable", !BoardSolver.IsSolvable(d));

        // Ordering: remove the blocker, then the blocked arrow frees up.
        Board o = Board.Parse("RR");
        Check("rightmost R clear", o.IsPathClear(1, 0));
        Check("leftmost R blocked", !o.IsPathClear(0, 0));
        o.TryRemove(1, 0);
        Check("leftmost R clear after blocker leaves", o.IsPathClear(0, 0));

        // Path scanning across multiple empty cells.
        Board col = Board.Parse("D/./.");
        Check("down arrow sees clear column", col.IsPathClear(0, 0));
    }

    private static void HeartsTests()
    {
        Console.WriteLine("Hearts / session:");

        var session = new GameSession(Board.Parse("RL"), 5);
        // Both arrows are blocked, so every tap wastes a heart.
        Check("blocked tap returns Blocked", session.Tap(0, 0) == TapResult.Blocked);
        Check("heart decremented to 4", session.Hearts == 4);
        session.Tap(0, 0); session.Tap(0, 0); session.Tap(0, 0);
        Check("last blocked tap is GameOver", session.Tap(0, 0) == TapResult.GameOver);
        Check("session finished on game over", session.IsFinished && session.IsGameOver);

        var win = new GameSession(Board.Parse("U"), 5);
        Check("clearing final arrow returns Won", win.Tap(0, 0) == TapResult.Won);
        Check("no heart lost on a valid escape", win.Hearts == 5);
        Check("move counter incremented", win.Moves == 1);
    }

    private static void LevelSolvabilityTests()
    {
        Console.WriteLine("Level solvability (" + LevelLibrary.Count + " levels):");
        for (int i = 0; i < LevelLibrary.Count; i++)
        {
            LevelDefinition def = LevelLibrary.Get(i);
            Board board = def.CreateBoard();
            List<int[]> solution = BoardSolver.Solve(board);
            bool solvable = solution != null;
            string detail = solvable
                ? ("solvable in " + solution.Count + " taps, " + board.RemainingCount + " arrows")
                : "UNSOLVABLE";
            Check("Level " + (i + 1) + " '" + def.Name + "' (" + board.Width + "x" + board.Height + ") -> " + detail, solvable);
        }
    }

    private static void DemoPlaythrough()
    {
        Console.WriteLine("\nDemo playthrough of Level 2 (solver order):");
        LevelDefinition def = LevelLibrary.Get(1);
        Board board = def.CreateBoard();
        var session = new GameSession(board, def.Hearts);
        List<int[]> solution = BoardSolver.Solve(def.CreateBoard());

        Console.WriteLine("Start (" + board.RemainingCount + " arrows, " + session.Hearts + " hearts):");
        PrintBoard(board);

        int step = 1;
        foreach (int[] cell in solution)
        {
            TapResult r = session.Tap(cell[0], cell[1]);
            Console.WriteLine("Tap #" + step + " at (col " + cell[0] + ", row " + cell[1] + ") -> " + r);
            step++;
        }
        Console.WriteLine("Final board:");
        PrintBoard(board);
        Check("demo level fully cleared", session.IsWon);
    }

    private static void PrintBoard(Board board)
    {
        for (int y = 0; y < board.Height; y++)
        {
            string line = "    ";
            for (int x = 0; x < board.Width; x++)
            {
                if (!board.IsOccupied(x, y)) { line += ". "; continue; }
                Direction dir = board.DirectionAt(x, y);
                if (dir == Direction.Up) line += "^ ";
                else if (dir == Direction.Right) line += "> ";
                else if (dir == Direction.Down) line += "v ";
                else line += "< ";
            }
            Console.WriteLine(line);
        }
    }
}
