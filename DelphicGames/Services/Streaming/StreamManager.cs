using DelphicGames.Data.Models;
using Stream = DelphicGames.Models.Stream;

namespace DelphicGames.Services.Streaming;

public class StreamManager
{
    private readonly Dictionary<Camera, List<Stream>> _cameraStreams = new();
    private readonly ILogger<StreamManager> _logger;
    private readonly StreamProcessor _streamProcessor;

    public StreamManager(StreamProcessor streamProcessor, ILogger<StreamManager> logger)
    {
        _streamProcessor = streamProcessor;
        _logger = logger;
    }

    // Запуск потока для определённой камеры и платформы
    public void StartStream(CameraPlatforms cameraPlatform)
    {
        try
        {
            var camera = cameraPlatform.Camera;

            if (!_cameraStreams.TryGetValue(camera, out var streams))
            {
                streams = new List<Stream>();
                _cameraStreams[camera] = streams;
            }

            var stream = _streamProcessor.StartStreamForPlatform(cameraPlatform);
            streams.Add(stream);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при запуске потока для камеры {CameraId} на платформе {PlatformId}",
                cameraPlatform.CameraId, cameraPlatform.PlatformId);
            throw;
        }
    }

    // Остановка потока для определённой камеры и платформы
    public void StopStream(CameraPlatforms cameraPlatform)
    {
        try
        {
            var camera = cameraPlatform.Camera;

            if (_cameraStreams.TryGetValue(camera, out var streams))
            {
                var stream = streams.FirstOrDefault(s => s.PlatformUrl == cameraPlatform.Platform.Url);

                if (stream != null)
                {
                    _streamProcessor.StopStreamForPlatform(stream);
                    streams.Remove(stream);

                    if (streams.Count == 0)
                    {
                        _cameraStreams.Remove(camera);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при остановке потока для камеры {CameraId} на платформе {PlatformId}",
                cameraPlatform.CameraId, cameraPlatform.PlatformId);
            throw;
        }
    }

    // Запуск всех потоков
    public void StartAllStreams(IEnumerable<CameraPlatforms> cameraPlatformsList)
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