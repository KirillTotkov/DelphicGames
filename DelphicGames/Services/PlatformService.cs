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

    public async Task<GetPlatformDto> CreatePlatform(AddPlatformDto platform)
    {
        ArgumentNullException.ThrowIfNull(platform);
        if (string.IsNullOrWhiteSpace(platform.Name))
        {
            throw new ValidationException("Название платформы не может быть пустым.");
        }

        if (string.IsNullOrWhiteSpace(platform.Url))
        {
            throw new ValidationException("URL платформы не может быть пустым.");
        }

        try
        {
            var exists = await _context.Platforms
                .AnyAsync(p => p.Name == platform.Name || p.Url == platform.Url);
            if (exists)
            {
                throw new DuplicateEntityException("Платформа с таким именем или URL-адресом уже существует.");
            }

            var newPlatform = new Platform
            {
                Name = platform.Name.Trim(),
                Url = platform.Url.Trim()
            };

            _context.Platforms.Add(newPlatform);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Создана новая платформа: {Id}", newPlatform.Id);

            return new GetPlatformDto(newPlatform.Id, newPlatform.Name, newPlatform.Url);
        }
        catch (Exception ex) when (ex is not DuplicateEntityException)
        {
            _logger.LogError(ex, "Ошибка создания платформы.");
            throw new ServiceException("Не удалось создать платформу", ex);
        }
    }

    public async Task<GetPlatformDto?> GetPlatform(int id)
    {
        var platform = await _context.Platforms.FindAsync(id);
        return platform != null ? new GetPlatformDto(platform.Id, platform.Name, platform.Url) : null;
    }

    public async Task<List<GetPlatformDto>> GetPlatforms()
    {
        return await _context.Platforms
            .AsNoTracking()
            .Select(p => new GetPlatformDto(p.Id, p.Name, p.Url))
            .ToListAsync();
    }

    public async Task<GetPlatformDto?> UpdatePlatform(int id, UpdatePlatformDto platform)
    {
        ArgumentNullException.ThrowIfNull(platform);

        if (string.IsNullOrWhiteSpace(platform.Name))
        {
            throw new ValidationException("Название платформы не может быть пустым.");
        }

        if (string.IsNullOrWhiteSpace(platform.Url))
        {
            throw new ValidationException("URL платформы не может быть пустым.");
        }

        try
        {
            var existingPlatform = await _context.Platforms.FindAsync(id);
            if (existingPlatform == null)
            {
                return null;
            }

            var exists = await _context.Platforms
                .AnyAsync(p => p.Id != id && (p.Name == platform.Name || p.Url == platform.Url));
            if (exists)
            {
                throw new DuplicateEntityException("Платформа с таким названием или URL-адресом уже существует.");
            }

            existingPlatform.Name = platform.Name.Trim();
            existingPlatform.Url = platform.Url.Trim();
            await _context.SaveChangesAsync();
            _logger.LogInformation("Обновлена платформа с ID: {Id}", existingPlatform.Id);

            return new GetPlatformDto(existingPlatform.Id, existingPlatform.Name, existingPlatform.Url);
        }
        catch (Exception ex) when (ex is not DuplicateEntityException)
        {
            _logger.LogError(ex, "Ошибка обновления платформы.");
            throw new ServiceException("Не удалось обновить платформу", ex);
        }
    }

    public async Task DeletePlatform(int id)
    {
        var platform = await _context.Platforms.FindAsync(id);
        if (platform != null)
        {
            _context.Platforms.Remove(platform);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Удалена платформа с ID: {Id}", id);
        }
    }
}

public class DuplicateEntityException : Exception
{
    public DuplicateEntityException(string message) : base(message)
    {
    }
}

public class ServiceException : Exception
{
    public ServiceException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

public class ValidationException : Exception
{
    public ValidationException(string message) : base(message)
    {
    }
}

public record AddPlatformDto(string Name, string Url);

public record GetPlatformDto(int Id, string Name, string Url);

public record UpdatePlatformDto(string Name, string Url);