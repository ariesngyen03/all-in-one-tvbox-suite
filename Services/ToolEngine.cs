using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using AllInOne_TVBox_Suite.Models;

namespace AllInOne_TVBox_Suite.Services
{
    public class ToolEngine
    {
        public string WorkDir { get; }
        public string AdbExe => Path.Combine(WorkDir, "adb.exe");
        public string FastbootExe => Path.Combine(WorkDir, "fastboot.exe");
        public string AdnlExe => Path.Combine(WorkDir, "adnl.exe");
        public string CompanionApkPath => Path.Combine(WorkDir, "dv9135.apk");
        public string EnvHelperApkPath => Path.Combine(WorkDir, "DVT_DV9135_EnvHelper.apk");

        private const string SDMC_SECRET = "ed12a270c07a58074126a0487de27f63";
        private readonly Action<string, string> _logger;

        public ToolEngine(Action<string, string> logger)
        {
            _logger = logger ?? ((msg, level) => { });
            WorkDir = Path.Combine(Path.GetTempPath(), "AllInOne_TVBox_Engine");
            Directory.CreateDirectory(WorkDir);
        }

        public void EnsureBinariesExtracted()
        {
            string[] resources = new[]
            {
                "adb.exe",
                "fastboot.exe",
                "adnl.exe",
                "AdbWinApi.dll",
                "AdbWinUsbApi.dll",
                "Amldbglog.dll",
                "AmlLibusb.dll",
                "AmlUsbScanX3.dll",
                "libwinpthread-1.dll",
                "dv9135.apk",
                "DVT_DV9135_EnvHelper.apk"
            };

            Assembly asm = Assembly.GetExecutingAssembly();
            foreach (string resName in resources)
            {
                string targetPath = Path.Combine(WorkDir, resName);
                try
                {
                    if (File.Exists(targetPath) && new FileInfo(targetPath).Length > 0)
                        continue;

                    using (Stream s = asm.GetManifestResourceStream(resName))
                    {
                        if (s != null)
                        {
                            using (FileStream fs = new FileStream(targetPath, FileMode.Create, FileAccess.Write))
                            {
                                s.CopyTo(fs);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger($"Không thể giải nén {resName}: {ex.Message}", "WARN");
                }
            }
        }

        public string RunProcess(string exePath, string args, int timeoutMs = 20000)
        {
            if (!File.Exists(exePath))
                return $"ERROR: Missing binary {Path.GetFileName(exePath)}";

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = args,
                    WorkingDirectory = WorkDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };

                using (var proc = new Process { StartInfo = psi })
                {
                    var stdout = new StringBuilder();
                    var stderr = new StringBuilder();

                    proc.OutputDataReceived += (s, e) => { if (e.Data != null) stdout.AppendLine(e.Data); };
                    proc.ErrorDataReceived += (s, e) => { if (e.Data != null) stderr.AppendLine(e.Data); };

                    proc.Start();
                    proc.BeginOutputReadLine();
                    proc.BeginErrorReadLine();

                    if (!proc.WaitForExit(timeoutMs))
                    {
                        try { proc.Kill(); } catch { }
                        return (stdout.ToString() + "\n" + stderr.ToString() + "\n[TIMEOUT]").Trim();
                    }

                    string combined = (stdout.ToString() + "\n" + stderr.ToString()).Trim();
                    return combined;
                }
            }
            catch (Exception ex)
            {
                return $"EXCEPTION: {ex.Message}";
            }
        }

        public string RunAdb(string transportId, string args, int timeoutMs = 20000)
        {
            string prefix = string.IsNullOrWhiteSpace(transportId) ? "" : $"-s \"{transportId}\" ";
            return RunProcess(AdbExe, prefix + args, timeoutMs);
        }

        public string RunFastboot(string transportId, string args, int timeoutMs = 20000)
        {
            string prefix = string.IsNullOrWhiteSpace(transportId) ? "" : $"-s \"{transportId}\" ";
            return RunProcess(FastbootExe, prefix + args, timeoutMs);
        }

        public string RunAdnl(string args, int timeoutMs = 15000)
        {
            return RunProcess(AdnlExe, args, timeoutMs);
        }

        public List<ConnectedDeviceItem> ScanAllConnectedDevices()
        {
            var result = new List<ConnectedDeviceItem>();

            // 1. Scan ADB devices
            string adbOut = RunProcess(AdbExe, "devices", 6000);
            if (!string.IsNullOrWhiteSpace(adbOut))
            {
                var lines = adbOut.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var raw in lines)
                {
                    string line = raw.Trim();
                    if (line.StartsWith("List of devices") || line.StartsWith("*"))
                        continue;

                    var parts = line.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                    {
                        string id = parts[0].Trim();
                        string state = parts[1].Trim().ToLowerInvariant();
                        bool isLan = id.Contains(":");

                        if (state == "device")
                        {
                            result.Add(new ConnectedDeviceItem
                            {
                                Id = id,
                                Mode = isLan ? ConnectionType.AdbLan : ConnectionType.AdbUsb,
                                DisplayText = isLan ? $"[ADB LAN] {id}" : $"[ADB USB] {id}"
                            });
                        }
                        else if (state == "unauthorized")
                        {
                            result.Add(new ConnectedDeviceItem
                            {
                                Id = id,
                                Mode = ConnectionType.Unauthorized,
                                DisplayText = $"[CHƯA CHO PHÉP USB DEBUG] {id}"
                            });
                        }
                    }
                }
            }

            // 2. Scan Fastboot devices
            string fbOut = RunProcess(FastbootExe, "devices", 4000);
            if (!string.IsNullOrWhiteSpace(fbOut) && !fbOut.StartsWith("ERROR") && !fbOut.StartsWith("EXCEPTION"))
            {
                var lines = fbOut.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var raw in lines)
                {
                    var parts = raw.Trim().Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2 && parts[1].ToLowerInvariant().Contains("fastboot"))
                    {
                        result.Add(new ConnectedDeviceItem
                        {
                            Id = parts[0].Trim(),
                            Mode = ConnectionType.Fastboot,
                            DisplayText = $"[FASTBOOT] {parts[0].Trim()}"
                        });
                    }
                }
            }

