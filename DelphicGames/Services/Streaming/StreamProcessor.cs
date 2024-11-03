using System.Diagnostics;
using DelphicGames.Data.Models;
using Stream = DelphicGames.Models.Stream;

namespace DelphicGames.Services.Streaming;

/// <summary>
/// Обрабатывает потоковое вещание на платформу
/// </summary>
public class StreamProcessor : IDisposable
{
    private const string FfmpegPath = "ffmpeg";
    private bool _disposed = false;

    public void Dispose()
    {
        // TODO: Implement IDisposable
    }

    public Stream StartStreamForPlatform(CameraPlatforms cameraPlatforms)
    {
        ArgumentNullException.ThrowIfNull(cameraPlatforms);

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
    private string GenerateFfmpegArguments(CameraPlatforms stream)
    {
        var command =
            " -y -fflags +genpts -thread_queue_size 512 -probesize 5000000 -analyzeduration 5000000 -timeout 5000000 -rtsp_transport tcp ";

        command += $"-i {stream.Camera.Url} -dn -sn -map 0:0 -codec:v copy -map 0:1 -codec:a aac -b:a 64k -shortest ";

        if (!stream.Platform.Url.EndsWith("/"))
        {
            stream.Platform.Url += "/";
        }

        var url = stream.Platform.Url + stream.Token;

        command += $"-f flv {url} ";

        return command.Trim();
    }
}