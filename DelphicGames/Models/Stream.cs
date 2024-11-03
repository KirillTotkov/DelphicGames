using System.Diagnostics;

namespace DelphicGames.Models;

public class Stream : IDisposable
{
    public string CameraUrl { get; set; }
    public string PlatformUrl { get; set; }
    public string Token { get; set; }
    public Process Process { get; set; }

    public void Dispose()
    {
        if (Process != null)
        {
            if (!Process.HasExited)
            {
                Process.Kill(true);
            }

            Process.Dispose();
            Process = null;
        }
    }
}