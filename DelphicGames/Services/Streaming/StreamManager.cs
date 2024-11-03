using DelphicGames.Data.Models;

namespace DelphicGames.Services.Streaming;

public class StreamManager
{
    private readonly Dictionary<Camera, List<Stream>> _cameraStreams = new();
    private readonly StreamProcessor _streamProcessor;

    private readonly ILogger<StreamManager> _logger;

    public StreamManager(StreamProcessor streamProcessor, ILogger<StreamManager> logger)
    {
        _streamProcessor = streamProcessor;
        _logger = logger;
    }

    // Запуск потока для определённой камеры и платформы
    public async Task StartStreamAsync(CameraPlatforms cameraPlatform)
    {
        try
        {
            var camera = cameraPlatform.Camera;

            if (!_cameraStreams.ContainsKey(camera))
            {
                _cameraStreams[camera] = new List<Stream>();
            }

            var stream = await _streamProcessor.StartStreamForPlatform(cameraPlatform);

            _cameraStreams[camera].Add(stream);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при запуске потока для камеры {CameraId} на платформе {PlatformId}", cameraPlatform.CameraId, cameraPlatform.PlatformId);
            throw;
        }
    }

    // Остановка потока для определённой камеры и платформы
    public void StopStream(CameraPlatforms cameraPlatform)
    {
        try
        {
            var camera = cameraPlatform.Camera;

            if (_cameraStreams.ContainsKey(camera))
            {
                var stream = _cameraStreams[camera]
                    .FirstOrDefault(s => s.PlatformUrl == cameraPlatform.Platform.Url);

                if (stream != null)
                {
                    _streamProcessor.StopStreamForPlatform(stream);
                    _cameraStreams[camera].Remove(stream);

                    if (_cameraStreams[camera].Count == 0)
                    {
                        _cameraStreams.Remove(camera);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при остановке потока для камеры {CameraId} на платформе {PlatformId}", cameraPlatform.CameraId, cameraPlatform.PlatformId);
            throw;
        }
    }

    // Запуск всех потоков
    public async Task StartAllStreamsAsync(IEnumerable<CameraPlatforms> cameraPlatformsList)
    {
        foreach (var cameraPlatform in cameraPlatformsList)
        {
            await StartStreamAsync(cameraPlatform);
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