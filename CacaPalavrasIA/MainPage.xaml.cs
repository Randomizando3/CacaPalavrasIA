using System.Collections.ObjectModel;
using System.Web;
using CacaPalavrasIA.Models;
using CacaPalavrasIA.Services;
using CacaPalavrasIA.Views;

namespace CacaPalavrasIA;

public partial class MainPage : ContentPage
{
    private readonly WordSearchGenerator _generator;

    private WordSearchPuzzle? _puzzle;

    public ObservableCollection<WordItemVm> WordsVm { get; } = new();

    public MainPage(WordSearchGenerator generator)
    {
        InitializeComponent();
        _generator = generator;

        WordsList.ItemsSource = WordsVm;

        // ✅ agora é EventHandler
        Board.SelectionFinished += OnBoardSelectionFinished;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_puzzle == null)
            await NewPuzzleAsync();
    }

    private async void OnNewPuzzleClicked(object sender, EventArgs e)
    {
        await NewPuzzleAsync();
    }

    private async Task NewPuzzleAsync()
    {
        try
        {
            StatusLabel.Text = "Gerando com IA (Groq)...";
            InstructionLabel.Text = "Um momento...";

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(45));
            _puzzle = await _generator.CreatePuzzleAsync(cts.Token);

            WordsVm.Clear();
            foreach (var w in _puzzle.Words.OrderBy(x => x.Word))
                WordsVm.Add(new WordItemVm(w.Word, w.Found));

            Board.SetPuzzle(_puzzle.Grid, _puzzle.Words);

            await SpeakTargetAsync();
            NavigateWebTo(_puzzle.CurrentTargetWord);

            StatusLabel.Text = "Pronto! Arraste sobre as letras para marcar a palavra.";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = "Falhou ao gerar com IA. Usando fallback.";
            await DisplayAlert("Ops", ex.Message, "OK");

            _puzzle = await _generator.CreatePuzzleAsync(CancellationToken.None);

            WordsVm.Clear();
            foreach (var w in _puzzle.Words.OrderBy(x => x.Word))
                WordsVm.Add(new WordItemVm(w.Word, w.Found));

            Board.SetPuzzle(_puzzle.Grid, _puzzle.Words);

            await SpeakTargetAsync();
            NavigateWebTo(_puzzle.CurrentTargetWord);
        }
    }

    // ✅ assinatura correta: object sender, SelectionEventArgs e
    private async void OnBoardSelectionFinished(object? sender, SelectionEventArgs e)
    {
        if (_puzzle == null) return;

        if (_generator.TryMarkSelection(_puzzle, e.Start, e.End, out var matched) && matched != null)
        {
            Board.MarkWordFound(matched);

            var item = WordsVm.FirstOrDefault(x => x.Word.Equals(matched.Word, StringComparison.OrdinalIgnoreCase));
            item?.SetFound(true);

            StatusLabel.Text = $"✅ Acertou: {matched.Word}!";

            NavigateWebTo(matched.Word);

            _generator.PickNextTarget(_puzzle);

            if (string.IsNullOrWhiteSpace(_puzzle.CurrentTargetWord))
            {
                InstructionLabel.Text = "🎉 Parabéns! Você encontrou todos!";
                try { await TextToSpeech.SpeakAsync("Parabéns! Você encontrou todos os animais!"); } catch { }
                return;
            }

            await SpeakTargetAsync();
        }
        else
        {
            StatusLabel.Text = "❌ Não foi dessa vez. Tente outra palavra!";
            try { await TextToSpeech.SpeakAsync("Não foi dessa vez. Tente de novo!"); } catch { }
        }
    }

    private async Task SpeakTargetAsync()
    {
        if (_puzzle == null) return;

        InstructionLabel.Text = $"Toque no animal que começa com: “{_puzzle.CurrentSyllable}”";

        try
        {
            await TextToSpeech.SpeakAsync($"Toque no animal que começa com {SpellForSpeech(_puzzle.CurrentSyllable)}.");
        }
        catch
        {
            // alguns devices/emuladores podem não ter TTS configurado
        }
    }

    private static string SpellForSpeech(string syl)
    {
        if (string.IsNullOrWhiteSpace(syl)) return "";
        if (syl.Length == 2) return $"{syl[0]} {syl[1]}";
        return syl;
    }

    private void NavigateWebTo(string animal)
    {
        if (string.IsNullOrWhiteSpace(animal)) return;

        var q = HttpUtility.UrlEncode(animal.ToLowerInvariant());
        var url = $"https://www.google.com/search?udm=2&q={q}";
        AnimalWeb.Source = new UrlWebViewSource { Url = url };
    }

    // ===== VM simples pro gabarito =====
    public sealed class WordItemVm : BindableObject
    {
        public string Word { get; }

        private bool _found;
        public string FoundIcon => _found ? "✅" : "⬜";

        public WordItemVm(string word, bool found)
        {
            Word = word;
            _found = found;
        }

        public void SetFound(bool found)
        {
            _found = found;
            OnPropertyChanged(nameof(FoundIcon));
        }
    }
}
