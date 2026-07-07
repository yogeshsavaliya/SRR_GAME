using System;
using System.Collections.Generic;
using System.IO;
using Arrows.Core;

// Headless PREVIEW renderer. It draws the REAL board state (from Arrows.Core)
// using the same palette and arrow glyphs as the in-game view, and animates a
// real BoardSolver playthrough into a sequence of PPM frames. This is a preview
// of the game logic/data; it is NOT the Unity runtime (which needs a license).
internal static class PreviewRender
{
    // Render resolution (downscaled by ffmpeg for anti-aliasing).
    private const int W = 1200;
    private const int H = 1600;
    private const int TopUI = 320;
    private const int BottomMargin = 100;
    private const int SideMargin = 90;

    private static byte[] _fb; // RGB framebuffer
    private static int _clipTop; // rows above this are not drawn (arrows vanish into the UI band)

    // Palette (light theme with dark line-art arrows, matches GameController).
    private static readonly byte[] Bg = { 247, 247, 250 };
    private static readonly byte[] Stroke = { 28, 36, 66 };
    private static readonly byte[] Green = { 77, 199, 120 };
    private static readonly byte[] HeartFull = { 237, 74, 92 };
    private static readonly byte[] NumberCol = { 28, 36, 66 };
    private static readonly byte[] White = { 255, 255, 255 };

    private static int _levelDisplay;
    private static int _maxHearts;

    private static int Main(string[] args)
    {
        int levelIndex = args.Length > 0 ? int.Parse(args[0]) : 4;
        string outDir = args.Length > 1 ? args[1] : "/tmp/frames";
        Directory.CreateDirectory(outDir);

        LevelDefinition def = LevelLibrary.Get(levelIndex);
        _levelDisplay = levelIndex + 1;
        _maxHearts = def.Hearts;

        Board board = def.CreateBoard();
        List<int[]> solution = BoardSolver.Solve(def.CreateBoard());
        if (solution == null) { Console.WriteLine("Level not solvable"); return 1; }

        _fb = new byte[W * H * 3];
        int frame = 0;

        // Hold the starting board.
        for (int i = 0; i < 14; i++) frame = Emit(board, -1, -1, 0f, outDir, frame);

        foreach (int[] cell in solution)
        {
            const int steps = 8;
            for (int s = 1; s <= steps; s++)
            {
                float p = (float)s / steps;
                frame = Emit(board, cell[0], cell[1], p, outDir, frame);
            }
            board.TryRemove(cell[0], cell[1]); // arrow has now left the board
        }

        // Hold the solved board with a win badge.
        for (int i = 0; i < 34; i++) frame = Emit(board, -1, -1, 0f, outDir, frame, true);

        Console.WriteLine("Rendered " + frame + " frames to " + outDir);
        return 0;
    }

    private static int Emit(Board board, int animX, int animY, float animP, string dir, int frame, bool win = false)
    {
        Clear(Bg);
        int cellPx, x0, y0;
        LayoutBoard(board, out cellPx, out x0, out y0);

        _clipTop = TopUI; // escaping arrows disappear when leaving the play area
        for (int y = 0; y < board.Height; y++)
            for (int x = 0; x < board.Width; x++)
            {
                if (!board.IsOccupied(x, y)) continue;
                Direction d = board.DirectionAt(x, y);
                float cx = x0 + (x + 0.5f) * cellPx;
                float cy = y0 + (y + 0.5f) * cellPx;
                float alpha = 1f;
                if (x == animX && y == animY)
                {
                    float travel = ((d == Direction.Left || d == Direction.Right)
                        ? board.Width : board.Height) * cellPx + cellPx;
                    cx += DirectionUtil.Dx(d) * travel * animP;
                    cy += DirectionUtil.Dy(d) * travel * animP;
                    alpha = 1f - Math.Max(0f, (animP - 0.55f) / 0.45f);
                }
                DrawArrow(cx, cy, cellPx, d, alpha);
            }

        _clipTop = 0;
        DrawHearts();
        Draw7SegNumber(W * 0.5f, 210, 96, _levelDisplay, NumberCol);
        if (win) DrawWinBadge();

        WritePpm(Path.Combine(dir, "f" + frame.ToString("D4") + ".ppm"));
        return frame + 1;
    }

    private static void LayoutBoard(Board board, out int cellPx, out int x0, out int y0)
    {
        int availW = W - SideMargin * 2;
        int availH = H - TopUI - BottomMargin;
        int cell = Math.Min(availW / board.Width, availH / board.Height);
        cellPx = cell;
        int boardW = cell * board.Width;
        int boardH = cell * board.Height;
        x0 = (W - boardW) / 2;
        y0 = TopUI + (availH - boardH) / 2;
    }

    // ---- drawing ---------------------------------------------------------

