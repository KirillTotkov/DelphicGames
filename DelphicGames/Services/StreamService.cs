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
    private readonly IHubContext<StreamHub> _hubContext;
    private readonly ILogger<StreamService> _logger;
    private readonly StreamManager _streamManager;

    public StreamService(ApplicationContext context, StreamManager streamManager, ILogger<StreamService> logger,
        IHubContext<StreamHub> hubContext)
    {
        _context = context;
        _streamManager = streamManager;
        _logger = logger;
        _hubContext = hubContext;
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
                // streamEntity.IsActive = true;
                break;
            case StreamStatus.Error:
            case StreamStatus.Completed:
                streamEntity.IsActive = false;
                await _streamManager.RemoveStreamFromNomination(streamEntity);
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

    public async Task AddStream(AddStreamDto streamDto)
    {
        if (streamDto == null)
        {
            throw new ArgumentNullException(nameof(streamDto), "Данные трансляции не должны быть null.");
        }

        ValidateInput(streamDto.Day, streamDto.StreamUrl, streamDto.PlatformName, streamDto.PlatformUrl,
            streamDto.Token);

        string? streamUrl = streamDto.StreamUrl.Trim();
        string? platformName = streamDto.PlatformName?.Trim();
        string? platformUrl = streamDto.PlatformUrl?.Trim();
        string? token = streamDto.Token?.Trim();

        if (!string.IsNullOrWhiteSpace(token))
        {
            var exists = await _context.Streams.AnyAsync(s =>
                s.Token != null && s.Token.Trim() == token &&
                s.PlatformUrl != null && s.PlatformUrl.Trim() == platformUrl);

            if (exists)
            {
                throw new InvalidOperationException("Трансляция с такими параметрами уже существует.");
            }
        }

        try
        {
            var nomination = await _context.Nominations
                .Include(n => n.Streams)
                .FirstOrDefaultAsync(n => n.Id == streamDto.NominationId);

            if (nomination == null)
            {
                throw new InvalidOperationException("Номинация не найдена.");
            }

            var stream = new StreamEntity
            {
                NominationId = streamDto.NominationId,
                Day = streamDto.Day,
                StreamUrl = streamUrl,
                PlatformName = platformName,
                PlatformUrl = platformUrl,
                Token = token,
                IsActive = false
            };

            await _context.Streams.AddAsync(stream);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Добавлена трансляция {StreamId} для номинации {NominationId}.", stream.Id,
                streamDto.NominationId);
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

            if (stream.IsActive)
            {
                throw new InvalidOperationException("Нельзя удалять запущенную трансляцию.");
            }

            await _streamManager.StopStream(stream);

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

    public async Task UpdateStream(int streamId, UpdateStreamDto streamDto)
    {
        if (streamDto == null)
        {
            throw new ArgumentNullException("Данные трансляции не должны быть null.");
        }

        try
        {
            var stream = await _context.Streams
                .FirstOrDefaultAsync(s => s.Id == streamId);

            if (stream == null)
            {
                throw new InvalidOperationException("Трансляция не найдена");
            }

            if (stream.IsActive)
            {
                throw new InvalidOperationException("Нельзя изменять запущенную трансляцию");
            }


            var platformName = streamDto.PlatformName?.Trim();
            var platformUrl = streamDto.PlatformUrl?.Trim();
            var token = streamDto.Token?.Trim();
            var streamUrl = streamDto.StreamUrl?.Trim();

            ValidateInput(streamDto.Day, streamDto.StreamUrl, platformName, platformUrl, token);


            if (!string.IsNullOrWhiteSpace(token))
            {
                var exists = await _context.Streams.AnyAsync(s =>
                    s.Id != streamId &&
                    s.Token != null && s.Token.Trim() == token &&
                    s.PlatformUrl != null && s.PlatformUrl.Trim() == platformUrl);

                if (exists)
                {
                    throw new InvalidOperationException("Трансляция с такими параметрами уже существует.");
                }
            }


            stream.Day = streamDto.Day;
            stream.PlatformName = platformName;
            stream.PlatformUrl = platformUrl;
            stream.StreamUrl = streamUrl;
            stream.Token = token;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Обновлена трансляции {StreamId}.", streamId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка изменения трансляции");
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

            await _streamManager.StartStream(stream).ConfigureAwait(false);
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
                await _streamManager.StopStream(stream).ConfigureAwait(false);
                stream.IsActive = false;
                await _context.SaveChangesAsync();
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
    public async Task<bool> StartStreamsByDay(int day)
    {
        try
        {
            var streamsByDay = await _context.Streams
                .Include(np => np.Nomination)
                .Where(np => np.Day == day && !np.IsActive &&
                             !string.IsNullOrEmpty(np.Token) && !string.IsNullOrEmpty(np.PlatformName) &&
                             !string.IsNullOrEmpty(np.PlatformUrl))
                .ToListAsync();

            if (streamsByDay.Count == 0)
            {
                throw new InvalidOperationException(
                    $"День {day}: Нет незапущенных трансляций или отсутствует информация о платформе.");
            }

            var totalStreams = await _context.Streams
                .Where(np => np.Day == day)
                .CountAsync();

            var startTasks = streamsByDay.Select(np => Task.Run(async () =>
            {
                await _streamManager.StartStream(np);
                np.IsActive = true;
            }));
            await Task.WhenAll(startTasks);

            await _context.SaveChangesAsync();

            if (streamsByDay.Count < totalStreams)
            {
                _logger.LogInformation("Не все трансляции были запущены за день {Day}.", day);
                return false; // Partial success
            }
            else
            {
                _logger.LogInformation("Все трансляции для дня {Day} успешно запущены.", day);
                return true; // All success
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при запуске всех трансляций для дня {Day}.", day);
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
                .Where(np =>
                    np.Day == day && np.IsActive && !string.IsNullOrEmpty(np.Token) &&
                    !string.IsNullOrEmpty(np.PlatformName) && !string.IsNullOrEmpty(np.PlatformUrl))
                .ToListAsync();

            if (streamsByDay.Count == 0)
            {
                throw new InvalidOperationException(
                    $"День {day}:\nНет запущенных трансляций или нет данных о платформе");
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

    public List<RunnersDto> GetActiveStreamsProcesses()
    {
        try
        {
            var runningStreams = _streamManager.GetActiveStreamsProcesses();

            return runningStreams.Select(s => new RunnersDto(s.StreamId, s.NominationUrl, s.PlatformUrl, s.Token)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching running streams");
            throw;
        }
    }

    private void ValidateInput(int day, string? streamUrl, string? platformName, string? platformUrl, string? token)
    {
        if (day <= 0)
        {
            throw new ArgumentException("День должен быть положительным числом.");
        }

        if (!string.IsNullOrWhiteSpace(streamUrl) && streamUrl.Trim().Length > 200)
        {
            throw new ArgumentException("URL потока превышает максимальную длину в 200 символов.");
        }

        if (!string.IsNullOrWhiteSpace(platformName) && platformName.Trim().Length > 100)
        {
            throw new ArgumentException("Название платформы превышает максимальную длину в 100 символов.");
        }

        if (!string.IsNullOrWhiteSpace(platformUrl) && platformUrl.Trim().Length > 200)
        {
            throw new ArgumentException("URL платформы превышает максимальную длину в 200 символов.");
        }

        if (!string.IsNullOrWhiteSpace(token) && token.Trim().Length > 500)
        {
            throw new ArgumentException("Token превышает максимальную длину в 500 символов.");
        }
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

public record UpdateStreamDto(
    int Day,
    string? PlatformName,
    string? PlatformUrl,
    string? Token,
    string? StreamUrl
);

public record PlatformStatusDto(
    string PlatformName,
    bool IsActive
);

public record AddDayDto(
    int NominationId,
    string? StreamUrl,
    int Day,
    List<DayStreamDto> DayStreams
);

public record DayStreamDto(
    string? PlatformName,
    string? PlatformUrl,
    string? Token
);

public record AddStreamDto(
    int NominationId,
    string StreamUrl,
    int Day,
    string PlatformName,
    string PlatformUrl,
    string Token
);

public record RunnersDto(
    int StreamId,
    string NominationUrl,
    string PlatformUrl,
    string Token
);