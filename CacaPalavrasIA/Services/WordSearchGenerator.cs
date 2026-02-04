using System.Globalization;
using System.Text;
using CacaPalavrasIA.Models;

namespace CacaPalavrasIA.Services;

public sealed class WordSearchGenerator
{
    private readonly GroqWordSearchService _groq;
    private readonly Random _rng = new();

    public WordSearchGenerator(GroqWordSearchService groq)
    {
        _groq = groq;
    }

    public async Task<WordSearchPuzzle> CreatePuzzleAsync(CancellationToken ct)
    {
        var animals = await _groq.GenerateAnimalsAsync(ct);

        // Normaliza e garante 10 itens válidos
        var words = animals
            .Select(NormalizeWord)
            .Where(w => w.Length is >= 3 and <= 10)
            .Distinct()
            .Take(10)
            .ToList();

        // fallback (caso IA venha ruim/offline)
        if (words.Count < 10)
        {
            var fallback = new[] { "BODE", "GATO", "PATO", "VACA", "LEAO", "TIGRE", "CAVALO", "ABELHA", "OVELHA", "CORUJA" }
                .Select(NormalizeWord)
                .ToList();

            foreach (var w in fallback)
                if (!words.Contains(w))
                    words.Add(w);

            words = words.Distinct().Take(10).ToList();
        }

        var puzzle = new WordSearchPuzzle();

        // limpa grid
        for (int r = 0; r < WordSearchPuzzle.Size; r++)
            for (int c = 0; c < WordSearchPuzzle.Size; c++)
                puzzle.Grid[r, c] = '\0';

        // coloca palavras
        foreach (var w in words)
        {
            var placed = TryPlaceWord(puzzle, w);
            if (!placed)
            {
                // se não couber, ignora (bem raro)
            }
        }

        // preenche vazios com letras
        for (int r = 0; r < WordSearchPuzzle.Size; r++)
        {
            for (int c = 0; c < WordSearchPuzzle.Size; c++)
            {
                if (puzzle.Grid[r, c] == '\0')
                    puzzle.Grid[r, c] = (char)('A' + _rng.Next(0, 26));
            }
        }

        // define alvo inicial
        PickNextTarget(puzzle);

        return puzzle;
    }

    public void PickNextTarget(WordSearchPuzzle puzzle)
    {
        var remaining = puzzle.Words.Where(w => !w.Found).ToList();
        if (remaining.Count == 0)
        {
            puzzle.CurrentTargetWord = "";
            puzzle.CurrentSyllable = "";
            return;
        }

        var next = remaining[_rng.Next(remaining.Count)];
        puzzle.CurrentTargetWord = next.Word;

        // “sílaba” simples: 2 letras (infantil, rápido)
        var syl = next.Word.Length >= 2 ? next.Word[..2] : next.Word;
        puzzle.CurrentSyllable = syl;
    }

    public bool TryMarkSelection(WordSearchPuzzle puzzle, (int r, int c) start, (int r, int c) end, out WordPlacement? matched)
    {
        matched = null;

        // Apenas horizontal OU vertical
        if (start.r != end.r && start.c != end.c)
            return false;

        var word = ReadWord(puzzle, start, end);
        if (string.IsNullOrWhiteSpace(word))
            return false;

        // tenta casar com qualquer palavra ainda não encontrada
        var hit = puzzle.Words.FirstOrDefault(w => !w.Found && w.Word.Equals(word, StringComparison.OrdinalIgnoreCase));
        if (hit == null)
            return false;

        hit.Found = true;
        matched = hit;
        return true;
    }

    public List<(int r, int c)> GetPath((int r, int c) start, (int r, int c) end)
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

    private string ReadWord(WordSearchPuzzle puzzle, (int r, int c) start, (int r, int c) end)
    {
        var sb = new StringBuilder();
        var path = GetPath(start, end);
        foreach (var (r, c) in path)
        {
            if (r < 0 || r >= WordSearchPuzzle.Size || c < 0 || c >= WordSearchPuzzle.Size)
                return "";
            sb.Append(puzzle.Grid[r, c]);
        }
        return sb.ToString();
    }

    private bool TryPlaceWord(WordSearchPuzzle puzzle, string word)
    {
        // tenta várias vezes
        for (int attempt = 0; attempt < 200; attempt++)
        {
            var dir = _rng.Next(2) == 0 ? WordDirection.Horizontal : WordDirection.Vertical;

            int maxRow = dir == WordDirection.Vertical ? WordSearchPuzzle.Size - word.Length : WordSearchPuzzle.Size - 1;
            int maxCol = dir == WordDirection.Horizontal ? WordSearchPuzzle.Size - word.Length : WordSearchPuzzle.Size - 1;

            if (maxRow < 0 || maxCol < 0) return false;

            int row = _rng.Next(0, maxRow + 1);
            int col = _rng.Next(0, maxCol + 1);

            if (!CanPlace(puzzle, word, row, col, dir))
                continue;

            Place(puzzle, word, row, col, dir);
            puzzle.Words.Add(new WordPlacement { Word = word, Row = row, Col = col, Direction = dir });
            return true;
        }

        return false;
    }

    private bool CanPlace(WordSearchPuzzle puzzle, string word, int row, int col, WordDirection dir)
    {
        for (int i = 0; i < word.Length; i++)
        {
            int r = dir == WordDirection.Horizontal ? row : row + i;
            int c = dir == WordDirection.Horizontal ? col + i : col;

            var existing = puzzle.Grid[r, c];
            if (existing != '\0' && existing != word[i])
                return false;
        }
        return true;
    }

    private void Place(WordSearchPuzzle puzzle, string word, int row, int col, WordDirection dir)
    {
        for (int i = 0; i < word.Length; i++)
        {
            int r = dir == WordDirection.Horizontal ? row : row + i;
            int c = dir == WordDirection.Horizontal ? col + i : col;

            puzzle.Grid[r, c] = word[i];
        }
    }

    private static string NormalizeWord(string input)
    {
        input = (input ?? "").Trim().ToUpperInvariant();

        // remove acentos
        var normalized = input.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var ch in normalized)
        {
            var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (uc != UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        }
        var noAccents = sb.ToString().Normalize(NormalizationForm.FormC);

        // mantém só letras
        var lettersOnly = new string(noAccents.Where(char.IsLetter).ToArray());
        return lettersOnly;
    }
}
