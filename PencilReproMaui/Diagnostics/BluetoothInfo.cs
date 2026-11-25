using CoreBluetooth;
using Foundation;

namespace PencilReproMaui;

/// <summary>
/// Bluetooth conditions that affect the SDK's Apple Pencil pairing detection. The SDK detects a paired Pencil with CoreBluetooth, which only works if
/// Info.plist has NSBluetoothAlwaysUsageDescription and Bluetooth access has been granted.
/// </summary>
public static class BluetoothInfo
{
    public const string UsageDescriptionKey = "NSBluetoothAlwaysUsageDescription";

    /// Whether the built app contains the key (MSBuild property PencilReproBluetoothUsageKey).
    public static bool HasUsageDescription =>
        NSBundle.MainBundle.ObjectForInfoDictionary(UsageDescriptionKey) != null;

    /// CBManager.authorization. Reading it never shows the prompt or creates a CBCentralManager.
    public static string Authorization => CBManager.Authorization switch
    {
        CBManagerAuthorization.NotDetermined => "notDetermined",
        CBManagerAuthorization.Restricted => "restricted",
        CBManagerAuthorization.Denied => "denied",
        CBManagerAuthorization.AllowedAlways => "allowedAlways",
        var other => $"unknown({(long)other})",
    };

    public static string Summary =>
        $"Info.plist {UsageDescriptionKey}={(HasUsageDescription ? "present" : "ABSENT")} CBManager.authorization={Authorization}";
}