            // 3. Scan ADNL devices
            string adnlOut = RunAdnl("devices", 3500);
            if (!string.IsNullOrWhiteSpace(adnlOut) &&
                (adnlOut.IndexOf("device", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 adnlOut.IndexOf("ADNL", StringComparison.OrdinalIgnoreCase) >= 0) &&
                adnlOut.IndexOf("no device", StringComparison.OrdinalIgnoreCase) < 0 &&
                adnlOut.IndexOf("0 device", StringComparison.OrdinalIgnoreCase) < 0)
            {
                foreach (var raw in adnlOut.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string l = raw.Trim();
                    if (l.Length > 2 && !l.StartsWith("List", StringComparison.OrdinalIgnoreCase))
                    {
                        result.Add(new ConnectedDeviceItem
                        {
                            Id = "ADNL_USB",
                            Mode = ConnectionType.Adnl,
                            DisplayText = $"[ADNL BURN MODE] {l}"
                        });
                        break;
                    }
                }
            }

            return result;
        }

        public DeviceTelemetry InspectDevice(ConnectedDeviceItem item)
        {
            var info = new DeviceTelemetry();
            if (item == null)
                return info;

            info.TransportId = item.Id;
            info.ConnectionMode = item.Mode;

            if (item.Mode == ConnectionType.Fastboot)
            {
                info.Model = "Fastboot Device";
                string prod = RunFastboot(item.Id, "getvar product", 4000);
                var m = Regex.Match(prod ?? "", @"product:\s*(\S+)", RegexOptions.IgnoreCase);
                if (m.Success) info.Model = m.Groups[1].Value;

                string unlocked = RunFastboot(item.Id, "getvar unlocked", 4000);
                if (unlocked.IndexOf("yes", StringComparison.OrdinalIgnoreCase) >= 0)
                    info.BootloaderStatus = "UNLOCKED";
                else
                    info.BootloaderStatus = "FASTBOOT MODE";
                return info;
            }

            if (item.Mode == ConnectionType.Adnl)
            {
                info.Model = "Amlogic ADNL Device";
                info.BootloaderStatus = "ADNL BURN MODE";
                info.BootloaderGeneration = "TPL/UBT v3 (ADNL)";
                return info;
            }

            if (item.Mode != ConnectionType.AdbUsb && item.Mode != ConnectionType.AdbLan)
                return info;

            // Fetch all properties in a single adb shell getprop call for high speed
            string allProps = RunAdb(item.Id, "shell getprop", 8000);
            var propDict = ParseGetPropOutput(allProps);

            info.Manufacturer = GetPropValue(propDict, "ro.product.manufacturer", "ro.product.vendor.manufacturer");
            info.Brand = GetPropValue(propDict, "ro.product.brand", "ro.product.system.brand");
            info.Model = GetPropValue(propDict, "ro.product.model", "ro.product.vendor.model");
            info.Board = GetPropValue(propDict, "ro.product.board", "ro.build.product", "ro.product.device");
            info.AndroidVersion = GetPropValue(propDict, "ro.build.version.release");
            info.SdkLevel = GetPropValue(propDict, "ro.build.version.sdk");
            info.SoCPlatform = GetPropValue(propDict, "ro.board.platform", "ro.hardware");
            info.SerialNumber = GetPropValue(propDict, "ro.serialno", "ro.boot.serialno");

            // Read MAC address
            string mac = GetPropValue(propDict, "ro.boot.mac", "ubootenv.var.ethaddr");
            if (mac == "---" || string.IsNullOrWhiteSpace(mac))
            {
                string netOut = RunAdb(item.Id, "shell \"cat /sys/class/net/eth0/address 2>/dev/null || cat /sys/class/net/wlan0/address 2>/dev/null\"", 4000);
                var macMatch = Regex.Match(netOut ?? "", @"([0-9A-Fa-f]{2}(:[0-9A-Fa-f]{2}){5})");
                if (macMatch.Success)
                    mac = macMatch.Groups[1].Value.ToUpperInvariant();
            }
            info.MacAddress = string.IsNullOrWhiteSpace(mac) ? "---" : mac.ToUpperInvariant();

            // SELinux status
            string enforce = RunAdb(item.Id, "shell getenforce", 4000).Trim();
            if (!string.IsNullOrWhiteSpace(enforce) && enforce.Length < 30)
                info.SelinuxStatus = enforce;

            // Bootloader lock & generation status
            string oemLock = GetPropValue(propDict, "ubootenv.var.oem_lock", "ro.boot.flash.locked");
            string ubootLock = GetPropValue(propDict, "ubootenv.var.lock");
            if (oemLock.Equals("unlock", StringComparison.OrdinalIgnoreCase) ||
                oemLock.Equals("0", StringComparison.OrdinalIgnoreCase) ||
                ubootLock.Equals("10100000", StringComparison.OrdinalIgnoreCase))
            {
                info.BootloaderStatus = "UNLOCKED";
            }
            else
            {
                info.BootloaderStatus = "LOCKED";
            }

            string blVer = GetPropValue(propDict, "ro.boot.bootloader", "ro.bootloader");
            if (info.AndroidVersion.Contains("14") || blVer.IndexOf("01.01.", StringComparison.OrdinalIgnoreCase) >= 0)
                info.BootloaderGeneration = "TPL/UBT v3 (ADNL)";
            else
                info.BootloaderGeneration = "UBT v2 (Fastboot)";

            return info;
        }

