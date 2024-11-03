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

    public async Task<Platform> CreatePlatform(Platform platform)
    {
        _context.Platforms.Add(platform);
        await _context.SaveChangesAsync();
        return platform;
    }

    public async Task CreatePlatforms(List<Platform> platforms)
    {
        _context.Platforms.AddRange(platforms);
        await _context.SaveChangesAsync();
    }

    public async Task<Platform?> GetPlatform(int id)
    {
        return await _context.Platforms.FindAsync(id);
    }

    public async Task<Platform?> GetPlatform(string name)
    {
        return await _context.Platforms.FirstOrDefaultAsync(p => p.Name == name);
    }

    public async Task<List<Platform>> GetPlatforms()
    {
        return await _context.Platforms.ToListAsync();
    }

    public async Task<Platform?> UpdatePlatform(int id, Platform platform)
    {
        var existingPlatform = await _context.Platforms.FindAsync(id);
        if (existingPlatform == null)
        {
            return null;
        }

        existingPlatform.Name = platform.Name;
        existingPlatform.Url = platform.Url;

        await _context.SaveChangesAsync();
        return existingPlatform;
    }

    public async Task<bool> DeletePlatform(int id)
    {
        var platform = await _context.Platforms.FindAsync(id);
        if (platform == null)
        {
            return false;
        }

        _context.Platforms.Remove(platform);
        await _context.SaveChangesAsync();
        return true;
    }
}