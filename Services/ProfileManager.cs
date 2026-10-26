using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using AllInOne_TVBox_Suite.Models;

namespace AllInOne_TVBox_Suite.Services
{
    public class ProfileManager
    {
        public string ProfilesDirectory { get; }
        public string AppsDirectory { get; }
        public string DumpsDirectory { get; }

        private readonly JavaScriptSerializer _serializer;

        public ProfileManager(string baseDir)
        {
            ProfilesDirectory = Path.Combine(baseDir, "Profiles");
            AppsDirectory = Path.Combine(baseDir, "Apps");
            DumpsDirectory = Path.Combine(baseDir, "Dumps");

            Directory.CreateDirectory(ProfilesDirectory);
            Directory.CreateDirectory(AppsDirectory);
            Directory.CreateDirectory(Path.Combine(AppsDirectory, "Launchers"));
            Directory.CreateDirectory(Path.Combine(AppsDirectory, "DefaultApps"));
            Directory.CreateDirectory(DumpsDirectory);

            _serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
            EnsureDefaultProfilesExist();
        }

        public List<DeviceProfile> LoadAllProfiles()
        {
            var list = new List<DeviceProfile>();
            try
            {
                string[] files = Directory.GetFiles(ProfilesDirectory, "*.json");
                foreach (string file in files)
                {
                    try
                    {
                        string json = File.ReadAllText(file, Encoding.UTF8);
                        var profile = _serializer.Deserialize<DeviceProfile>(json);
                        if (profile != null && !string.IsNullOrWhiteSpace(profile.Id))
                        {
                            list.Add(profile);
                        }
                    }
                    catch
                    {
                        // Ignore malformed JSON file
                    }
                }
            }
            catch
            {
            }

            if (list.Count == 0)
            {
                EnsureDefaultProfilesExist();
                return LoadAllProfiles();
            }

            return list.OrderBy(p => p.OemGroup).ThenBy(p => p.Name).ToList();
        }

        public void SaveProfile(DeviceProfile profile)
        {
            if (profile == null || string.IsNullOrWhiteSpace(profile.Id))
                return;

            string safeId = string.Concat(profile.Id.Split(Path.GetInvalidFileNameChars())).ToLowerInvariant();
            string path = Path.Combine(ProfilesDirectory, safeId + ".json");
            string json = FormatJson(_serializer.Serialize(profile));
            File.WriteAllText(path, json, Encoding.UTF8);
        }

        public DeviceProfile MatchProfile(DeviceTelemetry telemetry, List<DeviceProfile> profiles)
        {
            if (telemetry == null || profiles == null || profiles.Count == 0)
                return null;

            string combined = $"{telemetry.Manufacturer} {telemetry.Brand} {telemetry.Model} {telemetry.Board}".ToUpperInvariant();
            foreach (var profile in profiles)
            {
                if (profile.MatchKeywords != null)
                {
                    foreach (string kw in profile.MatchKeywords)
                    {
                        if (!string.IsNullOrWhiteSpace(kw) && combined.Contains(kw.ToUpperInvariant()))
                        {
                            return profile;
                        }
                    }
                }
            }

            return profiles.FirstOrDefault(p => p.Id == "generic_universal_box") ?? profiles.FirstOrDefault();
        }