        // ====================================================================
        // MODULE 2: ADB PASSWORD KEYGEN ALGORITHMS (OFFLINE)
        // ====================================================================

        public string GenerateSdmcPassAndroid14(string serialNumber)
        {
            if (string.IsNullOrWhiteSpace(serialNumber))
                return string.Empty;

            byte[] inputBytes = Encoding.UTF8.GetBytes(serialNumber.Trim() + SDMC_SECRET);
            byte[] saltBytes = Encoding.UTF8.GetBytes("sdmc");
            byte[] derivedKey;

            using (var pbkdf2 = new Rfc2898DeriveBytes(inputBytes, saltBytes, 10000))
            {
                derivedKey = pbkdf2.GetBytes(16);
            }

            var hexKey = new StringBuilder(32);
            foreach (byte b in derivedKey)
                hexKey.Append(b.ToString("x2"));

            byte[] aesKey = Encoding.ASCII.GetBytes(hexKey.ToString());
            byte[] cipherBytes;

            using (Aes aes = Aes.Create())
            {
                aes.Key = aesKey;
                aes.Mode = CipherMode.ECB;
                aes.Padding = PaddingMode.PKCS7;
                using (ICryptoTransform enc = aes.CreateEncryptor())
                {
                    cipherBytes = enc.TransformFinalBlock(inputBytes, 0, inputBytes.Length);
                }
            }

            string b64 = Convert.ToBase64String(cipherBytes).ToLowerInvariant();
            if (b64.Length < 6)
                return b64;

            return b64.Substring(0, 3) + b64.Substring(b64.Length - 3);
        }

