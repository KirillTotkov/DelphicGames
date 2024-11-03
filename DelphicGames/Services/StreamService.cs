using DelphicGames.Data;
using DelphicGames.Services.Streaming;
using Microsoft.EntityFrameworkCore;

namespace DelphicGames.Services;

public class StreamService : IDisposable
{
    private readonly ApplicationContext _context;
    private readonly ILogger<StreamService> _logger;
    private readonly StreamManager _streamManager;

    public StreamService(ApplicationContext context, StreamManager streamManager, ILogger<StreamService> logger)
    {
        _context = context;
        _streamManager = streamManager;
        _logger = logger;
    }

    public void Dispose()
    {
        _streamManager.Dispose();
    }

    // Запуск трансляции для определенной камеры на определенной платформе
    public void StartStream(int cameraId, int platformId)
    {
        try
        {
            var cameraPlatform = _context.CameraPlatforms
                .AsNoTracking()
                .FirstOrDefault(cp => cp.CameraId == cameraId && cp.PlatformId == platformId);

            if (cameraPlatform != null)
            {
                _streamManager.StartStream(cameraPlatform);
            }
            else
            {
                _logger.LogWarning("CameraPlatform не найдена для CameraId: {CameraId}, PlatformId: {PlatformId}",
                    cameraId, platformId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при запуске трансляции для CameraId: {CameraId}, PlatformId: {PlatformId}",
                cameraId, platformId);
            throw;
        }
    }

    // Остановка трансляции для определенной камеры на определенной платформе
    public void StopStream(int cameraId, int platformId)
    {
        try
        {
            var cameraPlatform = _context.CameraPlatforms
                .AsNoTracking()
                .FirstOrDefault(cp => cp.CameraId == cameraId && cp.PlatformId == platformId);

            if (cameraPlatform != null)
            {
                _streamManager.StopStream(cameraPlatform);
            }
            else
            {
                _logger.LogWarning("CameraPlatform не найдена для CameraId: {CameraId}, PlatformId: {PlatformId}",
                    cameraId, platformId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при остановке трансляции для CameraId: {CameraId}, PlatformId: {PlatformId}",
                cameraId, platformId);
            throw;
        }
    }

    // Запуск всех трансляций
    public void StartAllStreams()
    {
        try
        {
            var cameraPlatforms = _context.CameraPlatforms.AsNoTracking().ToList();
            _streamManager.StartAllStreams(cameraPlatforms);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при запуске всех трансляций.");
            throw;
        }
    }

    // Остановка всех трансляций
    public void StopAllStreams()
    {
        try
        {
            _streamManager.StopAllStreams();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при остановке всех трансляций.");
            throw;
        }
    }

    // Запуск трансляций на определенной платформе
    public void StartPlatformStreams(int platformId)
    {
        try
        {
            var cameraPlatforms = _context.CameraPlatforms
                .Where(cp => cp.PlatformId == platformId)
                .AsNoTracking()
                .ToList();

            foreach (var cp in cameraPlatforms)
            {
                _streamManager.StartStream(cp);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при запуске трансляций на PlatformId: {PlatformId}", platformId);
            throw;
        }
    }

    // Остановка трансляций на определенной платформе
    public void StopPlatformStreams(int platformId)
    {
        try
        {
            var cameraPlatforms = _context.CameraPlatforms
                .Where(cp => cp.PlatformId == platformId)
                .AsNoTracking()
                .ToList();

            foreach (var cp in cameraPlatforms)
            {
                _streamManager.StopStream(cp);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при остановке трансляций на PlatformId: {PlatformId}", platformId);
            throw;
        }
    }

    // Запуск трансляций для определенной камеры на всех платформах
    public void StartCameraStreams(int cameraId)
    {
        try
        {
            var cameraPlatforms = _context.CameraPlatforms
                .Where(cp => cp.CameraId == cameraId)
                .AsNoTracking()
                .ToList();

            foreach (var cp in cameraPlatforms)
            {
                _streamManager.StartStream(cp);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при запуске трансляций для CameraId: {CameraId}", cameraId);
            throw;
        }
    }

    // Остановка трансляций для определенной камеры на всех платформах
    public void StopCameraStreams(int cameraId)
    {
        try
        {
            var cameraPlatforms = _context.CameraPlatforms
                .Where(cp => cp.CameraId == cameraId)
                .AsNoTracking()
                .ToList();

            foreach (var cp in cameraPlatforms)
            {
                _streamManager.StopStream(cp);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при остановке трансляций для CameraId: {CameraId}", cameraId);
            throw;
        }
    }

    // Запуск трансляций для определенной номинации
    public void StartNominationStreamsAsync(int nominationId)
    {
        var cameras = _context.Cameras
            .Where(c => c.Nomination != null && c.Nomination.Id == nominationId)
            .AsNoTracking()
            .ToList();

        if (cameras == null || cameras.Count == 0)
        {
            throw new InvalidOperationException($"Камеры для номинации с ID {nominationId} не найдены.");
        }

        var cameraPlatforms = _context.CameraPlatforms
            .Where(cp => cameras.Contains(cp.Camera))
            .AsNoTracking()
            .ToList();

        if (cameraPlatforms == null || cameraPlatforms.Count == 0)
        {
            throw new InvalidOperationException("Платформы для указанных камер не найдены.");
        }

        foreach (var cp in cameraPlatforms)
        {
            try
            {
                _streamManager.StartStream(cp);
            }
            catch (Exception ex)
            {
                // Логирование ошибки и продолжение цикла
                _logger.LogError(ex, "Ошибка запуска трансляции для камеры {CameraId} на платформе {PlatformId}",
                    cp.CameraId, cp.PlatformId);
            }
        }
    }

    // Остановка трансляций для определенной номинации
    public void StopNominationStreams(int nominationId)
    {
        var cameras = _context.Cameras
            .Where(c => c.Nomination != null && c.Nomination.Id == nominationId)
            .AsNoTracking()
            .ToList();

        if (cameras == null || cameras.Count == 0)
        {
            throw new InvalidOperationException($"Камеры для номинации с ID {nominationId} не найдены.");
        }

        var cameraPlatforms = _context.CameraPlatforms
            .Where(cp => cameras.Contains(cp.Camera))
            .AsNoTracking()
            .ToList();

        if (cameraPlatforms == null || cameraPlatforms.Count == 0)
        {
            throw new InvalidOperationException("Платформы для указанных камер не найдены.");
        }

        foreach (var cp in cameraPlatforms)
        {
            try
            {
                _streamManager.StopStream(cp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка остановки трансляции для камеры {CameraId} на платформе {PlatformId}",
                    cp.CameraId, cp.PlatformId);
            }
        }
    }
}