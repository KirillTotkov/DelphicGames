using DelphicGames.Data.Models;
using Stream = DelphicGames.Models.Stream;

namespace DelphicGames.Services.Streaming;

public class StreamManager
{
    public Dictionary<int, List<Stream>> NominationStreams { get; } = new();
    private readonly ILogger<StreamManager> _logger;
    private readonly IStreamProcessor _streamProcessor;

    public StreamManager(IStreamProcessor streamProcessor, ILogger<StreamManager> logger)
    {
        _streamProcessor = streamProcessor;
        _logger = logger;
    }

    // Запуск потока для определённой камеры и платформы
    public void StartStream(NominationPlatform nominationPlatform)
    {
        try
        {
            var nominationId = nominationPlatform.NominationId;

            if (!NominationStreams.TryGetValue(nominationId, out var streams))
            {
                streams = new List<Stream>();
                NominationStreams[nominationId] = streams;
            }

            var stream = _streamProcessor.StartStreamForPlatform(nominationPlatform);
            streams.Add(stream);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    // Остановка потока для определённой камеры и платформы
    public void StopStream(NominationPlatform nominationPlatform)
    {
        try
        {
            var nominationId = nominationPlatform.NominationId;

            if (NominationStreams.TryGetValue(nominationId, out var streams))
            {
                var stream = streams.FirstOrDefault(s =>
                    s.PlatformUrl == nominationPlatform.Platform.Url && s.Token == nominationPlatform.Token);

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
                "FFmpeg ошибка при запуске потока для номинации {NominationId} на платформе {PlatformId}",
                nominationPlatform.NominationId, nominationPlatform.PlatformId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при остановке потока для номинации {NominationId} на платформе {PlatformId}",
                nominationPlatform.NominationId, nominationPlatform.PlatformId);
            throw;
        }
    }

    // Запуск всех потоков
    public void StartAllStreams(IEnumerable<NominationPlatform> cameraPlatformsList)
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
        StopAllStreams();
        _streamProcessor.Dispose();
    }
}