namespace PencilReproMaui;

/// <summary>
/// The variables named in the report, persisted with MAUI Preferences. Everything else is fixed
/// (see README "固定している条件").
/// </summary>
public sealed class ReproSettings
{
    public enum PencilMode { PencilOnly, System, FingerAndPencil }
    /// Report: "System + -toolManager:shouldSwitchToTool: returns NO".
    public enum SwitchOverride { Off, AlwaysNo }
    /// Report: "toolManager.pencilTool stays at the default PTFreeHandCreate"
    /// (the SDK 12.1.0 default is PTPencilDrawingCreate, so both are selectable).
    public enum PencilToolChoice { SdkDefault, FreeHand }
    /// Report, question 5.
    public enum TwoFingerScroll { NotCalled, CallNo, CallYes }

    public PencilMode Mode { get; set; } = PencilMode.PencilOnly;
    public bool Readonly { get; set; }
    public SwitchOverride Override { get; set; } = SwitchOverride.Off;
    public PencilToolChoice PencilTool { get; set; } = PencilToolChoice.SdkDefault;
    public TwoFingerScroll TwoFinger { get; set; } = TwoFingerScroll.NotCalled;

    public static ReproSettings Load()
    {
        var p = Preferences.Default;
        return new ReproSettings
        {
            Mode = (PencilMode)p.Get("mode", (int)PencilMode.PencilOnly),
            Readonly = p.Get("readonly", false),
            Override = (SwitchOverride)Math.Min(p.Get("override", (int)SwitchOverride.Off), (int)SwitchOverride.AlwaysNo),
            PencilTool = (PencilToolChoice)p.Get("pencilTool", (int)PencilToolChoice.SdkDefault),
            TwoFinger = (TwoFingerScroll)p.Get("twoFinger", (int)TwoFingerScroll.NotCalled),
        };
    }

    public void Save()
    {
        var p = Preferences.Default;
        p.Set("mode", (int)Mode);
        p.Set("readonly", Readonly);
        p.Set("override", (int)Override);
        p.Set("pencilTool", (int)PencilTool);
        p.Set("twoFinger", (int)TwoFinger);
    }

    public string[] DescriptionLines => new[]
    {
        $"toolManager.pencilInteractionMode = {Mode}",
        $"toolManager.readonly = {(Readonly ? "YES" : "NO")}",
        $"shouldSwitchToTool override = {Override}",
        $"toolManager.pencilTool = {PencilTool}",
        $"setMinimumTwoFingersToScrollEnabled = {TwoFinger}",
    };

    public string Compact =>
        $"mode={Mode} readonly={(Readonly ? "YES" : "NO")} override={Override} pencilTool={PencilTool} twoFinger={TwoFinger}";
}
