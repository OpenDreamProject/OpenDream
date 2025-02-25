using Robust.Client.UserInterface.Controls;

namespace OpenDreamClient.Interface.Controls.UI;

/// <summary>
/// The control responsible for sizing & layout of verb panels in INFO controls
/// Necessary because RT's grid control can't dynamically adjust the amount of columns
/// </summary>
public sealed class VerbPanelGrid : Container {
    private int _columns;
    private Vector2 _cellSize;

    protected override Vector2 MeasureOverride(Vector2 availableSize) {
        var largest = Vector2.Zero;
        int childCount = 0;

        // Measure every child to find the largest one
        foreach (var child in Children) {
            child.Measure(availableSize);
            largest = Vector2.Max(largest, child.DesiredSize);

            childCount++;
        }

        if (childCount == 0)
            return Vector2.Zero;

        // Find the maximum amount of columns we can fit in the available space
        if (float.IsPositiveInfinity(availableSize.X)) {
            // Just go with 3 if we have infinite horizontal space
            _columns = 3;
        } else {
            _columns = Math.Min(
                (int)(availableSize.X / largest.X),
                childCount
            );
        }

        // The size of each cell with the given columns
        _cellSize = largest with {
            X = availableSize.X / _columns
        };

        // Amount of rows is the number of children divided by the columns, rounded up
        int rows = (int)Math.Ceiling((double)childCount / _columns);

        return new Vector2(_cellSize.X * _columns, _cellSize.Y * rows);
    }

    protected override Vector2 ArrangeOverride(Vector2 finalSize) {
        Vector2 cellPosition = Vector2.Zero;
        int currentColumn = 0;
        int currentRow = 1;

        foreach (var child in Children) {
            child.Arrange(UIBox2.FromDimensions(cellPosition, _cellSize));

            currentColumn++;
            if (currentColumn == _columns) {
                currentColumn = 0;
                currentRow++;

                cellPosition.X = 0;
                cellPosition.Y += _cellSize.Y;
            } else {
                cellPosition.X += _cellSize.X;
            }
        }

        return new Vector2(_cellSize.X * _columns, _cellSize.Y * currentRow);
    }
}
