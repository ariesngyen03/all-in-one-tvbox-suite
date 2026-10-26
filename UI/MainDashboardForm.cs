using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using AllInOne_TVBox_Suite.Models;
using AllInOne_TVBox_Suite.Services;

namespace AllInOne_TVBox_Suite.UI
{
    public class MainDashboardForm : Form
    {
        private readonly ToolEngine _engine;
        private readonly ProfileManager _profileManager;

        private List<DeviceProfile> _profiles = new List<DeviceProfile>();
        private DeviceProfile _selectedProfile;
        private DeviceTelemetry _currentTelemetry = new DeviceTelemetry();
        private List<PackageEntry> _allPackages = new List<PackageEntry>();
        private bool _isScanning;

        // Header & Live Telemetry Controls
        private Label _lblConnectionBadge;
        private ComboBox _cboConnectedDevices;
        private TextBox _txtQuickLanIp;
        private TelemetryBadgeControl _badgeModel;
        private TelemetryBadgeControl _badgeAndroid;
        private TelemetryBadgeControl _badgeSoC;
        private TelemetryBadgeControl _badgeSerialMac;
        private TelemetryBadgeControl _badgeSelinux;
        private TelemetryBadgeControl _badgeBootloader;

        // Visual Step Progress
        private StepProgressControl _stepProgress;

        // Sidebar & Tab Pages
        private readonly List<SidebarNavButton> _navButtons = new List<SidebarNavButton>();
        private readonly Panel[] _tabPanels = new Panel[5];
        private Panel _mainWorkspaceContainer;

        // Tab 1 Controls (1-Click Center)
        private ComboBox _cboActiveProfile;
        private Label _lblProfileDesc;
        private ListBox _lstProfilePreview;

        // Tab 2 Controls (ADB Keygen & Connection)
        private ComboBox _cboKeygenModel;
        private TextBox _txtKeygenSn;
        private TextBox _txtKeygenMac;
        private TextBox _txtKeygenDate;
        private TextBox _txtGeneratedPass14;
        private TextBox _txtGeneratedPass11;

        // Tab 3 Controls (Interactive Package Manager & Launcher Studio)
        private DataGridView _gridPackages;
        private TextBox _txtSearchPkg;
        private ComboBox _cboFilterCategory;
        private Label _lblPkgSummary;

        // Tab 4 Controls (Bootloader & U-Boot Env Lab)
        private DataGridView _gridUbootEnv;
        private TextBox _txtCustomEnvKey;
        private TextBox _txtCustomEnvVal;

        // Tab 5 Controls (Device Research & APK Dumper)
        private CheckBox _chkDumpSettings;
        private CheckBox _chkDumpOem;
        private CheckBox _chkDumpFrameworks;
        private TextBox _txtNewProfileId;
        private TextBox _txtNewProfileName;
        private TextBox _txtNewProfileOem;

        // Smart Console Log Controls
        private RichTextBox _rtbConsole;
        private ProgressBar _progressBar;
        private Timer _autoScanTimer;

        public MainDashboardForm()
        {
            Text = "ALL-IN-ONE ANDROID TV BOX SUITE PRO // MASTER OFFLINE EDITION (DEVICE LAB)";
            Size = new Size(1380, 860);
            MinimumSize = new Size(1240, 760);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = ModernTheme.BgMain;
            ForeColor = ModernTheme.TextPrimary;
            Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            AllowDrop = true;

            try
            {
                var asm = System.Reflection.Assembly.GetExecutingAssembly();
                using (var iconStream = asm.GetManifestResourceStream("app.ico"))
                {
                    if (iconStream != null)
                        Icon = new Icon(iconStream);
                }
            }
            catch { }

            _engine = new ToolEngine(AppendLogThreadSafe);
            _profileManager = new ProfileManager(AppDomain.CurrentDomain.BaseDirectory);

            InitializeDashboardLayout();
            DragEnter += OnFormDragEnter;
            DragDrop += OnFormDragDrop;
            Shown += OnFormShown;
        }

        private async void OnFormShown(object sender, EventArgs e)
        {
            AppendLog("Khởi tạo hệ thống All-in-One TV Box Suite (Chế độ Master Offline - Không giới hạn Key/Credit)...", "INFO");
            await Task.Run(() => _engine.EnsureBinariesExtracted());
            ReloadProfilesList();
            SwitchTab(0);
            await RefreshDevicesAndInspectAsync(true);

            _autoScanTimer = new Timer { Interval = 4500 };
            _autoScanTimer.Tick += async (s, ev) =>
            {
                if (!_isScanning)
                    await RefreshDevicesAndInspectAsync(false);
            };
            _autoScanTimer.Start();
        }

        // ====================================================================
        // MAIN DASHBOARD LAYOUT CONSTRUCTION
        // ====================================================================

        private void InitializeDashboardLayout()
        {
            // 1. TOP HEADER BAR
            var topHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = ModernTheme.BgSidebar,
                Padding = new Padding(16, 8, 16, 8)
            };
            topHeader.Paint += (s, e) =>
            {
                using (var pen = new Pen(ModernTheme.BorderColor, 1))
                    e.Graphics.DrawLine(pen, 0, topHeader.Height - 1, topHeader.Width, topHeader.Height - 1);
            };

            var lblBrand = new Label
            {
                Text = "⚡ ALL-IN-ONE TV BOX SUITE PRO",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = ModernTheme.AccentCyan,
                AutoSize = true,
                Location = new Point(14, 11)
            };

            var lblEdition = new Label
            {
                Text = "MASTER OFFLINE LAB // MULTI-MODEL ENGINE",
                Font = new Font("Consolas", 8.5f, FontStyle.Bold),
                ForeColor = ModernTheme.AccentGreen,
                AutoSize = true,
                Location = new Point(315, 17)
            };

            // Right-aligned connection bar inside header
            var rightHeaderPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(0, 2, 0, 0)
            };

            _lblConnectionBadge = new Label
            {
                Text = "● CHƯA KẾT NỐI THIẾT BỊ",
                Font = new Font("Segoe UI", 8.8f, FontStyle.Bold),
                BackColor = Color.FromArgb(45, 20, 30),
                ForeColor = ModernTheme.AccentRed,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(195, 30),
                Margin = new Padding(4, 1, 8, 0)
            };

            _cboConnectedDevices = new ComboBox
            {
                Width = 220,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = ModernTheme.BgInput,
                ForeColor = ModernTheme.TextPrimary,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Consolas", 9.2f),
                Margin = new Padding(0, 3, 6, 0)
            };
            _cboConnectedDevices.SelectedIndexChanged += async (s, e) =>
            {
                if (_cboConnectedDevices.SelectedItem is ConnectedDeviceItem item)
                    await InspectSelectedDeviceAsync(item);
            };

            var btnScanNow = ModernTheme.CreateActionButton("Làm mới", ModernTheme.AccentBlue, Color.White, 30);
            btnScanNow.Width = 82;
            btnScanNow.Margin = new Padding(0, 1, 10, 0);
            btnScanNow.Click += async (s, e) => await RefreshDevicesAndInspectAsync(true);

