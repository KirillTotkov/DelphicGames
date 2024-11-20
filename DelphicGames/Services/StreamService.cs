using DelphicGames.Data;
using DelphicGames.Data.Models;
using DelphicGames.Hubs;
using DelphicGames.Services.Streaming;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace DelphicGames.Services;

public class StreamService : INotificationHandler<StreamStatusChangedEvent>
{
    private readonly ApplicationContext _context;
    private readonly ILogger<StreamService> _logger;
    private readonly StreamManager _streamManager;
    private readonly IHubContext<StreamHub> _hubContext;

    public StreamService(ApplicationContext context, StreamManager streamManager, ILogger<StreamService> logger,
        IHubContext<StreamHub> hubContext)
    {
        _context = context;
        _streamManager = streamManager;
        _logger = logger;
        _hubContext = hubContext;
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
                    nomination.Name,
                    nomination.Streams
                        .Where(s => !string.IsNullOrEmpty(s.Token))
                        .Select(s => new GetStreamDto(
                            s.Id,
                            s.Day,
                            s.PlatformName,
                            s.PlatformUrl,
                            s.Token,
                            s.IsActive,
                            s.StreamUrl
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

            var streams = dayDto.DayStreams.Select(dayStream =>
            {
                var stream = new StreamEntity
                {
                    NominationId = dayDto.NominationId,
                    PlatformName = dayStream.PlatformName,
                    PlatformUrl = dayStream.PlatformUrl,
                    Token = dayStream.Token,
                    StreamUrl = dayDto.StreamUrl,
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

    public async Task<GetStreamsDto> GetNominationStreams(int nominationId)
    {
        var nomination = await _context.Nominations
            .Include(n => n.Streams)
            .FirstOrDefaultAsync(n => n.Id == nominationId);

        if (nomination == null)
        {
            throw new InvalidOperationException("Номинация не найдена.");
        }

        var streams = nomination.Streams
            .Where(s => !string.IsNullOrEmpty(s.Token))
            .Select(s => new GetStreamDto(
                s.Id,
                s.Day,
                s.PlatformName,
                s.PlatformUrl,
                s.Token,
                s.IsActive,
                s.StreamUrl
            ))
            .ToList();

        return new GetStreamsDto(nomination.Id, nomination.Name, streams);
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

            _streamManager.StopStream(stream);

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

    public async Task StartStreamAsync(int streamId)
    {
        try
        {
            var stream = _context.Streams
                .Include(s => s.Nomination)
                .FirstOrDefault(s => s.Id == streamId);

            if (stream == null)
            {
                throw new InvalidOperationException("Stream not found.");
            }

            if (string.IsNullOrEmpty(stream.Token))
            {
                throw new InvalidOperationException("Token is empty.");
            }

            await _streamManager.StartStream(stream);
            stream.IsActive = true;
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting stream.");
            throw;
        }
    }

    public async Task StopStreamAsync(int streamId)
    {
        try
        {
            var stream = _context.Streams
                .FirstOrDefault(s => s.Id == streamId);

            if (stream != null)
            {
                await _streamManager.StopStream(stream);
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

    // Запуск всех трансляций для определенного дня
    public async Task StartStreamsByDay(int day)
    {
        try
        {
            var streamsByDay = await _context.Streams
                .Include(np => np.Nomination)
                .Where(np => np.Day == day && !string.IsNullOrEmpty(np.Token) && !np.IsActive)
                .ToListAsync();

            if (streamsByDay.Count == 0)
            {
                throw new InvalidOperationException($"Не запущенные трансляции для дня {day} не найдены.");
            }

            var startTasks = streamsByDay.Select(np => Task.Run(async () =>
            {
                await _streamManager.StartStream(np);
                np.IsActive = true;
            }));
            await Task.WhenAll(startTasks);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Все трансляции для дня {Day} запущены.", day);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при запуске всех трансляций для дня.");
            throw;
        }
    }

    // Остановка всех трансляций для определенного дня
    public async Task StopStreamsByDay(int day)
    {
        try
        {
            var streamsByDay = await _context.Streams
                .Include(np => np.Nomination)
                .Where(np => np.Day == day && np.IsActive)
                .ToListAsync();

            if (streamsByDay.Count == 0)
            {
                throw new InvalidOperationException($"Запущенные трансляции для дня {day} не найдены.");
            }

            var stopTasks = streamsByDay.Select(np => Task.Run(async () =>
            {
                await _streamManager.StopStream(np);
                np.IsActive = false;
            }));
            await Task.WhenAll(stopTasks);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Все трансляции для дня {Day} остановлены.", day);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при остановке всех трансляций для дня.");
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

            var startTasks = activePlatforms.Select(np => Task.Run(async () =>
            {
                await _streamManager.StartStream(np);
                np.IsActive = true;
            }));
            await Task.WhenAll(startTasks);

            await _context.SaveChangesAsync();

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

            await _streamManager.StopAllStreams();

            foreach (var platform in activePlatforms)
            {
                platform.IsActive = false;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Все трансляции остановлены.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при остановке всех трансляций.");
            throw;
        }
    }

    // Запуск трансляций для определенной номинации на всех платформах
    public async Task StartNominationStreams(int nominationId)
    {
        try
        {
            var nomination = await _context.Nominations
                .Include(n => n.Streams)
                .FirstOrDefaultAsync(n => n.Id == nominationId);

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
                    await _streamManager.StartStream(np);
                    np.IsActive = true;
                }
            }

            await _context.SaveChangesAsync();
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
    public async Task StopNominationStreams(int nominationId)
    {
        try
        {
            var nominationPlatforms = await _context.Streams
                .Include(np => np.Nomination)
                .Where(np => np.NominationId == nominationId && np.IsActive)
                .ToListAsync();

            foreach (var np in nominationPlatforms)
            {
                await _streamManager.StopStream(np);
                np.IsActive = false;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Трансляции для номинации {NominationId} остановлены.", nominationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при остановке трансляций для номинации.");
            throw;
        }
    }

    public async Task Handle(StreamStatusChangedEvent notification, CancellationToken cancellationToken)
    {
        var streamEntity = await _context.Streams.FindAsync(notification.StreamEntityId);
        if (streamEntity == null)
        {
            _logger.LogError($"StreamEntity with ID {notification.StreamEntityId} not found.");
            return;
        }

        switch (notification.Status)
        {
            case StreamStatus.Running:
                streamEntity.IsActive = true;
                break;
            case StreamStatus.Completed:
            case StreamStatus.Error:
                streamEntity.IsActive = false;
                await _streamManager.StopStream(streamEntity);
                break;
        }
        await _context.SaveChangesAsync(cancellationToken);

        var streamStatusDto = new
        {
            StreamId = streamEntity.Id,
            Status = notification.Status.ToString(),
            ErrorMessage = notification.ErrorMessage
        };

        await _hubContext.Clients.All.SendAsync("StreamStatusChanged", streamStatusDto, cancellationToken);

        _logger.LogInformation($"Stream status updated for StreamEntity ID {streamEntity.Id}: {notification.Status}");
    }
}

public record GetStreamsDto(
    int NominationId,
    string Nomination,
    List<GetStreamDto> Streams
);

public record GetStreamDto(
    int Id,
    int Day,
    string PlatformName,
    string PlatformUrl,
    string Token,
    bool IsActive,
    string StreamUrl
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