    private static void DrawArrow(float cx, float cy, int cellPx, Direction d, float alpha)
    {
        // Thin line-art arrow (shaft + chevron head), rotated per direction.
        float s = cellPx * 0.32f;
        float thick = cellPx * 0.08f;
        float ang = 0f;
        if (d == Direction.Right) ang = -90f;
        else if (d == Direction.Down) ang = 180f;
        else if (d == Direction.Left) ang = 90f;
        float rad = ang * (float)Math.PI / 180f;
        float ca = (float)Math.Cos(rad), sa = (float)Math.Sin(rad);

        float[] tail = Rot(cx, cy, ca, sa, new[] { 0f, -0.62f * s });
        float[] apex = Rot(cx, cy, ca, sa, new[] { 0f, 0.66f * s });
        float[] armL = Rot(cx, cy, ca, sa, new[] { -0.34f * s, 0.28f * s });
        float[] armR = Rot(cx, cy, ca, sa, new[] { 0.34f * s, 0.28f * s });

        StrokeLine(tail[0], tail[1], apex[0], apex[1], thick, Stroke, alpha);
        StrokeLine(apex[0], apex[1], armL[0], armL[1], thick, Stroke, alpha);
        StrokeLine(apex[0], apex[1], armR[0], armR[1], thick, Stroke, alpha);
    }

    private static float[] Rot(float cx, float cy, float ca, float sa, float[] v)
    {
        float rx = v[0] * ca - v[1] * sa;
        float ry = v[0] * sa + v[1] * ca;
        return new[] { cx + rx, cy - ry }; // screen y is down
    }

    private static void DrawHearts()
    {
        float size = 40f;
        float spacing = 108f;
        float startX = W * 0.5f - (_maxHearts - 1) * spacing * 0.5f;
        for (int i = 0; i < _maxHearts; i++)
            DrawHeart(startX + i * spacing, 110f, size, HeartFull);
    }

    private static void DrawHeart(float cx, float cy, float size, byte[] col)
    {
        float h = size;
        for (int y = (int)(cy - h); y <= (int)(cy + h); y++)
            for (int x = (int)(cx - h); x <= (int)(cx + h); x++)
            {
                float nx = (x - cx) / h;
                float ny = -(y - cy) / h; // math y up
                nx /= 1.15f;
                ny = ny / 1.15f + 0.15f;
                float a = nx * nx + ny * ny - 1f;
                if (a * a * a - nx * nx * ny * ny * ny <= 0f) SetPx(x, y, col, 1f);
            }
    }

    private static void DrawWinBadge()
    {
        // Dim the light background, draw a white card, a green badge and a check.
        for (int i = 0; i < _fb.Length; i += 3)
        {
            _fb[i] = (byte)(_fb[i] * 0.55f + 28 * 0.45f);
            _fb[i + 1] = (byte)(_fb[i + 1] * 0.55f + 36 * 0.45f);
            _fb[i + 2] = (byte)(_fb[i + 2] * 0.55f + 66 * 0.45f);
        }
        float cx = W * 0.5f, cy = H * 0.42f;
        FillRoundedRect(cx - 230, cy - 250, cx + 230, cy + 300, 40, White, 1f);
        FillRoundedRect(cx - 150, cy - 150, cx + 150, cy + 150, 60, Green, 1f);
        // Check mark strokes.
        StrokeLine(cx - 70, cy + 10, cx - 15, cy + 65, 26, White);
        StrokeLine(cx - 15, cy + 65, cx + 85, cy - 60, 26, White);
        Draw7SegNumber(cx, cy + 240, 80, _levelDisplay, NumberCol);
    }

    // ---- raster primitives ----------------------------------------------

    private static void Clear(byte[] col)
    {
        for (int i = 0; i < _fb.Length; i += 3)
        {
            _fb[i] = col[0]; _fb[i + 1] = col[1]; _fb[i + 2] = col[2];
        }
    }

    private static void SetPx(int x, int y, byte[] col, float alpha)
    {
        if (x < 0 || x >= W || y < _clipTop || y >= H) return;
        int idx = (y * W + x) * 3;
        if (alpha >= 1f)
        {
            _fb[idx] = col[0]; _fb[idx + 1] = col[1]; _fb[idx + 2] = col[2];
        }
        else if (alpha > 0f)
        {
            _fb[idx] = (byte)(_fb[idx] * (1 - alpha) + col[0] * alpha);
            _fb[idx + 1] = (byte)(_fb[idx + 1] * (1 - alpha) + col[1] * alpha);
            _fb[idx + 2] = (byte)(_fb[idx + 2] * (1 - alpha) + col[2] * alpha);
        }
    }

    private static void FillRoundedRect(float x0, float y0, float x1, float y1, float r, byte[] col, float alpha)
    {
        int ix0 = (int)Math.Floor(x0), ix1 = (int)Math.Ceiling(x1);
        int iy0 = (int)Math.Floor(y0), iy1 = (int)Math.Ceiling(y1);
        for (int y = iy0; y <= iy1; y++)
            for (int x = ix0; x <= ix1; x++)
            {
                float px = x + 0.5f, py = y + 0.5f;
                if (px < x0 || px > x1 || py < y0 || py > y1) continue;
                float ccx = Clamp(px, x0 + r, x1 - r);
                float ccy = Clamp(py, y0 + r, y1 - r);
                float dx = px - ccx, dy = py - ccy;
                if (dx * dx + dy * dy <= r * r || r <= 0f) SetPx(x, y, col, alpha);
            }
    }

