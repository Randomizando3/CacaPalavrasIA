namespace CacaPalavrasIA.Models;

public enum WordDirection { Horizontal, Vertical }

public sealed class WordPlacement
{
    public required string Word { get; init; }
    public required int Row { get; init; }
    public required int Col { get; init; }
    public required WordDirection Direction { get; init; }
    public bool Found { get; set; }

    public IEnumerable<(int r, int c)> Cells()
    {
        for (int i = 0; i < Word.Length; i++)
        {
            yield return Direction == WordDirection.Horizontal
                ? (Row, Col + i)
                : (Row + i, Col);
        }
    }
}
