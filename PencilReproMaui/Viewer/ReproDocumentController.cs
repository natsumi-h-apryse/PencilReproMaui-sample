using Foundation;
using ObjCRuntime;
using pdftron.PDF.Controls;
using pdftron.PDF.Tools;
using UIKit;

namespace PencilReproMaui;

/// <summary>
/// PTDocumentController subclass configured as the reported view-only viewer.
/// Every setting taken from the report is marked REPRO.
/// </summary>
public class ReproDocumentController : PTDocumentController
{
    readonly ReproSettings _s;

    public ReproDocumentController(ReproSettings settings)
    {
        _s = settings;
        Apply();
    }

    void Apply()
    {
        DiagLog.Shared.Log($"Configuring view-only viewer: {_s.Compact}", DiagLog.Category.App);

        var tm = ToolManager;
        // REPRO: toolManager.pencilInteractionMode (PencilOnly / System / FingerAndPencil)
        tm.PencilInteractionMode = _s.Mode switch
        {
            ReproSettings.PencilMode.PencilOnly => pdftron.PDF.PTPencilInteractionMode.PencilOnly,
            ReproSettings.PencilMode.System => pdftron.PDF.PTPencilInteractionMode.System,
            _ => pdftron.PDF.PTPencilInteractionMode.FingerAndPencil,
        };
        // REPRO: toolManager.readonly (YES / NO)
        tm.Readonly = _s.Readonly;
        // REPRO: toolManager.pencilTool (report: PTFreeHandCreate)
        if (_s.PencilTool == ReproSettings.PencilToolChoice.FreeHand)
            tm.PencilTool = new Class(typeof(PTFreeHandCreate));

        // REPRO: question 5, -[PTPDFViewCtrl setMinimumTwoFingersToScrollEnabled:]
        if (_s.TwoFinger != ReproSettings.TwoFingerScroll.NotCalled)
            PdfViewCtrl.SetMinimumTwoFingersToScrollEnabled(_s.TwoFinger == ReproSettings.TwoFingerScroll.CallYes);

        // REPRO: the view-only configuration from the report (5 lines). toolManager.tool is
        // never changed by the app: it stays at the SDK default, PTPanTool.
        ToolGroupsEnabled = false;
        SetToolGroupToolbarHidden(true, false);
        HidesNavigationBar = false;
        HidesBottomBar = false;
        SetControlsHidden(true, false);

        // REPRO: -toolManager:shouldSwitchToTool: from C#. With the MAUI binding, subscribing to a
        // PTToolManager event or setting a callback property REPLACES toolManager.delegate
        // (normally the PTDocumentController) with an internal delegate object of the binding.
        tm.ToolManagerToolChanged += (_, _) =>
            DiagLog.Shared.Log($"[C# event] ToolManagerToolChanged: tool={Name(tm.Tool)}", DiagLog.Category.Tool);
        tm.ShouldSwitchToTool = (manager, tool) =>
        {
            var result = _s.Override != ReproSettings.SwitchOverride.AlwaysNo;
            DiagLog.Shared.Log($"shouldSwitchToTool: {Name(manager.Tool)} -> {Name(tool)} = {(result ? "YES" : "NO")} (override={_s.Override})", DiagLog.Category.Tool);
            return result;
        };

        DiagLog.Shared.Log($"toolManager.delegate = {(Runtime.GetNSObject(DelegateHandle(tm)) is NSObject d ? GestureInfo.ClassName(d) : "nil")}; {BluetoothInfo.Summary} allowBluetoothPermissionPrompt={(tm.AllowBluetoothPermissionPrompt ? "YES" : "NO")}", DiagLog.Category.App);
    }

    static string Name(NSObject? o) => o == null ? "nil" : GestureInfo.ClassName(o);

    [System.Runtime.InteropServices.DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    static extern IntPtr IntPtr_objc_msgSend(IntPtr receiver, IntPtr selector);

    /// Reads the delegate through the ObjC runtime (the C# wrapper can report nil).
    static IntPtr DelegateHandle(PTToolManager tm) =>
        IntPtr_objc_msgSend(tm.Handle, Selector.GetHandle("delegate"));

    protected override void Dispose(bool disposing)
    {
        DiagLog.Shared.Log("ReproDocumentController disposed", DiagLog.Category.App);
        base.Dispose(disposing);
    }
}
