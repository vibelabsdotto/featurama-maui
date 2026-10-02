using RequestDeviceInfo = Featurama.Maui.Models.DeviceInfo;

namespace Featurama.Maui.UI.Utils;

internal static class DeviceMetadataProvider
{
    public static RequestDeviceInfo GetDeviceInfo(RequestDeviceInfo? explicitInfo = null)
    {
        // A caller's complete override wins, even when every field is null. Copy it so
        // later caller mutations cannot change metadata belonging to an open page.
        if (explicitInfo != null)
        {
            return new RequestDeviceInfo
            {
                Platform = explicitInfo.Platform,
                OsVersion = explicitInfo.OsVersion,
                DeviceModel = explicitInfo.DeviceModel,
                DeviceManufacturer = explicitInfo.DeviceManufacturer,
                DeviceType = explicitInfo.DeviceType,
                AppVersion = explicitInfo.AppVersion,
                AppBuild = explicitInfo.AppBuild,
                Locale = explicitInfo.Locale,
                ScreenWidth = explicitInfo.ScreenWidth,
                ScreenHeight = explicitInfo.ScreenHeight,
                ScreenScale = explicitInfo.ScreenScale,
            };
        }

        // Do not read device name, identifiers, locale, display, location or permissions.
        // Unsupported getters must not prevent someone from submitting feedback.
        return new RequestDeviceInfo
        {
            Platform = TryRead(() => Microsoft.Maui.Devices.DeviceInfo.Current.Platform.ToString()),
            OsVersion = TryRead(() => Microsoft.Maui.Devices.DeviceInfo.Current.VersionString),
            DeviceModel = TryRead(() => Microsoft.Maui.Devices.DeviceInfo.Current.Model),
            DeviceManufacturer = TryRead(() => Microsoft.Maui.Devices.DeviceInfo.Current.Manufacturer),
            DeviceType = TryRead(() => Microsoft.Maui.Devices.DeviceInfo.Current.DeviceType.ToString()),
            AppVersion = TryRead(() => Microsoft.Maui.ApplicationModel.AppInfo.Current.VersionString),
            AppBuild = TryRead(() => Microsoft.Maui.ApplicationModel.AppInfo.Current.BuildString),
        };
    }

    private static string? TryRead(Func<string> read)
    {
        try
        {
            var value = read();
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
        catch (Exception)
        {
            // Metadata is optional, including on platforms without an Essentials implementation.
            return null;
        }
    }
}
