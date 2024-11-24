using System.Collections.Concurrent;
using DelphicGames.Data.Models;
using DelphicGames.Models;

namespace DelphicGames.Services.Streaming;

public class StreamManager : IAsyncDisposable
{
    private readonly ConcurrentDictionary<int, List<StreamInfo>> _nominationStreams = new();

    private readonly ILogger<StreamManager> _logger;

    // private readonly IStreamProcessor _streamProcessor;
    private bool _disposed;
    private readonly IServiceScopeFactory _scopeFactory;


    public StreamManager(ILogger<StreamManager> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    // Запуск потока для определённой камеры и платформы
    public async Task StartStream(StreamEntity streamEntity)
    {
        using var scope = _scopeFactory.CreateScope();
        var streamProcessor = scope.ServiceProvider.GetRequiredService<IStreamProcessor>();

        try
        {
            var streams = _nominationStreams.GetOrAdd(streamEntity.NominationId, _ => new List<StreamInfo>());
            var stream = await streamProcessor.StartStreamForPlatform(streamEntity);
            streams.Add(stream);
        }
        catch (FfmpegProcessException ex)
        {
            _logger.LogError(ex,
                "FFmpeg error starting stream for nomination {NominationId} on platform {PlatformName}",
                streamEntity.NominationId, streamEntity.PlatformName);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error starting stream for nomination {NominationId} on platform {PlatformName}",
                streamEntity.NominationId, streamEntity.PlatformName);
            throw;
        }

    }

    // Остановка потока для определённой камеры и платформы
    public async Task StopStream(StreamEntity streamEntity)
    {
        using var scope = _scopeFactory.CreateScope();
        var streamProcessor = scope.ServiceProvider.GetRequiredService<IStreamProcessor>();

        try
        {
            if (_nominationStreams.TryGetValue(streamEntity.NominationId, out var streams))
            {
                var t = streams.Select(s => s.StreamId).ToList();
                _logger.LogInformation("StopStream !!!!!!   {t}  !!!!!1", string.Join(", ", t));

                var stream = streams.FirstOrDefault(s => s.StreamId == streamEntity.Id);

                if (stream != null)
                {
                    streamProcessor.StopStreamForPlatform(stream);
                    streams.Remove(stream);

                    if (!streams.Any())
                    {
                        _nominationStreams.TryRemove(streamEntity.NominationId, out _);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error stopping stream for nomination {NominationId} on platform {PlatformName}",
                streamEntity.NominationId, streamEntity.PlatformName);
            throw;
        }
    }

    // Запуск всех потоков
    public async Task StartAllStreams(IEnumerable<StreamEntity> streamEntities)
    {
        ArgumentNullException.ThrowIfNull(streamEntities);

        foreach (var entity in streamEntities)
        {
            await StartStream(entity);
        }
    }

    // Остановка всех потоков
    public async Task StopAllStreams(bool shouldLock = true)
    {

        try
        {
            foreach (var streams in _nominationStreams.Values)
            {
                foreach (var stream in streams.ToList())
                {
                    using var scope = _scopeFactory.CreateScope();
                    var streamProcessor = scope.ServiceProvider.GetRequiredService<IStreamProcessor>();
                    streamProcessor.StopStreamForPlatform(stream);
                    streams.Remove(stream);
                }
            }

            _nominationStreams.Clear();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping all streams");
            throw;
        }
    }

    public IEnumerable<StreamInfo> GetActiveStreamsProcesses()
    {
        return _nominationStreams.Values.SelectMany(s => s).ToList();
    }

    public async Task RemoveStreamFromNomination(StreamEntity streamEntity)
    {
        using var scope = _scopeFactory.CreateScope();
        var streamProcessor = scope.ServiceProvider.GetRequiredService<IStreamProcessor>();

        try
        {
            if (_nominationStreams.TryGetValue(streamEntity.NominationId, out var streams))
            {
                var stream = streams.FirstOrDefault(s => s.StreamId == streamEntity.Id);

                if (stream != null)
                {
                    streams.Remove(stream);

                    if (!streams.Any())
                    {
                        _nominationStreams.TryRemove(streamEntity.NominationId, out _);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error removing stream for nomination {NominationId} on platform {PlatformName}",
                streamEntity.NominationId, streamEntity.PlatformName);
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        _disposed = true;

        try
        {
            await StopAllStreams(false);
            _logger.LogInformation("StreamManager disposed asynchronously.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing StreamManager.");
        }

        GC.SuppressFinalize(this);
    }
}