using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace LanChat.Controls;

public class PinchZoomContainer : Border
{
    private double _scale = 1.0;
    private double _panX = 0.0;
    private double _panY = 0.0;

    private readonly ScaleTransform _scaleTransform = new(1.0, 1.0);
    private readonly TranslateTransform _translateTransform = new(0.0, 0.0);
    private readonly TransformGroup _transformGroup = new();

    // Multi-touch tracking by Pointer ID
    private readonly Dictionary<int, Point> _activePointers = new();
    private double _initialPinchDistance = 0.0;
    private double _pinchStartScale = 1.0;
    private Point _pinchStartCenter = new(0, 0);
    private double _pinchStartPanX = 0.0;
    private double _pinchStartPanY = 0.0;

    // Single-finger or mouse drag pan tracking (active when zoomed in)
    private Point? _lastSinglePanPoint;

    public double CurrentScale => _scale;

    public PinchZoomContainer()
    {
        ClipToBounds = true;
        Background = Brushes.Black;

        _transformGroup.Children.Add(_scaleTransform);
        _transformGroup.Children.Add(_translateTransform);

        PointerPressed += OnPointerPressed;
        PointerMoved += OnPointerMoved;
        PointerReleased += OnPointerReleased;
        PointerCaptureLost += OnPointerCaptureLost;
        PointerWheelChanged += OnPointerWheelChanged;

        // Desktop trackpad pinch-to-zoom (precision touchpad magnify gesture)
        AddHandler(InputElement.PointerTouchPadGestureMagnifyEvent, OnTouchpadMagnify);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        ApplyTransformsToChild();
        Reset();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ChildProperty)
        {
            ApplyTransformsToChild();
            Reset();
        }
    }

    private void ApplyTransformsToChild()
    {
        if (Child != null)
        {
            Child.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
            Child.RenderTransform = _transformGroup;
        }
    }

    public void Reset()
    {
        _scale = 1.0;
        _panX = 0.0;
        _panY = 0.0;
        _activePointers.Clear();
        _lastSinglePanPoint = null;
        _initialPinchDistance = 0.0;
        Cursor = Cursor.Default;
        UpdateTransforms();
    }

    private void UpdateTransforms()
    {
        _scaleTransform.ScaleX = _scale;
        _scaleTransform.ScaleY = _scale;
        _translateTransform.X = _panX;
        _translateTransform.Y = _panY;

        Cursor = _scale > 1.05 ? new Cursor(StandardCursorType.SizeAll) : Cursor.Default;
    }

    private void ClampPan()
    {
        if (_scale <= 1.02)
        {
            _scale = 1.0;
            _panX = 0.0;
            _panY = 0.0;
            return;
        }

        double w = Bounds.Width > 0 ? Bounds.Width : 400;
        double h = Bounds.Height > 0 ? Bounds.Height : 600;

        double maxPanX = (w * (_scale - 1.0)) / 2.0;
        double maxPanY = (h * (_scale - 1.0)) / 2.0;

        _panX = Math.Clamp(_panX, -maxPanX, maxPanX);
        _panY = Math.Clamp(_panY, -maxPanY, maxPanY);
    }

    private static double GetDistance(Point p1, Point p2)
    {
        double dx = p1.X - p2.X;
        double dy = p1.Y - p2.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(this);
        if (point.Properties.IsLeftButtonPressed)
        {
            int id = e.Pointer.Id;
            Point pos = e.GetPosition(this);
            _activePointers[id] = pos;

            if (_activePointers.Count == 1)
            {
                _lastSinglePanPoint = pos;
            }
            else if (_activePointers.Count >= 2)
            {
                // Start of 2-finger pinch gesture
                var points = _activePointers.Values.Take(2).ToArray();
                _initialPinchDistance = GetDistance(points[0], points[1]);
                _pinchStartScale = _scale;
                _pinchStartCenter = new Point((points[0].X + points[1].X) / 2.0, (points[0].Y + points[1].Y) / 2.0);
                _pinchStartPanX = _panX;
                _pinchStartPanY = _panY;
            }

            if (e.Pointer.Type == PointerType.Mouse)
            {
                e.Pointer.Capture(this);
            }
        }
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        int id = e.Pointer.Id;

        // On desktop mouse: only process drag if left button is actively held down
        var point = e.GetCurrentPoint(this);
        if (e.Pointer.Type == PointerType.Mouse && !point.Properties.IsLeftButtonPressed)
        {
            _activePointers.Remove(id);
            _lastSinglePanPoint = null;
            return;
        }

        if (!_activePointers.ContainsKey(id))
            return;

        _activePointers[id] = e.GetPosition(this);

        if (_activePointers.Count >= 2)
        {
            // 2 fingers pinch-to-zoom (Touchscreen or mobile)
            var points = _activePointers.Values.Take(2).ToArray();
            double currentDistance = GetDistance(points[0], points[1]);

            if (_initialPinchDistance > 15)
            {
                double factor = currentDistance / _initialPinchDistance;
                _scale = Math.Clamp(_pinchStartScale * factor, 1.0, 5.0);

                var currentCenter = new Point((points[0].X + points[1].X) / 2.0, (points[0].Y + points[1].Y) / 2.0);
                double centerDeltaX = currentCenter.X - _pinchStartCenter.X;
                double centerDeltaY = currentCenter.Y - _pinchStartCenter.Y;

                _panX = _pinchStartPanX + centerDeltaX;
                _panY = _pinchStartPanY + centerDeltaY;

                ClampPan();
                UpdateTransforms();
                e.Handled = true;
            }
        }
        else if (_activePointers.Count == 1 && _scale > 1.05)
        {
            // 1 finger or mouse drag panning only when zoomed in
            Point currentPos = e.GetPosition(this);
            if (_lastSinglePanPoint.HasValue)
            {
                var delta = currentPos - _lastSinglePanPoint.Value;
                _panX += delta.X;
                _panY += delta.Y;
                ClampPan();
                UpdateTransforms();
                e.Handled = true;
            }
            _lastSinglePanPoint = currentPos;
        }
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        int id = e.Pointer.Id;
        _activePointers.Remove(id);

        if (e.Pointer.Type == PointerType.Mouse && Equals(e.Pointer.Captured, this))
        {
            e.Pointer.Capture(null);
        }

        if (_activePointers.Count == 1)
        {
            _lastSinglePanPoint = _activePointers.Values.First();
        }
        else if (_activePointers.Count == 0)
        {
            _lastSinglePanPoint = null;
            if (_scale <= 1.05)
            {
                _scale = 1.0;
                _panX = 0.0;
                _panY = 0.0;
                UpdateTransforms();
            }
        }
    }

    private void OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        int id = e.Pointer.Id;
        _activePointers.Remove(id);

        if (_activePointers.Count == 1)
        {
            _lastSinglePanPoint = _activePointers.Values.First();
        }
        else if (_activePointers.Count == 0)
        {
            _lastSinglePanPoint = null;
            if (_scale <= 1.05)
            {
                _scale = 1.0;
                _panX = 0.0;
                _panY = 0.0;
                UpdateTransforms();
            }
        }
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        // Mouse wheel scroll zoom
        double factor = e.Delta.Y > 0 ? 1.15 : (1.0 / 1.15);
        _scale = Math.Clamp(_scale * factor, 1.0, 5.0);
        ClampPan();
        UpdateTransforms();
        e.Handled = true;
    }

    private void OnTouchpadMagnify(object? sender, PointerDeltaEventArgs e)
    {
        // Desktop touchpad pinch-to-zoom (precision trackpad magnify)
        double factor = 1.0 + e.Delta.X;
        if (factor > 0.1 && factor < 10.0)
        {
            _scale = Math.Clamp(_scale * factor, 1.0, 5.0);
            ClampPan();
            UpdateTransforms();
            e.Handled = true;
        }
    }
}
