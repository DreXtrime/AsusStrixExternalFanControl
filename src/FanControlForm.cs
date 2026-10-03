using System;
using System.Drawing;
using System.Windows.Forms;

namespace AsusFanControl;

/// <summary>
/// Small popup form for manual fan speed control.
/// Opens near the system tray when the tray icon is clicked.
/// </summary>
internal class FanControlForm : Form
{
    private readonly Action<int> _onSpeedChanged;
    private readonly TrackBar    _slider;
    private readonly Label       _percentLabel;
    private readonly Button      _applyButton;
    private readonly Button      _diagButton;
    private readonly CheckBox    _instantCheck;
    private int _lastApplied;

    public FanControlForm(int currentPercent, Action<int> onSpeedChanged)
    {
        _onSpeedChanged = onSpeedChanged;
        _lastApplied    = currentPercent;

        // Form setup
        Text            = "ASUS Fan Control";
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox     = false;
        MinimizeBox     = false;
        StartPosition   = FormStartPosition.Manual;
        Size            = new Size(300, 190);
        BackColor       = Color.FromArgb(30, 30, 30);
        ForeColor       = Color.White;
        TopMost         = true;
        ShowInTaskbar   = false;

        PositionNearTray();

        // Title label
        var title = new Label
        {
            Text      = "External Fan Speed",
            ForeColor = Color.FromArgb(180, 180, 180),
            Font      = new Font("Segoe UI", 9f),
            Location  = new Point(12, 12),
            AutoSize  = true
        };

        // Percentage label
        _percentLabel = new Label
        {
            Text      = $"{currentPercent}%",
            ForeColor = Color.White,
            Font      = new Font("Segoe UI", 24f, FontStyle.Bold),
            Location  = new Point(12, 32),
            Size      = new Size(80, 45),
            TextAlign = ContentAlignment.MiddleLeft
        };

        // Slider
        _slider = new TrackBar
        {
            Minimum     = 0,
            Maximum     = 100,
            Value       = currentPercent,
            TickFrequency= 10,
            SmallChange = 1,
            LargeChange = 10,
            Location    = new Point(8, 80),
            Size        = new Size(272, 32),
            BackColor   = Color.FromArgb(30, 30, 30)
        };
        _slider.ValueChanged += Slider_ValueChanged;

        // Instant apply checkbox
        _instantCheck = new CheckBox
        {
            Text      = "Apply instantly",
            Checked   = false,
            ForeColor = Color.FromArgb(180, 180, 180),
            Location  = new Point(12, 118),
            AutoSize  = true,
            BackColor = Color.Transparent
        };

        // Apply button
        _applyButton = new Button
        {
            Text      = "Apply",
            Location  = new Point(170, 113),
            Size      = new Size(110, 28),
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font      = new Font("Segoe UI", 9f)
        };
        _applyButton.FlatAppearance.BorderSize = 0;
        _applyButton.Click += (_, _) => Apply();

        // Diagnostic button (hidden by default, Ctrl+D to show)
        _diagButton = new Button
        {
            Text      = "Scan Registers",
            Location  = new Point(12, 148),
            Size      = new Size(268, 24),
            BackColor = Color.FromArgb(60, 60, 60),
            ForeColor = Color.FromArgb(180, 180, 180),
            FlatStyle = FlatStyle.Flat,
            Font      = new Font("Segoe UI", 8f),
            Visible   = false
        };
        _diagButton.FlatAppearance.BorderSize = 0;
        _diagButton.Click += DiagButton_Click;

        Controls.AddRange([title, _percentLabel, _slider, _instantCheck, _applyButton, _diagButton]);

        KeyPreview  = true;
        KeyDown    += FanControlForm_KeyDown;
        Deactivate += (_, _) => { if (!_diagButton.Visible) Hide(); };
    }

    private void Slider_ValueChanged(object? sender, EventArgs e)
    {
        _percentLabel.Text = $"{_slider.Value}%";
        if (_instantCheck.Checked)
            Apply();
    }

    private void Apply()
    {
        _lastApplied = _slider.Value;
        _onSpeedChanged(_slider.Value);
    }

    private void FanControlForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Control && e.KeyCode == Keys.D)
        {
            _diagButton.Visible = !_diagButton.Visible;
            Height = _diagButton.Visible ? 210 : 190;
        }
        if (e.KeyCode == Keys.Escape)
            Hide();
        if (e.KeyCode == Keys.Enter)
            Apply();
    }

    private void DiagButton_Click(object? sender, EventArgs e)
    {
        try
        {
            var controller = new FanController();
            string result  = controller.DiagnosticScan();
            MessageBox.Show(result, "I2C Register Scan", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Scan Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void PositionNearTray()
    {
        var screen  = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1920, 1080);
        Location    = new Point(screen.Right - Width - 12, screen.Bottom - Height - 12);
    }
}
