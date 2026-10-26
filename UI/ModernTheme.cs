using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AllInOne_TVBox_Suite.UI
{
    public static class ModernTheme
    {
        public static readonly Color BgMain = Color.FromArgb(11, 15, 25);        // #0B0F19
        public static readonly Color BgSidebar = Color.FromArgb(15, 23, 42);     // #0F172A
        public static readonly Color BgCard = Color.FromArgb(17, 24, 39);        // #111827
        public static readonly Color BgCardHeader = Color.FromArgb(30, 41, 59);  // #1E293B
        public static readonly Color BgInput = Color.FromArgb(2, 6, 23);         // #020617
        public static readonly Color BorderColor = Color.FromArgb(51, 65, 85);   // #334155

        public static readonly Color AccentCyan = Color.FromArgb(6, 182, 212);   // #06B6D4
        public static readonly Color AccentBlue = Color.FromArgb(59, 130, 246);  // #3B82F6
        public static readonly Color AccentGreen = Color.FromArgb(16, 185, 129); // #10B981
        public static readonly Color AccentAmber = Color.FromArgb(245, 158, 11); // #F59E0B
        public static readonly Color AccentRed = Color.FromArgb(244, 63, 94);    // #F43F5E
        public static readonly Color AccentPurple = Color.FromArgb(139, 92, 246);// #8B5CF6

        public static readonly Color TextPrimary = Color.FromArgb(248, 250, 252);
        public static readonly Color TextSecondary = Color.FromArgb(148, 163, 184);
        public static readonly Color TextMuted = Color.FromArgb(100, 116, 139);

        public static Button CreateActionButton(string text, Color backColor, Color foreColor, int height = 40)
        {
            var btn = new Button
            {
                Text = text,
                BackColor = backColor,
                ForeColor = foreColor,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
                Height = height,
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = ControlPaint.Light(backColor, 0.15f);
            btn.FlatAppearance.MouseOverBackColor = ControlPaint.Light(backColor, 0.12f);
            btn.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(backColor, 0.1f);
            return btn;
        }

        public static Panel CreateCardPanel(string title, Color accentColor, out Panel contentArea)
        {
            var outer = new Panel
            {
                BackColor = BgCard,
                Padding = new Padding(1)
            };
            outer.Paint += (s, e) =>
            {
                using (var pen = new Pen(BorderColor, 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, outer.Width - 1, outer.Height - 1);
                }
                using (var accentBrush = new SolidBrush(accentColor))
                {
                    e.Graphics.FillRectangle(accentBrush, 0, 0, 4, outer.Height);
                }
            };

            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 36,
                BackColor = BgCardHeader,
                Padding = new Padding(14, 0, 10, 0)
            };

            var lblTitle = new Label
            {
                Text = title,
                Dock = DockStyle.Fill,
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };
            header.Controls.Add(lblTitle);

            contentArea = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BgCard,
                Padding = new Padding(14, 10, 14, 10)
            };

            outer.Controls.Add(contentArea);
            outer.Controls.Add(header);
            return outer;
        }
    }

    public class TelemetryBadgeControl : Control
    {
        private string _caption = "THÔNG SỐ";
        private string _valueText = "---";
        private Color _indicatorColor = ModernTheme.TextMuted;

        public string Caption
        {
            get => _caption;
            set { _caption = value; Invalidate(); }
        }

        public string ValueText
        {
            get => _valueText;
            set { _valueText = value; Invalidate(); }
        }

        public Color IndicatorColor
        {
            get => _indicatorColor;
            set { _indicatorColor = value; Invalidate(); }
        }

        public TelemetryBadgeControl()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            Size = new Size(180, 54);
            BackColor = ModernTheme.BgCard;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (var bgBrush = new SolidBrush(ModernTheme.BgCard))
            {
                g.FillRectangle(bgBrush, 0, 0, Width, Height);
            }

            using (var borderPen = new Pen(ModernTheme.BorderColor, 1))
            {
                g.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
            }

            using (var topBarBrush = new SolidBrush(_indicatorColor))
            {
                g.FillRectangle(topBarBrush, 0, 0, Width, 3);
                g.FillEllipse(topBarBrush, 10, 11, 7, 7);
            }

            using (var capFont = new Font("Segoe UI", 7.5f, FontStyle.Bold))
            using (var capBrush = new SolidBrush(ModernTheme.TextSecondary))
            {
                g.DrawString(_caption, capFont, capBrush, new PointF(21, 7));
            }

            using (var valFont = new Font("Consolas", 10.2f, FontStyle.Bold))
            using (var valBrush = new SolidBrush(ModernTheme.TextPrimary))
            {
                var rect = new RectangleF(10, 25, Width - 16, 24);
                var sf = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
                g.DrawString(_valueText, valFont, valBrush, rect, sf);
            }
        }
    }

    public class StepProgressControl : Control
    {
        private readonly string[] _steps = new[]
        {
            "B1: Nhận diện Thiết bị",
            "B2: Chuẩn bị Môi trường",
            "B3: Thực thi Kịch bản",
            "B4: Xác minh & Hoàn tất"
        };

        private int _currentStep = 0; // 0 = idle, 1..4 = active step, 5 = all completed
        private bool _hasError = false;
        private string _statusMessage = "Sẵn sàng — Cắm cáp USB hoặc kết nối ADB LAN để bắt đầu.";

        public int CurrentStep
        {
            get => _currentStep;
            set { _currentStep = value; Invalidate(); }
        }

        public bool HasError
        {
            get => _hasError;
            set { _hasError = value; Invalidate(); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; Invalidate(); }
        }

        public StepProgressControl()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            Height = 62;
            BackColor = ModernTheme.BgCard;
        }

        public void SetStep(int step, string message, bool isError = false)
        {
            _currentStep = step;
            _statusMessage = message;
            _hasError = isError;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (var bg = new SolidBrush(ModernTheme.BgCard))
                g.FillRectangle(bg, 0, 0, Width, Height);

            using (var pen = new Pen(ModernTheme.BorderColor, 1))
                g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);

            int count = _steps.Length;
            int leftPad = 24;
            int rightPad = 24;
            int usableWidth = Math.Max(200, Width - leftPad - rightPad);
            int stepSpacing = usableWidth / count;

            for (int i = 0; i < count; i++)
            {
                int stepIndex = i + 1;
                int x = leftPad + i * stepSpacing;
                int y = 10;

                Color circleColor = ModernTheme.BorderColor;
                string badgeText = stepIndex.ToString();

                if (_currentStep > stepIndex || _currentStep == 5)
                {
                    circleColor = ModernTheme.AccentGreen;
                    badgeText = "✓";
                }
                else if (_currentStep == stepIndex)
                {
                    circleColor = _hasError ? ModernTheme.AccentRed : ModernTheme.AccentCyan;
                    badgeText = _hasError ? "!" : stepIndex.ToString();
                }

                // Connector line to next step
                if (i < count - 1)
                {
                    int nextX = leftPad + (i + 1) * stepSpacing;
                    Color lineColor = (_currentStep > stepIndex || _currentStep == 5)
                        ? ModernTheme.AccentGreen
                        : ModernTheme.BorderColor;
                    using (var linePen = new Pen(lineColor, 2))
                    {
                        g.DrawLine(linePen, x + 150, y + 11, nextX - 8, y + 11);
                    }
                }

                using (var brush = new SolidBrush(circleColor))
                {
                    g.FillEllipse(brush, x, y, 22, 22);
                }

                using (var numFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
                using (var numBrush = new SolidBrush( Color.White))
                {
                    var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString(badgeText, numFont, numBrush, new RectangleF(x, y, 22, 22), sf);
                }

                Color labelColor = (_currentStep >= stepIndex) ? ModernTheme.TextPrimary : ModernTheme.TextSecondary;
                using (var lblFont = new Font("Segoe UI", 8.5f, _currentStep == stepIndex ? FontStyle.Bold : FontStyle.Regular))
                using (var lblBrush = new SolidBrush(labelColor))
                {
                    g.DrawString(_steps[i], lblFont, lblBrush, new PointF(x + 27, y + 3));
                }
            }

            // Bottom status bar text
            Color statusColor = _hasError ? ModernTheme.AccentRed : (_currentStep == 5 ? ModernTheme.AccentGreen : ModernTheme.AccentCyan);
            using (var stFont = new Font("Consolas", 8.8f, FontStyle.Bold))
            using (var stBrush = new SolidBrush(statusColor))
            {
                g.DrawString("▶ " + _statusMessage, stFont, stBrush, new PointF(leftPad, 38));
            }
        }
    }

    public class SidebarNavButton : Control
    {
        public int TabIndexValue { get; set; }
        public string BadgeNumber { get; set; } = "1";
        public string Title { get; set; } = "Tab";
        public string Subtitle { get; set; } = "";

        private bool _isActive;
        private bool _isHovered;

        public bool IsActive
        {
            get => _isActive;
            set { _isActive = value; Invalidate(); }
        }

        public SidebarNavButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            Height = 58;
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _isHovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _isHovered = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Color bg = _isActive
                ? Color.FromArgb(30, 41, 59)
                : (_isHovered ? Color.FromArgb(22, 31, 48) : ModernTheme.BgSidebar);

            using (var brush = new SolidBrush(bg))
                g.FillRectangle(brush, 0, 0, Width, Height);

            if (_isActive)
            {
                using (var barBrush = new SolidBrush(ModernTheme.AccentCyan))
                    g.FillRectangle(barBrush, 0, 6, 4, Height - 12);
            }

            Color badgeBg = _isActive ? ModernTheme.AccentCyan : Color.FromArgb(51, 65, 85);
            using (var bBrush = new SolidBrush(badgeBg))
                g.FillEllipse(bBrush, 14, 15, 28, 28);

            using (var bFont = new Font("Segoe UI", 9.5f, FontStyle.Bold))
            using (var bTextBrush = new SolidBrush(_isActive ? Color.Black : ModernTheme.TextPrimary))
            {
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString(BadgeNumber, bFont, bTextBrush, new RectangleF(14, 15, 28, 28), sf);
            }

            using (var titleFont = new Font("Segoe UI", 9.5f, FontStyle.Bold))
            using (var titleBrush = new SolidBrush(_isActive ? ModernTheme.TextPrimary : ModernTheme.TextSecondary))
            {
                g.DrawString(Title, titleFont, titleBrush, new PointF(50, 10));
            }

            using (var subFont = new Font("Segoe UI", 7.8f, FontStyle.Regular))
            using (var subBrush = new SolidBrush(_isActive ? ModernTheme.AccentCyan : ModernTheme.TextMuted))
            {
                g.DrawString(Subtitle, subFont, subBrush, new PointF(50, 31));
            }
        }
    }
}
