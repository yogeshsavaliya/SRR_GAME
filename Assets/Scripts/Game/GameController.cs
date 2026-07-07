using System.Collections.Generic;
using Arrows.Core;
using UnityEngine;

namespace Arrows.Game
{
    /// <summary>
    /// Drives the whole game: builds the camera and sprites, loads levels,
    /// routes taps into the pure <see cref="GameSession"/>, animates results,
    /// tracks hearts, and shows the win / game-over overlays.
    /// </summary>
    public sealed class GameController : MonoBehaviour
    {
        private enum Phase { Playing, Won, Lost }

        // Palette.
        private static readonly Color Background = new Color(0.11f, 0.13f, 0.18f);
        private static readonly Color HeartFull = new Color(0.93f, 0.29f, 0.36f);
        private static readonly Color HeartEmpty = new Color(0.28f, 0.31f, 0.38f);
        private static readonly Color NumberColor = new Color(0.87f, 0.90f, 0.97f);
        private static readonly Color Dim = new Color(0f, 0f, 0f, 0.72f);
        private static readonly Color PanelColor = new Color(0.16f, 0.19f, 0.27f);
        private static readonly Color WinColor = new Color(0.30f, 0.78f, 0.47f);
        private static readonly Color LoseColor = new Color(0.93f, 0.34f, 0.36f);

        private static readonly Color[] DirColors =
        {
            new Color(0.26f, 0.53f, 0.96f), // Up
            new Color(0.30f, 0.78f, 0.47f), // Right
            new Color(0.96f, 0.60f, 0.23f), // Down
            new Color(0.66f, 0.42f, 0.90f)  // Left
        };

        private Camera _cam;
        private GameSession _session;
        private int _levelIndex;
        private Phase _phase = Phase.Playing;

        private int _width, _height;
        private float _boardCenterY;
        private float _orthoSize;
        private int _lastScreenW, _lastScreenH;

        private readonly Dictionary<long, ArrowTile> _tiles = new Dictionary<long, ArrowTile>();
        private GameObject _boardRoot;
        private GameObject _uiRoot;
        private GameObject _overlayRoot;
        private readonly List<ButtonHit> _buttons = new List<ButtonHit>();

        // Cached sprites.
        private Sprite[] _tileSprites;
        private Sprite _glyphSprite;
        private Sprite _heartSprite;
        private Sprite _panelSprite;
        private Sprite _dimSprite;
        private Sprite _checkSprite;
        private Sprite _crossSprite;
        private Sprite _replaySprite;
        private Sprite _nextSprite;

        private struct ButtonHit
        {
            public Vector2 Center;
            public Vector2 Half;
            public System.Action Action;
        }

        private void Awake()
        {
            _cam = Camera.main;
            if (_cam == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                _cam = camGo.AddComponent<Camera>();
            }
            _cam.orthographic = true;
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = Background;
            _cam.transform.position = new Vector3(0f, 0f, -10f);
            _cam.transform.rotation = Quaternion.identity;

            _tileSprites = new Sprite[4];
            for (int i = 0; i < 4; i++) _tileSprites[i] = TextureFactory.RoundedTile(DirColors[i]);
            _glyphSprite = TextureFactory.ArrowGlyph(Color.white);
            _heartSprite = TextureFactory.Heart(Color.white);
            _panelSprite = TextureFactory.RoundedTile(Color.white);
            _dimSprite = TextureFactory.Solid(Color.white);
            _checkSprite = TextureFactory.Check(Color.white);
            _crossSprite = TextureFactory.Cross(Color.white);
            _replaySprite = TextureFactory.Replay(Color.white);
            _nextSprite = TextureFactory.Triangle(Color.white);
        }

        private void Start()
        {
            LoadLevel(0);
        }

        // ---- level lifecycle -------------------------------------------------

        private void LoadLevel(int index)
        {
            _levelIndex = index;
            _phase = Phase.Playing;
            ClearOverlay();
            DestroyChildren(_boardRoot);
            DestroyChildren(_uiRoot);
            _tiles.Clear();

            Board board;
            int hearts;
            if (index < LevelLibrary.Count)
            {
                LevelDefinition def = LevelLibrary.Get(index);
                board = def.CreateBoard();
                hearts = def.Hearts;
            }
            else
            {
                // Endless mode after the handcrafted levels; always solvable.
                board = LevelGenerator.Generate(6, 6, 30, 1000 + index);
                hearts = 3;
            }

            _session = new GameSession(board, hearts);
            _width = board.Width;
            _height = board.Height;

            ComputeLayout();
            BuildBoard(board);
            BuildUi();
        }

