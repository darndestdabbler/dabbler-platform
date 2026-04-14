// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

namespace Dabbler.Components;

/// <summary>
/// Tracks the current cell position and computes navigation movements within a grid.
/// Pure logic — no Blazor or UI dependencies.
/// </summary>
/// <remarks>
/// Ported from <c>Dabbler.DataGrid.Controls.NavigationModel</c> (WPF).
/// Used by <see cref="DabblerDataGrid{T}"/> to handle keyboard cell-to-cell navigation.
/// </remarks>
public class GridNavigationModel
{
    private int _currentRow;
    private int _currentColumn;
    private int _rowCount;
    private int _columnCount;

    /// <summary>
    /// Gets or sets a predicate that determines whether a row should be skipped during Tab navigation.
    /// When set, <see cref="MoveToNextCell"/> and <see cref="MoveToPreviousCell"/> skip rows where this returns true.
    /// Also applied by other movement methods, unless <see cref="IsGroupHeaderRow"/> overrides it for that row.
    /// Used to skip group header rows during Tab so Tab only lands on data cells.
    /// </summary>
    public Func<int, bool>? ShouldSkipRow { get; set; }

    /// <summary>
    /// Gets or sets a predicate that returns true if the given row index is a group header row.
    /// When set, <see cref="MoveUp"/> and <see cref="MoveDown"/> will visit group header rows
    /// even if <see cref="ShouldSkipRow"/> would normally skip them — group headers are focusable
    /// via Up/Down but skipped by Tab.
    /// Also used by the keyboard handler to decide whether Left/Right keys should toggle
    /// expand/collapse instead of moving columns.
    /// </summary>
    public Func<int, bool>? IsGroupHeaderRow { get; set; }

    /// <summary>
    /// Gets the current row index.
    /// </summary>
    public int CurrentRow => _currentRow;

    /// <summary>
    /// Gets the current column index.
    /// </summary>
    public int CurrentColumn => _currentColumn;

    /// <summary>
    /// Gets or sets the total number of rows.
    /// </summary>
    public int RowCount
    {
        get => _rowCount;
        set
        {
            _rowCount = Math.Max(0, value);
            ClampPosition();
        }
    }

    /// <summary>
    /// Gets or sets the total number of columns.
    /// </summary>
    public int ColumnCount
    {
        get => _columnCount;
        set
        {
            _columnCount = Math.Max(0, value);
            ClampPosition();
        }
    }

    /// <summary>
    /// Raised when the current cell position changes.
    /// </summary>
    public event EventHandler? CurrentCellChanged;

