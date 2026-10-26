using System;
using System.Collections.Generic;

namespace AllInOne_TVBox_Suite.Models
{
    public enum ConnectionType
    {
        None = 0,
        AdbUsb = 1,
        AdbLan = 2,
        Fastboot = 3,
        Adnl = 4,
        Unauthorized = 5
    }

    public class ConnectedDeviceItem
    {
        public string Id { get; set; } = string.Empty;
        public ConnectionType Mode { get; set; } = ConnectionType.None;
        public string DisplayText { get; set; } = string.Empty;

        public override string ToString()
        {
            return DisplayText;
        }
    }

    public class DeviceTelemetry
    {
        public string TransportId { get; set; } = string.Empty;
        public ConnectionType ConnectionMode { get; set; } = ConnectionType.None;
        public string Manufacturer { get; set; } = "---";
        public string Brand { get; set; } = "---";
        public string Model { get; set; } = "---";
        public string Board { get; set; } = "---";
        public string AndroidVersion { get; set; } = "---";
        public string SdkLevel { get; set; } = "---";
        public string SoCPlatform { get; set; } = "---";
        public string SerialNumber { get; set; } = "---";
        public string MacAddress { get; set; } = "---";
        public string SelinuxStatus { get; set; } = "---";
        public string BootloaderStatus { get; set; } = "---";
        public string BootloaderGeneration { get; set; } = "---";
        public string MatchedProfileId { get; set; } = string.Empty;
    }

    public class DeviceProfile
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string OemGroup { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> MatchKeywords { get; set; } = new List<string>();
        public string KeygenMode { get; set; } = "NONE";
        public string BootloaderUnlockMode { get; set; } = "STANDARD_AML";
        public List<string> CarrierBloatPackages { get; set; } = new List<string>();
        public List<string> OtaUpdatePackages { get; set; } = new List<string>();
        public string PreferredLauncherPackage { get; set; } = string.Empty;
        public Dictionary<string, string> UbootEnvVars { get; set; } = new Dictionary<string, string>();

        public override string ToString()
        {
            return $"[{OemGroup}] {Name}";
        }
    }

    public class PackageEntry
    {
        public bool IsSelected { get; set; }
        public string PackageName { get; set; } = string.Empty;
        public string Category { get; set; } = "Hệ thống / Khác";
        public string Status { get; set; } = "Đang bật";
        public string ApkPath { get; set; } = string.Empty;
        public int PriorityOrder { get; set; } = 99;
    }
}
