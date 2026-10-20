#nullable enable
using System;
using UnityEngine;

namespace BlockPuzzle.Game
{
    /// <summary>
    /// The tutorial's hand: a disc that slides from the tray piece to where the finger should go, over and over, so the
    /// move is shown rather than told. It fades out while the player drags or the game is paused.
    /// </summary>
    public sealed class HandMarker : MonoBehaviour
    {
        const float CycleSeconds = 1.8f;

        SpriteRenderer? _renderer;
        Func<bool>? _hidden;
        Vector2 _from;
        Vector2 _to;
        float _time;

        public static HandMarker Create(Transform parent, float cellSize, Func<bool> hidden)
        {
            var go = new GameObject("Hand");
            go.transform.SetParent(parent, false);
            go.transform.localScale = Vector3.one * (cellSize * 0.8f);
            var marker = go.AddComponent<HandMarker>();
            marker._renderer = go.AddComponent<SpriteRenderer>();
            marker._renderer.sprite = CellSprite.Circle;
            marker._renderer.sortingOrder = 100;   // above the board, the ghost and the pieces
            marker._hidden = hidden;
            go.SetActive(false);
            return marker;
        }

        public bool IsShown => gameObject.activeSelf;

        /// <summary>Starts the loop from a tray slot's centre to the finger position for the target.</summary>
        public void Point(Vector2 from, Vector2 to)
        {
            _from = from;
            _to = to;
            _time = 0f;
            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);

        void Update()
        {
            if (_renderer == null) return;
            _time = (_time + Time.unscaledDeltaTime) % CycleSeconds;
            float t = _time / CycleSeconds;
            transform.position = Vector2.Lerp(_from, _to, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.15f, 0.7f, t)));
            float alpha = t < 0.1f ? t / 0.1f : t > 0.85f ? (1f - t) / 0.15f : 1f;   // appear, slide, hold, fade
            bool hidden = _hidden != null && _hidden();
            _renderer.color = new Color(1f, 1f, 1f, hidden ? 0f : 0.75f * alpha);
        }
    }
}
