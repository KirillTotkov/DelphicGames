using DelphicGames.Data;
using DelphicGames.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace DelphicGames.Services;

public class CameraService
{
    private readonly ApplicationContext _context;
    private readonly ILogger<CameraService> _logger;

    public CameraService(ApplicationContext context, ILogger<CameraService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<GetCameraDto> CreateCamera(AddCameraDto dto, string userId)
    {
        if (await _context.Cameras.AnyAsync(c => c.Name == dto.Name))
        {
            throw new InvalidOperationException($"Камера с именем {dto.Name} уже существует.");
        }

        if (await _context.Cameras.AnyAsync(c => c.Url == dto.Url))
        {
            throw new InvalidOperationException($"Камера с URL {dto.Url} уже существует.");
        }

        var camera = new Camera
        {
            Name = dto.Name,
            Url = dto.Url,
            UserId = userId
        };

        _context.Cameras.Add(camera);
        await _context.SaveChangesAsync();

        return new GetCameraDto(camera.Id, camera.Name, camera.Url);
    }

    public async Task<GetCameraDto?> GetCamera(int id)
    {
        var camera = await _context.Cameras.FirstOrDefaultAsync(c => c.Id == id);
        if (camera == null)
        {
            return null;
        }

        return new GetCameraDto(camera.Id, camera.Name, camera.Url);
    }

    public async Task<GetCameraDto?> GetCamera(string name)
    {
        var camera = await _context.Cameras.FirstOrDefaultAsync(c => c.Name == name);
        if (camera == null)
        {
            return null;
        }

        return new GetCameraDto(camera.Id, camera.Name, camera.Url);
    }

    public async Task<List<GetCameraDto>> GetCameras(List<string> userRoles, string userId)
    {
        IQueryable<Camera> query = _context.Cameras;

        if (userRoles.Contains(nameof(UserRoles.Specialist)))
        {
            query = query.Where(c => c.UserId == userId);
        }

        var camerasDb = await query.OrderBy(c => c.Id)
            .AsNoTracking()
            .ToListAsync();

        return camerasDb.Select(c => new GetCameraDto(c.Id, c.Name, c.Url)).ToList();
    }

    public async Task<Camera?> UpdateCamera(int id, UpdateCameraDto camera)
    {
        var existingCamera = await _context.Cameras.FirstOrDefaultAsync(c => c.Id == id);
        if (existingCamera == null)
        {
            return null;
        }

        // TODO кажется, что это условие лишнее
        if (string.IsNullOrWhiteSpace(camera.Url))
        {
            existingCamera.Url = camera.Url;
        }

        existingCamera.Url = camera.Url;

        await _context.SaveChangesAsync();
        return existingCamera;
    }

    public async Task<bool> DeleteCamera(int id)
    {
        var camera = await _context.Cameras.FindAsync(id);
        if (camera == null)
        {
            return false;
        }

        _context.Cameras.Remove(camera);
        await _context.SaveChangesAsync();
        return true;
    }

    //  камеры, у которых нет номинации или у которых номинация совпадает с переданным ID
    public async Task<List<GetCameraDto>> GetCamerasByNomination(int? nominationId = null)
    {
        var query = _context.Cameras.AsQueryable();

        if (nominationId.HasValue)
        {
            query = query.Where(c => c.NominationId == nominationId.Value || c.NominationId == null);
        }
        else
        {
            query = query.Where(c => c.NominationId == null);
        }

        var cameras = await query
            .OrderBy(c => c.Id)
            .AsNoTracking()
            .ToListAsync();

        return cameras.Select(c => new GetCameraDto(c.Id, c.Name, c.Url)).ToList();
    }

    public async Task<bool> CameraExists(int id)
    {
        return await _context.Cameras.AnyAsync(c => c.Id == id);
    }

    public async Task<bool> CameraExists(string name)
    {
        return await _context.Cameras.AnyAsync(c => c.Name == name);
    }
}

public record AddCameraDto(string Name, string Url);

public record UpdateCameraDto(string Url);

public record GetCameraDto(int Id, string Name, string Url);