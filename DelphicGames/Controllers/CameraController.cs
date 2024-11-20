using System.Security.Claims;
using DelphicGames.Data.Models;
using DelphicGames.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniExcelLibs;

namespace DelphicGames.Controllers;

[ApiController]
[Route("api/cameras")]
public class CameraController : ControllerBase
{
    private readonly CameraService _cameraService;

    public CameraController(CameraService cameraService)
    {
        _cameraService = cameraService;
    }

    [HttpPost]
    [Authorize(Roles = $"{nameof(UserRoles.Root)},{nameof(UserRoles.Specialist)},{nameof(UserRoles.Admin)}")]
    public async Task<IActionResult> AddCamera([FromBody] AddCameraDto dto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        try
        {
            var camera = await _cameraService.CreateCamera(dto, userId);
            return CreatedAtAction(nameof(GetCamera), new { Id = camera.Id }, camera);
        }
        catch (InvalidOperationException e)
        {
            return BadRequest(new { Error = e.Message });
        }
    }

    [HttpGet]
    [Authorize(Roles = $"{nameof(UserRoles.Root)},{nameof(UserRoles.Specialist)},{nameof(UserRoles.Admin)}")]
    public async Task<IActionResult> GetAllCameras()
    {
        var userRoles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var cameras = await _cameraService.GetCameras(userRoles, userId);
        return Ok(cameras);
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = $"{nameof(UserRoles.Root)},{nameof(UserRoles.Specialist)},{nameof(UserRoles.Admin)}")]
    public async Task<IActionResult> GetCamera(int id)
    {
        var camera = await _cameraService.GetCamera(id);
        if (camera == null)
        {
            return NotFound();
        }

        return Ok(camera);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{nameof(UserRoles.Root)},{nameof(UserRoles.Specialist)},{nameof(UserRoles.Admin)}")]
    public async Task<IActionResult> UpdateCamera(int id, [FromBody] UpdateCameraDto dto)
    {
        try
        {
            var camera = await _cameraService.UpdateCamera(id, dto);
            return Ok(camera);
        }
        catch (InvalidOperationException e)
        {
            return BadRequest(new { Error = e.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = $"{nameof(UserRoles.Root)},{nameof(UserRoles.Specialist)},{nameof(UserRoles.Admin)}")]
    public async Task<IActionResult> DeleteCamera(int id)
    {
        try
        {
            await _cameraService.DeleteCamera(id);
            return NoContent();
        }
        catch (InvalidOperationException e)
        {
            return BadRequest(new { Error = e.Message });
        }
    }


    [HttpGet("nominations")]
    [Authorize(Roles = $"{nameof(UserRoles.Root)},{nameof(UserRoles.Admin)}")]
    public async Task<IActionResult> GetCamerasByNomination([FromQuery] int? nominationId)
    {
        var cameras = await _cameraService.GetCamerasByNomination(nominationId);
        return Ok(cameras);
    }

    [HttpGet("export")]
    [Authorize(Roles = $"{nameof(UserRoles.Root)},{nameof(UserRoles.Specialist)},{nameof(UserRoles.Admin)}")]
    public async Task<IActionResult> ExportCamerasToExcel()
    {
        var cameras = await _cameraService.GetAllCameras();
        var data = cameras.Select(c => new
        {
            c.Name,
            c.Url
        }).ToList();

        var memoryStream = new MemoryStream();
        memoryStream.SaveAs(data);
        memoryStream.Seek(0, SeekOrigin.Begin);
        return new FileStreamResult(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
        {
            FileDownloadName = "cameras.xlsx"
        };
    }
}