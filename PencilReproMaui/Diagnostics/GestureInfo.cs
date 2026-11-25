using UIKit;

namespace PencilReproMaui;

public static class GestureInfo
{
    public static string StateName(UIGestureRecognizerState s) => s switch
    {
        UIGestureRecognizerState.Possible => "Possible",
        UIGestureRecognizerState.Began => "Began",
        UIGestureRecognizerState.Changed => "Changed",
        UIGestureRecognizerState.Ended => "Ended",
        UIGestureRecognizerState.Cancelled => "Cancelled",
        UIGestureRecognizerState.Failed => "Failed",
        _ => $"State{(long)s}",
    };

    [System.Runtime.InteropServices.DllImport("/usr/lib/libobjc.dylib")]
    static extern IntPtr object_getClass(IntPtr obj);
    [System.Runtime.InteropServices.DllImport("/usr/lib/libobjc.dylib")]
    static extern IntPtr class_getName(IntPtr cls);

    /// <summary>
    /// Runtime ObjC class of the instance (e.g. PTPDFScrollView, _PTToolManagerDelegate).
    /// NSObject.ClassHandle / GetType() would give the C# wrapper's static type instead.
    /// </summary>
    public static string ClassName(Foundation.NSObject o)
    {
        if (o.Handle == IntPtr.Zero) return "nil";
        var name = System.Runtime.InteropServices.Marshal.PtrToStringUTF8(class_getName(object_getClass(o.Handle)));
        return string.IsNullOrEmpty(name) ? o.GetType().Name : name;
    }

    public static string ShortName(UIGestureRecognizer gr)
    {
        if (gr.Name == "TouchObserver") return "TouchObserver";
        var n = ClassName(gr);
        if (n.StartsWith("UI")) n = n[2..];
        if (n.EndsWith("GestureRecognizer")) n = n[..^"GestureRecognizer".Length];
        return n.Length == 0 ? "GestureRecognizer" : n;
    }

    public static string Describe(UIGestureRecognizer gr)
    {
        var parts = new List<string> { ClassName(gr), $"state={StateName(gr.State)}", $"enabled={(gr.Enabled ? 1 : 0)}" };
        if (!string.IsNullOrEmpty(gr.Name)) parts.Add($"name={gr.Name}");
        parts.Add($"delegate={(gr.WeakDelegate is Foundation.NSObject d ? ClassName(d) : "nil")}");
        if (gr.NumberOfTouches > 0) parts.Add($"touches={gr.NumberOfTouches}");
        return string.Join(" ", parts);
    }
}
