using DelphicGames.Data;
using DelphicGames.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace DelphicGames.Services;

public class NominationService
{
    private readonly ApplicationContext _context;

    public NominationService(ApplicationContext context)
    {
        _context = context;
    }

    public async Task<Nomination> AddNomination(AddNominationDto dto)
    {
        var nomination = new Nomination { Name = dto.Name.Trim() };

        var cameras = await GetCamerasByIds(dto.CameraIds);

        // Проверка, не связана ли уже какая-либо камера с номинацией
        var alreadyNominated = await _context.Cameras
            .Where(c => dto.CameraIds.Contains(c.Id) && c.NominationId != null)
            .ToListAsync();

        if (alreadyNominated.Count != 0)
        {
            var ids = string.Join(", ", alreadyNominated.Select(c => c.Id));
            throw new ArgumentException($"Камеры с id {ids} уже связаны с номинацией");
        }

        nomination.Cameras.AddRange(cameras);

        await _context.Nominations.AddAsync(nomination);
        await _context.SaveChangesAsync();

        return nomination;
    }

    public async Task<List<GetNominationDto>> GetNominationsWithCameras()
    {
        var nominations = await _context.Nominations
            .AsNoTracking()
            .Include(n => n.Cameras)
            .ToListAsync();

        return nominations.Select(n => new GetNominationDto(n.Id, n.Name, n.Cameras.Select(c => c.Url).ToList()))
            .ToList();
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
        var nomination = await _context.Nominations.FindAsync(nominationId);

        if (nomination == null)
        {
            throw new ArgumentException($"Номинация с id {nominationId} не найдена");
        }

        _context.Nominations.Remove(nomination);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateNomination(int nominationId, AddNominationDto dto)
    {
        var nomination = await _context.Nominations
            .Include(n => n.Cameras)
            .FirstOrDefaultAsync(n => n.Id == nominationId);

        if (nomination == null)
        {
            throw new ArgumentException($"Номинация с id {nominationId} не найдена");
        }

        nomination.Name = dto.Name.Trim();

        var cameras = await GetCamerasByIds(dto.CameraIds);
        
        // Проверка, не связана ли уже какая-либо камера с номинацией
        var alreadyNominated = await _context.Cameras
            .Where(c => dto.CameraIds.Contains(c.Id) && c.NominationId != nominationId)
            .ToListAsync();

        if (alreadyNominated.Count != 0)
        {
            var ids = string.Join(", ", alreadyNominated.Select(c => c.Id));
            throw new ArgumentException($"Камеры с id {ids} уже связаны с номинацией");
        }

        nomination.Cameras.Clear();
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

public record AddNominationDto(string Name, List<int> CameraIds);

public record GetNominationDto(int Id, string Name, List<string> CameraUrls);

public record NominationDto(int Id, string Name);