using System.Diagnostics;
using DelphicGames.Data.Models;
using Serilog;
using Stream = DelphicGames.Models.Stream;

namespace DelphicGames.Services.Streaming;

/// <summary>
/// Тестовый обработчик потоков, который запускает простые процессы вместо ffmpeg
/// </summary>
public class TestStreamProcessor : IStreamProcessor
{
    private readonly ILogger<TestStreamProcessor> _logger;
    private bool _disposed = false;

    public TestStreamProcessor(ILogger<TestStreamProcessor> logger)
    {
        _logger = logger;
    }

    public Stream StartStreamForPlatform(NominationPlatform nominationPlatform)
    {
        ArgumentNullException.ThrowIfNull(nominationPlatform);

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/K echo Test Process",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            },
            EnableRaisingEvents = true
        };

        var logDirectory = Path.Combine("Logs", $"Camera_{nominationPlatform.NominationId}");
        Directory.CreateDirectory(logDirectory);

        var logFileName = $"test_process_{nominationPlatform.PlatformId}_{DateTime.Now:yyyyMMdd_HHmmss}.log";
        var logger = new LoggerConfiguration()
            .WriteTo.File(Path.Combine(logDirectory, logFileName))
            .CreateLogger();

        process.OutputDataReceived += (sender, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data))
            {
                logger.Information(args.Data);
            }
        };

        process.ErrorDataReceived += (sender, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data))
            {
                logger.Error(args.Data);
            }
        };

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            Thread.Sleep(4000);
            if (process.HasExited)
            {
                throw new InvalidOperationException("Процесс FFmpeg завершился неожиданно.");
            }

            _logger.LogInformation("Запущен ffmpeg тестовый процесс для номинации {NominationId} на платформе {PlatformId}",
                nominationPlatform.NominationId, nominationPlatform.PlatformId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при запуске тестового процесса для номинации {NominationId} на платформе {PlatformId}",
                nominationPlatform.NominationId, nominationPlatform.PlatformId);
            process.OutputDataReceived -= null;
            process.ErrorDataReceived -= null;
            logger.Dispose();
            throw;
        }

        var stream = new Stream
        {
            NominationUrl = nominationPlatform.Nomination.StreamUrl,
            PlatformUrl = nominationPlatform.Platform.Url,
            Token = nominationPlatform.Token,
            Process = process,
            Logger = logger
        };

        return stream;
    }

    public void StopStreamForPlatform(Stream stream)
    {
        try
        {
            if (!stream.Process.HasExited)
            {
                stream.Process.Kill(true);
                stream.Process.WaitForExit();
                stream.Logger.Information("Тестовая трансляция остановлена.");
                _logger.LogInformation("Остановлен тестовый процесс для камеры {CameraId}", stream.NominationUrl);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при остановке тестового процесса для камеры {CameraId}", stream.NominationUrl);
        }
        finally
        {
            stream.Process.OutputDataReceived -= null;
            stream.Process.ErrorDataReceived -= null;
            stream.Process.Dispose();
            stream.Logger.Dispose();
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            // Освобождение ресурсов
            _disposed = true;
        }
    }
}