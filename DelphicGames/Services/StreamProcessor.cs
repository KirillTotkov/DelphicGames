using System;
using System.Diagnostics;
using DelphicGames.Data.Models;

namespace DelphicGames.Services;

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


/// <summary>
/// Обрабатывает потоковое вещание на платформу
/// </summary>
public class StreamProcessor : IDisposable
{
    private const string FfmpegPath = "ffmpeg";
    private bool _disposed = false;
    public StreamProcessor()
    {
    }


    public async Task<Stream> StartStreamForPlatform(CameraPlatforms cameraPlatforms)
    {
        if (cameraPlatforms == null)
            throw new ArgumentNullException(nameof(cameraPlatforms));

        var ffmpegArguments = GenerateFfmpegArguments(cameraPlatforms);

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = FfmpegPath,
                Arguments = ffmpegArguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            },
            EnableRaisingEvents = true
        };

        process.OutputDataReceived += (sender, args) => Console.WriteLine(args.Data);
        process.ErrorDataReceived += (sender, args) => Console.Error.WriteLine(args.Data);

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Ошибка при запуске процесса ffmpeg: {ex.Message}");
            throw;
        }
        var stream = new Stream
        {
            CameraUrl = cameraPlatforms.Camera.Url,
            PlatformUrl = cameraPlatforms.Platform.Url,
            Token = cameraPlatforms.Token,
            Process = process
        };

        return stream;
    }

    public void StopStreamForPlatform(Stream stream)
    {
        stream.Process.Kill(true);
        stream.Process.Dispose();
    }

    private string GenerateFfmpegArguments(CameraPlatforms stream)
    {
        var command =
            $" -y -fflags +genpts -thread_queue_size 512 -probesize 5000000 -analyzeduration 5000000 -timeout 5000000 -rtsp_transport tcp ";

        command += $"-i {stream.Camera.Url} -dn -sn -map 0:0 -codec:v copy -map 0:1 -codec:a aac -b:a 64k -shortest ";

        if (!stream.Platform.Url.EndsWith("/"))
        {
            stream.Platform.Url += "/";
        }

        var url = stream.Platform.Url + stream.Token;

        command += $"-f flv {url} ";

        return command.Trim();
    }

    public void Dispose()
    {
        // TODO: Implement IDisposable
    }
}
