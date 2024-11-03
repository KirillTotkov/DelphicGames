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

    public async Task<Camera> CreateCamera(Camera camera)
    {
        _context.Cameras.Add(camera);
        await _context.SaveChangesAsync();
        return camera;
    }

    public async Task CreateCameras(List<Camera> cameras)
    {
        _context.Cameras.AddRange(cameras);
        await _context.SaveChangesAsync();
    }

    public async Task<Camera?> GetCamera(int id)
    {
        return await _context.Cameras.FindAsync(id);
    }

    public async Task<Camera?> GetCamera(string name)
    {
        return await _context.Cameras.FirstOrDefaultAsync(c => c.Name == name);
    }

    public async Task<List<Camera>> GetCameras()
    {
        return await _context.Cameras.ToListAsync();
    }

    public async Task<Camera?> UpdateCamera(int id, Camera camera)
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
    
    public async Task AddPlatformToCamera(int cameraId, int platformId)
    {
        var camera = await _context.Cameras.FindAsync(cameraId);
        if (camera == null)
        {
            throw new InvalidOperationException($"Камера с ID {cameraId} не найдена.");
        }

        var platform = await _context.Platforms.FindAsync(platformId);
        if (platform == null)
        {
            throw new InvalidOperationException($"Платформа с ID {platformId} не найдена.");
        }

        var cameraPlatform = new CameraPlatforms
        {
            Camera = camera,
            Platform = platform
        };

        _context.CameraPlatforms.Add(cameraPlatform);
        await _context.SaveChangesAsync();
    }
}