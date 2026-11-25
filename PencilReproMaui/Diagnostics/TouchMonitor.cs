using Foundation;
using UIKit;

namespace PencilReproMaui;

/// <summary>
/// Logs every touch reaching the window (finger vs Pencil, phase, receiving gesture recognizers).
///
/// MAUI creates its own UIWindow, so instead of overriding sendEvent (native app) a passive
/// observer gesture recognizer is attached to the window. It never recognizes, never cancels or
/// delays touches and recognizes simultaneously with everything, so it does not change delivery.
/// It shows up as "UIWindow.TouchObserver" in recognizer lists.
/// </summary>
public static class TouchMonitor
{
    public static event Action? AllTouchesEnded;
    public static event Action<int>? FingerTouchBegan;

    static readonly Dictionary<IntPtr, (int Id, string Kind, int Moves)> Active = new();
    static int _nextId = 1;
    static readonly HashSet<IntPtr> AttachedWindows = new();

    public static int FingerCount { get; private set; }
    public static int PencilCount { get; private set; }
    public static int ActiveCount => Active.Count;
    public static double LastAllEndedAt { get; private set; }
    public static string ActiveSummary => string.Join(", ", Active.Values.OrderBy(v => v.Id).Select(v => $"#{v.Id} {v.Kind}"));

    public static void AttachTo(UIWindow? window)
    {
        if (window == null || !AttachedWindows.Add(window.Handle)) return;
        window.AddGestureRecognizer(new ObserverRecognizer());
        DiagLog.Shared.Log($"TouchMonitor attached to {window.GetType().Name}", DiagLog.Category.App);
    }

    public static string Kind(UITouchType t) => t switch
    {
        UITouchType.Direct => "finger",
        UITouchType.Stylus => "pencil",
        UITouchType.Indirect => "indirect",
        UITouchType.IndirectPointer => "pointer",
        _ => $"type{(long)t}",
    };

    public static string Recognizers(UITouch touch, bool atEnd = false)
    {
        var grs = touch.GestureRecognizers ?? Array.Empty<UIGestureRecognizer>();
        if (grs.Length == 0)
            return atEnd ? "[] (cleared by UIKit at touch end; normal)" : "[] (no recognizer received this touch)";
        return "[" + string.Join(", ", grs.Select(g =>
            $"{(g.View?.GetType().Name ?? "nil")}.{GestureInfo.ShortName(g)}:{GestureInfo.StateName(g.State)}")) + "]";
    }

    static void Record(NSSet touches, UIView? window)
    {
        foreach (var obj in touches)
        {
            if (obj is not UITouch touch) continue;
            var key = touch.Handle.Handle;
            var kind = Kind(touch.Type);
            switch (touch.Phase)
            {
                case UITouchPhase.Began:
                {
                    var id = _nextId++;
                    Active[key] = (id, kind, 0);
                    if (touch.Type == UITouchType.Stylus) PencilCount++;
                    else if (touch.Type == UITouchType.Direct) FingerCount++;
                    var p = touch.LocationInView(window);
                    DiagLog.Shared.Log($"touch#{id} {kind} began at ({p.X:F0},{p.Y:F0}) view={(touch.View?.GetType().Name ?? "nil")} recognizers={Recognizers(touch)}", DiagLog.Category.Touch);
                    if (touch.Type == UITouchType.Direct) FingerTouchBegan?.Invoke(id);
                    break;
                }
                case UITouchPhase.Moved:
                    if (Active.TryGetValue(key, out var info))
                    {
                        info.Moves++;
                        Active[key] = info;
                        if (info.Moves == 1)
                            DiagLog.Shared.Log($"touch#{info.Id} {kind} first move recognizers={Recognizers(touch)}", DiagLog.Category.Touch);
                    }
                    break;
                case UITouchPhase.Ended:
                case UITouchPhase.Cancelled:
                {
                    Active.Remove(key, out var ended);
                    var phase = touch.Phase == UITouchPhase.Ended ? "ended" : "cancelled";
                    DiagLog.Shared.Log($"touch#{(ended.Id == 0 ? "?" : ended.Id.ToString())} {kind} {phase} moves={ended.Moves} recognizers={Recognizers(touch, atEnd: true)}", DiagLog.Category.Touch);
                    if (Active.Count == 0)
                    {
                        LastAllEndedAt = DiagLog.Shared.Elapsed;
                        AllTouchesEnded?.Invoke();
                    }
                    break;
                }
            }
        }
    }

    sealed class ObserverRecognizer : UIGestureRecognizer
    {
        public ObserverRecognizer()
        {
            Name = "TouchObserver";
            CancelsTouchesInView = false;
            DelaysTouchesBegan = false;
            DelaysTouchesEnded = false;
            ShouldRecognizeSimultaneously = (_, _) => true;
        }

        public override void TouchesBegan(NSSet touches, UIEvent evt) { base.TouchesBegan(touches, evt); Record(touches, View); }
        public override void TouchesMoved(NSSet touches, UIEvent evt) { base.TouchesMoved(touches, evt); Record(touches, View); }
        public override void TouchesEnded(NSSet touches, UIEvent evt) { base.TouchesEnded(touches, evt); Record(touches, View); FailIfIdle(); }
        public override void TouchesCancelled(NSSet touches, UIEvent evt) { base.TouchesCancelled(touches, evt); Record(touches, View); FailIfIdle(); }

        // Never recognize; once the sequence is over, fail so UIKit resets us to Possible.
        void FailIfIdle() { if (Active.Count == 0) State = UIGestureRecognizerState.Failed; }
    }
}
