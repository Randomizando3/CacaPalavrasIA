namespace CacaPalavrasIA;

public partial class App : Application
{
    public App(MainPage mainPage)
    {
        InitializeComponent(); // ✅ tem que ser a PRIMEIRA coisa

        // ✅ não use AppShell agora (pra evitar cadeia de recursos confusa)
        MainPage = new NavigationPage(mainPage)
        {
            BarBackgroundColor = Colors.White,
            BarTextColor = Colors.Black
        };
    }
}
