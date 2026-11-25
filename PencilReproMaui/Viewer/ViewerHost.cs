using Foundation;
using UIKit;

namespace PencilReproMaui;

/// <summary>
/// Builds the viewer stack: UINavigationController(ReproDocumentController) + diagnostics
/// session + diagnostics buttons in the navigation bar.
/// </summary>
public sealed class ViewerHost : IDisposable
{
    public UINavigationController Navigation { get; }
    public ReproDocumentController Controller { get; }
    DiagnosticsSession? _diag;
    readonly Action _close;

    public ViewerHost(ReproSettings settings, Action close)
    {
        _close = close;
        Controller = new ReproDocumentController(settings);
        Navigation = new UINavigationController(Controller);

        Controller.OpenDocumentWithURL(NSUrl.FromFilename(SampleDocument.CopyFresh()));
        Controller.Title = "View-only viewer";

        Controller.NavigationItem.LeftItemsSupplementBackButton = true;
        Controller.NavigationItem.LeftBarButtonItems = new[]
        {
            new UIBarButtonItem("Close", UIBarButtonItemStyle.Done, (_, _) => Close()),
            new UIBarButtonItem("Dump state", UIBarButtonItemStyle.Plain, (_, _) => _diag?.Dump("Dump state button")),
            new UIBarButtonItem("Overlay", UIBarButtonItemStyle.Plain, (_, _) => { if (_diag != null) _diag.OverlayVisible = !_diag.OverlayVisible; }),
            new UIBarButtonItem("Export log", UIBarButtonItemStyle.Plain, async (_, _) => await LogExporter.ExportAsync()),
        };
    }

    /// Call once the navigation controller's view is in a window.
    public void StartDiagnostics()
    {
        if (_diag != null) return;
        TouchMonitor.AttachTo(Navigation.View?.Window);
        _diag = new DiagnosticsSession("View-only viewer",
                                       Controller.View!, Controller.PdfViewCtrl, Controller.ToolManager);
        _diag.Start(showOverlay: true);
        DiagLog.Shared.Log($"Viewer hierarchy: {Ancestry(Controller.PdfViewCtrl)}", DiagLog.Category.App);
    }

    static string Ancestry(UIView v)
    {
        var names = new List<string>();
        for (UIView? x = v; x != null; x = x.Superview) names.Add(GestureInfo.ClassName(x));
        return string.Join(" < ", names);
    }

    void Close()
    {
        DiagLog.Shared.Log("Close tapped (view-only viewer)", DiagLog.Category.Ui);
        Dispose();
        _close();
    }

    public void Dispose()
    {
        _diag?.Dispose();
        _diag = null;
    }
}

public static class SampleDocument
{
    /// Bundled sample (12 pages, printed page numbers, no annotations). Generated with the Apryse
    /// API (PTElementBuilder); contains no customer data.
    public const string FileName = "PencilRepro-sample.pdf";

    /// Copies the bundled sample to Documents every time a viewer opens, so that annotations
    /// drawn with the Pencil in a previous run never carry over. Returns the path.
    public static string CopyFresh()
    {
        var dest = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), FileName);
        using (var src = FileSystem.OpenAppPackageFileAsync(FileName).GetAwaiter().GetResult())
        using (var dst = File.Create(dest))
            src.CopyTo(dst);
        DiagLog.Shared.Log($"Opening a fresh copy of {FileName}", DiagLog.Category.App);
        return dest;
    }
}
