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
        Size            = new Size(300, 230);
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
            Text      = currentPercent == 0 ? "Off" : $"{currentPercent}%",
            ForeColor = Color.White,
            Font      = new Font("Segoe UI", 24f, FontStyle.Bold),
            Location  = new Point(12, 30),
            Size      = new Size(120, 45),
            TextAlign = ContentAlignment.MiddleLeft
        };

        // Slider -- range 50-100 only; use Off button for 0
        int sliderVal = currentPercent < 50 ? 50 : currentPercent;
        _slider = new TrackBar
        {
            Minimum      = 50,
            Maximum      = 100,
            Value        = sliderVal,
            TickFrequency= 10,
            SmallChange  = 1,
            LargeChange  = 10,
            Location     = new Point(8, 82),
            Size         = new Size(272, 45),
            BackColor    = Color.FromArgb(30, 30, 30)
        };
        _slider.ValueChanged += Slider_ValueChanged;

        // Off button (sets fans to 0)
        var offButton = new Button
        {
            Text      = "Off (0%)",
            Location  = new Point(12, 138),
            Size      = new Size(80, 28),
            BackColor = Color.FromArgb(60, 60, 60),
            ForeColor = Color.FromArgb(200, 200, 200),
            FlatStyle = FlatStyle.Flat,
            Font      = new Font("Segoe UI", 9f)
        };
        offButton.FlatAppearance.BorderSize = 0;
        offButton.Click += (_, _) =>
        {
            _percentLabel.Text = "Off";
            _onSpeedChanged(0);
        };

        // Instant apply checkbox
        _instantCheck = new CheckBox
        {
            Text      = "Apply instantly",
            Checked   = false,
            ForeColor = Color.FromArgb(180, 180, 180),
            Location  = new Point(12, 176),
            AutoSize  = true,
            BackColor = Color.Transparent
        };

        // Apply button
        _applyButton = new Button
        {
            Text      = "Apply",
            Location  = new Point(170, 171),
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
            Location  = new Point(12, 205),
            Size      = new Size(268, 24),
            BackColor = Color.FromArgb(60, 60, 60),
            ForeColor = Color.FromArgb(180, 180, 180),
            FlatStyle = FlatStyle.Flat,
            Font      = new Font("Segoe UI", 8f),
            Visible   = false
        };
        _diagButton.FlatAppearance.BorderSize = 0;
        _diagButton.Click += DiagButton_Click;

        Controls.AddRange([title, _percentLabel, _slider, offButton, _instantCheck, _applyButton, _diagButton]);

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