        private void ComputeLayout()
        {
            float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
            const float topMargin = 2.6f;
            const float bottomMargin = 0.6f;
            const float sideMargin = 0.6f;

            float halfH = (_height + topMargin + bottomMargin) * 0.5f;
            float halfW = (_width + sideMargin * 2f) * 0.5f / Mathf.Max(0.1f, aspect);
            _orthoSize = Mathf.Max(halfH, halfW);
            _cam.orthographicSize = _orthoSize;
            _boardCenterY = (bottomMargin - topMargin) * 0.5f;

            _lastScreenW = Screen.width;
            _lastScreenH = Screen.height;
        }

        private Vector3 CellToWorld(int x, int y)
        {
            float wx = x - (_width - 1) * 0.5f;
            float wy = _boardCenterY + ((_height - 1) * 0.5f - y);
            return new Vector3(wx, wy, 0f);
        }

        private void BuildBoard(Board board)
        {
            if (_boardRoot == null) _boardRoot = new GameObject("Board");
            for (int y = 0; y < board.Height; y++)
                for (int x = 0; x < board.Width; x++)
                {
                    if (!board.IsOccupied(x, y)) continue;
                    Direction dir = board.DirectionAt(x, y);
                    var go = new GameObject("Arrow_" + x + "_" + y);
                    go.transform.SetParent(_boardRoot.transform, false);
                    var tile = go.AddComponent<ArrowTile>();
                    tile.Init(x, y, dir, CellToWorld(x, y), 1f,
                        _tileSprites[(int)dir], _glyphSprite, DirColors[(int)dir]);
                    _tiles[Key(x, y)] = tile;
                }
        }

        private void BuildUi()
        {
            if (_uiRoot == null) _uiRoot = new GameObject("UI");

            // Hearts row (top-center).
            float heartSize = 0.55f;
            float spacing = 0.72f;
            int max = _session.MaxHearts;
            float startX = -(max - 1) * spacing * 0.5f;
            float heartY = _orthoSize - 0.7f;
            for (int i = 0; i < max; i++)
            {
                bool filled = i < _session.Hearts;
                MakeSprite("Heart" + i, _heartSprite,
                    filled ? HeartFull : HeartEmpty,
                    new Vector3(startX + i * spacing, heartY, 0f), heartSize, 50, _uiRoot.transform);
            }

            // Level number (below hearts).
            Sprite numberSprite = TextureFactory.Number(_levelIndex + 1, NumberColor);
            float numHeight = 0.7f;
            float ratio = (float)numberSprite.texture.width / numberSprite.texture.height;
            var num = MakeSprite("Level", numberSprite, NumberColor,
                new Vector3(0f, _orthoSize - 1.7f, 0f), numHeight, 50, _uiRoot.transform);
            num.transform.localScale = new Vector3(numHeight * ratio, numHeight, 1f);
        }

        // ---- input -----------------------------------------------------------

        private void Update()
        {
            if (Screen.width != _lastScreenW || Screen.height != _lastScreenH)
                Relayout();

            if (!Input.GetMouseButtonDown(0)) return;
            Vector3 world = _cam.ScreenToWorldPoint(
                new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10f));

            if (_phase == Phase.Playing) HandleBoardTap(world);
            else HandleButtonTap(world);
        }

        private void HandleBoardTap(Vector3 world)
        {
            int x = Mathf.RoundToInt(world.x + (_width - 1) * 0.5f);
            int y = Mathf.RoundToInt((_height - 1) * 0.5f - (world.y - _boardCenterY));
            if (x < 0 || x >= _width || y < 0 || y >= _height) return;

            Vector3 center = CellToWorld(x, y);
            if (Mathf.Abs(world.x - center.x) > 0.5f || Mathf.Abs(world.y - center.y) > 0.5f) return;

            ArrowTile tile;
            if (!_tiles.TryGetValue(Key(x, y), out tile) || tile == null || tile.IsAnimating) return;

            TapResult result = _session.Tap(x, y);
            switch (result)
            {
                case TapResult.Escaped:
                case TapResult.Won:
                    _tiles.Remove(Key(x, y));
                    bool won = result == TapResult.Won;
                    tile.PlayEscape(_orthoSize, won ? (System.Action)ShowWin : null);
                    break;
                case TapResult.Blocked:
                    tile.PlayShake(RefreshHearts);
                    break;
                case TapResult.GameOver:
                    tile.PlayShake(null);
                    RefreshHearts();
                    ShowLose();
                    break;
            }
        }

