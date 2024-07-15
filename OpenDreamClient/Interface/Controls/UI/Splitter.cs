using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;

namespace OpenDreamClient.Interface.Controls.UI;

/// <summary>
/// A splitter control that gives 2 children a resizable amount of space.
/// Equivalent to BYOND's CHILD control.
/// </summary>
/// <remarks>Do not add children directly! Use <see cref="Left"/> and <see cref="Right"/>.</remarks>
public sealed class Splitter : Container {
    public float SplitterWidth {
        get => _splitterWidth;
        set {
            _splitterWidth = value;
            InvalidateMeasure();
        }
    }

    public Control? Left {
        get => _left;
        set {
            if (_left == value)
                return;

            if (_left != null)
                RemoveChild(_left);

            _left = value;
            if (_left != null)
                AddChild(_left);
        }
    }

    public Control? Right {
        get => _right;
        set {
            if (_right == value)
                return;

            if (_right != null)
                RemoveChild(_right);

            _right = value;
            if (_right != null)
                AddChild(_right);
        }
    }

    public float SplitterPercentage {
        get => _splitterPercentage;
        set {
            _splitterPercentage = Math.Clamp(value, 0.1f, 0.9f);
            InvalidateMeasure();
        }
    }

    public bool Vertical {
        get => _vertical;
        set {
            _vertical = value;
            _drag.DefaultCursorShape = value ? CursorShape.HResize : CursorShape.VResize;
            InvalidateMeasure();
        }
    }

    public StyleBox? DragStyleBoxOverride {
        get => _drag.StyleBoxOverride;
        set => _drag.StyleBoxOverride = value;
    }

    private readonly DragControl _drag = new();

    private float _splitterWidth = 5f;
    private float _splitterPercentage = 0.5f;
    private bool _vertical;
    private bool _dragging;
    private Control? _left, _right;

    public Splitter() {
        MouseFilter = MouseFilterMode.Stop;

        _drag.OnMouseMove += MouseMove;
        _drag.OnMouseDown += StartDragging;
        _drag.OnMouseUp += StopDragging;
        AddChild(_drag);
    }

    protected override Vector2 MeasureOverride(Vector2 availableSize) {
        var space = CalculateSpace(availableSize);

        _left?.Measure(space.LeftBox.Size);
        _drag.Measure(space.DragBox.Size);
        _right?.Measure(space.RightBox.Size);

        // Always take the full space
        return availableSize;
    }

    protected override Vector2 ArrangeOverride(Vector2 finalSize) {
        var space = CalculateSpace(finalSize);

        _left?.Arrange(space.LeftBox);
        _drag.Arrange(space.DragBox);
        _right?.Arrange(space.RightBox);

        // Always take the full space
        return finalSize;
    }

    protected override void MouseMove(GUIMouseMoveEventArgs args) {
        if (!_dragging)
            return;

        var relative = args.GlobalPosition - GlobalPosition;

        if (Vertical) {
            SplitterPercentage = relative.X / Size.X;
        } else {
            SplitterPercentage = relative.Y / Size.Y;
        }
    }

    private void StartDragging(GUIBoundKeyEventArgs args) {
        if (_dragging)
            return;

        _dragging = true;
        DefaultCursorShape = Vertical ? CursorShape.HResize : CursorShape.VResize;
    }

    private void StopDragging(GUIBoundKeyEventArgs args) {
        _dragging = false;
        DefaultCursorShape = CursorShape.Arrow;
    }

    private (UIBox2 LeftBox, UIBox2 DragBox, UIBox2 RightBox) CalculateSpace(Vector2 available) {
        if (_left != null && _right == null)
            return (UIBox2.FromDimensions(Vector2.Zero, available), default, default);
        if (_left == null && _right != null)
            return (default, default, UIBox2.FromDimensions(Vector2.Zero, available));
        if (_left == null && _right == null)
            return (default, default, default);

        var leftSize = Vertical
            ? available with { X = available.X * SplitterPercentage - SplitterWidth/2 }
            : available with { Y = available.Y * SplitterPercentage - SplitterWidth/2 };
        var rightSize = Vertical
            ? available with { X = available.X * (1f - SplitterPercentage) - SplitterWidth/2 }
            : available with { Y = available.Y * (1f - SplitterPercentage) - SplitterWidth/2 };
        var dragSize = Vertical
            ? available with { X = SplitterWidth }
            : available with { Y = SplitterWidth };
        var dragPos = Vertical
            ? leftSize with { Y = 0f }
            : leftSize with { X = 0f };
        var rightPos = Vertical
            ? new Vector2(leftSize.X + SplitterWidth, 0f)
            : new Vector2(0f, leftSize.Y + SplitterWidth);

        return (
            UIBox2.FromDimensions(Vector2.Zero, leftSize),
            UIBox2.FromDimensions(dragPos, dragSize),
            UIBox2.FromDimensions(rightPos, rightSize)
        );
    }

    private sealed class DragControl : Control {
        private static readonly StyleBox DragControlStyleBoxDefault = new StyleBoxFlat(Color.DarkGray) {
            BorderColor = Color.Gray,
            BorderThickness = new(1)
        };

        public event Action<GUIBoundKeyEventArgs>? OnMouseDown;
        public event Action<GUIBoundKeyEventArgs>? OnMouseUp;
        public event Action<GUIMouseMoveEventArgs>? OnMouseMove;

        public StyleBox? StyleBoxOverride;

        public DragControl() {
            MouseFilter = MouseFilterMode.Stop;
        }

        protected override void MouseMove(GUIMouseMoveEventArgs args) {
            base.MouseMove(args);
            OnMouseMove?.Invoke(args);
        }

        protected override void KeyBindDown(GUIBoundKeyEventArgs args) {
            base.KeyBindDown(args);
            if (args.Function == EngineKeyFunctions.UIClick)
                OnMouseDown?.Invoke(args);
        }

        protected override void KeyBindUp(GUIBoundKeyEventArgs args) {
            base.KeyBindUp(args);
            if (args.Function == EngineKeyFunctions.UIClick)
                OnMouseUp?.Invoke(args);
        }

        protected override void Draw(DrawingHandleScreen handle) {
            var styleBox = StyleBoxOverride ?? DragControlStyleBoxDefault;

            styleBox.Draw(handle, UIBox2.FromDimensions(Vector2.Zero, PixelSize), UIScale);
        }
    }
}
