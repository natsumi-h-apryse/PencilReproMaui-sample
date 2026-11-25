using Foundation;
using UIKit;

namespace PencilReproMaui;

/// <summary>Touch-transparent overlay (UserInteractionEnabled = false), top-right corner.</summary>
public sealed class DiagnosticsOverlayView : UIView
{
    readonly UILabel _label = new();

    public DiagnosticsOverlayView()
    {
        UserInteractionEnabled = false;
        _label.UserInteractionEnabled = false;
        BackgroundColor = UIColor.Black.ColorWithAlpha(0.55f);
        Layer.CornerRadius = 8;
        Layer.MasksToBounds = true;
        _label.Lines = 0;
        _label.Font = UIFont.GetMonospacedSystemFont(10.5f, UIFontWeight.Regular);
        _label.TextColor = UIColor.White;
        _label.LineBreakMode = UILineBreakMode.CharacterWrap;
        _label.TranslatesAutoresizingMaskIntoConstraints = false;
        AddSubview(_label);
        NSLayoutConstraint.ActivateConstraints(new[]
        {
            _label.LeadingAnchor.ConstraintEqualTo(LeadingAnchor, 8),
            _label.TrailingAnchor.ConstraintEqualTo(TrailingAnchor, -8),
            _label.TopAnchor.ConstraintEqualTo(TopAnchor, 6),
            _label.BottomAnchor.ConstraintEqualTo(BottomAnchor, -6),
        });
    }

    public void Install(UIView host)
    {
        TranslatesAutoresizingMaskIntoConstraints = false;
        host.AddSubview(this);
        NSLayoutConstraint.ActivateConstraints(new[]
        {
            TrailingAnchor.ConstraintEqualTo(host.SafeAreaLayoutGuide.TrailingAnchor, -8),
            TopAnchor.ConstraintEqualTo(host.SafeAreaLayoutGuide.TopAnchor, 8),
            WidthAnchor.ConstraintLessThanOrEqualTo(host.WidthAnchor, 0.36f),
            WidthAnchor.ConstraintLessThanOrEqualTo(420),
            HeightAnchor.ConstraintLessThanOrEqualTo(host.HeightAnchor, 0.4f),
        });
    }

    public void Update(IEnumerable<string> lines, string? stuck)
    {
        var text = new NSMutableAttributedString();
        var bold = UIFont.GetMonospacedSystemFont(11, UIFontWeight.Bold);
        if (stuck != null)
        {
            text.Append(new NSAttributedString("⚠️ " + stuck + "\n", new UIStringAttributes { ForegroundColor = UIColor.White, Font = bold }));
            BackgroundColor = UIColor.SystemRed.ColorWithAlpha(0.75f);
        }
        else
        {
            text.Append(new NSAttributedString("OK: no stuck recognizers\n", new UIStringAttributes { ForegroundColor = UIColor.SystemGreen, Font = bold }));
            BackgroundColor = UIColor.Black.ColorWithAlpha(0.55f);
        }
        text.Append(new NSAttributedString(string.Join("\n", lines)));
        _label.AttributedText = text;
    }
}
