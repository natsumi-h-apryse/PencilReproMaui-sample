using Foundation;
using pdftron.PDF;
using pdftron.PDF.Tools;
using UIKit;

namespace PencilReproMaui;

/// <summary>Per-viewer diagnostics: 1 s polling, tool-change logging, stuck detection, overlay.</summary>
public sealed class DiagnosticsSession : IDisposable
{
    readonly string _label;
    // Strong references on purpose: weak references to the C# wrappers let the GC collect the
    // wrappers (the native objects stay alive), which made every dump read "nil". The session is
    // disposed when the viewer closes, so this does not extend the viewer's lifetime.
    readonly UIView _host;
    readonly PTPDFViewCtrl _pdfViewCtrl;
    readonly PTToolManager? _toolManager;
    readonly DiagnosticsOverlayView _overlay = new();
    readonly List<NSObject> _observers = new();
    NSTimer? _timer;
    int _tick;
    string _lastSignature = "";
    StateSnapshot _last = new();
    double? _stuckSince;
    int _fingerAtStuck;
    string _stuckDetail = "";

    public DiagnosticsSession(string label, UIView host, PTPDFViewCtrl pdfViewCtrl, PTToolManager? toolManager)
    {
        _label = label;
        _host = host;
        _pdfViewCtrl = pdfViewCtrl;
        _toolManager = toolManager;
    }

    PTPDFViewCtrl? Pvc => _pdfViewCtrl;
    PTToolManager? Tm => _toolManager;

    public bool OverlayVisible { get => !_overlay.Hidden; set => _overlay.Hidden = !value; }

    public void Start(bool showOverlay)
    {
        if (_timer != null) return;
        _overlay.Install(_host);
        _overlay.Hidden = !showOverlay;
        DiagLog.Shared.Log($"=== Diagnostics session started: {_label} ===", DiagLog.Category.State);
        DiagLog.Shared.LogBlock("Settings:", ReproSettings.Load().DescriptionLines, DiagLog.Category.State);

        // REPRO: tool change notifications (independent of the tool manager delegate).
        var nc = NSNotificationCenter.DefaultCenter;
        _observers.Add(nc.AddObserver(new NSString(pdftron.PDF.Tools.Constants.PTToolManagerToolWillChangeNotification), n =>
        {
            if (n.Object is not PTToolManager tm || Tm == null || tm.Handle != Tm.Handle) return;
            var next = n.UserInfo?[pdftron.PDF.Tools.Constants.PTToolManagerToolUserInfoKey] as NSObject;
            DiagLog.Shared.Log($"ToolWillChange: {Name(tm.Tool)} -> {Name(next)}", DiagLog.Category.Tool);
        }));
        _observers.Add(nc.AddObserver(new NSString(pdftron.PDF.Tools.Constants.PTToolManagerToolDidChangeNotification), n =>
        {
            if (n.Object is not PTToolManager tm || Tm == null || tm.Handle != Tm.Handle) return;
            var prev = n.UserInfo?[pdftron.PDF.Tools.Constants.PTToolManagerPreviousToolUserInfoKey] as NSObject;
            DiagLog.Shared.Log($"ToolDidChange: {Name(prev)} -> {Name(tm.Tool)}", DiagLog.Category.Tool);
            Refresh("tool changed");
        }));
        TouchMonitor.AllTouchesEnded += OnAllTouchesEnded;
        TouchMonitor.FingerTouchBegan += OnFingerBegan;

        _timer = NSTimer.CreateRepeatingTimer(1.0, _ => Refresh("tick"));
        NSRunLoop.Main.AddTimer(_timer, NSRunLoopMode.Common);
        Dump("session start");
    }

    static string Name(NSObject? o) => o == null ? "nil" : GestureInfo.ClassName(o);

    void OnAllTouchesEnded() =>
        NSTimer.CreateScheduledTimer(0.3, _ => Refresh("touches ended"));

    void OnFingerBegan(int id)
    {
        if (_stuckSince == null) return;
        DiagLog.Shared.Log($"finger touch#{id} arrived at window while STUCK (pan: {string.Join(", ", _last.ScrollPanNonPossible)})", DiagLog.Category.Stuck);
    }

