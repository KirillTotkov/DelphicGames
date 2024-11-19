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

    public async Task AddDay(AddDayDto dayDto)
    {
        if (dayDto == null)
        {
            throw new ArgumentNullException(nameof(dayDto), "Данные дня не должны быть null.");
        }

        if (dayDto.Day <= 0)
        {
            throw new ArgumentException("День должен быть положительным числом.", nameof(dayDto.Day));
        }

        if (dayDto.DayStreams == null || !dayDto.DayStreams.Any())
        {
            throw new ArgumentException("Список трансляций не может быть пустым.", nameof(dayDto.DayStreams));
        }

        if (string.IsNullOrWhiteSpace(dayDto.StreamUrl))
        {
            throw new ArgumentException("URL трансляции не может быть пустым.", nameof(dayDto.StreamUrl));
        }

        if (await _context.Nominations.AnyAsync(n =>
            n.StreamUrl.ToLower() == dayDto.StreamUrl.ToLower() && n.Id != dayDto.NominationId))
        {
            throw new ArgumentException($"Номинация с ссылкой {dayDto.StreamUrl} уже существует");
        }

        foreach (var dayStream in dayDto.DayStreams)
        {
            if (string.IsNullOrWhiteSpace(dayStream.PlatformName))
            {
                throw new ArgumentException("Имя платформы не может быть пустым.", nameof(dayStream.PlatformName));
            }

            if (string.IsNullOrWhiteSpace(dayStream.PlatformUrl))
            {
                throw new ArgumentException("URL платформы не может быть пустым.", nameof(dayStream.PlatformUrl));
            }

            if (string.IsNullOrWhiteSpace(dayStream.Token))
            {
                throw new ArgumentException("Токен не может быть пустым.", nameof(dayStream.Token));
            }
        }

        try
        {
            var nomination = await _context.Nominations
                .Include(n => n.Streams)
                .FirstOrDefaultAsync(n => n.Id == dayDto.NominationId);

            if (nomination == null)
            {
                throw new InvalidOperationException("Номинация не найдена.");
            }

            nomination.StreamUrl = dayDto.StreamUrl;

            var streams = dayDto.DayStreams.Select(dayStream =>
            {
                var stream = new StreamEntity
                {
                    NominationId = dayDto.NominationId,
                    PlatformName = dayStream.PlatformName,
                    PlatformUrl = dayStream.PlatformUrl,
                    Token = dayStream.Token,
                    Day = dayDto.Day,
                    IsActive = false
                };

                return stream;
            }).ToList();

            await _context.Streams.AddRangeAsync(streams);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Добавлены трансляции для номинации {NominationId} на день {Day}.",
                dayDto.NominationId, dayDto.Day);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при добавлении трансляции.");
            throw;
        }
    }

    public async Task DeleteStream(int streamId)
    {
        try
        {
            var stream = await _context.Streams
                .FirstOrDefaultAsync(s => s.Id == streamId);

            if (stream == null)
            {
                throw new InvalidOperationException("Stream not found.");
            }

            _context.Streams.Remove(stream);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Удалена трансляция {StreamId}.", streamId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing stream.");
            throw;
        }
    }

    public async Task UpdateStream(int nominationId, int streamId, string token)
    {
        try
        {
            var stream = await _context.Streams
                .FirstOrDefaultAsync(s => s.NominationId == nominationId && s.Id == streamId);

            if (stream == null)
            {
                throw new InvalidOperationException("Stream not found.");
            }

            stream.Token = token;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Обновлен токен для трансляции {StreamId} для номинации {NominationId}.",
                streamId, nominationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating stream.");
            throw;
        }
    }

    // Запуск трансляции для определенной номинации на определенной платформе
    // Если трансляция уже запущена, то она будет перезапущена
    // Если токен не пустой, то он будет обновлен
    public void StartStream(AddDayDto dayDto)
    {
        try
        {
            foreach (var dayStream in dayDto.DayStreams)
            {
                var stream = _context.Streams
                    .Include(s => s.Nomination)
                    .FirstOrDefault(s => s.NominationId == dayDto.NominationId &&
                                         s.PlatformName == dayStream.PlatformName);

                if (stream != null)
                {
                    if (!string.IsNullOrEmpty(dayStream.Token))
                    {
                        stream.Token = dayStream.Token;
                        _context.SaveChanges();
                    }

                    _streamManager.StartStream(stream);
                    stream.IsActive = true;
                    _context.SaveChanges();
                }
                else
                {
                    throw new InvalidOperationException("Stream not found.");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting stream.");
            throw;
        }
    }

    // Остановка трансляции для определенной номинации на определенной платформе
    public void StopStream(int nominationId, string platformName)
    {
        try
        {
            var stream = _context.Streams
                .FirstOrDefault(s => s.NominationId == nominationId && s.PlatformName == platformName);

            if (stream != null)
            {
                _streamManager.StopStream(stream);
                stream.IsActive = false;
                _context.SaveChanges();
            }
            else
            {
                throw new InvalidOperationException("Stream not found.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping stream.");
            throw;
        }
    }

    // Запуск всех трансляций у которых есть токен
    public async Task StartAllStreams()
    {
        try
        {
            var activePlatforms = await _context.Streams
                .Include(np => np.Nomination)
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
            var activePlatforms = await _context.Streams
                .Include(np => np.Nomination)
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

    // Запуск трансляций для определенной номинации на всех платформах
    public void StartNominationStreams(int nominationId)
    {
        try
        {
            var nomination = _context.Nominations
                .Include(n => n.Streams)
                .FirstOrDefault(n => n.Id == nominationId);

            if (nomination == null)
            {
                _logger.LogWarning("Номинация с Id: {NominationId} не найдена.", nominationId);
                return;
            }

            if (nomination.Streams == null || nomination.Streams.Count == 0)
            {
                _logger.LogWarning("Номинация с Id: {NominationId} не привязана ни к одной платформе.", nominationId);
                return;
            }

            foreach (var np in nomination.Streams)
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
            var nominationPlatforms = _context.Streams
                .Include(np => np.Nomination)
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
    public async Task<List<GetStreamsDto>> GetAllStreams()
    {
        try
        {
            var nominations = await _context.Nominations
                .Include(n => n.Streams)
                .AsNoTracking()
                .ToListAsync();

            var groupedStreams = nominations
                .Select(nomination => new GetStreamsDto(
                    nomination.Id,
                    nomination.StreamUrl,
                    nomination.Name,
                    nomination.Streams
                        .Where(s => !string.IsNullOrEmpty(s.Token))
                        .Select(s => new GetStreamDto(
                            s.Id,
                            s.Day,
                            s.PlatformName,
                            s.PlatformUrl,
                            s.Token
                        ))
                        .ToList()
                ))
                .OrderBy(dto => dto.Nomination)
                .ToList();

            return groupedStreams;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching streams");
            throw;
        }
    }
}

public record GetStreamsDto(
    int NominationId,
    string StreamUrl,
    string Nomination,
    List<GetStreamDto> Streams
);

public record GetStreamDto(
    int Id,
    int Day,
    string PlatformName,
    string PlatformUrl,
    string Token
);

public record BroadcastDto(
    string Url,
    int NominationId,
    string Nomination,
    List<PlatformStatusDto> PlatformStatuses
);

public record PlatformStatusDto(
    string PlatformName,
    bool IsActive
);

public record AddDayDto(
    int NominationId,
    string StreamUrl,
    int Day,
    List<DayStreamDto> DayStreams
);

public record DayStreamDto(
    string PlatformName,
    string PlatformUrl,
    string Token
);