using DelphicGames.Data;
using DelphicGames.Data.Models;
using DelphicGames.Services.Streaming;
using Microsoft.EntityFrameworkCore;

namespace DelphicGames.Services;

public class NominationService
{
    private readonly ApplicationContext _context;
    private readonly StreamService _streamService;

    public NominationService(ApplicationContext context, StreamService streamService)
    {
        _context = context;
        _streamService = streamService;
    }

    public async Task<Nomination> AddNomination(AddNominationDto dto)
    {
        var name = dto.Name.Trim();
        var streamUrl = dto.StreamUrl.Trim();

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(streamUrl))
        {
            throw new ArgumentException("Имя и ссылка на стрим не могут быть пустыми");
        }

        if (await _context.Nominations.AnyAsync(n => n.Name.ToLower() == name.ToLower()))
        {
            throw new ArgumentException($"Номинация с именем {name} уже существует");
        }

        if (await _context.Nominations.AnyAsync(n => n.StreamUrl.ToLower() == streamUrl.ToLower()))
        {
            throw new ArgumentException($"Номинация с ссылкой {streamUrl} уже существует");
        }

        var nomination = new Nomination
        {
            Name = name,
            StreamUrl = streamUrl,
        };

        await ValidatePlatformTokens(dto.Platforms);

        var cameras = await GetCamerasByIds(dto.CameraIds);
        nomination.Cameras.AddRange(cameras);

        if (CheckPlatformsByIds(dto.Platforms.Select(p => p.PlatformId).ToList()).Result)
        {
            nomination.Platforms = dto.Platforms.Where(p => !string.IsNullOrWhiteSpace(p.Token)).Select(p =>
                new NominationPlatform
                {
                    PlatformId = p.PlatformId,
                    Token = p.Token,
                    IsActive = false
                }).ToList();
        }

        await _context.Nominations.AddAsync(nomination);
        await _context.SaveChangesAsync();

