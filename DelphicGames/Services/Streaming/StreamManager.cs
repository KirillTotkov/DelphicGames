using System.Collections.Concurrent;
using DelphicGames.Data.Models;
using DelphicGames.Models;

namespace DelphicGames.Services.Streaming;

public class StreamManager
{
    private readonly ConcurrentDictionary<int, List<StreamInfo>> _nominationStreams = new();
    private readonly ILogger<StreamManager> _logger;
    private readonly IStreamProcessor _streamProcessor;
    private bool _disposed;
    private readonly SemaphoreSlim _semaphore = new(1);

    public StreamManager(IStreamProcessor streamProcessor, ILogger<StreamManager> logger)
    {
        _streamProcessor = streamProcessor;
        _logger = logger;
    }

    // Запуск потока для определённой камеры и платформы
    public async Task StartStream(StreamEntity streamEntity)
    {
        try
        {
            await _semaphore.WaitAsync();
            var streams = _nominationStreams.GetOrAdd(streamEntity.NominationId, _ => new List<StreamInfo>());
            var stream = _streamProcessor.StartStreamForPlatform(streamEntity);
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
        try
        {
            await _semaphore.WaitAsync();
            if (_nominationStreams.TryGetValue(streamEntity.NominationId, out var streams))
            {
                var stream = streams.FirstOrDefault(s =>
                    s.PlatformUrl == streamEntity.PlatformUrl && s.Token == streamEntity.Token);

                if (stream != null)
                {
                    _streamProcessor.StopStreamForPlatform(stream);
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
        try
        {
            await _semaphore.WaitAsync();
            foreach (var streams in _nominationStreams.Values)
            {
                foreach (var stream in streams.ToList())
                {
                    _streamProcessor.StopStreamForPlatform(stream);
                }
            }
            _nominationStreams.Clear();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    // Проверка наличия активных потоков для номинации
    public bool HasActiveStreams(int nominationId)
    {
        return _nominationStreams.TryGetValue(nominationId, out var streams) && streams.Any();
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
                _streamProcessor.Dispose();
                _semaphore.Dispose();
            }
            finally
            {
                _disposed = true;
            }
        }
    }
}