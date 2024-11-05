using DelphicGames.Data;
using DelphicGames.Data.Models;
using DelphicGames.Services.Streaming;
using Microsoft.EntityFrameworkCore;

namespace DelphicGames.Services;

public class StreamService
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

    // Запуск трансляции для определенной камеры на определенной платформе
    // Если трансляция уже запущена, то она будет перезапущена
    // Если токен не пустой, то он будет обновлен
    public void StartStream(AddStreamDto streamDto)
    {
        try
        {
            var cameraPlatform = _context.CameraPlatforms
                .Include(cp => cp.Camera)
                .Include(cp => cp.Platform)
                .FirstOrDefault(cp => cp.CameraId == streamDto.CameraId && cp.PlatformId == streamDto.PlatformId);

            if (cameraPlatform != null)
            {
                if (!string.IsNullOrEmpty(streamDto.Token) && cameraPlatform.Token != streamDto.Token)
                {
                    cameraPlatform.Token = streamDto.Token.Trim();
                }

                _streamManager.StartStream(cameraPlatform);
                cameraPlatform.IsActive = true;
                _context.SaveChanges();
            }
            else
            {
                var camera = _context.Cameras.FirstOrDefault(c => c.Id == streamDto.CameraId);
                var platform = _context.Platforms.FirstOrDefault(p => p.Id == streamDto.PlatformId);

                if (camera == null || platform == null)
                {
                    _logger.LogWarning(
                        "Камера или платформа не найдены для CameraId: {CameraId}, PlatformId: {PlatformId}",
                        streamDto.CameraId, streamDto.PlatformId);

                    throw new InvalidOperationException("Камера или платформа не найдены.");
                }

                cameraPlatform = new CameraPlatform
                {
                    Camera = camera,
                    Platform = platform,
                    IsActive = true,
                };

                if (!string.IsNullOrEmpty(streamDto.Token))
                {
                    cameraPlatform.Token = streamDto.Token.Trim();
                }

                _streamManager.StartStream(cameraPlatform);

                _context.CameraPlatforms.Add(cameraPlatform);

                _context.SaveChanges();

                _logger.LogInformation("Трансляция начата для CameraId: {CameraId}, PlatformId: {PlatformId}",
                    streamDto.CameraId, streamDto.PlatformId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при запуске трансляции для CameraId: {CameraId}, PlatformId: {PlatformId}",
                streamDto.CameraId, streamDto.PlatformId);
            throw;
        }
    }

    // Остановка трансляции для определенной камеры на определенной платформе
    public void StopStream(int cameraId, int platformId)
    {
        try
        {
            var cameraPlatform = _context.CameraPlatforms
                .Include(cp => cp.Camera)
                .Include(cp => cp.Platform)
                .FirstOrDefault(cp => cp.CameraId == cameraId && cp.PlatformId == platformId);

            if (cameraPlatform != null)
            {
                _streamManager.StopStream(cameraPlatform);
                cameraPlatform.IsActive = false;
                _context.SaveChanges();
            }
            else
            {
                _logger.LogWarning("CameraPlatform не найдена для CameraId: {CameraId}, PlatformId: {PlatformId}",
                    cameraId, platformId);

                throw new InvalidOperationException("Камера или платформа не найдены.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при остановке трансляции для CameraId: {CameraId}, PlatformId: {PlatformId}",
                cameraId, platformId);
            throw;
        }
    }

    // Запуск всех трансляций у которых есть токен
    public void StartAllStreams()
    {
        try
        {
            var cameraPlatforms = _context.CameraPlatforms
                .Include(cp => cp.Camera)
                .Where(cp => !string.IsNullOrEmpty(cp.Token))
                .ToList();

            _streamManager.StartAllStreams(cameraPlatforms);

            foreach (var cp in cameraPlatforms)
            {
                cp.IsActive = true;
            }

            _context.SaveChanges();
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
            var cameraPlatforms = _context.CameraPlatforms.ToList();

            foreach (var cp in cameraPlatforms)
            {
                cp.IsActive = false;
            }

            _context.SaveChanges();
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
                .Include(cp => cp.Camera)
                .Include(cp => cp.Platform)
                .Where(cp => cp.PlatformId == platformId && !string.IsNullOrEmpty(cp.Token))
                .ToList();

            if (cameraPlatforms == null || cameraPlatforms.Count == 0)
            {
                throw new InvalidOperationException($"Камеры для платформы с ID {platformId} не найдены.");
            }

            foreach (var cp in cameraPlatforms)
            {
                _streamManager.StartStream(cp);
                cp.IsActive = true;
            }

            _context.SaveChanges();
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
                .Include(cp => cp.Camera)
                .Include(cp => cp.Platform)
                .Where(cp => cp.PlatformId == platformId)
                .ToList();

            if (cameraPlatforms == null || cameraPlatforms.Count == 0)
            {
                throw new InvalidOperationException($"Камеры для платформы с ID {platformId} не найдены.");
            }

            foreach (var cp in cameraPlatforms)
            {
                _streamManager.StopStream(cp);
                cp.IsActive = false;
            }

            _context.SaveChanges();
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
                .Include(cp => cp.Camera)
                .Include(cp => cp.Platform)
                .Where(cp => cp.CameraId == cameraId && !string.IsNullOrEmpty(cp.Token))
                .ToList();

            if (cameraPlatforms == null || cameraPlatforms.Count == 0)
            {
                throw new InvalidOperationException($"Платформы для камеры с ID {cameraId} не найдены.");
            }

            foreach (var cp in cameraPlatforms)
            {
                _streamManager.StartStream(cp);
                cp.IsActive = true;
            }

            _context.SaveChanges();
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
                .Include(cp => cp.Camera)
                .Include(cp => cp.Platform)
                .Where(cp => cp.CameraId == cameraId)
                .ToList();

            if (cameraPlatforms == null || cameraPlatforms.Count == 0)
            {
                throw new InvalidOperationException($"Платформы для камеры с ID {cameraId} не найдены.");
            }

            foreach (var cp in cameraPlatforms)
            {
                _streamManager.StopStream(cp);
                cp.IsActive = false;
            }

            _context.SaveChanges();
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
            .ToList();

        if (cameras == null || cameras.Count == 0)
        {
            throw new InvalidOperationException($"Камеры для номинации с ID {nominationId} не найдены.");
        }

        var cameraPlatforms = _context.CameraPlatforms
            .Include(cp => cp.Camera)
            .Include(cp => cp.Platform)
            .Where(cp => cameras.Contains(cp.Camera) && !string.IsNullOrEmpty(cp.Token))
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
                cp.IsActive = true;
            }
            catch (Exception ex)
            {
                // Логирование ошибки и продолжение цикла
                _logger.LogError(ex, "Ошибка запуска трансляции для камеры {CameraId} на платформе {PlatformId}",
                    cp.CameraId, cp.PlatformId);
            }
        }

        _context.SaveChanges();
    }

    // Остановка трансляций для определенной номинации
    public void StopNominationStreams(int nominationId)
    {
        var cameras = _context.Cameras
            .Where(c => c.Nomination != null && c.Nomination.Id == nominationId)
            .ToList();

        if (cameras == null || cameras.Count == 0)
        {
            throw new InvalidOperationException($"Камеры для номинации с ID {nominationId} не найдены.");
        }

        var cameraPlatforms = _context.CameraPlatforms
            .Include(cp => cp.Camera)
            .Include(cp => cp.Platform)
            .Where(cp => cameras.Contains(cp.Camera))
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
                cp.IsActive = false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка остановки трансляции для камеры {CameraId} на платформе {PlatformId}",
                    cp.CameraId, cp.PlatformId);
            }
        }

        _context.SaveChanges();
    }

    // Получение всех трансляций
    public async Task<List<BroadcastDto>> GetAllStreams()
    {
        var broadcasts = await _context.CameraPlatforms
            .Include(cp => cp.Camera)
                .ThenInclude(c => c.Nomination)
            .Include(cp => cp.Camera.City)
            .Include(cp => cp.Platform)
            .Where(cp => cp.Token != null && cp.Token != "")
            .AsNoTracking()
            .ToListAsync();

        var groupedBroadcasts = broadcasts
            .GroupBy(cp => cp.CameraId)
            .Select(g =>
            {
                var camera = g.First().Camera;
                return new BroadcastDto(
                    camera.Url,
                    camera.City?.Name ?? "N/A",
                    camera.Nomination?.Id ?? 0,
                    camera.Nomination?.Name ?? "N/A",
                    camera.Id,
                    camera.Name,
                    g.Select(cp => new PlatformStatusDto(
                        cp.Platform.Id,
                        cp.Platform.Name,
                        cp.IsActive
                    )).ToList()
                );
            })
            .OrderBy(b => b.Nomination)
            .ThenBy(b => b.City)
            .ToList();

        return groupedBroadcasts;
    }
}

public record BroadcastDto(
    string Url,
    string City,
    int NominationId,
    string Nomination,
    int CameraId,
    string CameraName,
    List<PlatformStatusDto> PlatformStatuses
);

public record PlatformStatusDto(
    int PlatformId,
    string Name,
    bool IsActive
);

public record AddStreamDto(
    int CameraId,
    int PlatformId,
    string? Token
);