using CacaPalavrasIA.Models;
using Microsoft.Maui.Controls.Shapes;

namespace CacaPalavrasIA.Views;

public partial class WordSearchBoard : ContentView
{
    public const int Size = WordSearchPuzzle.Size;

    private readonly Border[,] _cells = new Border[Size, Size];
    private readonly Label[,] _labels = new Label[Size, Size];

    private (int r, int c)? _dragStart;
    private (int r, int c)? _dragEnd;

    private bool _isDown;
    private HashSet<(int r, int c)> _foundCells = new();

    public event EventHandler<SelectionEventArgs>? SelectionFinished;

    public WordSearchBoard()
    {
        InitializeComponent();
        BuildGrid();

        // ? posição real do mouse/toque
        var pointer = new PointerGestureRecognizer();
        pointer.PointerPressed += OnPointerPressed;
        pointer.PointerMoved += OnPointerMoved;
        pointer.PointerReleased += OnPointerReleased;

        // ? “cancel” quando sai do controle (substitui PointerCanceled)
        pointer.PointerExited += OnPointerExited;

        GestureRecognizers.Add(pointer);
    }

    public void SetPuzzle(char[,] grid, IEnumerable<WordPlacement> foundWords)
    {
        _foundCells = new HashSet<(int r, int c)>(
            foundWords.Where(w => w.Found).SelectMany(w => w.Cells())
        );

        for (int r = 0; r < Size; r++)
            for (int c = 0; c < Size; c++)
                _labels[r, c].Text = grid[r, c].ToString();

        RedrawHighlights();
    }

    public void MarkWordFound(WordPlacement word)
    {
        foreach (var cell in word.Cells())
            _foundCells.Add(cell);

        RedrawHighlights();
    }

    private void BuildGrid()
    {
        BoardGrid.RowDefinitions.Clear();
        BoardGrid.ColumnDefinitions.Clear();
        BoardGrid.Children.Clear();

        for (int i = 0; i < Size; i++)
        {
            BoardGrid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
            BoardGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        }

        for (int r = 0; r < Size; r++)
        {
            for (int c = 0; c < Size; c++)
            {
                var lbl = new Label
                {
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center,
                    FontSize = 18,
                    FontAttributes = FontAttributes.Bold
                };

                var cell = new Border
                {
                    Stroke = Colors.Transparent,
                    StrokeThickness = 0,
                    BackgroundColor = Color.FromArgb("#FFF3F0FF"),
                    StrokeShape = new RoundRectangle { CornerRadius = 10 },
                    Content = lbl
                };

                _cells[r, c] = cell;
                _labels[r, c] = lbl;

                BoardGrid.Add(cell, c, r);
            }
        }
    }

    // ===================== POINTER =====================

    private void OnPointerPressed(object? sender, PointerEventArgs e)
    {
        _isDown = true;

        var cell = PointToCell(e);
        _dragStart = cell;
        _dragEnd = cell;

        RedrawHighlights();
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isDown) return;

        var cell = PointToCell(e);
        if (cell.HasValue)
        {
            _dragEnd = cell;
            RedrawHighlights();
        }
    }

    private void OnPointerReleased(object? sender, PointerEventArgs e)
    {
        if (!_isDown) return;
        _isDown = false;

        if (_dragStart.HasValue && _dragEnd.HasValue)
            SelectionFinished?.Invoke(this, new SelectionEventArgs(_dragStart.Value, _dragEnd.Value));

        _dragStart = null;
        _dragEnd = null;

        RedrawHighlights();
    }

    private void OnPointerExited(object? sender, PointerEventArgs e)
    {
        // Se o usuário estiver arrastando e sair do controle, cancela seleção
        if (!_isDown) return;

        _isDown = false;
        _dragStart = null;
        _dragEnd = null;
        RedrawHighlights();
    }

    private (int r, int c)? PointToCell(PointerEventArgs e)
    {
        var pos = e.GetPosition(BoardGrid);
        if (pos is null) return null;

        if (BoardGrid.Width <= 0 || BoardGrid.Height <= 0) return null;

        var cellW = BoardGrid.Width / Size;
        var cellH = BoardGrid.Height / Size;

        int c = (int)(pos.Value.X / cellW);
        int r = (int)(pos.Value.Y / cellH);

        // clamp
        if (r < 0) r = 0;
        if (c < 0) c = 0;
        if (r >= Size) r = Size - 1;
        if (c >= Size) c = Size - 1;

        return (r, c);
    }

    // ===================== UI =====================

    private void RedrawHighlights()
    {
        for (int r = 0; r < Size; r++)
        {
            for (int c = 0; c < Size; c++)
            {
                var isFound = _foundCells.Contains((r, c));
                _cells[r, c].BackgroundColor = isFound ? Color.FromArgb("#E9F8E6") : Color.FromArgb("#FFF3F0FF");
                _cells[r, c].Stroke = Colors.Transparent;
                _cells[r, c].StrokeThickness = 0;
            }
        }

        if (_dragStart.HasValue && _dragEnd.HasValue)
        {
            var s = _dragStart.Value;
            var e = _dragEnd.Value;

            if (s.r == e.r || s.c == e.c)
            {
                var path = GetPath(s, e);
                foreach (var (r, c) in path)
                {
                    if (_foundCells.Contains((r, c))) continue;
                    _cells[r, c].BackgroundColor = Color.FromArgb("#FFF9E6");
                    _cells[r, c].Stroke = Color.FromArgb("#7A5CFF");
                    _cells[r, c].StrokeThickness = 1.5;
                }
            }
        }
    }

    private static List<(int r, int c)> GetPath((int r, int c) start, (int r, int c) end)
    {
        var cells = new List<(int r, int c)>();

        if (start.r == end.r)
        {
            int r = start.r;
            int c1 = Math.Min(start.c, end.c);
            int c2 = Math.Max(start.c, end.c);
            for (int c = c1; c <= c2; c++) cells.Add((r, c));
        }
        else if (start.c == end.c)
        {
            int c = start.c;
            int r1 = Math.Min(start.r, end.r);
            int r2 = Math.Max(start.r, end.r);
            for (int r = r1; r <= r2; r++) cells.Add((r, c));
        }

        return cells;
    }
}

public sealed class SelectionEventArgs : EventArgs
{
    public (int r, int c) Start { get; }
    public (int r, int c) End { get; }

    public SelectionEventArgs((int r, int c) start, (int r, int c) end)
    {
        Start = start;
        End = end;
    }
}
