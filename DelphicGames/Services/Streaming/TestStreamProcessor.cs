using System.Diagnostics;
using DelphicGames.Data.Models;
using DelphicGames.Models;
using MediatR;
using Serilog;

namespace DelphicGames.Services.Streaming;

/// <summary>
/// Тестовый обработчик потоков, который запускает простые процессы вместо ffmpeg
/// </summary>
public class TestStreamProcessor : IStreamProcessor
{
    private readonly ILogger<TestStreamProcessor> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    private bool _disposed = false;

    public TestStreamProcessor(ILogger<TestStreamProcessor> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public async Task<StreamInfo> StartStreamForPlatform(StreamEntity streamEntity)
    {
        ArgumentNullException.ThrowIfNull(streamEntity);

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

        var logDirectory = Path.Combine("Logs", $"Camera_{streamEntity.NominationId}");
        Directory.CreateDirectory(logDirectory);

        var logFileName = $"test_process_{streamEntity.PlatformName}_{DateTime.Now:yyyyMMdd_HHmmss}.log";
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

        process.ErrorDataReceived += async (sender, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data))
            {
                logger.Error(args.Data);
                using var scope = _scopeFactory.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                var errorEvent = new StreamStatusChangedEvent(streamEntity.Id, StreamStatus.Error, args.Data);
                await mediator.Publish(errorEvent);
            }
        };

        process.Exited += async (sender, args) =>
       {
           _logger.LogInformation($"Stream process for entity {streamEntity.Id} has exited.");

           using var scope = _scopeFactory.CreateScope();
           var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

           var completedEvent = new StreamStatusChangedEvent(streamEntity.Id, StreamStatus.Completed);
           await mediator.Publish(completedEvent);
       };

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            using var scope = _scopeFactory.CreateScope();
            var mediatorStart = scope.ServiceProvider.GetRequiredService<IMediator>();
            var runningEvent = new StreamStatusChangedEvent(streamEntity.Id, StreamStatus.Running);
            await mediatorStart.Publish(runningEvent);

            _logger.LogInformation("Запущен ffmpeg тестовый процесс для номинации {NominationId} на платформе {PlatformName}",
                streamEntity.NominationId, streamEntity.PlatformName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при запуске тестового процесса для номинации {NominationId} на платформе {PlatformName}",
                streamEntity.NominationId, streamEntity.PlatformName);
            process.OutputDataReceived -= null;
            process.ErrorDataReceived -= null;
            logger.Dispose();
            throw;
        }

        var stream = new StreamInfo
        {
            NominationUrl = streamEntity.StreamUrl,
            PlatformUrl = streamEntity.PlatformUrl,
            Token = streamEntity.Token,
            Process = process,
            Logger = logger,
            StreamId = streamEntity.Id
        };

        return stream;
    }

    public void StopStreamForPlatform(StreamInfo streamInfo)
    {
        try
        {
            if (!streamInfo.Process.HasExited)
            {
                streamInfo.Process.Kill(true);
                streamInfo.Process.WaitForExit();
                streamInfo.Logger.Information("Тестовая трансляция остановлена.");
                _logger.LogInformation("Остановлен тестовый процесс для камеры {CameraId}", streamInfo.NominationUrl);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при остановке тестового процесса для камеры {CameraId}", streamInfo.NominationUrl);
        }
        finally
        {
            streamInfo.Process.OutputDataReceived -= null;
            streamInfo.Process.ErrorDataReceived -= null;
            streamInfo.Process.Dispose();
            streamInfo.Logger.Dispose();
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