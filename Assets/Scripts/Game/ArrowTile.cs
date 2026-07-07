using System.Collections;
using Arrows.Core;
using UnityEngine;

namespace Arrows.Game
{
    /// <summary>
    /// Visual representation of a single arrow on the board. Owns its tile
    /// background + arrow glyph and plays the escape / blocked animations.
    /// </summary>
    public sealed class ArrowTile : MonoBehaviour
    {
        public int GridX { get; private set; }
        public int GridY { get; private set; }
        public Direction Dir { get; private set; }
        public bool IsAnimating { get; private set; }

        private SpriteRenderer _glyph;
        private Color _baseColor;
        private Vector3 _homePosition;

        public void Init(int x, int y, Direction dir, Vector3 worldPos, float cellSize,
            Sprite glyphSprite, Color strokeColor)
        {
            GridX = x;
            GridY = y;
            Dir = dir;
            _homePosition = worldPos;
            _baseColor = strokeColor;
            transform.position = worldPos;

            float fill = cellSize * 0.92f;

            var glyphGo = new GameObject("Glyph");
            glyphGo.transform.SetParent(transform, false);
            _glyph = glyphGo.AddComponent<SpriteRenderer>();
            _glyph.sprite = glyphSprite;
            _glyph.color = strokeColor;
            _glyph.sortingOrder = 11;
            glyphGo.transform.localScale = new Vector3(fill, fill, 1f);
            glyphGo.transform.localRotation = Quaternion.Euler(0f, 0f, DirectionUtil.ZRotationDegrees(dir));
        }

        public void PlayEscape(float orthoSize, System.Action onComplete)
        {
            IsAnimating = true;
            StartCoroutine(EscapeRoutine(orthoSize, onComplete));
        }

        private IEnumerator EscapeRoutine(float orthoSize, System.Action onComplete)
        {
            var dir = new Vector3(DirectionUtil.Dx(Dir), -DirectionUtil.Dy(Dir), 0f);
            // Travel far enough to clear the screen regardless of position.
            Vector3 target = _homePosition + dir * (orthoSize * 2.4f + 2f);
            float dur = 0.28f;
            float t = 0f;
            Vector3 start = transform.position;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / dur);
                float ease = k * k; // accelerate away
                transform.position = Vector3.Lerp(start, target, ease);
                float a = 1f - Mathf.Clamp01((k - 0.6f) / 0.4f);
                SetAlpha(a);
                yield return null;
            }
            if (onComplete != null) onComplete();
            Destroy(gameObject);
        }

        public void PlayShake(System.Action onComplete)
        {
            IsAnimating = true;
            StartCoroutine(ShakeRoutine(onComplete));
        }

        private IEnumerator ShakeRoutine(System.Action onComplete)
        {
            // Shake perpendicular to the arrow's facing so the "stuck" read is clear.
            Vector3 perp = (Dir == Direction.Left || Dir == Direction.Right)
                ? Vector3.up : Vector3.right;
            float dur = 0.34f;
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = t / dur;
                float offset = Mathf.Sin(k * Mathf.PI * 6f) * 0.12f * (1f - k);
                transform.position = _homePosition + perp * offset;
                float redness = Mathf.Sin(k * Mathf.PI); // flash toward red then back
                if (_glyph != null)
                    _glyph.color = Color.Lerp(_baseColor, new Color(0.93f, 0.29f, 0.36f), redness);
                yield return null;
            }
            transform.position = _homePosition;
            if (_glyph != null) _glyph.color = _baseColor;
            IsAnimating = false;
            if (onComplete != null) onComplete();
        }

        private void SetAlpha(float a)
        {
            if (_glyph != null)
            {
                Color g = _glyph.color; g.a = a; _glyph.color = g;
            }
        }
    }
}