    /// <summary>
    /// Moves to a specific cell position.
    /// </summary>
    public void MoveTo(int row, int column)
    {
        if (_rowCount == 0 || _columnCount == 0) return;

        int newRow = Math.Clamp(row, 0, _rowCount - 1);
        int newCol = Math.Clamp(column, 0, _columnCount - 1);

        if (newRow != _currentRow || newCol != _currentColumn)
        {
            _currentRow = newRow;
            _currentColumn = newCol;
            CurrentCellChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Moves up one row, skipping rows where <see cref="ShouldSkipRow"/> returns true.</summary>
    /// <remarks>
    /// Group header rows (identified by <see cref="IsGroupHeaderRow"/>) are never skipped by
    /// Up/Down — they are reachable via arrow keys but skipped by Tab.
    /// </remarks>
    public void MoveUp()
    {
        int target = _currentRow - 1;
        while (target >= 0 && ShouldSkipInArrow(target))
            target--;
        if (target >= 0)
            MoveTo(target, _currentColumn);
    }

    /// <summary>Moves down one row, skipping rows where <see cref="ShouldSkipRow"/> returns true.</summary>
    /// <remarks>
    /// Group header rows (identified by <see cref="IsGroupHeaderRow"/>) are never skipped by
    /// Up/Down — they are reachable via arrow keys but skipped by Tab.
    /// </remarks>
    public void MoveDown()
    {
        int target = _currentRow + 1;
        while (target < _rowCount && ShouldSkipInArrow(target))
            target++;
        if (target < _rowCount)
            MoveTo(target, _currentColumn);
    }

    // Returns true only for rows that should be skipped during Up/Down navigation:
    // rows where ShouldSkipRow is true AND they are NOT group header rows
    // (group headers are reachable by arrow keys but skipped by Tab).
    private bool ShouldSkipInArrow(int row) =>
        ShouldSkipRow?.Invoke(row) == true && IsGroupHeaderRow?.Invoke(row) != true;

    /// <summary>Moves left one column.</summary>
    public void MoveLeft()
    {
        if (_currentColumn > 0)
            MoveTo(_currentRow, _currentColumn - 1);
    }

    /// <summary>Moves right one column.</summary>
    public void MoveRight()
    {
        if (_currentColumn < _columnCount - 1)
            MoveTo(_currentRow, _currentColumn + 1);
    }

    /// <summary>
    /// Moves to the next cell (right, then wraps to next row). Tab behavior.
    /// Skips rows where <see cref="ShouldSkipRow"/> returns true.
    /// </summary>
    public void MoveToNextCell()
    {
        if (_rowCount == 0 || _columnCount == 0) return;

        int nextCol = _currentColumn + 1;
        int nextRow = _currentRow;

        if (nextCol >= _columnCount)
        {
            nextCol = 0;
            nextRow++;
            while (nextRow < _rowCount && ShouldSkipRow?.Invoke(nextRow) == true)
                nextRow++;
        }

        if (nextRow < _rowCount)
        {
            MoveTo(nextRow, nextCol);
        }
    }

    /// <summary>
    /// Moves to the previous cell (left, then wraps to previous row). Shift+Tab behavior.
    /// Skips rows where <see cref="ShouldSkipRow"/> returns true.
    /// </summary>
    public void MoveToPreviousCell()
    {
        if (_rowCount == 0 || _columnCount == 0) return;

        int prevCol = _currentColumn - 1;
        int prevRow = _currentRow;

        if (prevCol < 0)
        {
            prevCol = _columnCount - 1;
            prevRow--;
            while (prevRow >= 0 && ShouldSkipRow?.Invoke(prevRow) == true)
                prevRow--;
        }

        if (prevRow >= 0)
        {
            MoveTo(prevRow, prevCol);
        }
    }

    /// <summary>Moves to the first column in the current row.</summary>
    public void MoveHome() => MoveTo(_currentRow, 0);

    /// <summary>Moves to the last column in the current row.</summary>
    public void MoveEnd() => MoveTo(_currentRow, _columnCount - 1);

    /// <summary>Moves to the first cell of the first row.</summary>
    public void MoveToStart() => MoveTo(0, 0);

    /// <summary>Moves to the last cell of the last row.</summary>
    public void MoveToEnd() => MoveTo(_rowCount - 1, _columnCount - 1);

    /// <summary>Moves to the first non-skipped row in the current column (Ctrl+Up).</summary>
    public void MoveToFirstRowInColumn()
    {
        int target = 0;
        while (target < _rowCount && ShouldSkipInArrow(target))
            target++;
        if (target < _rowCount)
            MoveTo(target, _currentColumn);
    }

    /// <summary>Moves to the last non-skipped row in the current column (Ctrl+Down).</summary>
    public void MoveToLastRowInColumn()
    {
        int target = _rowCount - 1;
        while (target >= 0 && ShouldSkipInArrow(target))
            target--;
        if (target >= 0)
            MoveTo(target, _currentColumn);
    }

    /// <summary>
    /// Moves to the next editable cell, skipping columns where <paramref name="isEditable"/> returns false
    /// and rows where <see cref="ShouldSkipRow"/> returns true.
    /// Wraps to the next row if needed.
    /// </summary>
    /// <param name="isEditable">Predicate that returns true if the column at the given index is editable.</param>
    /// <returns><c>true</c> if an editable cell was found; <c>false</c> if no editable cell exists.</returns>
    public bool MoveToNextEditableCell(Func<int, bool> isEditable)
    {
        if (_rowCount == 0 || _columnCount == 0) return false;

        int row = _currentRow;
        int col = _currentColumn + 1;

        // Search forward through all remaining cells
        int totalCells = _rowCount * _columnCount;
        int startLinear = row * _columnCount + col;

        for (int i = 0; i < totalCells; i++)
        {
            int linear = (startLinear + i) % totalCells;
            int r = linear / _columnCount;
            int c = linear % _columnCount;

            // Don't wrap past the last row (stop at end)
            if (r < _currentRow || (r == _currentRow && c <= _currentColumn))
            {
                if (i > 0 && r <= _currentRow) continue;
            }

            if (ShouldSkipRow?.Invoke(r) == true)
                continue;

            if (isEditable(c))
            {
                MoveTo(r, c);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Moves to the previous editable cell, skipping columns where <paramref name="isEditable"/> returns false
    /// and rows where <see cref="ShouldSkipRow"/> returns true.
    /// Wraps to the previous row if needed.
    /// </summary>
    /// <param name="isEditable">Predicate that returns true if the column at the given index is editable.</param>
    /// <returns><c>true</c> if an editable cell was found; <c>false</c> if no editable cell exists.</returns>
    public bool MoveToPreviousEditableCell(Func<int, bool> isEditable)
    {
        if (_rowCount == 0 || _columnCount == 0) return false;

        int row = _currentRow;
        int col = _currentColumn - 1;

        for (int r = row; r >= 0; r--)
        {
            if (ShouldSkipRow?.Invoke(r) == true)
                continue;

            int startCol = (r == row) ? col : _columnCount - 1;
            for (int c = startCol; c >= 0; c--)
            {
                if (isEditable(c))
                {
                    MoveTo(r, c);
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>Moves up by the specified number of rows (page up), landing on a non-skipped row.</summary>
    public void MovePageUp(int viewportRowCount)
    {
        int target = Math.Max(0, _currentRow - viewportRowCount);
        while (target >= 0 && ShouldSkipInArrow(target))
            target--;
        if (target < 0)
        {
            target = Math.Max(0, _currentRow - viewportRowCount);
            while (target < _currentRow && ShouldSkipInArrow(target))
                target++;
        }
        if (target >= 0 && target < _rowCount && !ShouldSkipInArrow(target))
            MoveTo(target, _currentColumn);
    }

    /// <summary>Moves down by the specified number of rows (page down), landing on a non-skipped row.</summary>
    public void MovePageDown(int viewportRowCount)
    {
        int target = Math.Min(_rowCount - 1, _currentRow + viewportRowCount);
        while (target < _rowCount && ShouldSkipInArrow(target))
            target++;
        if (target >= _rowCount)
        {
            target = Math.Min(_rowCount - 1, _currentRow + viewportRowCount);
            while (target > _currentRow && ShouldSkipInArrow(target))
                target--;
        }
        if (target >= 0 && target < _rowCount && !ShouldSkipInArrow(target))
            MoveTo(target, _currentColumn);
    }

    private void ClampPosition()
    {
        if (_rowCount == 0 || _columnCount == 0)
        {
            _currentRow = 0;
            _currentColumn = 0;
            return;
        }
        _currentRow = Math.Min(_currentRow, _rowCount - 1);
        _currentColumn = Math.Min(_currentColumn, _columnCount - 1);
    }
}
