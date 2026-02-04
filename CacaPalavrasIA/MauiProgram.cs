using CacaPalavrasIA.Services;

namespace CacaPalavrasIA;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>();

        // ✅ Services
        builder.Services.AddSingleton<GroqWordSearchService>();
        builder.Services.AddSingleton<WordSearchGenerator>();

        // ✅ Pages
        builder.Services.AddSingleton<MainPage>();

        return builder.Build();
    }
}
