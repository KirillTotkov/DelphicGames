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

    public async Task<GetCameraDto> CreateCamera(AddCameraDto dto)
    {
        if (await _context.Cameras.AnyAsync(c => c.Name == dto.Name))
        {
            throw new InvalidOperationException($"Камера с именем {dto.Name} уже существует.");
        }

        if (await _context.Cameras.AnyAsync(c => c.Url == dto.Url))
        {
            throw new InvalidOperationException($"Камера с URL {dto.Url} уже существует.");
        }

        var city = await _context.Cities.FirstOrDefaultAsync(c => c.Id == dto.City);
        if (city == null)
        {
            throw new InvalidOperationException($"Город с ID {dto.City} не найден.");
        }

        var camera = new Camera
        {
            Name = dto.Name,
            Url = dto.Url,
            City = city
        };

        _context.Cameras.Add(camera);
        await _context.SaveChangesAsync();

        return new GetCameraDto(camera.Id, camera.Name, camera.Url, camera.City.Name);
    }

    public async Task<GetCameraDto?> GetCamera(int id)
    {
        var camera = await _context.Cameras.Include(c => c.City).FirstOrDefaultAsync(c => c.Id == id);
        if (camera == null)
        {
            return null;
        }

        return new GetCameraDto(camera.Id, camera.Name, camera.Url, camera.City?.Name);
    }

    public async Task<GetCameraDto?> GetCamera(string name)
    {
        var camera = await _context.Cameras.Include(camera => camera.City).FirstOrDefaultAsync(c => c.Name == name);
        if (camera == null)
        {
            return null;
        }

        return new GetCameraDto(camera.Id, camera.Name, camera.Url, camera.City?.Name);
    }

    public async Task<List<GetCameraDto>> GetCameras()
    {
        var camerasDb = await _context.Cameras
            .Include(c => c.City)
            .OrderBy(c => c.Id)
            .AsNoTracking()
            .ToListAsync();

        return camerasDb.Select(c => new GetCameraDto(c.Id, c.Name, c.Url, c.City?.Name)).ToList();
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

    // Добавление платформы к камере 
    public async Task AddPlatformToCamera(int cameraId, int platformId)
    {
        var camera = await _context.Cameras.FindAsync(cameraId);
        if (camera == null)
        {
            throw new InvalidOperationException($"Камера с ID не найдена.");
        }

        var platform = await _context.Platforms.FindAsync(platformId);
        if (platform == null)
        {
            throw new InvalidOperationException($"Платформа с ID не найдена.");
        }

        var cameraPlatform = new CameraPlatform
        {
            Camera = camera,
            Platform = platform
        };

        _context.CameraPlatforms.Add(cameraPlatform);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<CityCameraDto>> GetCamerasByCity(int cityId, int? nominationId)
    {
        return await _context.Cameras
            .Where(c => c.CityId == cityId && (c.NominationId == null || c.NominationId == nominationId))
            .OrderBy(c => c.Id)
            .Select(c => new CityCameraDto(c.Id, c.Name, c.Url, c.City.Name))
            .ToListAsync();
    }

    public async Task<IEnumerable<CityCameraDto>> GetCamerasCity(int? nominationId)
    {
        if (nominationId == null)
        {
            return await _context.Cameras
                .Where(c => c.NominationId == null)
                .OrderBy(c => c.Id)
                .Select(c => new CityCameraDto(c.Id, c.Name, c.Url, c.City.Name))
                .ToListAsync();
        }

        return await _context.Cameras
            .Where(c => (c.NominationId != null && c.NominationId == nominationId) || c.NominationId == null)
            .OrderBy(c => c.Id)
            .Select(c => new CityCameraDto(c.Id, c.Name, c.Url, c.City.Name))
            .ToListAsync();
    }

    public async Task<IEnumerable<CityCameraDto>> GetCamerasByRegion(int regionId, int? nominationId)
    {
        if (nominationId != null)
        {
            return await _context.Cameras
                .Where(c => c.City.RegionId == regionId && c.NominationId == nominationId)
                .OrderBy(c => c.Id)
                .Select(c => new CityCameraDto(c.Id, c.Name, c.Url, c.City.Name))
                .ToListAsync();
        }

        return await _context.Cameras
            .Where(c => c.City.RegionId == regionId && c.NominationId == null)
            .OrderBy(c => c.Id)
            .Select(c => new CityCameraDto(c.Id, c.Name, c.Url, c.City.Name))
            .ToListAsync();
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

public record CityCameraDto(int Id, string Name, string Url, string CityName);

public record AddCameraDto(int City, string Name, string Url);

public record UpdateCameraDto( string Url);

public record GetCameraDto(int Id, string Name, string Url, string City);