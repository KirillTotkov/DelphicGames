using DelphicGames.Data.Models;
using Stream = DelphicGames.Models.Stream;

namespace DelphicGames.Services.Streaming;

public class StreamManager
{
    private readonly Dictionary<int, List<Stream>> _cameraStreams = new();
    private readonly ILogger<StreamManager> _logger;
    private readonly StreamProcessor _streamProcessor;

    public StreamManager(StreamProcessor streamProcessor, ILogger<StreamManager> logger)
    {
        _streamProcessor = streamProcessor;
        _logger = logger;
    }

    // Запуск потока для определённой камеры и платформы
    public void StartStream(CameraPlatform cameraPlatform)
    {
        try
        {
            int cameraId = cameraPlatform.CameraId;

            if (!_cameraStreams.TryGetValue(cameraId, out var streams))
            {
                streams = new List<Stream>();
                _cameraStreams[cameraId] = streams;
            }

            var stream = _streamProcessor.StartStreamForPlatform(cameraPlatform);
            streams.Add(stream);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    // Остановка потока для определённой камеры и платформы
    public void StopStream(CameraPlatform cameraPlatform)
    {
        try
        {
            int cameraId = cameraPlatform.CameraId;

            if (_cameraStreams.TryGetValue(cameraId, out var streams))
            {
                var stream = streams.FirstOrDefault(s =>
                    s.PlatformUrl == cameraPlatform.Platform.Url && s.Token == cameraPlatform.Token);

                if (stream != null)
                {
                    _streamProcessor.StopStreamForPlatform(stream);
                    streams.Remove(stream);

                    if (streams.Count == 0)
                    {
                        _cameraStreams.Remove(cameraId);
                    }
                }
            }
        }
        catch (FfmpegProcessException ex)
        {
            _logger.LogError(ex, "FFmpeg ошибка при запуске потока для камеры {CameraId}", ex.CameraId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при остановке потока для камеры {CameraId} на платформе {PlatformId}",
                cameraPlatform.CameraId, cameraPlatform.PlatformId);
            throw;
        }
    }

    // Запуск всех потоков
    public void StartAllStreams(IEnumerable<CameraPlatform> cameraPlatformsList)
    {
        foreach (var cameraPlatform in cameraPlatformsList)
        {
            StartStream(cameraPlatform);
        }
    }

    // Остановка всех потоков
    public void StopAllStreams()
    {
        foreach (var streams in _cameraStreams.Values)
        {
            foreach (var stream in streams)
            {
                _streamProcessor.StopStreamForPlatform(stream);
            }
        }

        _cameraStreams.Clear();
    }

    public void Dispose()
    {
        StopAllStreams();
        _streamProcessor.Dispose();
    }
}