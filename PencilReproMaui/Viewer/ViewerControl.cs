using Microsoft.Maui.Handlers;
using UIKit;

namespace PencilReproMaui;

/// <summary>
/// MAUI view whose handler embeds the native viewer, following the official
/// ApryseSDK/maui-sample (ViewHandler + native UIView that adds a UINavigationController's view).
/// </summary>
public class ViewerControl : View
{
    public Action? CloseRequested { get; set; }
}

public sealed class ViewerControlHandler : ViewHandler<ViewerControl, ViewerNativeView>
{
    public static readonly IPropertyMapper<ViewerControl, ViewerControlHandler> Mapper =
        new PropertyMapper<ViewerControl, ViewerControlHandler>(ViewMapper);

    public ViewerControlHandler() : base(Mapper) { }

    protected override ViewerNativeView CreatePlatformView() =>
        new(ReproSettings.Load(), () => VirtualView?.CloseRequested?.Invoke());

    protected override void DisconnectHandler(ViewerNativeView platformView)
    {
        platformView.Host.Dispose();
        base.DisconnectHandler(platformView);
    }
}

/// Same structure as maui-sample's DocumentViewerNativeView: the navigation controller's view is
/// added as a subview (the sample does not add it as a child view controller either).
public sealed class ViewerNativeView : UIView
{
    public ViewerHost Host { get; }

    public ViewerNativeView(ReproSettings settings, Action close)
    {
        Host = new ViewerHost(settings, close);
        var navView = Host.Navigation.View!;
        AddSubview(navView);
        navView.Frame = Bounds;
        navView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
    }

    public override void MovedToWindow()
    {
        base.MovedToWindow();
        if (Window != null) Host.StartDiagnostics();
    }
}
