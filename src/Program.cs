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
    MessageBox.Show(
        $"Failed to initialize NVIDIA API:\n{ex.Message}\n\n" +
        "Make sure you have an NVIDIA GPU and are running as Administrator.",
        "ASUS Fan Control - Startup Error",
        MessageBoxButtons.OK,
        MessageBoxIcon.Error);
    return;
}

Application.Run(new TrayApp());
GC.KeepAlive(mutex);
