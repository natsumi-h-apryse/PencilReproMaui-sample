namespace PencilReproMaui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureMauiHandlers(h =>
            {
                h.AddHandler(typeof(ViewerControl), typeof(ViewerControlHandler));
            });
        return builder.Build();
    }
}

public sealed class App : Application
{
    protected override Window CreateWindow(IActivationState? activationState) =>
        new(new NavigationPage(new HomePage()));
}
