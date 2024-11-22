using System.Diagnostics;
using Serilog.Core;

namespace DelphicGames.Models;

public class StreamInfo : IDisposable
{
    public int StreamId { get; set; }
    public string NominationUrl { get; set; }
    public string PlatformUrl { get; set; }
    public string Token { get; set; }
    public Process Process { get; set; }
    public Logger Logger { get; set; }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing) return;
        if (Process == null) return;

        if (!Process.HasExited)
        {
            Process.Kill(true);
        }

        Process.Dispose();
        Process = null;
    }
}