namespace PencilReproMaui;

/// <summary>MAUI page containing the ViewerControl, pushed modally (full screen).</summary>
public sealed class ViewerPage : ContentPage
{
    public ViewerPage()
    {
        Microsoft.Maui.Controls.NavigationPage.SetHasNavigationBar(this, false);
        var control = new ViewerControl();
        control.CloseRequested = async () => await Navigation.PopModalAsync();
        Content = control;
    }
}