    private static void FillTriangle(float ax, float ay, float bx, float by, float cx, float cy, byte[] col, float alpha)
    {
        int x0 = (int)Math.Floor(Math.Min(ax, Math.Min(bx, cx)));
        int x1 = (int)Math.Ceiling(Math.Max(ax, Math.Max(bx, cx)));
        int y0 = (int)Math.Floor(Math.Min(ay, Math.Min(by, cy)));
        int y1 = (int)Math.Ceiling(Math.Max(ay, Math.Max(by, cy)));
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float px = x + 0.5f, py = y + 0.5f;
                float d1 = Sign(px, py, ax, ay, bx, by);
                float d2 = Sign(px, py, bx, by, cx, cy);
                float d3 = Sign(px, py, cx, cy, ax, ay);
                bool neg = d1 < 0 || d2 < 0 || d3 < 0;
                bool pos = d1 > 0 || d2 > 0 || d3 > 0;
                if (!(neg && pos)) SetPx(x, y, col, alpha);
            }
    }

    private static void StrokeLine(float ax, float ay, float bx, float by, float thick, byte[] col, float alpha = 1f)
    {
        float half = thick * 0.5f;
        int x0 = (int)Math.Floor(Math.Min(ax, bx) - half);
        int x1 = (int)Math.Ceiling(Math.Max(ax, bx) + half);
        int y0 = (int)Math.Floor(Math.Min(ay, by) - half);
        int y1 = (int)Math.Ceiling(Math.Max(ay, by) + half);
        float dx = bx - ax, dy = by - ay;
        float len2 = dx * dx + dy * dy;
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float px = x + 0.5f, py = y + 0.5f;
                float t = len2 <= 0f ? 0f : Clamp01(((px - ax) * dx + (py - ay) * dy) / len2);
                float qx = ax + t * dx, qy = ay + t * dy;
                float ddx = px - qx, ddy = py - qy;
                if (ddx * ddx + ddy * ddy <= half * half) SetPx(x, y, col, alpha);
            }
    }

    private static void Draw7SegNumber(float centerX, float centerY, float height, int value, byte[] col)
    {
        if (value < 0) value = 0;
        string digits = value.ToString();
        float dw = height * 0.62f;
        float gap = height * 0.22f;
        float total = digits.Length * dw + (digits.Length - 1) * gap;
        float x = centerX - total * 0.5f;
        for (int i = 0; i < digits.Length; i++)
        {
            DrawDigit(x, centerY - height * 0.5f, dw, height, digits[i] - '0', col);
            x += dw + gap;
        }
    }

    private static void DrawDigit(float ox, float oy, float w, float h, int d, byte[] col)
    {
        bool[][] map =
        {
            new[]{true,true,true,false,true,true,true},
            new[]{false,false,true,false,false,true,false},
            new[]{true,false,true,true,true,false,true},
            new[]{true,false,true,true,false,true,true},
            new[]{false,true,true,true,false,true,false},
            new[]{true,true,false,true,false,true,true},
            new[]{true,true,false,true,true,true,true},
            new[]{true,false,true,false,false,true,false},
            new[]{true,true,true,true,true,true,true},
            new[]{true,true,true,true,false,true,true}
        };
        bool[] s = map[d]; // [top, top-left, top-right, mid, bot-left, bot-right, bottom]
        float t = h * 0.12f;
        float top = oy;               // screen y-down: oy is the top edge
        float bottom = oy + h;
        float midY = oy + h * 0.5f;
        if (s[0]) FillRect(ox, top, ox + w, top + t, col);                   // top
        if (s[3]) FillRect(ox, midY - t * 0.5f, ox + w, midY + t * 0.5f, col); // middle
        if (s[6]) FillRect(ox, bottom - t, ox + w, bottom, col);             // bottom
        if (s[1]) FillRect(ox, top, ox + t, midY, col);                      // top-left
        if (s[2]) FillRect(ox + w - t, top, ox + w, midY, col);              // top-right
        if (s[4]) FillRect(ox, midY, ox + t, bottom, col);                   // bottom-left
        if (s[5]) FillRect(ox + w - t, midY, ox + w, bottom, col);           // bottom-right
    }

    private static void FillRect(float x0, float y0, float x1, float y1, byte[] col)
    {
        for (int y = (int)y0; y < (int)y1; y++)
            for (int x = (int)x0; x < (int)x1; x++)
                SetPx(x, y, col, 1f);
    }

    private static float Sign(float px, float py, float ax, float ay, float bx, float by)
    {
        return (px - bx) * (ay - by) - (ax - bx) * (py - by);
    }

    private static float Clamp(float v, float a, float b) { return v < a ? a : (v > b ? b : v); }
    private static float Clamp01(float v) { return Clamp(v, 0f, 1f); }

    private static void WritePpm(string path)
    {
        using (var fs = new FileStream(path, FileMode.Create))
        {
            byte[] header = System.Text.Encoding.ASCII.GetBytes("P6\n" + W + " " + H + "\n255\n");
            fs.Write(header, 0, header.Length);
            fs.Write(_fb, 0, _fb.Length);
        }
    }
}