        return nomination;
    }

    public async Task<List<GetNominationDto>> GetNominationsWithCameras()
    {
        var nominations = await _context.Nominations
            .Include(n => n.Cameras)
            .Include(n => n.Platforms)
            .ThenInclude(np => np.Platform)
            .AsNoTracking()
            .AsSplitQuery()
            .Select(n => new GetNominationDto(
                n.Id,
                n.Name,
                n.StreamUrl,
                n.Cameras.Select(c => new GetCameraDto(c.Id, c.Name, c.Url)).ToList(),
                n.Platforms.Select(np => new GetNominationPlatformDto(np.PlatformId, np.Platform.Name, np.Token))
                    .ToList()
            ))
            .ToListAsync();

        return nominations;
    }

    public async Task<GetNominationDto?> GetNominationWithCameras(int nominationId)
    {
        var nomination = await _context.Nominations
            .Include(n => n.Cameras)
            .Include(n => n.Platforms)
            .ThenInclude(np => np.Platform)
            .AsSplitQuery()
            .FirstOrDefaultAsync(n => n.Id == nominationId);

        return nomination == null ? null : new GetNominationDto(
            nomination.Id,
            nomination.Name,
            nomination.StreamUrl,
            nomination.Cameras.Select(c => new GetCameraDto(c.Id, c.Name, c.Url)).ToList(),
            nomination.Platforms.Select(np => new GetNominationPlatformDto(np.PlatformId, np.Platform.Name, np.Token)).ToList()
        );
    }

    public async Task<List<NominationDto>> GetNominations()
    {
        var nominations = await _context.Nominations
            .AsNoTracking()
            .Select(n => new NominationDto(n.Id, n.Name))
            .ToListAsync();

        return nominations;
    }

    public async Task DeleteNomination(int nominationId)
    {
        var nomination = await _context.Nominations
            .Include(n => n.Platforms)
            .FirstOrDefaultAsync(n => n.Id == nominationId);

        if (nomination == null)
        {
            throw new ArgumentException($"Nomination with id {nominationId} not found");
        }

        _streamService.StopNominationStreams(nominationId);

        _context.Nominations.Remove(nomination);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateNomination(int nominationId, AddNominationDto dto)
    {
        var nomination = await _context.Nominations
            .Include(n => n.Cameras)
            .Include(n => n.Platforms)
            .FirstOrDefaultAsync(n => n.Id == nominationId);

        if (nomination == null)
        {
            throw new ArgumentException($"Номинация с id {nominationId} не найдена");
        }

        var name = dto.Name.Trim();
        var streamUrl = dto.StreamUrl.Trim();

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(streamUrl))
        {
            throw new ArgumentException("Имя и ссылка на стрим не могут быть пустыми");
        }

        if (await _context.Nominations.AnyAsync(n => n.Name.ToLower() == name.ToLower() && n.Id != nominationId))
        {
            throw new ArgumentException($"Номинация с именем {name} уже существует");
        }

        if (await _context.Nominations.AnyAsync(n =>
                n.StreamUrl.ToLower() == streamUrl.ToLower() && n.Id != nominationId))
        {
            throw new ArgumentException($"Номинация с ссылкой {streamUrl} уже существует");
        }

        await ValidatePlatformTokens(dto.Platforms, nominationId);

        nomination.Name = name;
        nomination.StreamUrl = streamUrl;

        nomination.Cameras.Clear();
        var cameras = await GetCamerasByIds(dto.CameraIds);
        nomination.Cameras.AddRange(cameras);

        nomination.Platforms.Clear();
        nomination.Platforms = dto.Platforms.Select(p => new NominationPlatform
        {
            PlatformId = p.PlatformId,
            Token = p.Token,
            IsActive = false
        }).ToList();

        await _context.SaveChangesAsync();
    }

    private async Task<bool> CheckPlatformsByIds(List<int> platformIds)
    {
        var platforms = await _context.Platforms.Where(p => platformIds.Contains(p.Id)).ToListAsync();
        if (platforms.Count != platformIds.Count)
        {
            throw new ArgumentException("Некоторые платформы не найдены");
        }

        return true;
    }

    private async Task<List<Camera>> GetCamerasByIds(List<int> cameraIds)
    {
        var cameras = await _context.Cameras
            .Where(c => cameraIds.Contains(c.Id))
            .ToListAsync();

        if (cameras.Count != cameraIds.Count)
        {
            var missingIds = cameraIds.Except(cameras.Select(c => c.Id)).ToList();
            throw new ArgumentException($"Камеры с id {string.Join(", ", missingIds)} не найдены");
        }

        return cameras;
    }

    // проверка, используется ли токен
    private async Task ValidatePlatformTokens(List<NominationPlatformDto> platforms, int? nominationId = null)
    {
        if (platforms.Select(p => p.Token).Distinct().Count() != platforms.Count)
        {
            throw new ArgumentException("Токены платформ не должны повторяться");
        }

        foreach (var platformDto in platforms)
        {
            if (string.IsNullOrWhiteSpace(platformDto.Token))
                continue;

            var query = _context.Nominations
                .Include(n => n.Platforms).ThenInclude(nominationPlatform => nominationPlatform.Platform)
                .Where(n => n.Platforms.Any(np => np.Token == platformDto.Token));

            if (nominationId.HasValue)
            {
                query = query.Where(n => n.Id != nominationId.Value);
            }

            var nomination = await query.FirstOrDefaultAsync();

            if (nomination != null)
            {
                var platform = await _context.Platforms.FindAsync(platformDto.PlatformId);
                var existingPlatform = nomination.Platforms.First(np => np.Token == platformDto.Token).Platform;
                if (platform == null)
                {
                    throw new ArgumentException($"Платформа с id {platformDto.PlatformId} не найдена");
                }

                throw new ArgumentException(
                    $"Токен  платформы '{platform.Name}' уже используется в номинации '{nomination.Name}' для платформы '{existingPlatform.Name}'");
            }
        }
    }
}

public record AddNominationDto(
    string Name,
    string StreamUrl,
    List<int> CameraIds,
    List<NominationPlatformDto> Platforms);

public record GetNominationDto(
    int Id,
    string Name,
    string StreamUrl,
    List<GetCameraDto> Cameras,
    List<GetNominationPlatformDto> Platforms);

public record NominationDto(int Id, string Name);

public record GetNominationPlatformDto(int PlatformId, string PlatformName, string? Token);

public record NominationPlatformDto(int PlatformId, string? Token);