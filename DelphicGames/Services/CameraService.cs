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

    public async Task<Camera> CreateCamera(AddCameraDto dto)
    {
        if (await _context.Cameras.AnyAsync(c => c.Name == dto.Name))
        {
            throw new InvalidOperationException($"Камера с именем {dto.Name} уже существует.");
        }

        var camera = new Camera
        {
            Name = dto.Name,
            Url = dto.Url
        };

        _context.Cameras.Add(camera);
        await _context.SaveChangesAsync();
        return camera;
    }

    public async Task CreateCameras(List<Camera> cameras)
    {
        _context.Cameras.AddRange(cameras);
        await _context.SaveChangesAsync();
    }

    public async Task<GetCameraDto?> GetCamera(int id)
    {
        var camera = await _context.Cameras.FindAsync(id);
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

    public async Task<List<GetCameraDto>> GetCameras()
    {
        return await _context.Cameras
            .Select(c => new GetCameraDto(c.Id, c.Name, c.Url))
            .ToListAsync();
    }

    public async Task<Camera?> UpdateCamera(int id, UpdateCameraDto camera)
    {
        var existingCamera = await _context.Cameras.FindAsync(id);
        if (existingCamera == null)
        {
            return null;
        }

        existingCamera.Name = camera.Name;
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
public record UpdateCameraDto(string Name, string Url);
public record GetCameraDto(int Id, string Name, string Url);