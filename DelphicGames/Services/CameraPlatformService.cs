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

    public async Task<GetCameraPlatformDto> CreateCameraPlatform(AddCameraPlatformDto dto)
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
            Url = dto.Url
        };

        _context.Cameras.Add(camera);

        var platforms = _context.Platforms.ToList();
        foreach (Platform platform in platforms)
        {
            _context.CameraPlatforms.Add(new CameraPlatform
            {
                CameraId = camera.Id,
                Camera = camera,
                PlatformId = platform.Id,
                Platform = platform,
                Token = string.Empty,
                IsActive = false
            });
        }

        await _context.SaveChangesAsync();

        return new GetCameraPlatformDto(
            camera.Id,
            camera.Name,
            camera.Url,
            new List<string>()
        );
    }

    public async Task<List<CameraDTO>> GetCameraPlatforms()
    {
        // List<GetCameraPlatformDto> res = new List<GetCameraPlatformDto>();
        // foreach(var cam in _context.Cameras)
        // {
        //     List<string> tokens = new List<string>();

        //     foreach(var cm in _context.CameraPlatforms)
        //     {
        //         if(cm.CameraId == cam.Id)
        //             tokens.Add(cm.Token);
        //     }

        //     res.Add(new GetCameraPlatformDto(cam.Id, cam.Name, cam.Url,
        //         tokens));
        // }

        // return res;

        return await _context.Cameras
            .Include(c => c.CameraPlatforms)
            .Select(c => new CameraDTO
            {
                id = c.Id,
                Name = c.Name,
                Url = c.Url,
                TokenVK = c.CameraPlatforms.FirstOrDefault(p => p.PlatformId == 1).Token,
                TokenOK = c.CameraPlatforms.FirstOrDefault(p => p.PlatformId == 2).Token,
                TokenRT = c.CameraPlatforms.FirstOrDefault(p => p.PlatformId == 3).Token,
                TokenTG = c.CameraPlatforms.FirstOrDefault(p => p.PlatformId == 4).Token
            }).ToListAsync();
    }

    public async Task<bool> DeleteCameraPlatform(int id)
    {
        var cameraPlatform = await _context.CameraPlatforms.FindAsync(id);
        if (cameraPlatform == null)
        {
            return false;
        }

        _context.CameraPlatforms.Remove(cameraPlatform);
        await _context.SaveChangesAsync();

        var camera = await _context.Cameras.FindAsync(id);
        if (camera == null)
        {
            return false;
        }

        _context.Cameras.Remove(camera);
        await _context.SaveChangesAsync();

        return true;
    }
}

public record AddCameraPlatformDto(string Name, string Url);

public record GetPlatformTokens(string TokenVk, string TokenOk, string TokenRb, string TokenTg);

public record GetCameraPlatformDto(int Id, string Name, string Url, List<String> PlatformTokens);