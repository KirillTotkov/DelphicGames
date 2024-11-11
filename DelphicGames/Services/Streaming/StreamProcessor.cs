using System.Diagnostics;
using DelphicGames.Data.Models;
using Serilog;
using Stream = DelphicGames.Models.Stream;

namespace DelphicGames.Services.Streaming;

/// <summary>
/// Обрабатывает потоковое вещание на платформу
/// </summary>
public class StreamProcessor : IStreamProcessor
{
    private const string FfmpegPath = "ffmpeg";
    private readonly ILogger<StreamProcessor> _logger;
    private bool _disposed = false;

    public StreamProcessor(ILogger<StreamProcessor> logger)
    {
        _logger = logger;
    }

    public Stream StartStreamForPlatform(NominationPlatform nominationPlatform)
    {
        ArgumentNullException.ThrowIfNull(nominationPlatform);

        if (string.IsNullOrWhiteSpace(nominationPlatform.Token))
        {
            throw new InvalidOperationException("Токен для трансляции не указан.");
        }

        var ffmpegArguments = GenerateFfmpegArguments(nominationPlatform);

        // Создание директории для логов камеры
        var nominationId = nominationPlatform.NominationId;
        var logDirectory = Path.Combine("Logs", $"Camera_{nominationId}");
        Directory.CreateDirectory(logDirectory);

        // Генерация уникального имени файла лога с временной меткой
        var logFileName = $"ffmpeg_{nominationPlatform.PlatformId}_{DateTime.Now:yyyyMMdd_HHmmss}.log";

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

        DataReceivedEventHandler outputHandler = (sender, args) =>
          {
              if (!string.IsNullOrEmpty(args.Data))
              {
                  logger.Information(args.Data);
              }
          };

        DataReceivedEventHandler errorHandler = (sender, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data))
            {
                if (args.Data.Contains("Error", StringComparison.OrdinalIgnoreCase))
                    logger.Error(args.Data);
                else if (args.Data.Contains("Warning", StringComparison.OrdinalIgnoreCase))
                    logger.Warning(args.Data);
                else
                    logger.Information(args.Data);
            }
        };

        process.OutputDataReceived += outputHandler;
        process.ErrorDataReceived += errorHandler;

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

            _logger.LogInformation("Запущен ffmpeg процесс для камеры {CameraId}", nominationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при запуске процесса ffmpeg для камеры {CameraId}", nominationId);
            process.OutputDataReceived -= outputHandler;
            process.ErrorDataReceived -= errorHandler;
            logger.Dispose();
            throw new InvalidOperationException("Не удалось начать трансляцию.", ex);
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
                stream.Logger.Information("Трансляция остановлена.");
                _logger.LogInformation("Остановлен ffmpeg процесс для камеры {CameraId}", stream.NominationUrl);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при остановке процесса ffmpeg для камеры {CameraId}", stream.NominationUrl);
        }
        finally
        {
            stream.Process.OutputDataReceived -= null;
            stream.Process.ErrorDataReceived -= null;
            stream.Process.Dispose();
            stream.Logger.Dispose();
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
    /// <param name="stream"></param>
    /// <returns></returns>
    private string GenerateFfmpegArguments(NominationPlatform stream)
    {
        var command =
            " -y -fflags +genpts -thread_queue_size 512 -probesize 5000000 -analyzeduration 5000000 -timeout 5000000 -rtsp_transport tcp ";

        command += $"-i {stream.Nomination.StreamUrl} -dn -sn -map 0:0 -codec:v copy -map 0:1 -codec:a aac -b:a 64k -shortest ";

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