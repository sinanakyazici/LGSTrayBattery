namespace LGSTrayPrimitives;

public class AppSettings
{
    public UISettings UI { get; set; } = null!;

    public HttpServerSettings HTTPServer { get; set; } = null!;

    public IDeviceManagerSettings GHub { get; set; } = null!;

    public NativeDeviceManagerSettings Native { get; set; } = null!;

    public NumericDisplaySettings NumericDisplay { get; set; } = new();

    // Keys are partial device name patterns (case-insensitive), e.g. "G915" matches "G915 X Lightspeed..."
    public Dictionary<string, NumericDisplayDeviceOverride> DeviceColor { get; set; } = [];

    public NumericDisplayDeviceOverride? FindDeviceOverride(string deviceName) =>
        DeviceColor.FirstOrDefault(kvp =>
            deviceName.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase)).Value;
}

public class NumericDisplayDeviceOverride
{
    // Empty = falls back to global NumericDisplay setting
    public string TextColor { get; set; } = "";
    public string BackgroundColor { get; set; } = "";
}

public class UISettings
{
    public bool EnableRichToolTips { get; set; }
}

public class NumericDisplaySettings
{
    public float FontSizeMultiplier { get; set; } = 0.8f;
    // Empty string = use theme color; accepts "#RRGGBB" or named colors (e.g. "white")
    public string TextColor { get; set; } = "";
    // Empty string = transparent; accepts "#RRGGBB" or named colors
    public string BackgroundColor { get; set; } = "";
}

public class HttpServerSettings
{
    public bool Enabled { get; set; }
    public int Port { get; set; }

    private string _addr = null!;
    public string Addr
    {
        get => _addr;
        set => _addr = (value == "0.0.0.0") ? "+" : value;
    }

    public bool UseIpv6 { get; set; }

    public string UrlPrefix => $"http://{Addr}:{Port}";
}

public class IDeviceManagerSettings
{
    public bool Enabled { get; set; }
}

public class NativeDeviceManagerSettings : IDeviceManagerSettings
{
    public int RetryTime { get; set; } = 10;
    public int PollPeriod { get; set; } = 600;

    public IEnumerable<string> DisabledDevices { get; set; } = [];
}
