using DelphicGames.Data;
using DelphicGames.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace DelphicGames.Services;

public class PlatformService
{
    private readonly ApplicationContext _context;
    private readonly ILogger<PlatformService> _logger;

    public PlatformService(ApplicationContext context, ILogger<PlatformService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task CreatePlatform(AddPlatformDto platform)
    {
        var existingPlatform = await GetPlatform(platform.Name);
        if (existingPlatform != null)
        {
            throw new InvalidOperationException("Platform already exists");
        }

        var newPlatform = new Platform
        {
            Name = platform.Name,
            Url = platform.Url
        };

        _context.Platforms.Add(newPlatform);
        await _context.SaveChangesAsync();
    }

    public async Task<GetPlatformDto?> GetPlatform(int id)
    {
        var platform = await _context.Platforms.FindAsync(id);
        if (platform == null)
        {
            return null;
        }

        return new GetPlatformDto(platform.Id, platform.Name, platform.Url);
    }

    public async Task<Platform?> GetPlatform(string name)
    {
        return await _context.Platforms.FirstOrDefaultAsync(p => p.Name == name);
    }

    public async Task<List<GetPlatformDto>> GetPlatforms()
    {
        return await _context.Platforms
            .Select(p => new GetPlatformDto(p.Id, p.Name, p.Url))
            .ToListAsync();
    }

    public async Task UpdatePlatform(int id, UpdatePlatformDto platform)
    {
        var existingPlatform = await _context.Platforms.FindAsync(id);
        if (existingPlatform == null)
        {
            return;
        }

        existingPlatform.Name = platform.Name;
        existingPlatform.Url = platform.Url;

        await _context.SaveChangesAsync();
    }

    public async Task DeletePlatform(int id)
    {
        var platform = await _context.Platforms.FindAsync(id);
        if (platform == null)
        {
            return;
        }

        _context.Platforms.Remove(platform);
        await _context.SaveChangesAsync();
    }
}

public record AddPlatformDto(string Name, string Url);

public record GetPlatformDto(int Id, string Name, string Url);

public record UpdatePlatformDto(string Name, string Url);