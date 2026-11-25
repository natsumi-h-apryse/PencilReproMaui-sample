namespace PencilReproMaui;

/// <summary>Home: environment, the variables named in the report (persisted), entry point.</summary>
public sealed class HomePage : ContentPage
{
    readonly ReproSettings _s = ReproSettings.Load();

    public HomePage()
    {
        Title = "Apryse Pencil Repro (MAUI)";
        var stack = new VerticalStackLayout { Padding = 20, Spacing = 10 };

        stack.Add(Header("ENVIRONMENT"));
        foreach (var l in EnvironmentInfo.Lines) stack.Add(new Label { Text = l, FontSize = 14 });

        stack.Add(Header("SETTINGS — applied when the viewer is opened"));
        stack.Add(Choice("toolManager.pencilInteractionMode", _s.Mode, v => _s.Mode = v));
        stack.Add(Toggle("toolManager.readonly", _s.Readonly, v => _s.Readonly = v));
        stack.Add(Choice("shouldSwitchToTool override", _s.Override, v => _s.Override = v));
        stack.Add(Choice("toolManager.pencilTool", _s.PencilTool, v => _s.PencilTool = v));
        stack.Add(Choice("setMinimumTwoFingersToScrollEnabled", _s.TwoFinger, v => _s.TwoFinger = v));
        stack.Add(new Label
        {
            Text = "Always applied: the 5-line view-only configuration of the report. Info.plist NSBluetoothAlwaysUsageDescription is a build setting (-p:PencilReproBluetoothUsageKey=true|false).",
            FontSize = 12, TextColor = Colors.Gray,
        });

        stack.Add(Header("OPEN"));
        var open = new Button { Text = "Open view-only viewer" };
        open.Clicked += async (_, _) => await OpenAsync();
        stack.Add(open);

        var export = new Button { Text = "Export log" };
        export.Clicked += async (_, _) => await LogExporter.ExportAsync();
        stack.Add(export);

        Content = new ScrollView { Content = stack };
    }

    async Task OpenAsync()
    {
        _s.Save();
        DiagLog.Shared.Log($"Presenting viewer with settings: {_s.Compact}", DiagLog.Category.Ui);
        await Navigation.PushModalAsync(new ViewerPage(), true);
    }

    static Label Header(string text) => new() { Text = text, FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Colors.Gray, Margin = new Thickness(0, 16, 0, 0) };

    View Toggle(string title, bool value, Action<bool> set)
    {
        var sw = new Switch { IsToggled = value };
        sw.Toggled += (_, e) => { set(e.Value); _s.Save(); };
        return Row(title, sw);
    }

    View Choice<T>(string title, T value, Action<T> set) where T : struct, Enum
    {
        var values = Enum.GetValues<T>();
        var picker = new Picker { ItemsSource = values.Select(v => v.ToString()).ToList(), SelectedIndex = Array.IndexOf(values, value), WidthRequest = 220 };
        picker.SelectedIndexChanged += (_, _) => { if (picker.SelectedIndex >= 0) { set(values[picker.SelectedIndex]); _s.Save(); } };
        return Row(title, picker);
    }

    static View Row(string title, View control)
    {
        var grid = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
        grid.Add(new Label { Text = title, VerticalOptions = LayoutOptions.Center, FontSize = 15 }, 0);
        grid.Add(control, 1);
        return grid;
    }
}
