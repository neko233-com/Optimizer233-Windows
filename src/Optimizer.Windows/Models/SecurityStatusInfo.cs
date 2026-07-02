namespace Optimizer_Windows.Models;

public sealed class SecurityStatusInfo
{
    public bool AntivirusEnabled { get; set; }

    public bool RealTimeProtectionEnabled { get; set; }

    public bool FirewallDomainEnabled { get; set; }

    public bool FirewallPrivateEnabled { get; set; }

    public bool FirewallPublicEnabled { get; set; }

    public string AntivirusEngineVersion { get; set; } = string.Empty;

    public string LastQuickScanTime { get; set; } = string.Empty;

    public bool TamperProtected { get; set; }
}
