using System.Runtime.InteropServices;
using UIKit;

namespace PencilReproMaui;

public static class EnvironmentInfo
{
    [DllImport("/usr/lib/libc.dylib")]
    static extern int sysctlbyname(string name, byte[]? oldp, ref nint oldlenp, IntPtr newp, nint newlen);

    public static string MachineIdentifier
    {
        get
        {
            nint len = 0;
            sysctlbyname("hw.machine", null, ref len, IntPtr.Zero, 0);
            var buf = new byte[len];
            sysctlbyname("hw.machine", buf, ref len, IntPtr.Zero, 0);
            return System.Text.Encoding.UTF8.GetString(buf).TrimEnd('\0');
        }
    }

    public static string OsVersion => $"{UIDevice.CurrentDevice.SystemName} {UIDevice.CurrentDevice.SystemVersion}";

    /// <summary>pdftron.PDFNet.GetVersion() (the binding exposes the numeric version).</summary>
    public static string SdkVersion => pdftron.PDFNet.GetVersion().ToString("0.0###", System.Globalization.CultureInfo.InvariantCulture);

    public static string PackageVersion =>
        typeof(pdftron.PDF.Tools.PTToolManager).Assembly.GetName().Version?.ToString() ?? "?";

    public static string DotnetVersion => RuntimeInformation.FrameworkDescription;

    public static bool OsPencilOnlyDrawing => UIPencilInteraction.PrefersPencilOnlyDrawing;

    public static string[] Lines => new[]
    {
        $"Device: {UIDevice.CurrentDevice.Model} ({MachineIdentifier})",
        $"OS: {OsVersion}",
        $"Apryse SDK (runtime, PDFNet.GetVersion): {SdkVersion}",
        "NuGet: PDFTron.iOS / PDFTron.iOS.Tools 12.1.0",
        $"Stack: .NET MAUI, {DotnetVersion}",
        $"OS 'Only Draw with Apple Pencil': {(OsPencilOnlyDrawing ? "ON" : "OFF")}",
        $"Bluetooth: {BluetoothInfo.Summary}",
    };

    public static string SummaryLine => string.Join(" | ", Lines);
}
