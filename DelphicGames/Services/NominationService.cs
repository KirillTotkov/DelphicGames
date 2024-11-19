using DelphicGames.Data;
using DelphicGames.Data.Models;
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

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Имя не может быть пустым");
        }

        if (await _context.Nominations.AnyAsync(n => n.Name.ToLower() == name.ToLower()))
        {
            throw new ArgumentException($"Номинация с именем {name} уже существует");
        }

        var nomination = new Nomination
        {
            Name = name,
        };

        var cameras = await GetCamerasByIds(dto.CameraIds);
        nomination.Cameras.AddRange(cameras);

        await _context.Nominations.AddAsync(nomination);
        await _context.SaveChangesAsync();

        return nomination;
    }

    public async Task<List<GetNominationDto>> GetNominationsWithCameras()
    {
        var nominations = await _context.Nominations
            .Include(n => n.Cameras)
            .Include(n => n.Streams)
            .AsNoTracking()
            .Select(n => new GetNominationDto(
                n.Id,
                n.Name,
                n.Cameras.Select(c => new GetCameraDto(c.Id, c.Name, c.Url)).ToList()
            ))
            .ToListAsync();

        return nominations;
    }

    public async Task<GetNominationDto?> GetNominationWithCameras(int nominationId)
    {
        var nomination = await _context.Nominations
            .Include(n => n.Cameras)
            .Include(n => n.Streams)
            .FirstOrDefaultAsync(n => n.Id == nominationId);

        return nomination == null
            ? null
            : new GetNominationDto(
                nomination.Id,
                nomination.Name,
                nomination.Cameras.Select(c => new GetCameraDto(c.Id, c.Name, c.Url)).ToList()
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
            .Include(n => n.Streams)
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
            .Include(n => n.Streams)
            .FirstOrDefaultAsync(n => n.Id == nominationId);

        if (nomination == null)
        {
            throw new ArgumentException($"Номинация с id {nominationId} не найдена");
        }

        // Проверка активных потоков
        if (nomination.Streams.Any(np => np.IsActive))
        {
            throw new ArgumentException("Нельзя изменить номинацию, пока она транслируется");
        }

        var name = dto.Name.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Имя не может быть пустым");
        }

        if (await _context.Nominations.AnyAsync(n => n.Name.ToLower() == name.ToLower() && n.Id != nominationId))
        {
            throw new ArgumentException($"Номинация с именем {name} уже существует");
        }

        nomination.Name = name;

        nomination.Cameras.Clear();
        var cameras = await GetCamerasByIds(dto.CameraIds);
        nomination.Cameras.AddRange(cameras);

        await _context.SaveChangesAsync();
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
}

public record AddNominationDto(
    string Name,
    List<int> CameraIds
);

public record GetNominationDto(
    int Id,
    string Name,
    List<GetCameraDto> Cameras
);

public record NominationDto(int Id, string Name);