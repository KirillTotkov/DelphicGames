using System.Collections.Concurrent;
using DelphicGames.Data.Models;
using DelphicGames.Models;

namespace DelphicGames.Services.Streaming;

public class StreamManager
{
    private readonly ConcurrentDictionary<int, List<StreamInfo>> _nominationStreams = new();
    private readonly ILogger<StreamManager> _logger;
    // private readonly IStreamProcessor _streamProcessor;
    private bool _disposed;
    private readonly SemaphoreSlim _semaphore = new(1);
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
            await _semaphore.WaitAsync();
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
        finally
        {
            _semaphore.Release();
        }
    }

    // Остановка потока для определённой камеры и платформы
    public async Task StopStream(StreamEntity streamEntity)
    {
        using var scope = _scopeFactory.CreateScope();
        var streamProcessor = scope.ServiceProvider.GetRequiredService<IStreamProcessor>();

        try
        {
            await _semaphore.WaitAsync();
            if (_nominationStreams.TryGetValue(streamEntity.NominationId, out var streams))
            {
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
        finally
        {
            _semaphore.Release();
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
    public async Task StopAllStreams()
    {
        using var scope = _scopeFactory.CreateScope();
        var streamProcessor = scope.ServiceProvider.GetRequiredService<IStreamProcessor>();

        try
        {
            await _semaphore.WaitAsync();
            foreach (var streams in _nominationStreams.Values)
            {
                foreach (var stream in streams.ToList())
                {
                    streamProcessor.StopStreamForPlatform(stream);
                }
            }
            _nominationStreams.Clear();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public IEnumerable<StreamInfo> GetActiveStreamsProcesses()
    {
        return _nominationStreams.Values.SelectMany(s => s).ToList();
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
            _semaphore.Wait();
            try
            {
                StopAllStreams().GetAwaiter().GetResult();
                _semaphore.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing StreamManager.");
            }
            finally
            {
                _disposed = true;
            }
        }
    }
}