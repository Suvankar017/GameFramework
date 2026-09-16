using System;
using UnityEngine;

namespace GameFramework.Input
{
    /// <summary>
    /// Opt-in gesture recognition (tap, long press, drag, swipe) built on top of a single
    /// <see cref="PointerState"/> stream. Deliberately not part of <see cref="IInputService"/> —
    /// most games only need raw pointer/touch state, so a feature that wants gestures instantiates
    /// one of these itself and feeds it every frame:
    /// <code>
    /// _recognizer.Update(inputService.Pointer, timeService.UnscaledTime);
    /// </code>
    /// </summary>
    public sealed class PointerGestureRecognizer
    {
        public event Action<Vector2> Tapped;
        public event Action<Vector2> LongPressed;
        public event Action<Vector2> DragStarted;
        public event Action<Vector2, Vector2> DragUpdated; // (current position, delta since last update)
        public event Action<Vector2> DragEnded;
        public event Action<Vector2> Swiped; // direction, normalized

        public float TapMaxDuration = 0.3f;
        public float TapMaxMovement = 20f;
        public float LongPressDuration = 0.6f;
        public float DragStartThreshold = 10f;
        public float SwipeMinDistance = 60f;
        public float SwipeMaxDuration = 0.5f;

        private bool _isTracking;
        private bool _isDragging;
        private bool _longPressFired;
        private float _pressStartTime;
        private Vector2 _pressStartPosition;
        private Vector2 _lastPosition;

        public void Update(PointerState pointer, float unscaledTime)
        {
            if (pointer.WasPressedThisFrame)
            {
                _isTracking = true;
                _isDragging = false;
                _longPressFired = false;
                _pressStartTime = unscaledTime;
                _pressStartPosition = pointer.ScreenPosition;
                _lastPosition = pointer.ScreenPosition;
                return;
            }

            if (!_isTracking)
            {
                return;
            }

            if (pointer.IsDown)
            {
                float distanceFromStart = Vector2.Distance(_pressStartPosition, pointer.ScreenPosition);

                if (!_isDragging && distanceFromStart >= DragStartThreshold)
                {
                    _isDragging = true;
                    DragStarted?.Invoke(_pressStartPosition);
                }

                if (_isDragging)
                {
                    Vector2 delta = pointer.ScreenPosition - _lastPosition;
                    if (delta != Vector2.zero)
                    {
                        DragUpdated?.Invoke(pointer.ScreenPosition, delta);
                    }
                }

                if (!_longPressFired && !_isDragging && unscaledTime - _pressStartTime >= LongPressDuration)
                {
                    _longPressFired = true;
                    LongPressed?.Invoke(pointer.ScreenPosition);
                }

                _lastPosition = pointer.ScreenPosition;
                return;
            }

            if (pointer.WasReleasedThisFrame)
            {
                float duration = unscaledTime - _pressStartTime;
                float distance = Vector2.Distance(_pressStartPosition, pointer.ScreenPosition);

                if (_isDragging)
                {
                    DragEnded?.Invoke(pointer.ScreenPosition);

                    if (distance >= SwipeMinDistance && duration <= SwipeMaxDuration)
                    {
                        Swiped?.Invoke((pointer.ScreenPosition - _pressStartPosition).normalized);
                    }
                }
                else if (!_longPressFired && duration <= TapMaxDuration && distance <= TapMaxMovement)
                {
                    Tapped?.Invoke(pointer.ScreenPosition);
                }

                _isTracking = false;
                _isDragging = false;
            }
        }
    }
}
