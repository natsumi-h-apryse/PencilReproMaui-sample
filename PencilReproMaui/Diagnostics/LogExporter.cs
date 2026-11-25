namespace PencilReproMaui;

/// <summary>Writes the log to Documents (visible in Files/Finder) and opens the share sheet.</summary>
public static class LogExporter
{
    public static async Task ExportAsync()
    {
        var name = $"PencilReproMaui-log-{DateTime.Now:yyyyMMdd-HHmmss}.txt";
        var text = "PencilRepro MAUI session log\n"
                 + string.Join("\n", EnvironmentInfo.Lines) + "\n"
                 + "Current settings:\n" + string.Join("\n", ReproSettings.Load().DescriptionLines.Select(l => "  " + l)) + "\n"
                 + new string('-', 80) + "\n"
                 + DiagLog.Shared.Snapshot() + "\n";
        var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var path = Path.Combine(docs, name);
        await File.WriteAllTextAsync(path, text);
        DiagLog.Shared.Log($"Exported log: {name}", DiagLog.Category.Ui);
        await Share.Default.RequestAsync(new ShareFileRequest { Title = name, File = new ShareFile(path) });
    }
}
