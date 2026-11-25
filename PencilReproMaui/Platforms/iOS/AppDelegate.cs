using Foundation;
using UIKit;

namespace PencilReproMaui;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
    {
        // Same as the official maui-sample: any non-license string => demo mode (watermark).
        pdftron.PDFNet.Initialize("");
        var ok = base.FinishedLaunching(application, launchOptions);
        DiagLog.Shared.Log($"App launched (MAUI). {EnvironmentInfo.SummaryLine}", DiagLog.Category.App);
        return ok;
    }
}
