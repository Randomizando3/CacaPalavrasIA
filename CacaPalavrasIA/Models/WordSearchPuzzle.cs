namespace CacaPalavrasIA.Models;

public sealed class WordSearchPuzzle
{
    public const int Size = 10;

    public char[,] Grid { get; } = new char[Size, Size];
    public List<WordPlacement> Words { get; } = new();

    public string CurrentTargetWord { get; set; } = "";
    public string CurrentSyllable { get; set; } = "";
}
