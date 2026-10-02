namespace Featurama.Maui.Models;

public sealed class DeviceInfo
{
    public string? Platform { get; set; }
    public string? OsVersion { get; set; }
    public string? DeviceModel { get; set; }
    public string? DeviceManufacturer { get; set; }
    public string? DeviceType { get; set; }
    public string? AppVersion { get; set; }
    public string? AppBuild { get; set; }
    public string? Locale { get; set; }
    public double? ScreenWidth { get; set; }
    public double? ScreenHeight { get; set; }
    public double? ScreenScale { get; set; }
}
