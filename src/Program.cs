using System;
using System.Windows.Forms;
using AsusFanControl;

// Single-instance check
var mutex = new System.Threading.Mutex(true, "AsusFanControl_SingleInstance", out bool isFirst);
if (!isFirst)
{
    MessageBox.Show("ASUS Fan Control is already running.\nCheck the system tray.",
        "Already Running", MessageBoxButtons.OK, MessageBoxIcon.Information);
    return;
}

Application.EnableVisualStyles();
Application.SetCompatibleTextRenderingDefault(false);

try
{
    NvApi.Initialize();
}
catch (Exception ex)
{
    // Write full details to a log file next to the exe for debugging
    string logPath = Path.Combine(AppContext.BaseDirectory, "AsusFanControl_error.log");
    string details = $"[{DateTime.Now}]\r\n{ex}\r\n\r\n";
    File.AppendAllText(logPath, details);

    MessageBox.Show(
        $"Failed to initialize NVIDIA API:\n\n{ex}\n\n" +
        $"Full log written to:\n{logPath}",
        "ASUS Fan Control - Startup Error",
        MessageBoxButtons.OK,
        MessageBoxIcon.Error);
    return;
}

Application.Run(new TrayApp());
GC.KeepAlive(mutex);
