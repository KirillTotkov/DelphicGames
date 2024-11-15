using DelphicGames.Data;
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

    // Запуск трансляции для определенной номинации на определенной платформе
    // Если трансляция уже запущена, то она будет перезапущена
    // Если токен не пустой, то он будет обновлен
    public void StartStream(AddStreamDto streamDto)
    {
        // TODO Использовать транзакции
        try
        {
            var nominationPlatform = _context.NominationPlatforms
                .Include(np => np.Nomination)
                .Include(np => np.Platform)
                .FirstOrDefault(
                    np => np.NominationId == streamDto.NominationId && np.PlatformId == streamDto.PlatformId);

            if (nominationPlatform != null)
            {
                if (!string.IsNullOrEmpty(nominationPlatform.Token))
                {
                    _streamManager.StartStream(nominationPlatform);
                    nominationPlatform.IsActive = true;
                    _context.SaveChanges();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при запуске трансляции.");
            throw;
        }
    }

    // Остановка трансляции для определенной номинации на определенной платформе
    public void StopStream(int nominationId, int platformId)
    {
        try
        {
            var nominationPlatform = _context.NominationPlatforms
                .Include(np => np.Nomination)
                .Include(np => np.Platform)
                .FirstOrDefault(np => np.NominationId == nominationId && np.PlatformId == platformId);

            if (nominationPlatform != null)
            {
                _streamManager.StopStream(nominationPlatform);
                nominationPlatform.IsActive = false;
                _context.SaveChanges();
            }
            else
            {
                _logger.LogWarning(
                    "Трансляция не найдена для NominationId: {NominationId}, PlatformId: {PlatformId}",
                    nominationId, platformId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при остановке трансляции.");
            throw;
        }
    }

    // Запуск всех трансляций у которых есть токен
    public async Task StartAllStreams()
    {
        try
        {
            var activePlatforms = await _context.NominationPlatforms
                .Include(np => np.Nomination)
                .Include(np => np.Platform)
                .Where(np => !string.IsNullOrEmpty(np.Token) && np.IsActive)
                .ToListAsync();

            var startTasks = activePlatforms.Select(np => Task.Run(() => _streamManager.StartStream(np)));
            await Task.WhenAll(startTasks);

            _logger.LogInformation("Все трансляции запущены.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при запуске всех трансляций.");
            throw;
        }
    }

    // Остановка всех трансляций
    public async Task StopAllStreams()
    {
        try
        {
            var activePlatforms = await _context.NominationPlatforms
                .Include(np => np.Nomination)
                .Include(np => np.Platform)
                .Where(np => np.IsActive)
                .ToListAsync();

            var stopTasks = activePlatforms.Select(np => Task.Run(() => _streamManager.StopStream(np)));
            await Task.WhenAll(stopTasks);

            _logger.LogInformation("Все трансляции остановлены.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при остановке всех трансляций.");
            throw;
        }
    }

    // Запуск трансляций на определенной платформе
    public async Task StartPlatformStreams(int platformId)
    {
        try
        {
            var cameraPlatforms = await _context.NominationPlatforms
                .Include(np => np.Nomination)
                .Include(np => np.Platform)
                .Where(np => np.PlatformId == platformId && !string.IsNullOrEmpty(np.Token) && !np.IsActive)
                .ToListAsync();

            if (cameraPlatforms == null || cameraPlatforms.Count == 0)
            {
                _logger.LogWarning("Нет трансляций для платформы с PlatformId: {PlatformId}", platformId);
                return;
            }

            var startTasks = cameraPlatforms.Select(np => Task.Run(() =>
            {
                _streamManager.StartStream(np);
                np.IsActive = true;
            }));

            await Task.WhenAll(startTasks);

            await _context.SaveChangesAsync();
            _logger.LogInformation("Трансляции для платформы {PlatformId} запущены.", platformId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при запуске трансляций для платформы.");
            throw;
        }
    }

    // Остановка трансляций на определенной платформе
    public async Task StopPlatformStreams(int platformId)
    {
        try
        {
            var platformStreams = await _context.NominationPlatforms
                .Include(np => np.Nomination)
                .Include(np => np.Platform)
                .Where(np => np.PlatformId == platformId && np.IsActive)
                .ToListAsync();

            var stopTasks = platformStreams.Select(np => Task.Run(() =>
            {
                _streamManager.StopStream(np);
                np.IsActive = false;
            }));

            await Task.WhenAll(stopTasks);

            await _context.SaveChangesAsync();
            _logger.LogInformation("Трансляции для платформы {PlatformId} остановлены.", platformId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при остановке трансляций для платформы.");
            throw;
        }
    }

    // Запуск трансляций для определенной номинации на всех платформах
    public void StartNominationStreams(int nominationId)
    {
        try
        {
            var nomination = _context.Nominations
                .Include(n => n.Platforms)
                .FirstOrDefault(n => n.Id == nominationId);

            if (nomination == null)
            {
                _logger.LogWarning("Номинация с Id: {NominationId} не найдена.", nominationId);
                return;
            }

            if (nomination.Platforms == null || nomination.Platforms.Count == 0)
            {
                _logger.LogWarning("Номинация с Id: {NominationId} не привязана ни к одной платформе.", nominationId);
                return;
            }

            foreach (var np in nomination.Platforms)
            {
                if (!string.IsNullOrEmpty(np.Token))
                {
                    _streamManager.StartStream(np);
                    np.IsActive = true;
                }
            }

            _context.SaveChanges();
            _logger.LogInformation("Трансляции для номинации {NominationId} запущены на всех платформах.",
                nominationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при запуске трансляций для номинации.");
            throw;
        }
    }

    // Остановка трансляций для определенной номинации
    public void StopNominationStreams(int nominationId)
    {
        try
        {
            var nominationPlatforms = _context.NominationPlatforms
                .Include(np => np.Nomination)
                .Include(np => np.Platform)
                .Where(np => np.NominationId == nominationId && np.IsActive)
                .ToList();

            foreach (var np in nominationPlatforms)
            {
                _streamManager.StopStream(np);
                np.IsActive = false;
            }

            _context.SaveChanges();
            _logger.LogInformation("Трансляции для номинации {NominationId} остановлены.", nominationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при остановке трансляций для номинации.");
            throw;
        }
    }

    // Получение всех трансляций
    public async Task<List<BroadcastDto>> GetAllStreams()
    {
        try
        {
            var streams = await _context.NominationPlatforms
                .Include(np => np.Nomination)
                .Include(np => np.Platform)
                .Where(cp => cp.Token != null && cp.Token != "")
                .AsNoTracking()
                .ToListAsync();

            var groupedSteams = streams
                .GroupBy(cp => cp.NominationId)
                .Select(g =>
                {
                    var nomination = g.First().Nomination;
                    return new BroadcastDto(
                        nomination.StreamUrl,
                        nomination.Id,
                        nomination.Name,
                        g.Select(cp => new PlatformStatusDto(
                            cp.Platform.Id,
                            cp.Platform.Name,
                            cp.IsActive
                        )).ToList()
                    );
                })
                .OrderBy(b => b.Nomination)
                .ToList();

            return groupedSteams;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении всех трансляций.");
            throw;
        }
    }

    // HasActiveStreams
    public bool HasActiveStreams(int nominationId)
    {
        return _streamManager.HasActiveStreams(nominationId);
    }
}

public record BroadcastDto(
    string Url,
    int NominationId,
    string Nomination,
    List<PlatformStatusDto> PlatformStatuses
);

public record PlatformStatusDto(
    int PlatformId,
    string Name,
    bool IsActive
);

public record AddStreamDto(
    int NominationId,
    int PlatformId,
    string? Token
);