            _txtQuickLanIp = new TextBox
            {
                Width = 135,
                Text = "192.168.1.",
                BackColor = ModernTheme.BgInput,
                ForeColor = ModernTheme.AccentCyan,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 9.5f),
                Margin = new Padding(0, 4, 4, 0)
            };

            var btnConnectLan = ModernTheme.CreateActionButton("Kết nối LAN", ModernTheme.AccentGreen, Color.Black, 30);
            btnConnectLan.Width = 98;
            btnConnectLan.Margin = new Padding(0, 1, 0, 0);
            btnConnectLan.Click += async (s, e) => await ConnectLanAdbAsync(_txtQuickLanIp.Text);

            rightHeaderPanel.Controls.Add(_lblConnectionBadge);
            rightHeaderPanel.Controls.Add(_cboConnectedDevices);
            rightHeaderPanel.Controls.Add(btnScanNow);
            rightHeaderPanel.Controls.Add(_txtQuickLanIp);
            rightHeaderPanel.Controls.Add(btnConnectLan);

            topHeader.Controls.Add(rightHeaderPanel);
            topHeader.Controls.Add(lblEdition);
            topHeader.Controls.Add(lblBrand);

            // 2. LIVE DEVICE TELEMETRY BAR (6 BADGES)
            var telemetryPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 68,
                ColumnCount = 6,
                RowCount = 1,
                BackColor = ModernTheme.BgMain,
                Padding = new Padding(12, 7, 12, 7)
            };
            for (int i = 0; i < 6; i++)
                telemetryPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.66f));

            _badgeModel = new TelemetryBadgeControl { Caption = "HÃNG / MODEL BOX", ValueText = "---", Dock = DockStyle.Fill, Margin = new Padding(4, 0, 4, 0) };
            _badgeAndroid = new TelemetryBadgeControl { Caption = "PHIÊN BẢN ANDROID", ValueText = "---", Dock = DockStyle.Fill, Margin = new Padding(4, 0, 4, 0) };
            _badgeSoC = new TelemetryBadgeControl { Caption = "CHIP XỬ LÝ (SOC)", ValueText = "---", Dock = DockStyle.Fill, Margin = new Padding(4, 0, 4, 0) };
            _badgeSerialMac = new TelemetryBadgeControl { Caption = "SERIAL / ĐỊA CHỈ MAC", ValueText = "---", Dock = DockStyle.Fill, Margin = new Padding(4, 0, 4, 0) };
            _badgeSelinux = new TelemetryBadgeControl { Caption = "TRẠNG THÁI SELINUX", ValueText = "---", Dock = DockStyle.Fill, Margin = new Padding(4, 0, 4, 0) };
            _badgeBootloader = new TelemetryBadgeControl { Caption = "BOOTLOADER / CHẾ ĐỘ", ValueText = "---", Dock = DockStyle.Fill, Margin = new Padding(4, 0, 4, 0) };

            telemetryPanel.Controls.Add(_badgeModel, 0, 0);
            telemetryPanel.Controls.Add(_badgeAndroid, 1, 0);
            telemetryPanel.Controls.Add(_badgeSoC, 2, 0);
            telemetryPanel.Controls.Add(_badgeSerialMac, 3, 0);
            telemetryPanel.Controls.Add(_badgeSelinux, 4, 0);
            telemetryPanel.Controls.Add(_badgeBootloader, 5, 0);

            // 3. LEFT SIDEBAR NAVIGATION (5 TABS)
            var sidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 248,
                BackColor = ModernTheme.BgSidebar,
                Padding = new Padding(0, 8, 0, 8)
            };
            sidebar.Paint += (s, e) =>
            {
                using (var pen = new Pen(ModernTheme.BorderColor, 1))
                    e.Graphics.DrawLine(pen, sidebar.Width - 1, 0, sidebar.Width - 1, sidebar.Height);
            };

            string[,] navMeta = new[,]
            {
                { "1", "Trung Tâm 1-Click", "Auto-Unlock & Kịch bản nhanh" },
                { "2", "Tạo Pass ADB & LAN", "Giải mã mật khẩu đa dòng Box" },
                { "3", "Quản Lý App & Debloat", "Tắt app nhà mạng & Cài Launcher" },
                { "4", "Bootloader & U-Boot", "Mở khóa U-Boot, Fastboot, ADNL" },
                { "5", "Rút APK & Nghiên Cứu", "Hút dữ liệu từ Box mới để mở rộng" }
            };

            for (int i = 4; i >= 0; i--)
            {
                int idx = i;
                var btn = new SidebarNavButton
                {
                    TabIndexValue = idx,
                    BadgeNumber = navMeta[idx, 0],
                    Title = navMeta[idx, 1],
                    Subtitle = navMeta[idx, 2],
                    Dock = DockStyle.Top
                };
                btn.Click += (s, e) => SwitchTab(idx);
                _navButtons.Insert(0, btn);
                sidebar.Controls.Add(btn);
            }

            // Quick Folder Shortcut buttons at bottom of sidebar
            var sidebarFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 136,
                Padding = new Padding(12, 8, 12, 8),
                BackColor = ModernTheme.BgSidebar
            };

            var btnOpenProfilesDir = ModernTheme.CreateActionButton("📂 Mở Thư Mục Profiles (.json)", Color.FromArgb(30, 41, 59), ModernTheme.TextPrimary, 34);
            btnOpenProfilesDir.Dock = DockStyle.Top;
            btnOpenProfilesDir.Click += (s, e) => Process.Start("explorer.exe", _profileManager.ProfilesDirectory);

            var spacer1 = new Panel { Dock = DockStyle.Top, Height = 6 };

            var btnOpenAppsDir = ModernTheme.CreateActionButton("📦 Mở Thư Mục Kho APK Cài Đặt", Color.FromArgb(30, 41, 59), ModernTheme.TextPrimary, 34);
            btnOpenAppsDir.Dock = DockStyle.Top;
            btnOpenAppsDir.Click += (s, e) => Process.Start("explorer.exe", _profileManager.AppsDirectory);

            var spacer2 = new Panel { Dock = DockStyle.Top, Height = 6 };

            var btnOpenDumpsDir = ModernTheme.CreateActionButton("🔬 Mở Thư Mục Dumps Nghiên Cứu", Color.FromArgb(30, 41, 59), ModernTheme.AccentCyan, 34);
            btnOpenDumpsDir.Dock = DockStyle.Top;
            btnOpenDumpsDir.Click += (s, e) => Process.Start("explorer.exe", _profileManager.DumpsDirectory);

            sidebarFooter.Controls.Add(btnOpenDumpsDir);
            sidebarFooter.Controls.Add(spacer2);
            sidebarFooter.Controls.Add(btnOpenAppsDir);
            sidebarFooter.Controls.Add(spacer1);
            sidebarFooter.Controls.Add(btnOpenProfilesDir);
            sidebar.Controls.Add(sidebarFooter);

            // 4. BOTTOM SMART CONSOLE LOG DRAWER
            var bottomConsoleCard = ModernTheme.CreateCardPanel("NHẬT KÝ THỰC THI HỆ THỐNG (SMART CONSOLE & TIẾN ĐỘ)", ModernTheme.AccentCyan, out Panel consoleContent);
            bottomConsoleCard.Dock = DockStyle.Bottom;
            bottomConsoleCard.Height = 185;
            bottomConsoleCard.Padding = new Padding(12, 0, 12, 10);

            var consoleToolbar = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 28,
                Padding = new Padding(0, 4, 0, 0)
            };

            _progressBar = new ProgressBar
            {
                Dock = DockStyle.Fill,
                Style = ProgressBarStyle.Continuous,
                Value = 0
            };

            var btnCopyLog = ModernTheme.CreateActionButton("Sao chép Log", Color.FromArgb(30, 41, 59), ModernTheme.TextPrimary, 24);
            btnCopyLog.Width = 105;
            btnCopyLog.Dock = DockStyle.Right;
            btnCopyLog.Click += (s, e) =>
            {
                if (!string.IsNullOrEmpty(_rtbConsole.Text))
                {
                    Clipboard.SetText(_rtbConsole.Text);
                    AppendLog("Đã sao chép toàn bộ nội dung Nhật ký vào Clipboard.", "OK");
                }
            };

            var btnClearLog = ModernTheme.CreateActionButton("Xóa Log", Color.FromArgb(30, 41, 59), ModernTheme.TextSecondary, 24);
            btnClearLog.Width = 85;
            btnClearLog.Dock = DockStyle.Right;
            btnClearLog.Click += (s, e) => _rtbConsole.Clear();

            consoleToolbar.Controls.Add(_progressBar);
            consoleToolbar.Controls.Add(btnClearLog);
            consoleToolbar.Controls.Add(btnCopyLog);

            _rtbConsole = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = ModernTheme.BgInput,
                ForeColor = ModernTheme.TextPrimary,
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                Font = new Font("Consolas", 9.2f)
            };

            consoleContent.Controls.Add(_rtbConsole);
            consoleContent.Controls.Add(consoleToolbar);

            // 5. MAIN CENTER WORKSPACE (STEP PROGRESS + ACTIVE TAB PANEL)
            var centerArea = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(14, 4, 14, 8),
                BackColor = ModernTheme.BgMain
            };

            _stepProgress = new StepProgressControl
            {
                Dock = DockStyle.Top
            };

            _mainWorkspaceContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 10, 0, 0),
                BackColor = ModernTheme.BgMain
            };

            _tabPanels[0] = BuildTab1OneClickCenter();
            _tabPanels[1] = BuildTab2AdbKeygen();
            _tabPanels[2] = BuildTab3PackageManager();
            _tabPanels[3] = BuildTab4BootloaderLab();
            _tabPanels[4] = BuildTab5ResearchDumper();

            foreach (var p in _tabPanels)
            {
                p.Dock = DockStyle.Fill;
                p.Visible = false;
                _mainWorkspaceContainer.Controls.Add(p);
            }

            centerArea.Controls.Add(_mainWorkspaceContainer);
            centerArea.Controls.Add(_stepProgress);

            Controls.Add(centerArea);
            Controls.Add(bottomConsoleCard);
            Controls.Add(sidebar);
            Controls.Add(telemetryPanel);
            Controls.Add(topHeader);
        }

        private void SwitchTab(int index)
        {
            for (int i = 0; i < _tabPanels.Length; i++)
            {
                _tabPanels[i].Visible = (i == index);
                _navButtons[i].IsActive = (i == index);
            }
        }

        // ====================================================================
        // TAB 1: TRUNG TÂM 1-CLICK (AUTO-UNLOCK & DEVICE PROFILES)
        // ====================================================================

        private Panel BuildTab1OneClickCenter()
        {
            var container = new TableLayoutPanel
            {
                ColumnCount = 2,
                RowCount = 1,
                BackColor = ModernTheme.BgMain
            };
            container.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48f));
            container.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52f));

            // Left Card: Profile Selector & Details
            var cardProfile = ModernTheme.CreateCardPanel("1. HỒ SƠ THIẾT BỊ ĐANG ÁP DỤNG (DEVICE PROFILE)", ModernTheme.AccentCyan, out Panel leftContent);
            cardProfile.Dock = DockStyle.Fill;
            cardProfile.Margin = new Padding(0, 0, 6, 0);

            var lblSelect = new Label
            {
                Text = "Chọn dòng Box (Tự động nhận diện khi cắm cáp hoặc chọn thủ công):",
                ForeColor = ModernTheme.TextSecondary,
                Dock = DockStyle.Top,
                Height = 22
            };

            _cboActiveProfile = new ComboBox
            {
                Dock = DockStyle.Top,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = ModernTheme.BgInput,
                ForeColor = ModernTheme.AccentCyan,
                Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold),
                Height = 32
            };
            _cboActiveProfile.SelectedIndexChanged += (s, e) =>
            {
                if (_cboActiveProfile.SelectedItem is DeviceProfile prof)
                {
                    _selectedProfile = prof;
                    UpdateProfilePreviewUI(prof);
                }
            };

            _lblProfileDesc = new Label
            {
                Text = "---",
                ForeColor = ModernTheme.TextPrimary,
                Dock = DockStyle.Top,
                Height = 48,
                Padding = new Padding(0, 8, 0, 4)
            };

            var lblPreviewHeader = new Label
            {
                Text = "Chi tiết kịch bản nằm trong Profile này:",
                ForeColor = ModernTheme.AccentAmber,
                Font = new Font("Segoe UI", 8.8f, FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 24
            };

            _lstProfilePreview = new ListBox
            {
                Dock = DockStyle.Fill,
                BackColor = ModernTheme.BgInput,
                ForeColor = ModernTheme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 9f)
            };

            leftContent.Controls.Add(_lstProfilePreview);
            leftContent.Controls.Add(lblPreviewHeader);
            leftContent.Controls.Add(_lblProfileDesc);
            leftContent.Controls.Add(_cboActiveProfile);
            leftContent.Controls.Add(lblSelect);

            // Right Card: 1-Click Execution Actions
            var cardActions = ModernTheme.CreateCardPanel("2. BẢNG ĐIỀU KHIỂN THỰC THI 1-CLICK (CHẠY OFFLINE 100%)", ModernTheme.AccentGreen, out Panel rightContent);
            cardActions.Dock = DockStyle.Fill;
            cardActions.Margin = new Padding(6, 0, 0, 0);

            var layoutActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true
            };

            var btnSoftUnlock1Click = ModernTheme.CreateActionButton(
                "🚀 MỞ KHÓA PHẦN MỀM 1-CLICK (DEBLOAT + CHẶN OTA + CÀI LAUNCHER)",
                ModernTheme.AccentGreen, Color.Black, 50);
            btnSoftUnlock1Click.Width = 560;
            btnSoftUnlock1Click.Margin = new Padding(0, 4, 0, 4);
            btnSoftUnlock1Click.Click += async (s, e) => await ExecuteOneClickSoftUnlockAsync();

            var lblSoftHint = new Label
            {
                Text = "• Áp dụng cho MỌI dòng Box đã kết nối ADB: Tự động đóng băng các gói khóa của nhà mạng,\n  chặn cập nhật OTA khóa lại máy, và cài đặt toàn bộ file .apk trong thư mục Apps/.",
                ForeColor = ModernTheme.TextSecondary,
                AutoSize = true,
                Margin = new Padding(4, 0, 0, 14)
            };

            var btnHardUnlock1Click = ModernTheme.CreateActionButton(
                "🔓 MỞ KHÓA BOOTLOADER / U-BOOT 1-CLICK (DÀNH CHO NẠP ROM ATV)",
                ModernTheme.AccentAmber, Color.Black, 50);
            btnHardUnlock1Click.Width = 560;
            btnHardUnlock1Click.Margin = new Padding(0, 4, 0, 4);
            btnHardUnlock1Click.Click += async (s, e) => await ExecuteOneClickBootloaderUnlockAsync();

            var lblHardHint = new Label
            {
                Text = "• Mở khóa biến môi trường U-Boot (oem_lock=unlock, avb2=0, EnableSelinux=permissive)\n  và tự động đưa máy về chế độ Fastboot hoặc ADNL Burn Mode để nạp ROM.",
                ForeColor = ModernTheme.TextSecondary,
                AutoSize = true,
                Margin = new Padding(4, 0, 0, 14)
            };

            var btnInstallCompanion = ModernTheme.CreateActionButton(
                "📲 CÀI ĐẶT & KÍCH HOẠT APP HỖ TRỢ DV9135.APK (MỞ CÀI APP NGOÀI)",
                ModernTheme.AccentBlue, Color.White, 42);
            btnInstallCompanion.Width = 560;
            btnInstallCompanion.Margin = new Padding(0, 4, 0, 6);
            btnInstallCompanion.Click += async (s, e) => await InstallAndTriggerCompanionApkAsync();

            var btnQuickDump = ModernTheme.CreateActionButton(
                "🔬 QUÉT & HÚT DỮ LIỆU BOX ĐANG CẮM (DÀNH CHO MÁY MỚI CHƯA CÓ PROFILE)",
                ModernTheme.AccentPurple, Color.White, 42);
            btnQuickDump.Width = 560;
            btnQuickDump.Margin = new Padding(0, 4, 0, 6);
            btnQuickDump.Click += (s, e) => SwitchTab(4);

            layoutActions.Controls.Add(btnSoftUnlock1Click);
            layoutActions.Controls.Add(lblSoftHint);
            layoutActions.Controls.Add(btnHardUnlock1Click);
            layoutActions.Controls.Add(lblHardHint);
            layoutActions.Controls.Add(btnInstallCompanion);
            layoutActions.Controls.Add(btnQuickDump);

            rightContent.Controls.Add(layoutActions);

            container.Controls.Add(cardProfile, 0, 0);
            container.Controls.Add(cardActions, 1, 0);
            return container;
        }

        private void UpdateProfilePreviewUI(DeviceProfile prof)
        {
            if (prof == null) return;
            _lblProfileDesc.Text = prof.Description;
            _lstProfilePreview.Items.Clear();
            _lstProfilePreview.Items.Add($"=== THÔNG TIN PROFILE: {prof.Name} ===");
            _lstProfilePreview.Items.Add($"• Mã định danh File  : Profiles/{prof.Id}.json");
            _lstProfilePreview.Items.Add($"• Thuật toán Pass ADB: {prof.KeygenMode}");
            _lstProfilePreview.Items.Add($"• Cơ chế Bootloader  : {prof.BootloaderUnlockMode}");
            _lstProfilePreview.Items.Add("");
            _lstProfilePreview.Items.Add($"--- GÓI NHÀ MẠNG SẼ ĐÓNG BĂNG ({prof.CarrierBloatPackages.Count} gói) ---");
            foreach (var p in prof.CarrierBloatPackages)
                _lstProfilePreview.Items.Add($"  [x] {p}");
            _lstProfilePreview.Items.Add("");
            _lstProfilePreview.Items.Add($"--- GÓI CẬP NHẬT OTA SẼ CHẶN ({prof.OtaUpdatePackages.Count} gói) ---");
            foreach (var p in prof.OtaUpdatePackages)
                _lstProfilePreview.Items.Add($"  [x] {p}");
        }

        // ====================================================================
        // TAB 2: TẠO PASS ADB & KẾT NỐI (ADB KEYGEN STUDIO)
        // ====================================================================

        private Panel BuildTab2AdbKeygen()
        {
            var container = new TableLayoutPanel
            {
                ColumnCount = 2,
                RowCount = 1,
                BackColor = ModernTheme.BgMain
            };
            container.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52f));
            container.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48f));

            var cardInput = ModernTheme.CreateCardPanel("NHẬP THÔNG TIN SERIAL / MAC TRÊN MÀN HÌNH TV", ModernTheme.AccentCyan, out Panel leftContent);
            cardInput.Dock = DockStyle.Fill;
            cardInput.Margin = new Padding(0, 0, 6, 0);

            var layoutLeft = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };

            layoutLeft.Controls.Add(new Label { Text = "1. Chọn Hãng / Dòng Box cần tính mật khẩu ADB:", ForeColor = ModernTheme.TextSecondary, AutoSize = true, Margin = new Padding(0, 2, 0, 4) });
            _cboKeygenModel = new ComboBox
            {
                Width = 490,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = ModernTheme.BgInput,
                ForeColor = ModernTheme.AccentCyan,
                Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 10)
            };
            _cboKeygenModel.Items.Add("Viettel TV360 — SDMC DV9135 (Android 14 & Android 11)");
            _cboKeygenModel.Items.Add("Viettel TV360 — ZTE B866V2F / B866V6M (Module mở rộng)");
            _cboKeygenModel.Items.Add("FPT Play Box — P650 / H650 / T650 / S550 (Module mở rộng)");
            _cboKeygenModel.SelectedIndex = 0;
            layoutLeft.Controls.Add(_cboKeygenModel);

            layoutLeft.Controls.Add(new Label { Text = "2. Số Serial Number (SN) của Box (Ví dụ: G624157039535):", ForeColor = ModernTheme.TextSecondary, AutoSize = true, Margin = new Padding(0, 2, 0, 4) });
            _txtKeygenSn = new TextBox
            {
                Width = 490,
                BackColor = ModernTheme.BgInput,
                ForeColor = ModernTheme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 11.5f, FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 10)
            };
            _txtKeygenSn.TextChanged += (s, e) => ComputeOfflineKeygenValues();
            layoutLeft.Controls.Add(_txtKeygenSn);

            layoutLeft.Controls.Add(new Label { Text = "3. Địa chỉ MAC (Cần cho Android 11 - Có hoặc không có dấu hai chấm đều được):", ForeColor = ModernTheme.TextSecondary, AutoSize = true, Margin = new Padding(0, 2, 0, 4) });
            _txtKeygenMac = new TextBox
            {
                Width = 490,
                BackColor = ModernTheme.BgInput,
                ForeColor = ModernTheme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 11.5f, FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 10)
            };
            _txtKeygenMac.TextChanged += (s, e) => ComputeOfflineKeygenValues();
            layoutLeft.Controls.Add(_txtKeygenMac);

            layoutLeft.Controls.Add(new Label { Text = "4. Ngày hiển thị trên góc màn hình TV (Định dạng YYYYMMDD - dùng cho Android 11):", ForeColor = ModernTheme.TextSecondary, AutoSize = true, Margin = new Padding(0, 2, 0, 4) });
            _txtKeygenDate = new TextBox
            {
                Width = 490,
                Text = DateTime.Now.ToString("yyyyMMdd"),
                BackColor = ModernTheme.BgInput,
                ForeColor = ModernTheme.AccentAmber,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 11.5f, FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 12)
            };
            _txtKeygenDate.TextChanged += (s, e) => ComputeOfflineKeygenValues();
            layoutLeft.Controls.Add(_txtKeygenDate);

            var btnAutoFillFromBox = ModernTheme.CreateActionButton("⚡ Tự động lấy SN & MAC từ Box đang cắm & Giải mã ngay", ModernTheme.AccentCyan, Color.Black, 40);
            btnAutoFillFromBox.Width = 490;
            btnAutoFillFromBox.Click += (s, e) =>
            {
                if (_currentTelemetry != null && _currentTelemetry.SerialNumber != "---")
                {
                    _txtKeygenSn.Text = _currentTelemetry.SerialNumber;
                    if (_currentTelemetry.MacAddress != "---")
                        _txtKeygenMac.Text = _currentTelemetry.MacAddress;
                    ComputeOfflineKeygenValues();
                    AppendLog("Đã tự động lấy SN/MAC từ thiết bị đang kết nối và tạo mật khẩu ADB.", "OK");
                }
                else
                {
                    AppendLog("Hiện chưa đọc được SN tự động qua ADB — Bạn hãy gõ trực tiếp SN trên tem máy hoặc màn hình TV.", "WARN");
                }
            };
            layoutLeft.Controls.Add(btnAutoFillFromBox);
            leftContent.Controls.Add(layoutLeft);

            // Right Card: Big Password Display
            var cardOutput = ModernTheme.CreateCardPanel("KẾT QUẢ MẬT KHẨU BẬT ADB (CHẠY OFFLINE TRỰC TIẾP)", ModernTheme.AccentGreen, out Panel rightContent);
            cardOutput.Dock = DockStyle.Fill;
            cardOutput.Margin = new Padding(6, 0, 0, 0);

            var layoutRight = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };

            layoutRight.Controls.Add(new Label
            {
                Text = "MẬT KHẨU ADB ANDROID 14 (6 KÝ TỰ — CHỈ CẦN NHẬP SERIAL):",
                ForeColor = ModernTheme.AccentCyan,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, 6, 0, 6)
            });

            _txtGeneratedPass14 = new TextBox
            {
                Width = 440,
                ReadOnly = true,
                Text = "---",
                TextAlign = HorizontalAlignment.Center,
                BackColor = ModernTheme.BgInput,
                ForeColor = ModernTheme.AccentGreen,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 22f, FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 8)
            };
            layoutRight.Controls.Add(_txtGeneratedPass14);

            var btnCopyPass14 = ModernTheme.CreateActionButton("📋 Sao chép Pass Android 14", Color.FromArgb(30, 41, 59), ModernTheme.TextPrimary, 34);
            btnCopyPass14.Width = 440;
            btnCopyPass14.Margin = new Padding(0, 0, 0, 20);
            btnCopyPass14.Click += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(_txtGeneratedPass14.Text) && _txtGeneratedPass14.Text != "---")
                {
                    Clipboard.SetText(_txtGeneratedPass14.Text);
                    AppendLog($"Đã copy mật khẩu ADB Android 14: {_txtGeneratedPass14.Text}", "OK");
                }
            };
            layoutRight.Controls.Add(btnCopyPass14);

            layoutRight.Controls.Add(new Label
            {
                Text = "MẬT KHẨU ADB ANDROID 11 (10 KÝ TỰ — CẦN SERIAL + MAC + NGÀY):",
                ForeColor = ModernTheme.AccentAmber,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, 4, 0, 6)
            });

            _txtGeneratedPass11 = new TextBox
            {
                Width = 440,
                ReadOnly = true,
                Text = "---",
                TextAlign = HorizontalAlignment.Center,
                BackColor = ModernTheme.BgInput,
                ForeColor = ModernTheme.AccentAmber,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 22f, FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 8)
            };
            layoutRight.Controls.Add(_txtGeneratedPass11);

            var btnCopyPass11 = ModernTheme.CreateActionButton("📋 Sao chép Pass Android 11", Color.FromArgb(30, 41, 59), ModernTheme.TextPrimary, 34);
            btnCopyPass11.Width = 440;
            btnCopyPass11.Click += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(_txtGeneratedPass11.Text) && _txtGeneratedPass11.Text != "---")
                {
                    Clipboard.SetText(_txtGeneratedPass11.Text);
                    AppendLog($"Đã copy mật khẩu ADB Android 11: {_txtGeneratedPass11.Text}", "OK");
                }
            };
            layoutRight.Controls.Add(btnCopyPass11);

            rightContent.Controls.Add(layoutRight);

            container.Controls.Add(cardInput, 0, 0);
            container.Controls.Add(cardOutput, 1, 0);
            return container;
        }

        private void ComputeOfflineKeygenValues()
        {
            string sn = _txtKeygenSn.Text.Trim();
            string mac = _txtKeygenMac.Text.Trim();
            string date = _txtKeygenDate.Text.Trim();

            if (!string.IsNullOrWhiteSpace(sn))
            {
                _txtGeneratedPass14.Text = _engine.GenerateSdmcPassAndroid14(sn);
            }
            else
            {
                _txtGeneratedPass14.Text = "---";
            }

            if (!string.IsNullOrWhiteSpace(sn) && !string.IsNullOrWhiteSpace(mac))
            {
                _txtGeneratedPass11.Text = _engine.GenerateSdmcPassAndroid11(mac, sn, date);
            }
            else
            {
                _txtGeneratedPass11.Text = "(Nhập thêm MAC)";
            }
        }

        // ====================================================================
        // TAB 3: QUẢN LÝ ỨNG DỤNG, DEBLOAT & LAUNCHER STUDIO
        // ====================================================================

        private Panel BuildTab3PackageManager()
        {
            var card = ModernTheme.CreateCardPanel("QUẢN LÝ ỨNG DỤNG TRỰC QUAN — ĐÓNG BĂNG APP NHÀ MẠNG, CHẶN OTA, CÀI LAUNCHER & LƯU PROFILE", ModernTheme.AccentGreen, out Panel content);

            // Top Filter Bar
            var topBar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 42,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            var btnScanPkgs = ModernTheme.CreateActionButton("🔍 Quét Toàn Bộ App Trên Box", ModernTheme.AccentCyan, Color.Black, 34);
            btnScanPkgs.Width = 215;
            btnScanPkgs.Margin = new Padding(0, 0, 8, 0);
            btnScanPkgs.Click += async (s, e) => await ScanDevicePackagesAsync();

            _txtSearchPkg = new TextBox
            {
                Width = 230,
                BackColor = ModernTheme.BgInput,
                ForeColor = ModernTheme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10f),
                Margin = new Padding(0, 4, 8, 0)
            };
            _txtSearchPkg.TextChanged += (s, e) => FilterPackagesGrid();

            _cboFilterCategory = new ComboBox
            {
                Width = 225,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = ModernTheme.BgInput,
                ForeColor = ModernTheme.AccentCyan,
                Font = new Font("Segoe UI", 9.5f),
                Margin = new Padding(0, 4, 10, 0)
            };
            _cboFilterCategory.Items.Add("Tất cả phân loại ứng dụng");
            _cboFilterCategory.Items.Add("[ĐỎ] App Nhà Mạng / Kiểm Soát");
            _cboFilterCategory.Items.Add("[CAM] Cập Nhật OTA / Hệ Thống");
            _cboFilterCategory.Items.Add("[XANH] Giao Diện Launcher");
            _cboFilterCategory.Items.Add("[XANH DƯƠNG] App Cài Thêm (User)");
            _cboFilterCategory.SelectedIndex = 0;
            _cboFilterCategory.SelectedIndexChanged += (s, e) => FilterPackagesGrid();

            _lblPkgSummary = new Label
            {
                Text = "Chưa quét danh sách gói (Hãy bấm nút Quét hoặc kéo-thả file .apk vào cửa sổ này để cài đặt ngay).",
                ForeColor = ModernTheme.TextSecondary,
                AutoSize = true,
                Margin = new Padding(4, 9, 0, 0)
            };

            topBar.Controls.Add(btnScanPkgs);
            topBar.Controls.Add(_txtSearchPkg);
            topBar.Controls.Add(_cboFilterCategory);
            topBar.Controls.Add(_lblPkgSummary);

            // Bottom Action Bar for Tab 3
            var bottomBar = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 46,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(0, 6, 0, 0)
            };

            var btnDisableSelected = ModernTheme.CreateActionButton("❄️ Đóng Băng (Disable) Các Gói Đã Tích", ModernTheme.AccentAmber, Color.Black, 36);
            btnDisableSelected.Width = 265;
            btnDisableSelected.Margin = new Padding(0, 0, 8, 0);
            btnDisableSelected.Click += async (s, e) => await BatchProcessSelectedPackagesAsync("DISABLE");

            var btnEnableSelected = ModernTheme.CreateActionButton("✓ Bật Lại (Enable) Gói Đã Tích", Color.FromArgb(30, 41, 59), ModernTheme.AccentGreen, 36);
            btnEnableSelected.Width = 205;
            btnEnableSelected.Margin = new Padding(0, 0, 8, 0);
            btnEnableSelected.Click += async (s, e) => await BatchProcessSelectedPackagesAsync("ENABLE");

            var btnUninstallSelected = ModernTheme.CreateActionButton("🗑️ Gỡ Bỏ (Uninstall User 0)", ModernTheme.AccentRed, Color.White, 36);
            btnUninstallSelected.Width = 205;
            btnUninstallSelected.Margin = new Padding(0, 0, 8, 0);
            btnUninstallSelected.Click += async (s, e) => await BatchProcessSelectedPackagesAsync("UNINSTALL");

            var btnInstallApkDialog = ModernTheme.CreateActionButton("📦 Chọn File APK Cài Vào Box...", ModernTheme.AccentBlue, Color.White, 36);
            btnInstallApkDialog.Width = 215;
            btnInstallApkDialog.Margin = new Padding(0, 0, 8, 0);
            btnInstallApkDialog.Click += async (s, e) => await PromptAndInstallApkFilesAsync();

            var btnSaveToProfile = ModernTheme.CreateActionButton("💾 Lưu Các Gói Đã Tích Thành Profile Chuẩn", ModernTheme.AccentPurple, Color.White, 36);
            btnSaveToProfile.Width = 275;
            btnSaveToProfile.Click += (s, e) => SaveCheckedPackagesToCurrentProfile();

            bottomBar.Controls.Add(btnDisableSelected);
            bottomBar.Controls.Add(btnEnableSelected);
            bottomBar.Controls.Add(btnUninstallSelected);
            bottomBar.Controls.Add(btnInstallApkDialog);
            bottomBar.Controls.Add(btnSaveToProfile);

            // Center DataGridView
            _gridPackages = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = ModernTheme.BgInput,
                ForeColor = ModernTheme.TextPrimary,
                GridColor = ModernTheme.BorderColor,
                BorderStyle = BorderStyle.FixedSingle,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                EnableHeadersVisualStyles = false,
                RowTemplate = { Height = 28 }
            };

            _gridPackages.ColumnHeadersDefaultCellStyle.BackColor = ModernTheme.BgCardHeader;
            _gridPackages.ColumnHeadersDefaultCellStyle.ForeColor = ModernTheme.AccentCyan;
            _gridPackages.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            _gridPackages.ColumnHeadersHeight = 32;

            _gridPackages.DefaultCellStyle.BackColor = ModernTheme.BgInput;
            _gridPackages.DefaultCellStyle.ForeColor = ModernTheme.TextPrimary;
            _gridPackages.DefaultCellStyle.SelectionBackColor = Color.FromArgb(30, 58, 95);
            _gridPackages.DefaultCellStyle.SelectionForeColor = Color.White;
            _gridPackages.DefaultCellStyle.Font = new Font("Consolas", 9.2f);

            var colCheck = new DataGridViewCheckBoxColumn { HeaderText = "Chọn", FillWeight = 7f };
            var colCat = new DataGridViewTextBoxColumn { HeaderText = "Phân Loại Gói", ReadOnly = true, FillWeight = 23f };
            var colPkg = new DataGridViewTextBoxColumn { HeaderText = "Tên Gói (Package Name)", ReadOnly = true, FillWeight = 32f };
            var colState = new DataGridViewTextBoxColumn { HeaderText = "Trạng Thái", ReadOnly = true, FillWeight = 16f };
            var colPath = new DataGridViewTextBoxColumn { HeaderText = "Đường Dẫn File APK Trên Box", ReadOnly = true, FillWeight = 22f };

            _gridPackages.Columns.AddRange(colCheck, colCat, colPkg, colState, colPath);

            content.Controls.Add(_gridPackages);
            content.Controls.Add(bottomBar);
            content.Controls.Add(topBar);
            return card;
        }

        // ====================================================================
        // TAB 4: BOOTLOADER, U-BOOT ENV & CHẾ ĐỘ NẠP ROM
        // ====================================================================

        private Panel BuildTab4BootloaderLab()
        {
            var container = new TableLayoutPanel
            {
                ColumnCount = 2,
                RowCount = 1,
                BackColor = ModernTheme.BgMain
            };
            container.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44f));
            container.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 56f));

            // Left Card: Quick Boot Mode Switcher & Direct Unlock
            var cardModes = ModernTheme.CreateCardPanel("ĐIỀU KHIỂN CHẾ ĐỘ BOOT & MỞ KHÓA U-BOOT", ModernTheme.AccentAmber, out Panel leftContent);
            cardModes.Dock = DockStyle.Fill;
            cardModes.Margin = new Padding(0, 0, 6, 0);

            var flowModes = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };

            var btnUnlockDv9135 = ModernTheme.CreateActionButton("🔓 Mở Khóa U-Boot DV9135 Offline (Android 11 & 14)", ModernTheme.AccentGreen, Color.Black, 44);
            btnUnlockDv9135.Width = 445;
            btnUnlockDv9135.Margin = new Padding(0, 2, 0, 10);
            btnUnlockDv9135.Click += async (s, e) => await ExecuteOneClickBootloaderUnlockAsync();

            var btnRebootFastboot = ModernTheme.CreateActionButton("⚡ Đưa Máy Vào Chế Độ FASTBOOT (adb reboot bootloader)", ModernTheme.AccentCyan, Color.Black, 38);
            btnRebootFastboot.Width = 445;
            btnRebootFastboot.Margin = new Padding(0, 0, 0, 8);
            btnRebootFastboot.Click += async (s, e) => await RunRebootCommandAsync("reboot bootloader", "Fastboot Mode");

            var btnRebootBurn = ModernTheme.CreateActionButton("🔥 Đưa Máy Vào AMLOGIC BURN MODE (adb reboot update)", ModernTheme.AccentAmber, Color.Black, 38);
            btnRebootBurn.Width = 445;
            btnRebootBurn.Margin = new Padding(0, 0, 0, 8);
            btnRebootBurn.Click += async (s, e) => await RunRebootCommandAsync("reboot update", "Amlogic USB Burning Mode");

            var btnRebootAdnl = ModernTheme.CreateActionButton("🚀 Kích Hoạt Chế Độ ADNL (TPL/UBT v3 - preboot=adnl 1200)", ModernTheme.AccentPurple, Color.White, 38);
            btnRebootAdnl.Width = 445;
            btnRebootAdnl.Margin = new Padding(0, 0, 0, 8);
            btnRebootAdnl.Click += async (s, e) => await TriggerAdnlBootModeAsync();

            var btnRebootRecovery = ModernTheme.CreateActionButton("🛠️ Đưa Máy Vào Chế Độ RECOVERY (adb reboot recovery)", ModernTheme.AccentBlue, Color.White, 38);
            btnRebootRecovery.Width = 445;
            btnRebootRecovery.Margin = new Padding(0, 0, 0, 8);
            btnRebootRecovery.Click += async (s, e) => await RunRebootCommandAsync("reboot recovery", "Recovery Mode");

            var btnRebootNormal = ModernTheme.CreateActionButton("🔄 Khởi Động Lại Bình Thường (ADB / Fastboot / ADNL)", Color.FromArgb(30, 41, 59), ModernTheme.TextPrimary, 38);
            btnRebootNormal.Width = 445;
            btnRebootNormal.Click += async (s, e) => await RebootToNormalSystemAsync();

            flowModes.Controls.Add(btnUnlockDv9135);
            flowModes.Controls.Add(btnRebootFastboot);
            flowModes.Controls.Add(btnRebootBurn);
            flowModes.Controls.Add(btnRebootAdnl);
            flowModes.Controls.Add(btnRebootRecovery);
            flowModes.Controls.Add(btnRebootNormal);
            leftContent.Controls.Add(flowModes);

            // Right Card: Live U-Boot Environment Inspector & Editor
            var cardEnv = ModernTheme.CreateCardPanel("BẢNG ĐỌC & CHỈNH SỬA TRỰC TIẾP BIẾN MÔI TRƯỜNG U-BOOT (UBOOTENV)", ModernTheme.AccentCyan, out Panel rightContent);
            cardEnv.Dock = DockStyle.Fill;
            cardEnv.Margin = new Padding(6, 0, 0, 0);

            var topEnvBar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 38,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            var btnReadEnv = ModernTheme.CreateActionButton("📥 Đọc Toàn Bộ Biến U-Boot Hiện Tại", ModernTheme.AccentCyan, Color.Black, 32);
            btnReadEnv.Width = 250;
            btnReadEnv.Click += async (s, e) => await RefreshUbootEnvTableAsync();
            topEnvBar.Controls.Add(btnReadEnv);

            var bottomEnvBar = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 42,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(0, 6, 0, 0)
            };

            _txtCustomEnvKey = new TextBox
            {
                Width = 220,
                Text = "ubootenv.var.oem_lock",
                BackColor = ModernTheme.BgInput,
                ForeColor = ModernTheme.AccentCyan,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 9.5f),
                Margin = new Padding(0, 3, 6, 0)
            };

            _txtCustomEnvVal = new TextBox
            {
                Width = 150,
                Text = "unlock",
                BackColor = ModernTheme.BgInput,
                ForeColor = ModernTheme.AccentGreen,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 9.5f),
                Margin = new Padding(0, 3, 6, 0)
            };

            var btnWriteCustomEnv = ModernTheme.CreateActionButton("✍️ Ghi Biến U-Boot", ModernTheme.AccentGreen, Color.Black, 32);
            btnWriteCustomEnv.Width = 150;
            btnWriteCustomEnv.Click += async (s, e) => await WriteCustomUbootVarAsync();

            bottomEnvBar.Controls.Add(_txtCustomEnvKey);
            bottomEnvBar.Controls.Add(_txtCustomEnvVal);
            bottomEnvBar.Controls.Add(btnWriteCustomEnv);

            _gridUbootEnv = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = ModernTheme.BgInput,
                ForeColor = ModernTheme.TextPrimary,
                GridColor = ModernTheme.BorderColor,
                BorderStyle = BorderStyle.FixedSingle,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                EnableHeadersVisualStyles = false,
                RowTemplate = { Height = 26 }
            };
            _gridUbootEnv.ColumnHeadersDefaultCellStyle.BackColor = ModernTheme.BgCardHeader;
            _gridUbootEnv.ColumnHeadersDefaultCellStyle.ForeColor = ModernTheme.AccentCyan;
            _gridUbootEnv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            _gridUbootEnv.DefaultCellStyle.BackColor = ModernTheme.BgInput;
            _gridUbootEnv.DefaultCellStyle.ForeColor = ModernTheme.TextPrimary;
            _gridUbootEnv.DefaultCellStyle.Font = new Font("Consolas", 9.2f);

            _gridUbootEnv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tên Biến U-Boot / Hệ Thống", ReadOnly = true, FillWeight = 55f });
            _gridUbootEnv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Giá Trị Hiện Tại", ReadOnly = true, FillWeight = 45f });
            _gridUbootEnv.SelectionChanged += (s, e) =>
            {
                if (_gridUbootEnv.SelectedRows.Count > 0)
                {
                    _txtCustomEnvKey.Text = Convert.ToString(_gridUbootEnv.SelectedRows[0].Cells[0].Value);
                    _txtCustomEnvVal.Text = Convert.ToString(_gridUbootEnv.SelectedRows[0].Cells[1].Value);
                }
            };

            rightContent.Controls.Add(_gridUbootEnv);
            rightContent.Controls.Add(bottomEnvBar);
            rightContent.Controls.Add(topEnvBar);

            container.Controls.Add(cardModes, 0, 0);
            container.Controls.Add(cardEnv, 1, 0);
            return container;
        }

        // ====================================================================
        // TAB 5: RÚT APK HỆ THỐNG & NGHIÊN CỨU BOX MỚI (RESEARCH DUMPER)
        // ====================================================================

        private Panel BuildTab5ResearchDumper()
        {
            var container = new TableLayoutPanel
            {
                ColumnCount = 2,
                RowCount = 1,
                BackColor = ModernTheme.BgMain
            };
            container.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            container.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

            // Left Card: 1-Click System APK & Prop Extractor
            var cardDumper = ModernTheme.CreateCardPanel("1. HÚT TỰ ĐỘNG FILE APK HỆ THỐNG & THÔNG SỐ TỪ BOX MỚI", ModernTheme.AccentPurple, out Panel leftContent);
            cardDumper.Dock = DockStyle.Fill;
            cardDumper.Margin = new Padding(0, 0, 6, 0);

            var flowDump = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };

            flowDump.Controls.Add(new Label
            {
                Text = "Khi bạn cắm một dòng Box mới (ZTE, FPT, VNPT...) vào máy tính:\nCông cụ này sẽ tự động kéo toàn bộ các file cần thiết về thư mục Dumps/ để chúng ta\ndịch ngược thuật toán mật khẩu ADB và tìm lỗ hổng mở khóa Bootloader của dòng đó.",
                ForeColor = ModernTheme.TextSecondary,
                AutoSize = true,
                Margin = new Padding(0, 4, 0, 14)
            });

            _chkDumpSettings = new CheckBox
            {
                Text = "Kéo các file Settings.apk / TvSettings.apk (Chứa thuật toán kiểm tra Pass ADB)",
                Checked = true,
                ForeColor = ModernTheme.TextPrimary,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 10)
            };

            _chkDumpOem = new CheckBox
            {
                Text = "Kéo toàn bộ APK đặc thù của Nhà mạng & Hãng (Viettel, FPT, VNPT, ZTE, SDMC, OTA...)",
                Checked = true,
                ForeColor = ModernTheme.TextPrimary,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 10)
            };

            _chkDumpFrameworks = new CheckBox
            {
                Text = "Kéo thư viện điều khiển phần cứng (/vendor/framework/droidlogic.jar & build.prop)",
                Checked = true,
                ForeColor = ModernTheme.TextPrimary,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 18)
            };

            var btnRunFullDump = ModernTheme.CreateActionButton("🔬 BẮT ĐẦU HÚT DỮ LIỆU BOX ĐANG CẮM VỀ MÁY TÍNH", ModernTheme.AccentPurple, Color.White, 48);
            btnRunFullDump.Width = 480;
            btnRunFullDump.Click += async (s, e) => await ExecuteDeviceResearchDumpAsync();

            flowDump.Controls.Add(_chkDumpSettings);
            flowDump.Controls.Add(_chkDumpOem);
            flowDump.Controls.Add(_chkDumpFrameworks);
            flowDump.Controls.Add(btnRunFullDump);
            leftContent.Controls.Add(flowDump);

            // Right Card: Create New Device Profile Template
            var cardNewProf = ModernTheme.CreateCardPanel("2. TẠO NHANH HỒ SƠ THIẾT BỊ MỚI (NEW PROFILE BUILDER)", ModernTheme.AccentCyan, out Panel rightContent);
            cardNewProf.Dock = DockStyle.Fill;
            cardNewProf.Margin = new Padding(6, 0, 0, 0);

            var flowProf = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };

            flowProf.Controls.Add(new Label { Text = "Mã File Profile (viết liền không dấu, ví dụ: fpt_t650_box):", ForeColor = ModernTheme.TextSecondary, AutoSize = true });
            _txtNewProfileId = new TextBox
            {
                Width = 460,
                Text = "custom_new_box",
                BackColor = ModernTheme.BgInput,
                ForeColor = ModernTheme.AccentCyan,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 10.5f),
                Margin = new Padding(0, 4, 0, 12)
            };

            flowProf.Controls.Add(new Label { Text = "Tên hiển thị của dòng Box (ví dụ: FPT Play Box T650 - Android 11):", ForeColor = ModernTheme.TextSecondary, AutoSize = true });
            _txtNewProfileName = new TextBox
            {
                Width = 460,
                Text = "Dòng Box Mới (Tùy chỉnh)",
                BackColor = ModernTheme.BgInput,
                ForeColor = ModernTheme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10f),
                Margin = new Padding(0, 4, 0, 12)
            };

            flowProf.Controls.Add(new Label { Text = "Nhóm Nhà Mạng / Hãng (ví dụ: 1. VIETTEL TV360 / 2. FPT PLAY BOX):", ForeColor = ModernTheme.TextSecondary, AutoSize = true });
            _txtNewProfileOem = new TextBox
            {
                Width = 460,
                Text = "5. HỒ SƠ TỰ TẠO",
                BackColor = ModernTheme.BgInput,
                ForeColor = ModernTheme.AccentAmber,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10f),
                Margin = new Padding(0, 4, 0, 16)
            };

            var btnCreateProfile = ModernTheme.CreateActionButton("➕ Tạo Mới Profile & Chuyển Sang Tab 3 Chọn Gói Khóa", ModernTheme.AccentCyan, Color.Black, 44);
            btnCreateProfile.Width = 460;
            btnCreateProfile.Click += (s, e) => CreateNewCustomProfile();

            flowProf.Controls.Add(_txtNewProfileId);
            flowProf.Controls.Add(_txtNewProfileName);
            flowProf.Controls.Add(_txtNewProfileOem);
            flowProf.Controls.Add(btnCreateProfile);
            rightContent.Controls.Add(flowProf);

            container.Controls.Add(cardDumper, 0, 0);
            container.Controls.Add(cardNewProf, 1, 0);
            return container;
        }

        // ====================================================================
        // CORE OPERATIONS & EVENT HANDLERS
        // ====================================================================

        private void ReloadProfilesList()
        {
            _profiles = _profileManager.LoadAllProfiles();
            _cboActiveProfile.Items.Clear();
            foreach (var p in _profiles)
                _cboActiveProfile.Items.Add(p);

            if (_cboActiveProfile.Items.Count > 0)
                _cboActiveProfile.SelectedIndex = 0;
        }

        private async Task RefreshDevicesAndInspectAsync(bool logIfEmpty)
        {
            if (_isScanning) return;
            _isScanning = true;

            try
            {
                var devices = await Task.Run(() => _engine.ScanAllConnectedDevices());
                string previousId = (_cboConnectedDevices.SelectedItem as ConnectedDeviceItem)?.Id;

                _cboConnectedDevices.Items.Clear();
                foreach (var d in devices)
                    _cboConnectedDevices.Items.Add(d);

                if (devices.Count == 0)
                {
                    _lblConnectionBadge.Text = "● CHƯA KẾT NỐI THIẾT BỊ";
                    _lblConnectionBadge.BackColor = Color.FromArgb(45, 20, 30);
                    _lblConnectionBadge.ForeColor = ModernTheme.AccentRed;
                    ResetTelemetryBadges();
                    if (logIfEmpty)
                        AppendLog("Chưa phát hiện thiết bị qua USB ADB / Fastboot / ADNL.", "WARN");
                }
                else
                {
                    int idx = 0;
                    if (!string.IsNullOrEmpty(previousId))
                    {
                        int found = devices.FindIndex(x => x.Id == previousId);
                        if (found >= 0) idx = found;
                    }
                    _cboConnectedDevices.SelectedIndex = idx;
                }
            }
            finally
            {
                _isScanning = false;
            }
        }

        private async Task InspectSelectedDeviceAsync(ConnectedDeviceItem item)
        {
            if (item == null) return;

            if (item.Mode == ConnectionType.Unauthorized)
            {
                _lblConnectionBadge.Text = "⚠ CHỜ BẤM CHO PHÉP TRÊN TV";
                _lblConnectionBadge.BackColor = Color.FromArgb(60, 45, 10);
                _lblConnectionBadge.ForeColor = ModernTheme.AccentAmber;
                _stepProgress.SetStep(1, "Hãy nhìn lên màn hình TV và bấm 'Luôn cho phép từ máy tính này' (Allow USB Debugging).", true);
                return;
            }

            _lblConnectionBadge.Text = $"● ĐÃ KẾT NỐI: {item.Mode.ToString().ToUpperInvariant()}";
            _lblConnectionBadge.BackColor = Color.FromArgb(12, 50, 38);
            _lblConnectionBadge.ForeColor = ModernTheme.AccentGreen;

            _currentTelemetry = await Task.Run(() => _engine.InspectDevice(item));

            _badgeModel.ValueText = $"{_currentTelemetry.Manufacturer} {_currentTelemetry.Model}".Trim();
            _badgeModel.IndicatorColor = ModernTheme.AccentCyan;

            _badgeAndroid.ValueText = _currentTelemetry.AndroidVersion != "---"
                ? $"Android {_currentTelemetry.AndroidVersion} (SDK {_currentTelemetry.SdkLevel})"
                : "---";
            _badgeAndroid.IndicatorColor = ModernTheme.AccentGreen;

            _badgeSoC.ValueText = $"{_currentTelemetry.SoCPlatform} ({_currentTelemetry.Board})";
            _badgeSoC.IndicatorColor = ModernTheme.AccentBlue;

            _badgeSerialMac.ValueText = $"{_currentTelemetry.SerialNumber} | {_currentTelemetry.MacAddress}";
            _badgeSerialMac.IndicatorColor = ModernTheme.AccentPurple;

            _badgeSelinux.ValueText = _currentTelemetry.SelinuxStatus;
            _badgeSelinux.IndicatorColor = _currentTelemetry.SelinuxStatus.Equals("Permissive", StringComparison.OrdinalIgnoreCase)
                ? ModernTheme.AccentGreen
                : ModernTheme.AccentAmber;

            _badgeBootloader.ValueText = $"{_currentTelemetry.BootloaderStatus} ({_currentTelemetry.BootloaderGeneration})";
            _badgeBootloader.IndicatorColor = _currentTelemetry.BootloaderStatus.Contains("UNLOCK")
                ? ModernTheme.AccentGreen
                : ModernTheme.AccentRed;

            // Auto-match device profile
            var matched = _profileManager.MatchProfile(_currentTelemetry, _profiles);
            if (matched != null)
            {
                _cboActiveProfile.SelectedItem = matched;
                _stepProgress.SetStep(1, $"Đã nhận diện {_currentTelemetry.Model} (Android {_currentTelemetry.AndroidVersion}) -> Tự động chọn Profile: {matched.Name}");
            }

            // Auto-populate Keygen inputs if SN/MAC available
            if (_currentTelemetry.SerialNumber != "---")
                _txtKeygenSn.Text = _currentTelemetry.SerialNumber;
            if (_currentTelemetry.MacAddress != "---")
                _txtKeygenMac.Text = _currentTelemetry.MacAddress;
        }

        private void ResetTelemetryBadges()
        {
            _badgeModel.ValueText = "---";
            _badgeModel.IndicatorColor = ModernTheme.TextMuted;
            _badgeAndroid.ValueText = "---";
            _badgeAndroid.IndicatorColor = ModernTheme.TextMuted;
            _badgeSoC.ValueText = "---";
            _badgeSoC.IndicatorColor = ModernTheme.TextMuted;
            _badgeSerialMac.ValueText = "---";
            _badgeSerialMac.IndicatorColor = ModernTheme.TextMuted;
            _badgeSelinux.ValueText = "---";
            _badgeSelinux.IndicatorColor = ModernTheme.TextMuted;
            _badgeBootloader.ValueText = "---";
            _badgeBootloader.IndicatorColor = ModernTheme.TextMuted;
        }

        private async Task ConnectLanAdbAsync(string ipInput)
        {
            string ip = (ipInput ?? "").Trim();
            if (string.IsNullOrWhiteSpace(ip)) return;
            if (!ip.Contains(":")) ip += ":5555";

            AppendLog($"Đang kết nối ADB qua mạng LAN tới {ip}...", "CMD");
            string res = await Task.Run(() => _engine.RunProcess(_engine.AdbExe, $"connect {ip}", 8000));
            AppendLog($"Kết quả kết nối LAN: {res}", res.IndexOf("connected", StringComparison.OrdinalIgnoreCase) >= 0 ? "OK" : "WARN");
            await RefreshDevicesAndInspectAsync(false);
        }

        private async Task ExecuteOneClickSoftUnlockAsync()
        {
            if (!EnsureAdbConnected(out string transportId)) return;
            if (_selectedProfile == null) return;

            _progressBar.Value = 15;
            _stepProgress.SetStep(1, $"Bắt đầu Mở khóa Phần mềm 1-Click theo hồ sơ: {_selectedProfile.Name}...");
            AppendLog($"=== BẮT ĐẦU MỞ KHÓA PHẦN MỀM 1-CLICK ({_selectedProfile.Name}) ===", "CMD");

            await Task.Run(() =>
            {
                Invoke(new Action(() =>
                {
                    _stepProgress.SetStep(2, "Đang vô hiệu hóa các gói ứng dụng khóa của nhà mạng (Debloat)...");
                    _progressBar.Value = 40;
                }));

                foreach (string pkg in _selectedProfile.CarrierBloatPackages)
                {
                    _engine.SetPackageEnabledState(transportId, pkg, false);
                }

                Invoke(new Action(() =>
                {
                    _stepProgress.SetStep(3, "Đang chặn các dịch vụ tự động cập nhật OTA & cài đặt gói APK trong thư mục Apps/...");
                    _progressBar.Value = 75;
                }));

                foreach (string otaPkg in _selectedProfile.OtaUpdatePackages)
                {
                    _engine.SetPackageEnabledState(transportId, otaPkg, false);
                }

                // Install any APKs placed inside Apps/Launchers and Apps/DefaultApps
                string[] apkFiles = Directory.GetFiles(_profileManager.AppsDirectory, "*.apk", SearchOption.AllDirectories);
                foreach (string apk in apkFiles)
                {
                    _engine.InstallApkFile(transportId, apk);
                }
            });

            _progressBar.Value = 100;
            _stepProgress.SetStep(5, "Hoàn tất Mở khóa Phần mềm 1-Click! Các ứng dụng nhà mạng & OTA đã bị khóa.");
            AppendLog("=== HOÀN TẤT MỞ KHÓA PHẦN MỀM 1-CLICK THÀNH CÔNG ===", "OK");
        }

        private async Task ExecuteOneClickBootloaderUnlockAsync()
        {
            if (!EnsureAdbConnected(out string transportId)) return;

            _progressBar.Value = 10;
            bool ok = await Task.Run(() =>
                _engine.ApplyDv9135BootloaderUnlockOffline(transportId, _currentTelemetry, (step, msg) =>
                {
                    Invoke(new Action(() =>
                    {
                        _stepProgress.SetStep(step, msg);
                        _progressBar.Value = Math.Min(90, step * 22);
                    }));
                })
            );

            _progressBar.Value = 100;
            if (ok)
            {
                _stepProgress.SetStep(5, "Mở khóa U-Boot Environment hoàn tất (oem_lock=unlock, avb2=0)!");
                AppendLog("=== ĐÃ MỞ KHÓA U-BOOT ENVIRONMENT THÀNH CÔNG ===", "OK");
                await InspectSelectedDeviceAsync(_cboConnectedDevices.SelectedItem as ConnectedDeviceItem);
            }
            else
            {
                _stepProgress.SetStep(3, "Mở khóa U-Boot gặp lỗi — Kiểm tra chi tiết dưới Nhật ký.", true);
            }
        }

        private async Task InstallAndTriggerCompanionApkAsync()
        {
            if (!EnsureAdbConnected(out string transportId)) return;

            _stepProgress.SetStep(2, "Đang cài đặt và kích hoạt dv9135.apk (Companion App)...");
            await Task.Run(() =>
            {
                _engine.EnsureBinariesExtracted();
                if (_engine.InstallApkFile(transportId, _engine.CompanionApkPath))
                {
                    _engine.RunAdb(transportId, "shell monkey -p com.dvt.dv9135 -c android.intent.category.LAUNCHER 1", 6000);
                    _engine.RunAdb(transportId, "shell am broadcast -a com.dvt.dv9135.CMD --es cmd enable_unknown_sources", 6000);
                }
            });
            _stepProgress.SetStep(5, "Đã cài đặt và kích hoạt dv9135.apk trên thiết bị.");
        }

        private async Task ScanDevicePackagesAsync()
        {
            if (!EnsureAdbConnected(out string transportId)) return;

            _stepProgress.SetStep(2, "Đang quét danh sách toàn bộ gói ứng dụng trên Box...");
            _progressBar.Value = 35;

            _allPackages = await Task.Run(() => _engine.GetDevicePackages(transportId, _selectedProfile));
            FilterPackagesGrid();

            _progressBar.Value = 100;
            _stepProgress.SetStep(5, $"Đã quét xong {_allPackages.Count} gói ứng dụng trên thiết bị.");
            AppendLog($"Đã tải danh sách {_allPackages.Count} gói ứng dụng từ thiết bị.", "OK");
        }

        private void FilterPackagesGrid()
        {
            string q = (_txtSearchPkg.Text ?? "").Trim().ToLowerInvariant();
            string catFilter = _cboFilterCategory.SelectedIndex <= 0 ? "" : _cboFilterCategory.SelectedItem.ToString();

            _gridPackages.Rows.Clear();
            int shown = 0;
            int selectedCount = 0;

            foreach (var p in _allPackages)
            {
                if (!string.IsNullOrEmpty(q) && !p.PackageName.ToLowerInvariant().Contains(q) && !p.ApkPath.ToLowerInvariant().Contains(q))
                    continue;

                if (!string.IsNullOrEmpty(catFilter) && p.Category != catFilter)
                    continue;

                int rowIdx = _gridPackages.Rows.Add(p.IsSelected, p.Category, p.PackageName, p.Status, p.ApkPath);
                var row = _gridPackages.Rows[rowIdx];
                row.Tag = p;

                if (p.Category.StartsWith("[ĐỎ]"))
                    row.Cells[1].Style.ForeColor = ModernTheme.AccentRed;
                else if (p.Category.StartsWith("[CAM]"))
                    row.Cells[1].Style.ForeColor = ModernTheme.AccentAmber;
                else if (p.Category.StartsWith("[XANH]"))
                    row.Cells[1].Style.ForeColor = ModernTheme.AccentGreen;
                else if (p.Category.StartsWith("[XANH DƯƠNG]"))
                    row.Cells[1].Style.ForeColor = ModernTheme.AccentCyan;

                if (p.Status.Contains("Disabled"))
                    row.Cells[3].Style.ForeColor = ModernTheme.TextMuted;
                else
                    row.Cells[3].Style.ForeColor = ModernTheme.AccentGreen;

                shown++;
                if (p.IsSelected) selectedCount++;
            }

            _lblPkgSummary.Text = $"Hiển thị: {shown}/{_allPackages.Count} gói | Đang tích chọn: {selectedCount} gói";
        }

        private async Task BatchProcessSelectedPackagesAsync(string action)
        {
            if (!EnsureAdbConnected(out string transportId)) return;

            var targets = new List<PackageEntry>();
            foreach (DataGridViewRow row in _gridPackages.Rows)
            {
                bool isChecked = Convert.ToBoolean(row.Cells[0].Value);
                if (isChecked && row.Tag is PackageEntry pe)
                {
                    pe.IsSelected = true;
                    targets.Add(pe);
                }
            }

            if (targets.Count == 0)
            {
                AppendLog("Bạn chưa tích chọn gói ứng dụng nào trong bảng.", "WARN");
                return;
            }

            _stepProgress.SetStep(3, $"Đang thực hiện {action} trên {targets.Count} gói ứng dụng đã chọn...");
            await Task.Run(() =>
            {
                foreach (var pkg in targets)
                {
                    if (action == "DISABLE")
                        _engine.SetPackageEnabledState(transportId, pkg.PackageName, false);
                    else if (action == "ENABLE")
                        _engine.SetPackageEnabledState(transportId, pkg.PackageName, true);
                    else if (action == "UNINSTALL")
                        _engine.UninstallPackageForUser0(transportId, pkg.PackageName);
                }
            });

            await ScanDevicePackagesAsync();
        }

        private void SaveCheckedPackagesToCurrentProfile()
        {
            if (_selectedProfile == null) return;

            var checkedPkgs = new List<string>();
            foreach (DataGridViewRow row in _gridPackages.Rows)
            {
                if (Convert.ToBoolean(row.Cells[0].Value) && row.Tag is PackageEntry pe)
                {
                    checkedPkgs.Add(pe.PackageName);
                }
            }

            if (checkedPkgs.Count == 0)
            {
                AppendLog("Hãy tích chọn các gói cần khóa trước khi bấm Lưu vào Profile.", "WARN");
                return;
            }

            foreach (string pkg in checkedPkgs)
            {
                if (!_selectedProfile.CarrierBloatPackages.Contains(pkg, StringComparer.OrdinalIgnoreCase))
                    _selectedProfile.CarrierBloatPackages.Add(pkg);
            }

            _profileManager.SaveProfile(_selectedProfile);
            UpdateProfilePreviewUI(_selectedProfile);
            AppendLog($"Đã lưu {checkedPkgs.Count} gói vào Profile '{_selectedProfile.Name}' (Profiles/{_selectedProfile.Id}.json).", "OK");
        }

        private async Task PromptAndInstallApkFilesAsync()
        {
            if (!EnsureAdbConnected(out string transportId)) return;

            using (var ofd = new OpenFileDialog
            {
                Title = "Chọn một hoặc nhiều file APK để cài vào Box",
                Filter = "Android Package (*.apk)|*.apk",
                Multiselect = true
            })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    await InstallMultipleApkFilesAsync(transportId, ofd.FileNames);
                }
            }
        }

        private async Task InstallMultipleApkFilesAsync(string transportId, string[] files)
        {
            _stepProgress.SetStep(3, $"Đang cài đặt {files.Length} file APK vào thiết bị...");
            await Task.Run(() =>
            {
                foreach (string f in files)
                {
                    _engine.InstallApkFile(transportId, f);
                }
            });
            _stepProgress.SetStep(5, $"Hoàn tất cài đặt {files.Length} file APK.");
        }

        private void OnFormDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }

        private async void OnFormDragDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            string[] apks = files.Where(f => f.EndsWith(".apk", StringComparison.OrdinalIgnoreCase)).ToArray();

            if (apks.Length > 0 && EnsureAdbConnected(out string transportId))
            {
                await InstallMultipleApkFilesAsync(transportId, apks);
            }
        }

        private async Task RefreshUbootEnvTableAsync()
        {
            if (!EnsureAdbConnected(out string transportId)) return;

            var dict = await Task.Run(() => _engine.ReadUbootVariables(transportId));
            _gridUbootEnv.Rows.Clear();
            foreach (var kv in dict)
            {
                int idx = _gridUbootEnv.Rows.Add(kv.Key, kv.Value);
                if (kv.Value.Equals("unlock", StringComparison.OrdinalIgnoreCase) || kv.Value == "0" || kv.Value == "10100000")
                    _gridUbootEnv.Rows[idx].Cells[1].Style.ForeColor = ModernTheme.AccentGreen;
            }
            AppendLog("Đã đọc bảng biến môi trường U-Boot từ thiết bị.", "OK");
        }

        private async Task WriteCustomUbootVarAsync()
        {
            if (!EnsureAdbConnected(out string transportId)) return;
            string k = _txtCustomEnvKey.Text.Trim();
            string v = _txtCustomEnvVal.Text.Trim();
            if (string.IsNullOrEmpty(k)) return;

            bool isAndroid11 = _currentTelemetry.AndroidVersion.Contains("11");
            await Task.Run(() => _engine.WriteSingleBootEnv(transportId, isAndroid11, k, v));
            await RefreshUbootEnvTableAsync();
        }

        private async Task RunRebootCommandAsync(string adbCommand, string modeName)
        {
            if (!EnsureAdbConnected(out string transportId)) return;
            AppendLog($"Đang chuyển thiết bị sang {modeName} ({adbCommand})...", "CMD");
            await Task.Run(() => _engine.RunAdb(transportId, adbCommand, 8000));
            _stepProgress.SetStep(4, $"Đã gửi lệnh chuyển sang {modeName}.");
        }

        private async Task TriggerAdnlBootModeAsync()
        {
            if (!EnsureAdbConnected(out string transportId)) return;
            bool isAndroid11 = _currentTelemetry.AndroidVersion.Contains("11");

            AppendLog("Đang cấu hình biến preboot=adnl 1200 và khởi động vào chế độ ADNL...", "CMD");
            await Task.Run(() =>
            {
                _engine.WriteSingleBootEnv(transportId, isAndroid11, "ubootenv.var.preboot", "adnl 1200;setenv preboot;saveenv;");
                _engine.WriteSingleBootEnv(transportId, isAndroid11, "ubootenv.var.reboot_mode", "cold_boot");
                _engine.RunAdb(transportId, "reboot", 6000);
            });
            _stepProgress.SetStep(5, "Đã gửi lệnh khởi động vào chế độ Amlogic ADNL Burn Mode.");
        }

        private async Task RebootToNormalSystemAsync()
        {
            var item = _cboConnectedDevices.SelectedItem as ConnectedDeviceItem;
            if (item == null) return;

            await Task.Run(() =>
            {
                if (item.Mode == ConnectionType.Fastboot)
                    _engine.RunFastboot(item.Id, "reboot", 6000);
                else if (item.Mode == ConnectionType.Adnl)
                    _engine.RunAdnl("oem \"reset\"", 6000);
                else
                    _engine.RunAdb(item.Id, "reboot", 6000);
            });
            AppendLog("Đã gửi lệnh khởi động lại thiết bị.", "OK");
        }

        private async Task ExecuteDeviceResearchDumpAsync()
        {
            if (!EnsureAdbConnected(out string transportId)) return;

            _stepProgress.SetStep(2, "Đang hút file APK hệ thống, getprop và ContentProvider từ Box về máy tính...");
            _progressBar.Value = 30;

            bool pullSettings = _chkDumpSettings.Checked;
            bool pullOem = _chkDumpOem.Checked;
            bool pullFw = _chkDumpFrameworks.Checked;

            string outFolder = await Task.Run(() =>
                _engine.DumpDeviceResearchPackage(transportId, _currentTelemetry, _profileManager.DumpsDirectory, pullSettings, pullOem, pullFw)
            );

            _progressBar.Value = 100;
            _stepProgress.SetStep(5, $"Đã lưu trọn bộ dữ liệu nghiên cứu Box tại: {outFolder}");
            Process.Start("explorer.exe", outFolder);
        }

        private void CreateNewCustomProfile()
        {
            string id = _txtNewProfileId.Text.Trim();
            string name = _txtNewProfileName.Text.Trim();
            string oem = _txtNewProfileOem.Text.Trim();

            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name))
            {
                AppendLog("Vui lòng nhập Mã File và Tên hiển thị cho Profile mới.", "WARN");
                return;
            }

            var newProf = new DeviceProfile
            {
                Id = id,
                Name = name,
                OemGroup = oem,
                Description = $"Hồ sơ tùy chỉnh cho {name} (Tạo ngày {DateTime.Now:dd/MM/yyyy}).",
                MatchKeywords = new List<string> { _currentTelemetry.Model != "---" ? _currentTelemetry.Model : id }
            };

            _profileManager.SaveProfile(newProf);
            ReloadProfilesList();
            _cboActiveProfile.SelectedItem = _profiles.FirstOrDefault(p => p.Id == id);
            SwitchTab(2);
            AppendLog($"Đã tạo Profile mới '{name}'. Hãy quét ứng dụng ở Tab 3, tích chọn các gói khóa và bấm 'Lưu vào Profile'.", "OK");
        }

        private bool EnsureAdbConnected(out string transportId)
        {
            transportId = string.Empty;
            var item = _cboConnectedDevices.SelectedItem as ConnectedDeviceItem;
            if (item == null || (item.Mode != ConnectionType.AdbUsb && item.Mode != ConnectionType.AdbLan))
            {
                AppendLog("Chưa có thiết bị kết nối ở chế độ ADB! Hãy cắm cáp USB hoặc kết nối IP LAN trước.", "ERR");
                _stepProgress.SetStep(1, "Lỗi: Chưa tìm thấy thiết bị ở chế độ ADB.", true);
                return false;
            }

            transportId = item.Id;
            return true;
        }

        private void AppendLogThreadSafe(string message, string level)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => AppendLog(message, level)));
            }
            else
            {
                AppendLog(message, level);
            }
        }

        private void AppendLog(string message, string level = "INFO")
        {
            if (_rtbConsole == null || _rtbConsole.IsDisposed) return;

            Color color = ModernTheme.TextPrimary;
            if (level == "OK") color = ModernTheme.AccentGreen;
            else if (level == "WARN") color = ModernTheme.AccentAmber;
            else if (level == "ERR") color = ModernTheme.AccentRed;
            else if (level == "CMD") color = ModernTheme.AccentCyan;

            string line = $"[{DateTime.Now:HH:mm:ss}] [{level}] {message}{Environment.NewLine}";
            _rtbConsole.SelectionStart = _rtbConsole.TextLength;
            _rtbConsole.SelectionLength = 0;
            _rtbConsole.SelectionColor = color;
            _rtbConsole.AppendText(line);
            _rtbConsole.ScrollToCaret();
        }
    }
}