    public void Dump(string reason)
    {
        var snap = StateDumper.Snapshot(Pvc, Tm);
        DiagLog.Shared.LogBlock($"STATE DUMP ({reason}) [{_label}] touches active={TouchMonitor.ActiveCount} finger={TouchMonitor.FingerCount} pencil={TouchMonitor.PencilCount}", snap.Lines, DiagLog.Category.State);
        _lastSignature = snap.Signature;
        Apply(snap);
    }

    void Refresh(string reason)
    {
        if (_timer == null) return;
        var snap = StateDumper.Snapshot(Pvc, Tm);
        if (reason == "tick") _tick++;
        if (snap.Signature != _lastSignature)
        {
            DiagLog.Shared.LogBlock($"STATE CHANGED ({reason}) [{_label}]", snap.Lines, DiagLog.Category.State);
            _lastSignature = snap.Signature;
        }
        else if (reason == "tick")
        {
            DiagLog.Shared.Log($"tick #{_tick}: state unchanged; active touches={TouchMonitor.ActiveCount}", DiagLog.Category.State);
        }
        Apply(snap);
    }

    void Apply(StateSnapshot snap)
    {
        _last = snap;
        EvaluateStuck(snap);
        var lines = new List<string> { $"[{_label}]" };
        lines.AddRange(snap.OverlayLines);
        lines.Add($"touches: finger={TouchMonitor.FingerCount} pencil={TouchMonitor.PencilCount} active={TouchMonitor.ActiveCount} {TouchMonitor.ActiveSummary}");
        _overlay.Update(lines, StuckMessage());
        _overlay.Superview?.BringSubviewToFront(_overlay);
    }

    /// Stuck rule: no touch active for >= 0.25 s, yet a recognizer on
    /// PTPDFScrollView / PTPDFViewCtrl is not Possible.
    void EvaluateStuck(StateSnapshot snap)
    {
        var idle = DiagLog.Shared.Elapsed - TouchMonitor.LastAllEndedAt;
        if (TouchMonitor.ActiveCount != 0 || idle < 0.25) return;
        if (snap.NonPossible.Count > 0)
        {
            var detail = string.Join(", ", snap.NonPossible);
            if (_stuckSince == null)
            {
                _stuckSince = DiagLog.Shared.Elapsed;
                _fingerAtStuck = TouchMonitor.FingerCount;
                DiagLog.Shared.LogBlock($"STUCK DETECTED: recognizers not reset to Possible while no touches are active: {detail}", snap.Lines, DiagLog.Category.Stuck);
            }
            else if (detail != _stuckDetail)
            {
                DiagLog.Shared.Log($"STUCK detail changed: {detail}", DiagLog.Category.Stuck);
            }
            _stuckDetail = detail;
        }
        else if (_stuckSince is double since)
        {
            DiagLog.Shared.Log($"RECOVERED after {DiagLog.Shared.Elapsed - since:F1}s: all recognizers back to Possible", DiagLog.Category.Stuck);
            _stuckSince = null;
            _stuckDetail = "";
        }
    }

    string? StuckMessage()
    {
        if (_stuckSince is not double since) return null;
        var pan = _last.ScrollPanNonPossible.Count == 0 ? "" : "\nscroll pan: " + string.Join(", ", _last.ScrollPanNonPossible);
        return $"STUCK for {DiagLog.Shared.Elapsed - since:F0}s; finger touches since: {TouchMonitor.FingerCount - _fingerAtStuck}{pan}\nnot Possible: {_stuckDetail}";
    }

    public void Dispose()
    {
        if (_timer == null) return;
        _timer.Invalidate();
        _timer = null;
        foreach (var o in _observers) NSNotificationCenter.DefaultCenter.RemoveObserver(o);
        _observers.Clear();
        TouchMonitor.AllTouchesEnded -= OnAllTouchesEnded;
        TouchMonitor.FingerTouchBegan -= OnFingerBegan;
        _overlay.RemoveFromSuperview();
        DiagLog.Shared.Log($"=== Diagnostics session ended: {_label} ===", DiagLog.Category.State);
    }
}
