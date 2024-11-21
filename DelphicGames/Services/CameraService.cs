using DelphicGames.Data;
using DelphicGames.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace DelphicGames.Services;

public class CameraService
{
    private readonly ApplicationContext _context;
    private readonly ILogger<CameraService> _logger;
    private const int MaxNameLength = 50;
    private const int MaxUrlLength = 200;

    public CameraService(ApplicationContext context, ILogger<CameraService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<GetCameraDto> CreateCamera(AddCameraDto dto, string userId)
    {
        ValidateCameraInput(dto.Name, dto.Url);

        await ValidateDuplicates(dto);

        var camera = new Camera
        {
            Name = dto.Name.Trim(),
            Url = dto.Url.Trim(),
            UserId = userId
        };

        try
        {
            _context.Cameras.Add(camera);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Camera created: {Name}", camera.Name);
            return new GetCameraDto(camera.Id, camera.Name, camera.Url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating camera {Name}", dto.Name);
            throw new InvalidOperationException("Ошибка создания камеры", ex);
        }
    }

    public async Task<GetCameraDto?> GetCamera(int id)
    {
        var camera = await _context.Cameras
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

        return camera == null ? null : new GetCameraDto(camera.Id, camera.Name, camera.Url);
    }

    public async Task<List<GetCameraDto>> GetCameras(List<string> userRoles, string userId)
    {
        var query = userRoles.Contains(nameof(UserRoles.Specialist))
            ? _context.Cameras.Where(c => c.UserId == userId)
            : _context.Cameras;

        var cameras = await query
            .OrderBy(c => c.Id)
            .AsNoTracking()
            .Select(c => new GetCameraDto(c.Id, c.Name, c.Url))
            .ToListAsync();

        return cameras;
    }

    public async Task<GetCameraDto?> UpdateCamera(int id, UpdateCameraDto dto)
    {
        var existingCamera = await _context.Cameras
            .Include(c => c.Nomination)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (existingCamera == null) return null;

        if (existingCamera.NominationId.HasValue)
        {
            throw new InvalidOperationException("Невозможно удалить камеру, связанную с номинацией.");
        }

        ValidateCameraInput(dto.Name, dto.Url);
        await ValidateDuplicatesForUpdate(id, dto);

        if (existingCamera.Url == dto.Url.Trim() && existingCamera.Name == dto.Name.Trim())
        {
            return new GetCameraDto(existingCamera.Id, existingCamera.Name, existingCamera.Url);
        }

        try
        {
            existingCamera.Name = dto.Name.Trim();
            existingCamera.Url = dto.Url.Trim();
            await _context.SaveChangesAsync();
            _logger.LogInformation("Camera updated: {Id}", id);
            return new GetCameraDto(existingCamera.Id, existingCamera.Name, existingCamera.Url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating camera {Id}", id);
            throw new InvalidOperationException("Ошибка обновления камеры", ex);
        }
    }

    public async Task<bool> DeleteCamera(int id)
    {
        var camera = await _context.Cameras
        .Include(c => c.Nomination)
        .FirstOrDefaultAsync(c => c.Id == id);

        if (camera == null) return false;

        if (camera.NominationId.HasValue)
        {
            throw new InvalidOperationException("Невозможно удалить камеру, связанную с номинацией.");
        }

        try
        {
            _context.Cameras.Remove(camera);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Camera deleted: {Id}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting camera {Id}", id);
            throw new InvalidOperationException("Failed to delete camera", ex);
        }
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
            .Select(c => new GetCameraDto(c.Id, c.Name, c.Url))
            .ToListAsync();

        return cameras;
    }

    private static void ValidateCameraInput(string name, string url)
    {
        if (string.IsNullOrEmpty(name) || name.Trim().Length > MaxNameLength)
        {
            throw new ArgumentException($"Название камеры не может быть пустым или длиннее, чем {MaxNameLength} символов");
        }

        if (string.IsNullOrEmpty(url) || url.Trim().Length > MaxUrlLength)
        {
            throw new ArgumentException($"URL не может быть пустым или длиннее, чем {MaxUrlLength} символов");
        }
    }

    private async Task ValidateDuplicates(AddCameraDto dto)
    {
        if (await _context.Cameras.AnyAsync(c => c.Name.Trim().ToLower() == dto.Name.Trim().ToLower()))
        {
            throw new InvalidOperationException($"Камера с таким названием уже есть");
        }

        if (await _context.Cameras.AnyAsync(c => c.Url.Trim().ToLower() == dto.Url.Trim().ToLower()))
        {
            throw new InvalidOperationException($"Камера с таким url уже есть");
        }
    }

    private async Task ValidateDuplicatesForUpdate(int id, UpdateCameraDto dto)
    {
        if (await _context.Cameras.AnyAsync(c => c.Name.Trim().ToLower() == dto.Name.Trim().ToLower() && c.Id != id))
        {
            throw new InvalidOperationException($"Камера с таким названием уже существует");
        }

        if (await _context.Cameras.AnyAsync(c => c.Url.Trim().ToLower() == dto.Url.Trim().ToLower() && c.Id != id))
        {
            throw new InvalidOperationException($"Камера с таким url уже существует");
        }
    }

    public async Task<List<GetCameraDto>> GetAllCameras()
    {
        var cameras = await _context.Cameras
            .AsNoTracking()
            .Select(c => new GetCameraDto(c.Id, c.Name, c.Url))
            .ToListAsync();

        return cameras;
    }
}

public record AddCameraDto(string Name, string Url);

public record UpdateCameraDto(string Name, string Url);

public record GetCameraDto(int Id, string Name, string Url);