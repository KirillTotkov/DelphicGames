using DelphicGames.Data.Models;
using DelphicGames.Models;

namespace DelphicGames.Services.Streaming;

public class StreamManager
{
    public Dictionary<int, List<StreamInfo>> NominationStreams { get; } = new();
    private readonly ILogger<StreamManager> _logger;
    private readonly IStreamProcessor _streamProcessor;
    private bool _disposed;

    public StreamManager(IStreamProcessor streamProcessor, ILogger<StreamManager> logger)
    {
        _streamProcessor = streamProcessor;
        _logger = logger;
    }

    // Запуск потока для определённой камеры и платформы
    public void StartStream(StreamEntity streamEntity)
    {
        try
        {
            var nominationId = streamEntity.NominationId;

            if (!NominationStreams.TryGetValue(nominationId, out var streams))
            {
                streams = new List<StreamInfo>();
                NominationStreams[nominationId] = streams;
            }

            var stream = _streamProcessor.StartStreamForPlatform(streamEntity);
            streams.Add(stream);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    // Остановка потока для определённой камеры и платформы
    public void StopStream(StreamEntity streamEntity)
    {
        try
        {
            var nominationId = streamEntity.NominationId;

            if (NominationStreams.TryGetValue(nominationId, out var streams))
            {
                var stream = streams.FirstOrDefault(s =>
                    s.PlatformUrl == streamEntity.PlatformUrl && s.Token == streamEntity.Token);

                if (stream != null)
                {
                    _streamProcessor.StopStreamForPlatform(stream);
                    streams.Remove(stream);

                    if (streams.Count == 0)
                    {
                        NominationStreams.Remove(nominationId);
                    }
                }
            }
        }
        catch (FfmpegProcessException ex)
        {
            _logger.LogError(ex,
                "FFmpeg ошибка при запуске потока для номинации {NominationId} на платформе {PlatformName}",
                streamEntity.NominationId, streamEntity.PlatformName);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при остановке потока для номинации {NominationId} на платформе {PlatformName}",
                streamEntity.NominationId, streamEntity.PlatformName);
            throw;
        }
    }

    // Запуск всех потоков
    public void StartAllStreams(IEnumerable<Data.Models.StreamEntity> cameraPlatformsList)
    {
        foreach (var cameraPlatform in cameraPlatformsList)
        {
            StartStream(cameraPlatform);
        }
    }

    // Остановка всех потоков
    public void StopAllStreams()
    {
        foreach (var streams in NominationStreams.Values)
        {
            foreach (var stream in streams)
            {
                _streamProcessor.StopStreamForPlatform(stream);
            }
        }

        NominationStreams.Clear();
    }

    // Проверка наличия активных потоков для номинации
    public bool HasActiveStreams(int nominationId)
    {
        return NominationStreams.TryGetValue(nominationId, out var streams) && streams.Any();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            StopAllStreams();
            _streamProcessor.Dispose();
        }

        _disposed = true;
    }

}