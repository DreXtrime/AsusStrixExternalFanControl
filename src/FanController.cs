using System;

namespace AsusFanControl;

/// <summary>
/// Controls the ASUS GTX 1080 Strix external fan headers via NvAPI I2C.
///
/// The external fans are wired to an onboard microcontroller (I2C address 0x52, port 1).
/// Fan speed is set by writing a PWM byte (0x00-0xFF) to register 0x11.
///
/// NOTE: Register 0x11 was identified via x32dbg capture of Vender.dll.
/// If fans don't respond, try nearby registers (0x10, 0x12) or check
/// the diagnostic read output to see what the controller returns.
/// </summary>
internal class FanController
{
    // I2C parameters captured from Vender.dll via x32dbg
    private const byte I2CAddress    = 0x52;
    private const byte PortId        = 1;
    private const byte FanRegister   = 0x11;

    private int _lastSetPercent = -1;

    /// <summary>
    /// Sets both external fan headers to the given speed (0-100%).
    /// </summary>
    public void SetSpeed(int percent)
    {
        percent = Math.Clamp(percent, 0, 100);
        byte pwm = PercentToPwm(percent);
        NvApi.I2CWrite(I2CAddress, PortId, FanRegister, pwm);
        _lastSetPercent = percent;
    }

    /// <summary>
    /// Reads back the current PWM value from the controller.
    /// Useful for verifying the write worked or diagnosing register issues.
    /// </summary>
    public int ReadCurrentSpeed()
    {
        byte pwm = NvApi.I2CRead(I2CAddress, PortId, FanRegister);
        return PwmToPercent(pwm);
    }

    /// <summary>
    /// Scans I2C registers 0x00-0x1F and returns their values as a hex dump.
    /// Use this to explore the controller if register 0x11 doesn't work.
    /// </summary>
    public string DiagnosticScan()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"I2C scan: address=0x{I2CAddress:X2} port={PortId}");
        sb.AppendLine("Reg  Val");
        for (byte reg = 0x00; reg < 0x20; reg++)
        {
            try
            {
                byte val = NvApi.I2CRead(I2CAddress, PortId, reg);
                sb.AppendLine($"0x{reg:X2}  0x{val:X2} ({val})");
            }
            catch (NvApiException ex)
            {
                sb.AppendLine($"0x{reg:X2}  ERROR 0x{ex.ErrorCode:X8}");
            }
        }
        return sb.ToString();
    }

    // Captured: 52% = 0x85 (133), which matches percent * 255 / 100
    private static byte PercentToPwm(int percent) => (byte)(percent * 255 / 100);
    private static int  PwmToPercent(byte pwm)    => (int)Math.Round(pwm * 100.0 / 255.0);
}