        private void HandleButtonTap(Vector3 world)
        {
            for (int i = 0; i < _buttons.Count; i++)
            {
                ButtonHit b = _buttons[i];
                if (Mathf.Abs(world.x - b.Center.x) <= b.Half.x &&
                    Mathf.Abs(world.y - b.Center.y) <= b.Half.y)
                {
                    if (b.Action != null) b.Action();
                    return;
                }
            }
        }

        // ---- ui state --------------------------------------------------------

        private void RefreshHearts()
        {
            // Rebuild just the hearts row to reflect the current count.
            DestroyChildren(_uiRoot);
            BuildUi();
        }

        private void Relayout()
        {
            ComputeLayout();
            foreach (KeyValuePair<long, ArrowTile> kv in _tiles)
            {
                ArrowTile t = kv.Value;
                if (t != null && !t.IsAnimating)
                    t.transform.position = CellToWorld(t.GridX, t.GridY);
            }
            DestroyChildren(_uiRoot);
            BuildUi();
            if (_phase == Phase.Won) ShowWin();
            else if (_phase == Phase.Lost) ShowLose();
        }

        private void ShowWin()
        {
            _phase = Phase.Won;
            BuildOverlay(WinColor, _checkSprite, true);
        }

        private void ShowLose()
        {
            _phase = Phase.Lost;
            BuildOverlay(LoseColor, _crossSprite, false);
        }

        private void BuildOverlay(Color accent, Sprite icon, bool win)
        {
            ClearOverlay();
            _overlayRoot = new GameObject("Overlay");

            // Dim background covering the whole view.
            var dim = MakeSprite("Dim", _dimSprite, Dim, Vector3.zero, 1f, 100, _overlayRoot.transform);
            dim.transform.localScale = new Vector3(_orthoSize * 4f, _orthoSize * 3f, 1f);

            // Panel.
            var panel = MakeSprite("Panel", _panelSprite, PanelColor, new Vector3(0f, 0.2f, 0f), 1f, 101, _overlayRoot.transform);
            panel.transform.localScale = new Vector3(4.4f, 3.4f, 1f);

            // Icon badge.
            MakeSprite("Badge", _panelSprite, accent, new Vector3(0f, 1.25f, 0f), 1.3f, 102, _overlayRoot.transform);
            MakeSprite("Icon", icon, Color.white, new Vector3(0f, 1.25f, 0f), 0.9f, 103, _overlayRoot.transform);

            // Level number (shows which level you're on) under the badge.
            Sprite numberSprite = TextureFactory.Number(_levelIndex + 1, NumberColor);
            float ratio = (float)numberSprite.texture.width / numberSprite.texture.height;
            var num = MakeSprite("OverlayLevel", numberSprite, NumberColor, new Vector3(0f, 0.15f, 0f), 0.55f, 103, _overlayRoot.transform);
            num.transform.localScale = new Vector3(0.55f * ratio, 0.55f, 1f);

            _buttons.Clear();
            if (win)
            {
                bool hasNext = true; // endless generation means there is always a next level
                AddButton(new Vector3(-1.0f, -1.05f, 0f), _replaySprite, HeartEmpty, () => LoadLevel(_levelIndex));
                if (hasNext)
                    AddButton(new Vector3(1.0f, -1.05f, 0f), _nextSprite, WinColor, () => LoadLevel(_levelIndex + 1));
            }
            else
            {
                AddButton(new Vector3(0f, -1.05f, 0f), _replaySprite, LoseColor, () => LoadLevel(_levelIndex));
            }
        }

        private void AddButton(Vector3 pos, Sprite icon, Color bg, System.Action action)
        {
            float size = 1.2f;
            MakeSprite("Btn", _panelSprite, bg, pos, size, 102, _overlayRoot.transform);
            MakeSprite("BtnIcon", icon, Color.white, pos, size * 0.6f, 103, _overlayRoot.transform);
            _buttons.Add(new ButtonHit
            {
                Center = new Vector2(pos.x, pos.y),
                Half = new Vector2(size * 0.5f, size * 0.5f),
                Action = action
            });
        }

        private void ClearOverlay()
        {
            _buttons.Clear();
            if (_overlayRoot != null) Destroy(_overlayRoot);
            _overlayRoot = null;
        }

        // ---- helpers ---------------------------------------------------------

        private SpriteRenderer MakeSprite(string name, Sprite sprite, Color color,
            Vector3 pos, float scale, int order, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(scale, scale, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
            return sr;
        }

        private static void DestroyChildren(GameObject root)
        {
            if (root == null) return;
            for (int i = root.transform.childCount - 1; i >= 0; i--)
                Destroy(root.transform.GetChild(i).gameObject);
        }

        private static long Key(int x, int y)
        {
            return ((long)x << 32) ^ (uint)y;
        }
    }
}