        private void EnsureDefaultProfilesExist()
        {
            if (Directory.GetFiles(ProfilesDirectory, "*.json").Length > 0)
                return;

            var defaults = new List<DeviceProfile>
            {
                new DeviceProfile
                {
                    Id = "viettel_sdmc_dv9135",
                    Name = "SDMC DV9135 (Viettel TV360 - Android 11 & 14)",
                    OemGroup = "1. VIETTEL TV360",
                    Description = "Hỗ trợ đầy đủ 100%: Tạo Pass ADB (Android 11 & 14), Mở khóa U-Boot Offline (platformapi / System EnvHelper APK), Debloat TV360 & chặn OTA.",
                    MatchKeywords = new List<string> { "DV9135", "SDMC", "SEI" },
                    KeygenMode = "SDMC_DV9135",
                    BootloaderUnlockMode = "DV9135_SYSTEM_ENV",
                    CarrierBloatPackages = new List<string>
                    {
                        "com.viettel.tv360",
                        "com.viettel.iptv",
                        "com.viettel.launcher",
                        "com.sdmc.launcher",
                        "com.sdmc.tr069",
                        "com.sdmc.tms",
                        "com.viettel.tr069",
                        "com.viettel.agent"
                    },
                    OtaUpdatePackages = new List<string>
                    {
                        "com.sdmc.ota",
                        "com.droidlogic.otaupgrade",
                        "com.google.android.tungsten.setupwraith",
                        "com.android.dynsystem"
                    },
                    PreferredLauncherPackage = "com.spocky.projengmenu",
                    UbootEnvVars = new Dictionary<string, string>
                    {
                        { "ubootenv.var.lock", "10100000" },
                        { "ubootenv.var.oem_lock", "unlock" },
                        { "ubootenv.var.avb2", "0" },
                        { "ubootenv.var.EnableSelinux", "permissive" },
                        { "ubootenv.var.product.security", "disable" },
                        { "ubootenv.var.adb_debug", "1" },
                        { "ubootenv.var.console_enable", "1" }
                    }
                },
                new DeviceProfile
                {
                    Id = "viettel_zte_b866v2f",
                    Name = "ZTE B866V2F / B866V6M (Viettel TV360)",
                    OemGroup = "1. VIETTEL TV360",
                    Description = "Kịch bản Debloat gói khóa ZTE/Viettel, tắt TR-069 quản lý từ xa, chặn cập nhật OTA và thay thế Launcher ngoài.",
                    MatchKeywords = new List<string> { "B866V2F", "B866V6M", "B866", "ZTE" },
                    KeygenMode = "ZTE_CUSTOM",
                    BootloaderUnlockMode = "STANDARD_AML",
                    CarrierBloatPackages = new List<string>
                    {
                        "com.viettel.tv360",
                        "com.zte.iptvclient.android.launcher",
                        "com.zte.stb.tr069",
                        "com.zte.ums.agent",
                        "com.zte.LockScreen",
                        "com.viettel.tr069"
                    },
                    OtaUpdatePackages = new List<string>
                    {
                        "com.zte.upgrade",
                        "com.droidlogic.otaupgrade",
                        "com.zte.ota"
                    },
                    PreferredLauncherPackage = "com.spocky.projengmenu",
                    UbootEnvVars = new Dictionary<string, string>
                    {
                        { "ubootenv.var.oem_lock", "unlock" },
                        { "ubootenv.var.EnableSelinux", "permissive" }
                    }
                },
                new DeviceProfile
                {
                    Id = "fpt_playbox_all",
                    Name = "FPT Play Box (P650 / H650 / T650 / S550)",
                    OemGroup = "2. FPT PLAY BOX",
                    Description = "Kịch bản mở khóa phần mềm cho FPT Play Box (SEI Robotics / Skyworth): Tắt FPT Launcher ép đăng nhập, chặn OTA khóa lại máy và cài Launcher ATV.",
                    MatchKeywords = new List<string> { "FPT", "P650", "H650", "T650", "S550", "SKYWORTH" },
                    KeygenMode = "FPT_CUSTOM",
                    BootloaderUnlockMode = "STANDARD_AML",
                    CarrierBloatPackages = new List<string>
                    {
                        "com.fptplay.tv",
                        "com.fpt.launcher",
                        "tv.fpt.fptplaybox",
                        "com.fpt.tr069",
                        "com.fpt.agent",
                        "com.fpt.voice",
                        "com.skyworth.lafite.srtnj.service"
                    },
                    OtaUpdatePackages = new List<string>
                    {
                        "com.fpt.ota",
                        "com.skyworth.ota",
                        "com.droidlogic.otaupgrade",
                        "abupdate.fpt.com"
                    },
                    PreferredLauncherPackage = "com.spocky.projengmenu",
                    UbootEnvVars = new Dictionary<string, string>
                    {
                        { "ubootenv.var.oem_lock", "unlock" },
                        { "ubootenv.var.EnableSelinux", "permissive" }
                    }
                },
                new DeviceProfile
                {
                    Id = "vnpt_smartbox_mytv",
                    Name = "VNPT SmartBox 2 / MyTV Box",
                    OemGroup = "3. VNPT / MYTV",
                    Description = "Kịch bản gỡ khóa màn hình MyTV/VNPT Technology, tắt dịch vụ quản lý từ xa và nạp giao diện Android TV chuẩn.",
                    MatchKeywords = new List<string> { "VNPT", "MYTV", "SMB2" },
                    KeygenMode = "VNPT_CUSTOM",
                    BootloaderUnlockMode = "STANDARD_AML",
                    CarrierBloatPackages = new List<string>
                    {
                        "vn.vnpttech.launcher",
                        "com.vnpt.mytv",
                        "vn.vnpttech.tr069",
                        "vn.vnpttech.appstore"
                    },
                    OtaUpdatePackages = new List<string>
                    {
                        "vn.vnpttech.ota",
                        "com.droidlogic.otaupgrade"
                    },
                    PreferredLauncherPackage = "com.spocky.projengmenu",
                    UbootEnvVars = new Dictionary<string, string>()
                },
                new DeviceProfile
                {
                    Id = "generic_universal_box",
                    Name = "Box Quốc Tế / Đa Năng (X96, TX3, H96, Tanix...)",
                    OemGroup = "4. ĐA NĂNG / QUỐC TẾ",
                    Description = "Áp dụng cho mọi dòng Box đã bật ADB: Tối ưu hóa hệ thống, chặn cập nhật ngầm, cài combo ứng dụng giải trí và chuyển nhanh chế độ nạp ROM.",
                    MatchKeywords = new List<string> { "X96", "TX3", "H96", "TANIX", "AMLOGIC", "ROCKCHIP" },
                    KeygenMode = "NONE",
                    BootloaderUnlockMode = "STANDARD_AML",
                    CarrierBloatPackages = new List<string>(),
                    OtaUpdatePackages = new List<string>
                    {
                        "com.droidlogic.otaupgrade",
                        "com.adups.fota",
                        "com.adups.fota.sysoper"
                    },
                    PreferredLauncherPackage = "com.spocky.projengmenu",
                    UbootEnvVars = new Dictionary<string, string>()
                }
            };

            foreach (var p in defaults)
            {
                SaveProfile(p);
            }
        }

        private static string FormatJson(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            var sb = new StringBuilder();
            int indent = 0;
            bool inQuotes = false;

            for (int i = 0; i < input.Length; i++)
            {
                char ch = input[i];
                if (ch == '"' && (i == 0 || input[i - 1] != '\\'))
                {
                    inQuotes = !inQuotes;
                    sb.Append(ch);
                }
                else if (!inQuotes)
                {
                    if (ch == '{' || ch == '[')
                    {
                        sb.Append(ch);
                        sb.AppendLine();
                        indent++;
                        sb.Append(new string(' ', indent * 2));
                    }
                    else if (ch == '}' || ch == ']')
                    {
                        sb.AppendLine();
                        indent = Math.Max(0, indent - 1);
                        sb.Append(new string(' ', indent * 2));
                        sb.Append(ch);
                    }
                    else if (ch == ',')
                    {
                        sb.Append(ch);
                        sb.AppendLine();
                        sb.Append(new string(' ', indent * 2));
                    }
                    else if (ch == ':')
                    {
                        sb.Append(": ");
                    }
                    else
                    {
                        sb.Append(ch);
                    }
                }
                else
                {
                    sb.Append(ch);
                }
            }
            return sb.ToString();
        }
    }
}
