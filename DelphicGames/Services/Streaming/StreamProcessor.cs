using System.Diagnostics;
using DelphicGames.Data.Models;
using DelphicGames.Hubs;
using DelphicGames.Models;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Serilog;

namespace DelphicGames.Services.Streaming;

/// <summary>
/// Обрабатывает потоковое вещание на платформу
/// </summary>
public class StreamProcessor : IStreamProcessor
{
    private const string FfmpegPath = "ffmpeg";
    private readonly ILogger<StreamProcessor> _logger;
    private bool _disposed = false;
    private readonly IServiceScopeFactory _scopeFactory;

    public StreamProcessor(ILogger<StreamProcessor> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }


    public async Task<StreamInfo> StartStreamForPlatform(StreamEntity streamEntity)
    {
        ArgumentNullException.ThrowIfNull(streamEntity);

        if (string.IsNullOrWhiteSpace(streamEntity.Token))
        {
            throw new InvalidOperationException("Токен для трансляции не указан.");
        }

        var ffmpegArguments = GenerateFfmpegArguments(streamEntity);

        // Создание директории для логов камеры
        var nominationId = streamEntity.NominationId;
        var logDirectory = Path.Combine("Logs", $"Camera_{nominationId}");
        Directory.CreateDirectory(logDirectory);

        // Генерация уникального имени файла лога с временной меткой
        var logFileName = $"ffmpeg_{streamEntity.PlatformName}_{DateTime.Now:yyyyMMdd_HHmmss}.log";

        // Настройка Serilog для записи логов ffmpeg
        var logger = new LoggerConfiguration()
            .WriteTo.File(Path.Combine(logDirectory, logFileName))
            .CreateLogger();

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
                if (args.Data.Contains("Error", StringComparison.OrdinalIgnoreCase))
                {
                    logger.Error(args.Data);

                    using var scope = _scopeFactory.CreateScope();
                    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                    var errorEvent = new StreamStatusChangedEvent(streamEntity.Id, StreamStatus.Error, args.Data);
                    await mediator.Publish(errorEvent);
                }
                else if (args.Data.Contains("Warning", StringComparison.OrdinalIgnoreCase))
                    logger.Warning(args.Data);
                else
                    logger.Information(args.Data);
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

            _logger.LogInformation("Запущен ffmpeg процесс для камеры {CameraId}", nominationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при запуске процесса ffmpeg для камеры {CameraId}", nominationId);
            process.OutputDataReceived -= null;
            process.ErrorDataReceived -= null;
            process.Exited -= null;
            logger.Dispose();
            throw new InvalidOperationException("Не удалось начать трансляцию.", ex);
        }

        var stream = new StreamInfo
        {
            NominationUrl = streamEntity.StreamUrl,
            PlatformUrl = streamEntity.PlatformUrl,
            Token = streamEntity.Token,
            Process = process,
            Logger = logger
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
                streamInfo.Logger.Information("Трансляция остановлена.");
                _logger.LogInformation("Остановлен ffmpeg процесс для камеры {CameraId}", streamInfo.NominationUrl);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при остановке процесса ffmpeg для камеры {CameraId}",
                streamInfo.NominationUrl);
        }
        finally
        {
            streamInfo.Process.OutputDataReceived -= null;
            streamInfo.Process.ErrorDataReceived -= null;
            streamInfo.Process.Exited -= null;
            streamInfo.Process.Dispose();
            streamInfo.Logger.Dispose();
        }
    }

    /// <summary>
    /// Генерирует аргументы для запуска ffmpeg
    /// -y: Перезаписывает выходной файл без запроса подтверждения.
    /// -fflags +genpts: Генерирует временные метки представления (PTS), которые могут быть полезны для потоков, в которых отсутствуют временные метки.
    /// -thread_queue_size 512: Устанавливает размер очереди входного потока, что помогает предотвратить ошибки переполнения буфера для входных данных с большой задержкой.
    /// -probesize 5000000 и -analyzeduration 5000000: Увеличьте продолжительность зондирования и анализа для обработки сложных потоков или потоков с большой задержкой.
    /// -timeout 5000000: Устанавливает тайм-аут для RTSP-подключений, полезно для повторного подключения в случае нестабильности сети.
    /// -reconnect 1: Позволяет повторно подключиться в случае потери соединения.
    /// -reconnect_streamed 1: Позволяет ffmpeg повторно подключаться к прямым трансляциям (полезно для RTSP или RTMP).
    /// -reconnect_delay_max 5: Устанавливает максимальную задержку в секундах перед попыткой повторного подключения.
    /// -rtsp_transport tcp: использует TCP в качестве транспортного протокола для RTSP, который, как правило, более стабилен, чем UDP для потоковой передачи.
    /// -i rtsp://url: Указывает входной поток RTSP.
    /// -dn -sn: Отключает потоки данных (например, скрытые субтитры или метаданные) и субтитров.
    /// -map 0:0 -codec:v copy: Отображает видеопоток со входа (первый поток) и копирует его без повторного кодирования.
    /// -map 0:1 -codec:a aac -b:a 64k: сопоставляет аудиопоток (второй поток), перекодирует его в формат AAC и устанавливает битрейт звука 64 кбит/с.
    /// -shortest: Гарантирует, что вывод заканчивается, когда заканчивается самый короткий входной поток.
    /// -f flv rtmp://url/key: выводит поток в формате FLV на указанный RTMP-сервер.
    /// </summary>
    /// <param name="streamEntity"></param>
    /// <returns></returns>
    private string GenerateFfmpegArguments(StreamEntity streamEntity)
    {
        var command =
            " -thread_queue_size 512 -rtmp_buffer 5000 -rtmp_live live";

        command +=
            $"-i {streamEntity.StreamUrl} -c copy ";

        if (!streamEntity.PlatformUrl.EndsWith("/"))
        {
            streamEntity.PlatformUrl += "/";
        }

        var url = streamEntity.PlatformUrl + streamEntity.Token;

        command += $"-f flv {url} ";

        return command.Trim();
    }

    public void Dispose()
    {
        // TODO: Implement IDisposable
    }
}