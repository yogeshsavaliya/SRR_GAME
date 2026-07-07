using UnityEngine;

namespace Arrows.Game
{
    /// <summary>
    /// Procedurally generates every sprite the game needs (arrows, rounded
    /// tiles, hearts, digits, icons) so the project ships with zero art assets
    /// and no font dependency. All sprites use pixelsPerUnit == width, so a
    /// sprite is exactly one world unit unless scaled.
    /// </summary>
    public static class TextureFactory
    {
        private const int Res = 128;

        private static Sprite ToSprite(Texture2D tex)
        {
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f), tex.width);
        }

        private static Color32[] NewCanvas(int size)
        {
            var px = new Color32[size * size];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(0, 0, 0, 0);
            return px;
        }

        private static Texture2D Bake(Color32[] px, int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.SetPixels32(px);
            return tex;
        }

        // ---- primitive fills -------------------------------------------------

        private static void FillRoundedRect(Color32[] px, int size, float x0, float y0,
            float x1, float y1, float radius, Color32 color)
        {
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float px0 = x + 0.5f, py0 = y + 0.5f;
                    if (px0 < x0 || px0 > x1 || py0 < y0 || py0 > y1) continue;

                    float cx = Mathf.Clamp(px0, x0 + radius, x1 - radius);
                    float cy = Mathf.Clamp(py0, y0 + radius, y1 - radius);
                    float dx = px0 - cx, dy = py0 - cy;
                    if (dx * dx + dy * dy <= radius * radius || radius <= 0f)
                        px[y * size + x] = color;
                }
        }

        private static void FillTriangle(Color32[] px, int size,
            float ax, float ay, float bx, float by, float cx, float cy, Color32 color)
        {
            float minX = Mathf.Min(ax, Mathf.Min(bx, cx));
            float maxX = Mathf.Max(ax, Mathf.Max(bx, cx));
            float minY = Mathf.Min(ay, Mathf.Min(by, cy));
            float maxY = Mathf.Max(ay, Mathf.Max(by, cy));
            int x0 = Mathf.Max(0, Mathf.FloorToInt(minX));
            int x1 = Mathf.Min(size - 1, Mathf.CeilToInt(maxX));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(minY));
            int y1 = Mathf.Min(size - 1, Mathf.CeilToInt(maxY));

            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float pxc = x + 0.5f, pyc = y + 0.5f;
                    float d1 = Sign(pxc, pyc, ax, ay, bx, by);
                    float d2 = Sign(pxc, pyc, bx, by, cx, cy);
                    float d3 = Sign(pxc, pyc, cx, cy, ax, ay);
                    bool hasNeg = d1 < 0 || d2 < 0 || d3 < 0;
                    bool hasPos = d1 > 0 || d2 > 0 || d3 > 0;
                    if (!(hasNeg && hasPos)) px[y * size + x] = color;
                }
        }

        private static float Sign(float px, float py, float ax, float ay, float bx, float by)
        {
            return (px - bx) * (ay - by) - (ax - bx) * (py - by);
        }

        // ---- public sprite builders -----------------------------------------

        public static Sprite RoundedTile(Color32 color)
        {
            var px = NewCanvas(Res);
            float pad = Res * 0.06f;
            FillRoundedRect(px, Res, pad, pad, Res - pad, Res - pad, Res * 0.18f, color);
            return ToSprite(Bake(px, Res));
        }

        /// <summary>A white arrow glyph (stem + head) pointing up, on transparency.</summary>
        public static Sprite ArrowGlyph(Color32 color)
        {
            var px = NewCanvas(Res);
            float cx = Res * 0.5f;
            // Head (triangle) in the upper half.
            FillTriangle(px, Res,
                cx, Res * 0.86f,
                Res * 0.24f, Res * 0.50f,
                Res * 0.76f, Res * 0.50f, color);
            // Stem (rectangle) in the lower half.
            FillRoundedRect(px, Res, Res * 0.40f, Res * 0.16f, Res * 0.60f, Res * 0.56f, Res * 0.05f, color);
            return ToSprite(Bake(px, Res));
        }

        public static Sprite Heart(Color32 color)
        {
            var px = NewCanvas(Res);
            for (int y = 0; y < Res; y++)
                for (int x = 0; x < Res; x++)
                {
                    // Normalize to [-1,1], y up, point at the bottom.
                    float nx = (x + 0.5f) / Res * 2f - 1f;
                    float ny = (y + 0.5f) / Res * 2f - 1f;
                    nx /= 1.15f;
                    ny = ny / 1.15f + 0.15f;
                    float a = nx * nx + ny * ny - 1f;
                    if (a * a * a - nx * nx * ny * ny * ny <= 0f)
                        px[y * Res + x] = color;
                }
            return ToSprite(Bake(px, Res));
        }

        public static Sprite Circle(Color32 color)
        {
            var px = NewCanvas(Res);
            float r = Res * 0.46f;
            float cx = Res * 0.5f, cy = Res * 0.5f;
            for (int y = 0; y < Res; y++)
                for (int x = 0; x < Res; x++)
                {
                    float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                    if (dx * dx + dy * dy <= r * r) px[y * Res + x] = color;
                }
            return ToSprite(Bake(px, Res));
        }

        public static Sprite Solid(Color32 color)
        {
            var px = NewCanvas(Res);
            for (int i = 0; i < px.Length; i++) px[i] = color;
            return ToSprite(Bake(px, Res));
        }

        public static Sprite Check(Color32 color)
        {
            var px = NewCanvas(Res);
            StrokeLine(px, Res, 0.22f, 0.52f, 0.42f, 0.32f, 0.10f, color);
            StrokeLine(px, Res, 0.42f, 0.32f, 0.80f, 0.72f, 0.10f, color);
            return ToSprite(Bake(px, Res));
        }

        public static Sprite Cross(Color32 color)
        {
            var px = NewCanvas(Res);
            StrokeLine(px, Res, 0.28f, 0.28f, 0.72f, 0.72f, 0.10f, color);
            StrokeLine(px, Res, 0.72f, 0.28f, 0.28f, 0.72f, 0.10f, color);
            return ToSprite(Bake(px, Res));
        }

        /// <summary>Right-pointing triangle ("next" / play icon).</summary>
        public static Sprite Triangle(Color32 color)
        {
            var px = NewCanvas(Res);
            FillTriangle(px, Res, Res * 0.30f, Res * 0.24f, Res * 0.30f, Res * 0.76f, Res * 0.78f, Res * 0.50f, color);
            return ToSprite(Bake(px, Res));
        }

        /// <summary>Circular "replay" arrow.</summary>
        public static Sprite Replay(Color32 color)
        {
            var px = NewCanvas(Res);
            float cx = Res * 0.5f, cy = Res * 0.5f;
            float rOuter = Res * 0.34f, rInner = Res * 0.22f;
            for (int y = 0; y < Res; y++)
                for (int x = 0; x < Res; x++)
                {
                    float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                    float d2 = dx * dx + dy * dy;
                    if (d2 > rInner * rInner && d2 < rOuter * rOuter)
                    {
                        float ang = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg; // -180..180
                        if (ang > -50f) px[y * Res + x] = color; // gap for the arrowhead
                    }
                }
            // Arrowhead near the gap (top-right).
            FillTriangle(px, Res, Res * 0.80f, Res * 0.60f, Res * 0.66f, Res * 0.74f, Res * 0.86f, Res * 0.80f, color);
            return ToSprite(Bake(px, Res));
        }

        /// <summary>Renders an integer using a compact 7-segment style into one sprite.</summary>
        public static Sprite Number(int value, Color32 color)
        {
            if (value < 0) value = 0;
            string digits = value.ToString();
            int dw = 80, dh = 128, gap = 16;
            int width = digits.Length * dw + (digits.Length - 1) * gap;
            if (width < 1) width = 1;
            var px = new Color32[width * dh];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(0, 0, 0, 0);

            for (int i = 0; i < digits.Length; i++)
            {
                int ox = i * (dw + gap);
                DrawDigit(px, width, dh, ox, digits[i] - '0', color);
            }
            var tex = new Texture2D(width, dh, TextureFormat.RGBA32, false);
            tex.SetPixels32(px);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, width, dh), new Vector2(0.5f, 0.5f), dh);
        }

        private static void DrawDigit(Color32[] px, int width, int height, int ox, int d, Color32 color)
        {
            // Segments: 0 top,1 top-left,2 top-right,3 mid,4 bot-left,5 bot-right,6 bottom.
            bool[][] map = new bool[][]
            {
                new[]{true,true,true,false,true,true,true},    // 0
                new[]{false,false,true,false,false,true,false},// 1
                new[]{true,false,true,true,true,false,true},   // 2
                new[]{true,false,true,true,false,true,true},   // 3
                new[]{false,true,true,true,false,true,false},  // 4
                new[]{true,true,false,true,false,true,true},   // 5
                new[]{true,true,false,true,true,true,true},    // 6
                new[]{true,false,true,false,false,true,false}, // 7
                new[]{true,true,true,true,true,true,true},     // 8
                new[]{true,true,true,true,false,true,true}     // 9
            };
            bool[] s = map[d];
            float t = 12f;      // stroke thickness
            float m = 14f;      // margin
            float w = 80f;
            float midY = height * 0.5f;
            // Horizontal segments.
            if (s[0]) FillSeg(px, width, height, ox + m, height - m - t, ox + w - m, height - m, color);          // top
            if (s[3]) FillSeg(px, width, height, ox + m, midY - t * 0.5f, ox + w - m, midY + t * 0.5f, color);    // middle
            if (s[6]) FillSeg(px, width, height, ox + m, m, ox + w - m, m + t, color);                            // bottom
            // Vertical segments.
            if (s[1]) FillSeg(px, width, height, ox + m, midY, ox + m + t, height - m, color);                    // top-left
            if (s[2]) FillSeg(px, width, height, ox + w - m - t, midY, ox + w - m, height - m, color);            // top-right
            if (s[4]) FillSeg(px, width, height, ox + m, m, ox + m + t, midY, color);                             // bot-left
            if (s[5]) FillSeg(px, width, height, ox + w - m - t, m, ox + w - m, midY, color);                     // bot-right
        }

        private static void FillSeg(Color32[] px, int width, int height, float x0, float y0, float x1, float y1, Color32 color)
        {
            int ix0 = Mathf.Max(0, Mathf.FloorToInt(x0));
            int ix1 = Mathf.Min(width - 1, Mathf.CeilToInt(x1));
            int iy0 = Mathf.Max(0, Mathf.FloorToInt(y0));
            int iy1 = Mathf.Min(height - 1, Mathf.CeilToInt(y1));
            for (int y = iy0; y <= iy1; y++)
                for (int x = ix0; x <= ix1; x++)
                    px[y * width + x] = color;
        }

        private static void StrokeLine(Color32[] px, int size, float ax, float ay, float bx, float by, float thick, Color32 color)
        {
            float x0 = ax * size, y0 = ay * size, x1 = bx * size, y1 = by * size;
            float half = thick * size * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float px0 = x + 0.5f, py0 = y + 0.5f;
                    float dx = x1 - x0, dy = y1 - y0;
                    float len2 = dx * dx + dy * dy;
                    float t = len2 <= 0f ? 0f : Mathf.Clamp01(((px0 - x0) * dx + (py0 - y0) * dy) / len2);
                    float qx = x0 + t * dx, qy = y0 + t * dy;
                    float ddx = px0 - qx, ddy = py0 - qy;
                    if (ddx * ddx + ddy * ddy <= half * half) px[y * size + x] = color;
                }
        }
    }
}
