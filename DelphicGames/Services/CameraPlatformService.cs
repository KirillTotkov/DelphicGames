using DelphicGames.Data;
using DelphicGames.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace DelphicGames.Services;

public class CameraPlatformService
{
    private readonly ApplicationContext _context;

    public CameraPlatformService(ApplicationContext context)
    {
        _context = context;
    }
    
    public async Task<List<CameraDto>> GetCameraPlatforms()
    {
        var r = await _context.Cameras
            .Include(c => c.CameraPlatforms)
            .ThenInclude(cp => cp.Platform)
            .Include(c => c.City).ToListAsync();

        return r.Select(c => new CameraDto
        {
            Id = c.Id,
            City = c.City?.Name,
            Name = c.Name,
            Url = c.Url,
            PlatformTokens = c.CameraPlatforms.Select(cp => new GetPlatformTokens(
                cp.PlatformId,
                cp.Platform.Name,
                cp?.Token
            )).ToList()
        }).ToList();
    }

    public async Task<GetCameraPlatformDto> UpdateCameraPlatform(int id, UpdateCameraPlatformDto dto)
    {
        var camera = await _context.Cameras.FindAsync(id);
        if (camera == null)
        {
            throw new InvalidOperationException($"Камера с ID {id} не найдена.");
        }

        camera.Url = dto.Url;

        var cameraPlatforms = await _context.CameraPlatforms
            .Where(cp => cp.CameraId == id)
            .ToListAsync();

        foreach (var platform in dto.PlatformTokens)
        {
            var cameraPlatform = cameraPlatforms.FirstOrDefault(cp => cp.PlatformId == platform.Id);
            if (cameraPlatform == null)
            {
                throw new InvalidOperationException($"Платформа с ID {platform.Id} не найдена.");
            }

            cameraPlatform.Token = platform.Token;
        }

        await _context.SaveChangesAsync();

        return new GetCameraPlatformDto(
            camera.Id,
            camera.Name,
            camera.Url,
            dto.PlatformTokens
        );
    }

    public async Task<bool> DeleteCameraPlatform(int id)
    {
        var camera = await _context.Cameras.FindAsync(id);
        if (camera == null)
        {
            return false;
        }

        var cameraPlatforms = await _context.CameraPlatforms
            .Where(cp => cp.CameraId == id)
            .ToListAsync();

        _context.Cameras.Remove(camera);
        _context.CameraPlatforms.RemoveRange(cameraPlatforms);
        await _context.SaveChangesAsync();

        return true;
    }
}

public record AddCameraPlatformDto(string Name, string Url);

public record GetCameraPlatformDto(int Id, string Name, string Url, List<GetPlatformTokens> PlatformTokens);

public record UpdateCameraPlatformDto(string Url, List<GetPlatformTokens> PlatformTokens);

public record GetPlatformTokens(int Id, string Name, string Token);

public class CameraDto
{
    public int Id { get; set; }
    public string City { get; set; }
    public string Name { get; set; }
    public string Url { get; set; }
    public List<GetPlatformTokens> PlatformTokens { get; set; }
}