using CoreGraphics;
using pdftron.PDF;
using pdftron.PDF.Tools;
using pdftron.PDF.Utils;
using UIKit;

namespace PencilReproMaui;

public sealed class StateSnapshot
{
    public List<string> Lines { get; } = new();
    public List<string> OverlayLines { get; } = new();
    public List<string> NonPossible { get; } = new();
    public List<string> ScrollPanNonPossible { get; } = new();
    public string Signature { get; set; } = "";
}

/// <summary>Collects the viewer state (tool manager, gesture recognizers, hit test, windows) for the log and overlay.</summary>
public static class StateDumper
{
    static string Cls(Foundation.NSObject? o) => o == null ? "nil" : GestureInfo.ClassName(o);

    [System.Runtime.InteropServices.DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    static extern IntPtr IntPtr_objc_msgSend(IntPtr receiver, IntPtr selector);

    static IntPtr ObjCDelegate(PTToolManager tm) =>
        IntPtr_objc_msgSend(tm.Handle, ObjCRuntime.Selector.GetHandle("delegate"));

    public static StateSnapshot Snapshot(PTPDFViewCtrl? pdfViewCtrl, PTToolManager? tm)
    {
        var s = new StateSnapshot();
        var sig = new List<string>();

        if (tm != null)
        {
            var tool = Cls(tm.Tool);
            var pencilTool = tm.PencilTool?.Name ?? "nil";
            // Read the delegate through the ObjC runtime (C# wrappers can report nil here).
            var delegateName = Cls(ObjCRuntime.Runtime.GetNSObject(ObjCDelegate(tm)));
            s.Lines.Add($"toolManager.tool={tool} pencilTool={pencilTool} pencilInteractionMode={tm.PencilInteractionMode} readonly={(tm.Readonly ? "YES" : "NO")} annotationsCreatedWithPencilOnly={(tm.AnnotationsCreatedWithPencilOnly ? "YES" : "NO")} delegate={delegateName}");
            if (tm.Tool is { } t)
            {
                s.Lines.Add($"toolManager.tool view: hidden={(t.Hidden ? "YES" : "NO")} userInteractionEnabled={(t.UserInteractionEnabled ? "YES" : "NO")} superview={Cls(t.Superview)}");
                foreach (var gr in t.GestureRecognizers ?? Array.Empty<UIGestureRecognizer>())
                    s.Lines.Add("  tool GR - " + GestureInfo.Describe(gr));
            }
            s.OverlayLines.Add($"tool: {tool}  pencilTool: {pencilTool}");
            s.OverlayLines.Add($"mode: {tm.PencilInteractionMode}  readonly: {(tm.Readonly ? "YES" : "NO")}  pencilOnly: {(tm.AnnotationsCreatedWithPencilOnly ? "YES" : "NO")}");
            s.OverlayLines.Add($"delegate: {delegateName}");
            sig.Add($"tool={tool}|del={delegateName}");
        }
        else
        {
            s.Lines.Add("toolManager: none");
        }

        var sm = PTToolsSettingsManager.SharedManager;
        s.Lines.Add($"PTToolsSettingsManager.sharedManager: applePencilDrawsInk={(sm.ApplePencilDrawsInk ? "YES" : "NO")} pencilInteractionMode={sm.PencilInteractionMode}");
        s.Lines.Add($"OS 'Only Draw with Apple Pencil' (UIPencilInteraction.prefersPencilOnlyDrawing)={(EnvironmentInfo.OsPencilOnlyDrawing ? "ON" : "OFF")}");
        // Bluetooth conditions for the SDK's Apple Pencil pairing detection. Part of the signature
        // so that a permission change is logged as a state change.
        var prompt = tm == null ? "-" : (tm.AllowBluetoothPermissionPrompt ? "YES" : "NO");
        var bt = $"{BluetoothInfo.Summary} toolManager.allowBluetoothPermissionPrompt={prompt}";
        s.Lines.Add($"Bluetooth: {bt}");
        s.OverlayLines.Add($"BT: plistKey={(BluetoothInfo.HasUsageDescription ? "YES" : "NO")} auth={BluetoothInfo.Authorization} prompt={prompt}");
        sig.Add($"bt={bt}");

        if (pdfViewCtrl == null) { s.Lines.Add("pdfViewCtrl: nil"); s.Signature = string.Join("|", sig); return s; }

        var scrollViews = FindPdfScrollViews(pdfViewCtrl);
        if (scrollViews.Count == 0) s.Lines.Add("PTPDFScrollView: NOT FOUND under pdfViewCtrl");
        for (int i = 0; i < scrollViews.Count; i++)
        {
            var sv = scrollViews[i];
            var label = $"{Cls(sv)}[{i}]";
            s.Lines.Add($"{label}: scrollEnabled={(sv.ScrollEnabled ? "YES" : "NO")} userInteractionEnabled={(sv.UserInteractionEnabled ? "YES" : "NO")} hidden={(sv.Hidden ? "YES" : "NO")} panGR.minTouches={sv.PanGestureRecognizer.MinimumNumberOfTouches} zoomScale={sv.ZoomScale:F2} contentOffset={(int)sv.ContentOffset.X},{(int)sv.ContentOffset.Y}");
            var (desc, compact, bad) = DescribeRecognizers(sv, label);
            s.Lines.AddRange(desc);
            s.NonPossible.AddRange(bad);
            s.OverlayLines.Add($"{label}: {compact}");
            sig.Add($"{label}={compact}|{sv.ScrollEnabled}{sv.UserInteractionEnabled}{sv.Hidden}");
            if (sv.PanGestureRecognizer.State != UIGestureRecognizerState.Possible)
                s.ScrollPanNonPossible.Add($"{label}.Pan:{GestureInfo.StateName(sv.PanGestureRecognizer.State)}");
        }

        s.Lines.Add($"PTPDFViewCtrl: userInteractionEnabled={(pdfViewCtrl.UserInteractionEnabled ? "YES" : "NO")} hidden={(pdfViewCtrl.Hidden ? "YES" : "NO")} multipleTouch={(pdfViewCtrl.MultipleTouchEnabled ? "YES" : "NO")}");
        var (d2, c2, b2) = DescribeRecognizers(pdfViewCtrl, "PTPDFViewCtrl");
        s.Lines.AddRange(d2);
        s.NonPossible.AddRange(b2);
        s.OverlayLines.Add($"PTPDFViewCtrl: {c2}");
        sig.Add($"pvc={c2}|{pdfViewCtrl.UserInteractionEnabled}{pdfViewCtrl.Hidden}");

        // MAUI-specific: gesture recognizers on ancestors (MAUI may add some to its container views).
        var ancestors = new List<string>();
        for (var v = pdfViewCtrl.Superview; v != null; v = v.Superview)
        {
            var grs = v.GestureRecognizers;
            if (grs is { Length: > 0 })
                ancestors.Add($"{Cls(v)}: " + string.Join(" ", grs.Select(g => $"{GestureInfo.ShortName(g)}:{GestureInfo.StateName(g.State)}")));
        }
        if (ancestors.Count > 0)
        {
            s.Lines.Add("ancestor views with gesture recognizers:");
            s.Lines.AddRange(ancestors.Select(a => "  - " + a));
        }

        if (pdfViewCtrl.Window is { } window)
        {
            var center = pdfViewCtrl.ConvertPointToView(new CGPoint(pdfViewCtrl.Bounds.GetMidX(), pdfViewCtrl.Bounds.GetMidY()), window);
            var hit = window.HitTest(center, null);
            var windows = window.WindowScene?.Windows ?? Array.Empty<UIWindow>();
            s.Lines.Add($"window.hitTest(page center {center.X:F0},{center.Y:F0}) = {Cls(hit)} (superview {Cls(hit?.Superview)})");
            s.Lines.Add($"windows in scene: {windows.Length} [{string.Join(", ", windows.Select(w => Cls(w) + (w.IsKeyWindow ? "*key" : "")))}]");
            s.OverlayLines.Add($"hitTest(center): {Cls(hit)}  windows: {windows.Length}");
            sig.Add($"hit={Cls(hit)}|w={windows.Length}");
        }

        s.Signature = string.Join("|", sig);
        return s;
    }

    public static List<UIScrollView> FindPdfScrollViews(UIView root)
    {
        var named = new List<UIScrollView>();
        var any = new List<UIScrollView>();
        var queue = new Queue<(UIView, int)>(root.Subviews.Select(v => (v, 1)));
        while (queue.Count > 0)
        {
            var (v, depth) = queue.Dequeue();
            if (v is UIScrollView sv)
            {
                if (Cls(sv).Contains("PDFScrollView")) named.Add(sv); else any.Add(sv);
            }
            if (depth < 4) foreach (var c in v.Subviews) queue.Enqueue((c, depth + 1));
        }
        return named.Count > 0 ? named : any.Take(2).ToList();
    }

    static (List<string>, string, List<string>) DescribeRecognizers(UIView view, string label)
    {
        var grs = view.GestureRecognizers ?? Array.Empty<UIGestureRecognizer>();
        var lines = new List<string> { $"{label} gestureRecognizers ({grs.Length}):" };
        var compact = new List<string>();
        var bad = new List<string>();
        foreach (var gr in grs)
        {
            lines.Add("  - " + GestureInfo.Describe(gr));
            var shortDesc = $"{GestureInfo.ShortName(gr)}:{GestureInfo.StateName(gr.State)}{(gr.Enabled ? "" : "(off)")}";
            compact.Add(shortDesc);
            if (gr.State != UIGestureRecognizerState.Possible) bad.Add($"{label}.{shortDesc}");
        }
        return (lines, string.Join(" ", compact), bad);
    }
}