        public string GenerateSdmcPassAndroid11(string macAddress, string serialNumber, string dateYyyyMmDd)
        {
            if (string.IsNullOrWhiteSpace(macAddress) || string.IsNullOrWhiteSpace(serialNumber))
                return string.Empty;

            string cleanMac = macAddress.Replace(":", "").Replace("-", "").Trim().ToLowerInvariant();
            string cleanSn = serialNumber.Trim().ToUpperInvariant();
            string cleanDate = string.IsNullOrWhiteSpace(dateYyyyMmDd)
                ? DateTime.Now.ToString("yyyyMMdd")
                : dateYyyyMmDd.Replace("-", "").Replace("/", "").Trim();

            string md5Hex;
            using (MD5 md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(cleanMac + cleanSn + SDMC_SECRET));
                var sb = new StringBuilder(32);
                foreach (byte b in hash)
                    sb.Append(b.ToString("x2"));
                md5Hex = sb.ToString();
            }

            string keyHex;
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(cleanDate));
                var sb = new StringBuilder(32);
                for (int i = 0; i < 16; i++)
                    sb.Append(hash[i].ToString("x2"));
                keyHex = sb.ToString();
            }

            byte[] plainBytes = Encoding.ASCII.GetBytes(md5Hex);
            byte[] aesKey = Encoding.ASCII.GetBytes(keyHex);
            byte[] cipherBytes;

            using (Aes aes = Aes.Create())
            {
                aes.Key = aesKey;
                aes.Mode = CipherMode.ECB;
                aes.Padding = PaddingMode.PKCS7;
                using (ICryptoTransform enc = aes.CreateEncryptor())
                {
                    cipherBytes = enc.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
                }
            }

            string b64 = Convert.ToBase64String(cipherBytes);
            if (b64.Length < 10)
                return b64;

            return b64.Substring(0, 5) + b64.Substring(b64.Length - 5);
        }

        // ====================================================================
        // MODULE 3: INTERACTIVE PACKAGE MANAGER, DEBLOAT & LAUNCHER STUDIO
        // ====================================================================

        public List<PackageEntry> GetDevicePackages(string transportId, DeviceProfile activeProfile)
        {
            var result = new List<PackageEntry>();
            if (string.IsNullOrWhiteSpace(transportId))
                return result;

            string allOut = RunAdb(transportId, "shell pm list packages -f", 12000);
            string disabledOut = RunAdb(transportId, "shell pm list packages -d", 8000);

            var disabledSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string line in (disabledOut ?? "").Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (line.StartsWith("package:"))
                    disabledSet.Add(line.Substring("package:".Length).Trim());
            }

            var bloatSet = new HashSet<string>(activeProfile?.CarrierBloatPackages ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
            var otaSet = new HashSet<string>(activeProfile?.OtaUpdatePackages ?? new List<string>(), StringComparer.OrdinalIgnoreCase);

            foreach (string raw in (allOut ?? "").Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string line = raw.Trim();
                if (!line.StartsWith("package:"))
                    continue;

                string body = line.Substring("package:".Length);
                int lastEq = body.LastIndexOf('=');
                if (lastEq <= 0)
                    continue;

                string apkPath = body.Substring(0, lastEq).Trim();
                string pkgName = body.Substring(lastEq + 1).Trim();
                bool isDisabled = disabledSet.Contains(pkgName);

                var entry = new PackageEntry
                {
                    PackageName = pkgName,
                    ApkPath = apkPath,
                    Status = isDisabled ? "Đã đóng băng (Disabled)" : "Đang hoạt động"
                };

                string lowerPkg = pkgName.ToLowerInvariant();
                if (bloatSet.Contains(pkgName) ||
                    lowerPkg.Contains("viettel") ||
                    lowerPkg.Contains("tv360") ||
                    lowerPkg.Contains("fpt") ||
                    lowerPkg.Contains("vnpt") ||
                    lowerPkg.Contains("mytv") ||
                    lowerPkg.Contains("tr069") ||
                    lowerPkg.Contains("sdmc") ||
                    lowerPkg.Contains("zte."))
                {
                    entry.Category = "[ĐỎ] App Nhà Mạng / Kiểm Soát";
                    entry.PriorityOrder = 1;
                    entry.IsSelected = !isDisabled;
                }
                else if (otaSet.Contains(pkgName) ||
                         lowerPkg.Contains("ota") ||
                         lowerPkg.Contains("upgrade") ||
                         lowerPkg.Contains("fota") ||
                         lowerPkg.Contains("updater"))
                {
                    entry.Category = "[CAM] Cập Nhật OTA / Hệ Thống";
                    entry.PriorityOrder = 2;
                    entry.IsSelected = !isDisabled;
                }
                else if (lowerPkg.Contains("launcher") ||
                         lowerPkg.Contains("projengmenu") ||
                         lowerPkg.Contains("tvhome"))
                {
                    entry.Category = "[XANH] Giao Diện Launcher";
                    entry.PriorityOrder = 3;
                    entry.IsSelected = false;
                }
                else if (apkPath.StartsWith("/data/app"))
                {
                    entry.Category = "[XANH DƯƠNG] App Cài Thêm (User)";
                    entry.PriorityOrder = 4;
                    entry.IsSelected = false;
                }
                else
                {
                    entry.Category = "[XÁM] Hệ Thống Android";
                    entry.PriorityOrder = 5;
                    entry.IsSelected = false;
                }

                result.Add(entry);
            }

            return result
                .OrderBy(p => p.PriorityOrder)
                .ThenBy(p => p.PackageName)
                .ToList();
        }

        public bool SetPackageEnabledState(string transportId, string packageName, bool enable)
        {
            if (enable)
            {
                string res = RunAdb(transportId, $"shell pm enable {packageName}", 8000);
                _logger($"[ENABLE] {packageName} -> {res}", "INFO");
                return res.IndexOf("enabled", StringComparison.OrdinalIgnoreCase) >= 0;
            }
            else
            {
                string res1 = RunAdb(transportId, $"shell pm disable-user --user 0 {packageName}", 8000);
                string res2 = RunAdb(transportId, $"shell am force-stop {packageName}", 5000);
                _logger($"[DISABLE] {packageName} -> {res1}", "OK");
                return true;
            }
        }

        public bool UninstallPackageForUser0(string transportId, string packageName)
        {
            string res = RunAdb(transportId, $"shell pm uninstall -k --user 0 {packageName}", 10000);
            bool ok = res.IndexOf("Success", StringComparison.OrdinalIgnoreCase) >= 0;
            _logger($"[UNINSTALL] {packageName} -> {res}", ok ? "OK" : "WARN");
            return ok;
        }

        public bool InstallApkFile(string transportId, string apkFilePath)
        {
            if (!File.Exists(apkFilePath))
            {
                _logger($"Không tìm thấy file APK: {apkFilePath}", "ERR");
                return false;
            }

            _logger($"Đang cài đặt {Path.GetFileName(apkFilePath)}...", "CMD");
            string res = RunAdb(transportId, $"install -r -d \"{apkFilePath}\"", 60000);
            bool ok = res.IndexOf("Success", StringComparison.OrdinalIgnoreCase) >= 0;
            _logger($"Kết quả cài {Path.GetFileName(apkFilePath)}: {res}", ok ? "OK" : "ERR");
            return ok;
        }

        // ====================================================================
        // MODULE 4: U-BOOT ENVIRONMENT & BOOTLOADER LAB
        // ====================================================================

        public Dictionary<string, string> ReadUbootVariables(string transportId)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string[] keys = new[]
            {
                "ubootenv.var.lock",
                "ubootenv.var.oem_lock",
                "ubootenv.var.avb2",
                "ubootenv.var.EnableSelinux",
                "ubootenv.var.product.security",
                "ubootenv.var.adb_debug",
                "ubootenv.var.console_enable",
                "ubootenv.var.bootcmd",
                "ubootenv.var.preboot",
                "ro.boot.flash.locked",
                "ro.boot.verifiedbootstate"
            };

            string rawProps = RunAdb(transportId, "shell getprop", 8000);
            var allProps = ParseGetPropOutput(rawProps);

            foreach (string k in keys)
            {
                if (allProps.TryGetValue(k, out string val))
                    dict[k] = val;
                else
                    dict[k] = "(chưa thiết lập)";
            }

            return dict;
        }

        public bool ApplyDv9135BootloaderUnlockOffline(string transportId, DeviceTelemetry telemetry, Action<int, string> stepCallback)
        {
            stepCallback?.Invoke(1, "Bước 1/4: Kiểm tra kết nối và phiên bản Android...");
            bool isAndroid11 = telemetry.AndroidVersion.Contains("11");
            bool isTplV3 = telemetry.BootloaderGeneration.Contains("v3") || telemetry.AndroidVersion.Contains("14");

            _logger($"Bắt đầu mở khóa Bootloader Offline cho {telemetry.Model} (Android {telemetry.AndroidVersion}, {telemetry.BootloaderGeneration})", "CMD");

            if (!isAndroid11)
            {
                stepCallback?.Invoke(2, "Bước 2/4: Cài đặt DVT_DV9135_EnvHelper.apk (System UID)...");
                EnsureBinariesExtracted();
                if (!InstallApkFile(transportId, EnvHelperApkPath))
                {
                    _logger("Không thể cài đặt EnvHelper APK (Kiểm tra chữ ký hệ thống hoặc kết nối ADB).", "ERR");
                    return false;
                }
            }
            else
            {
                stepCallback?.Invoke(2, "Bước 2/4: Kích hoạt cổng content://com.sdmc.platformapi (Android 11)...");
            }

            stepCallback?.Invoke(3, "Bước 3/4: Ghi cấu hình mở khóa vào phân vùng U-Boot Environment...");
            var envCommands = BuildDv9135UnlockVariables(isTplV3);

            foreach (var kv in envCommands)
            {
                WriteSingleBootEnv(transportId, isAndroid11, kv.Key, kv.Value);
            }

            stepCallback?.Invoke(4, "Bước 4/4: Xác minh trạng thái ubootenv sau khi ghi...");
            string verifyLock = ReadSingleBootEnv(transportId, isAndroid11, "ubootenv.var.oem_lock");
            _logger($"Kết quả xác minh ubootenv.var.oem_lock = {verifyLock}", "OK");
            return true;
        }

        public void WriteSingleBootEnv(string transportId, bool useSdmcPlatformApi, string key, string value)
        {
            RunAdb(transportId, $"shell setprop {key} \"{value}\"", 5000);
            if (useSdmcPlatformApi)
            {
                string cmd = $"shell \"content call --uri content://com.sdmc.platformapi --method '' --extra method:i:19 --extra 's1:s:{key}' --extra 's2:s:{value}'\"";
                string res = RunAdb(transportId, cmd, 6000);
                _logger($"[U-BOOT API11] {key} = {value} -> {res}", "INFO");
            }
            else
            {
                string cmd = $"shell content call --uri content://com.dvt.dv9135.env/env --method set --arg \"{key}\" --extra value:s:\"{value}\"";
                string res = RunAdb(transportId, cmd, 6000);
                _logger($"[U-BOOT ENV14] {key} = {value} -> {res}", "INFO");
            }
        }

        public string ReadSingleBootEnv(string transportId, bool useSdmcPlatformApi, string key)
        {
            if (!useSdmcPlatformApi)
            {
                string res = RunAdb(transportId, $"shell content call --uri content://com.dvt.dv9135.env/env --method get --arg \"{key}\"", 5000);
                if (!string.IsNullOrWhiteSpace(res))
                    return res.Trim();
            }
            return RunAdb(transportId, $"shell getprop {key}", 5000).Trim();
        }

        private Dictionary<string, string> BuildDv9135UnlockVariables(bool isTplV3)
        {
            var vars = new Dictionary<string, string>
            {
                { "ubootenv.var.lock", "10100000" },
                { "ubootenv.var.oem_lock", "unlock" },
                { "ubootenv.var.avb2", "0" },
                { "ubootenv.var.EnableSelinux", "permissive" },
                { "ubootenv.var.product.security", "disable" },
                { "ubootenv.var.adb_debug", "1" },
                { "ubootenv.var.console_enable", "1" }
            };

            if (isTplV3)
            {
                vars["ubootenv.var.bootcmd"] = "run storeboot";
                vars["ubootenv.var.preboot"] = "adnl 1200;setenv preboot;saveenv;";
                vars["ubootenv.var.reboot_mode"] = "cold_boot";
            }
            else
            {
                vars["ubootenv.var.bootcmd"] = "fastboot";
                vars["ubootenv.var.reboot_mode"] = "fastboot";
            }

            return vars;
        }

        // ====================================================================
        // MODULE 5: SYSTEM APK & DEVICE RESEARCH DUMPER (TAB 5)
        // ====================================================================

        public string DumpDeviceResearchPackage(string transportId, DeviceTelemetry telemetry, string baseDumpFolder, bool pullSettingsApk, bool pullOemApks, bool pullFrameworkJars)
        {
            string safeModel = string.Concat((telemetry?.Model ?? "UnknownBox").Split(Path.GetInvalidFileNameChars())).Replace(" ", "_");
            string folderName = $"{safeModel}_{DateTime.Now:yyyyMMdd_HHmmss}";
            string targetDir = Path.Combine(baseDumpFolder, folderName);
            Directory.CreateDirectory(targetDir);

            _logger($"Đang tạo gói dữ liệu nghiên cứu tại: {targetDir}", "CMD");

            // 1. Dump full getprop
            string props = RunAdb(transportId, "shell getprop", 10000);
            File.WriteAllText(Path.Combine(targetDir, "1_getprop_full.txt"), props ?? "", Encoding.UTF8);
            _logger("Đã lưu 1_getprop_full.txt", "OK");

            // 2. Dump full package list with paths
            string pkgs = RunAdb(transportId, "shell pm list packages -f", 10000);
            File.WriteAllText(Path.Combine(targetDir, "2_packages_with_paths.txt"), pkgs ?? "", Encoding.UTF8);
            _logger("Đã lưu 2_packages_with_paths.txt", "OK");

            // 3. Dump content providers & sharedUserId info
            string providers = RunAdb(transportId, "shell \"dumpsys package providers | grep -E 'Provider|authority|sharedUser'\"", 12000);
            File.WriteAllText(Path.Combine(targetDir, "3_content_providers.txt"), providers ?? "", Encoding.UTF8);
            _logger("Đã lưu 3_content_providers.txt", "OK");

            // Parse package paths
            var apkLines = (pkgs ?? "").Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            string apkSubDir = Path.Combine(targetDir, "Extracted_APKs");
            Directory.CreateDirectory(apkSubDir);

            foreach (string raw in apkLines)
            {
                string line = raw.Trim();
                if (!line.StartsWith("package:")) continue;
                string body = line.Substring("package:".Length);
                int eq = body.LastIndexOf('=');
                if (eq <= 0) continue;

                string remotePath = body.Substring(0, eq).Trim();
                string pkgName = body.Substring(eq + 1).Trim().ToLowerInvariant();

                bool shouldPull = false;
                if (pullSettingsApk && (pkgName.Contains("settings") || pkgName.Contains("tvsettings") || pkgName.Contains("setup")))
                    shouldPull = true;

                if (pullOemApks && (pkgName.Contains("viettel") || pkgName.Contains("fpt") || pkgName.Contains("vnpt") ||
                                    pkgName.Contains("zte") || pkgName.Contains("sdmc") || pkgName.Contains("skyworth") ||
                                    pkgName.Contains("droidlogic") || pkgName.Contains("ota") || pkgName.Contains("tr069")))
                    shouldPull = true;

                if (shouldPull)
                {
                    string localApk = Path.Combine(apkSubDir, $"{pkgName}.apk");
                    _logger($"Đang hút APK: {pkgName} ({remotePath})...", "CMD");
                    RunAdb(transportId, $"pull \"{remotePath}\" \"{localApk}\"", 30000);
                }
            }

            if (pullFrameworkJars)
            {
                string fwSubDir = Path.Combine(targetDir, "Vendor_Frameworks");
                Directory.CreateDirectory(fwSubDir);
                string[] jars = new[]
                {
                    "/vendor/framework/droidlogic.jar",
                    "/system/framework/droidlogic.jar",
                    "/system/etc/prop.default",
                    "/vendor/build.prop"
                };

                foreach (string remoteFile in jars)
                {
                    string localFile = Path.Combine(fwSubDir, Path.GetFileName(remoteFile));
                    RunAdb(transportId, $"pull \"{remoteFile}\" \"{localFile}\"", 15000);
                }
            }

            _logger($"Hoàn tất trích xuất dữ liệu Box mới tại: {targetDir}", "OK");
            return targetDir;
        }

        private static Dictionary<string, string> ParseGetPropOutput(string output)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(output))
                return dict;

            var regex = new Regex(@"\[([^\]]+)\]:\s*\[([^\]]*)\]");
            foreach (Match m in regex.Matches(output))
            {
                dict[m.Groups[1].Value.Trim()] = m.Groups[2].Value.Trim();
            }
            return dict;
        }

        private static string GetPropValue(Dictionary<string, string> dict, params string[] keys)
        {
            foreach (string k in keys)
            {
                if (dict.TryGetValue(k, out string v) && !string.IsNullOrWhiteSpace(v))
                    return v;
            }
            return "---";
        }
    }
}
