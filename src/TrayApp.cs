using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AsusFanControl;

/// <summary>
/// System tray application context.
/// The app lives entirely in the tray -- no main window.
/// Click or double-click the icon to open the fan speed popup.
/// </summary>
internal class TrayApp : ApplicationContext
{
    private readonly NotifyIcon    _trayIcon;
    private readonly FanController _fan;
    private FanControlForm?        _form;
    private int _currentPercent = 50;

    public TrayApp()
    {
        _fan = new FanController();

        _trayIcon = new NotifyIcon
        {
            Icon    = MakeTrayIcon(_currentPercent),
            Text    = TrayText(_currentPercent),
            Visible = true
        };

        var menu = new ContextMenuStrip { BackColor = Color.FromArgb(30, 30, 30) };
        AddMenuItem(menu, "Fan Control...", (_, _) => ShowForm());
        menu.Items.Add(new ToolStripSeparator());
        AddMenuItem(menu, "0%",   (_, _) => QuickSet(0));
        AddMenuItem(menu, "25%",  (_, _) => QuickSet(25));
        AddMenuItem(menu, "50%",  (_, _) => QuickSet(50));
        AddMenuItem(menu, "75%",  (_, _) => QuickSet(75));
        AddMenuItem(menu, "100%", (_, _) => QuickSet(100));
        menu.Items.Add(new ToolStripSeparator());
        AddMenuItem(menu, "Exit", (_, _) => Exit());

        _trayIcon.ContextMenuStrip = menu;
        _trayIcon.MouseClick      += (_, e) => { if (e.Button == MouseButtons.Left) ShowForm(); };

        // Apply initial speed
        ApplySpeed(_currentPercent);
    }

    public void ApplySpeed(int percent)
    {
        percent = Math.Clamp(percent, 0, 100);
        try
        {
            _fan.SetSpeed(percent);
            _currentPercent        = percent;
            _trayIcon.Icon         = MakeTrayIcon(percent);
            _trayIcon.Text         = TrayText(percent);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to set fan speed: {ex.Message}\n\n" +
                "Make sure you are running as Administrator.\n" +
                "If the problem persists, use Ctrl+D in the Fan Control window to scan I2C registers.",
                "ASUS Fan Control",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private void ShowForm()
    {
        if (_form == null || _form.IsDisposed)
            _form = new FanControlForm(_currentPercent, ApplySpeed);

        _form.Show();
        _form.BringToFront();
    }

    private void QuickSet(int percent)
    {
        ApplySpeed(percent);
        _form?.Close(); // reset form so it shows updated value next time
    }

    private void Exit()
    {
        _trayIcon.Visible = false;
        Application.Exit();
    }

    // --- Icon drawing ---
    // Draws a small bar chart in the tray icon showing the current fan %.
    private static Icon MakeTrayIcon(int percent)
    {
        var bmp = new Bitmap(16, 16, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.Transparent);
            g.SmoothingMode = SmoothingMode.None;

            // Background pill
            using var bgBrush = new SolidBrush(Color.FromArgb(200, 40, 40, 40));
            g.FillRectangle(bgBrush, 0, 0, 16, 16);

            // Fill bar (blue -> red based on percent)
            int fill = (int)(16 * percent / 100.0);
            if (fill > 0)
            {
                int r = (int)(percent * 2.55);
                int b = 255 - r;
                using var fillBrush = new SolidBrush(Color.FromArgb(220, r, 80, b));
                g.FillRectangle(fillBrush, 0, 16 - fill, 16, fill);
            }

            // Percentage text
            string text = percent == 100 ? "FF" : $"{percent:D2}";
            using var font  = new Font("Arial", 5f, FontStyle.Bold);
            using var brush = new SolidBrush(Color.White);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(text, font, brush, new RectangleF(0, 0, 16, 16), sf);
        }

        return Icon.FromHandle(bmp.GetHicon());
    }

    private static string TrayText(int percent) => $"ASUS Fan Control - {percent}%";

    private static void AddMenuItem(ContextMenuStrip menu, string text, EventHandler handler)
    {
        var item = new ToolStripMenuItem(text) { ForeColor = Color.White, BackColor = Color.FromArgb(30, 30, 30) };
        item.Click += handler;
        menu.Items.Add(item);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _trayIcon.Dispose();
            _form?.Dispose();
        }
        base.Dispose(disposing);
    }
}
