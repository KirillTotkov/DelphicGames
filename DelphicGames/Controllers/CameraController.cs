using DelphicGames.Data.Models;
using DelphicGames.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DelphicGames.Controllers;

[ApiController]
[Route("api/cameras")]
[Authorize(Roles = $"{nameof(UserRoles.Root)},{nameof(UserRoles.Specialist)}")]
public class CameraController : ControllerBase
{
    private readonly CameraService _cameraService;

    public CameraController(CameraService cameraService)
    {
        _cameraService = cameraService;
    }

    [HttpPost]
    public async Task<IActionResult> AddCamera([FromBody] AddCameraDto dto)
    {
        try
        {
            var camera = await _cameraService.CreateCamera(dto);
            return CreatedAtAction(nameof(GetCamera), new { Id = camera.Id }, camera);
        }
        catch (InvalidOperationException e)
        {
            return BadRequest(new { Error = e.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCamera(int id)
    {
        var camera = await _cameraService.GetCamera(id);
        if (camera == null)
        {
            return NotFound();
        }

        return Ok(camera);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllCameras()
    {
        var cameras = await _cameraService.GetCameras();
        return Ok(cameras);
    }

    [HttpPut("{id}")]
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

    [HttpDelete("{id}")]
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

    [HttpPost("{cameraId}/platforms/{platformId}")]
    public async Task<IActionResult> AddPlatformToCamera(int cameraId, int platformId)
    {
        try
        {
            await _cameraService.AddPlatformToCamera(cameraId, platformId);
            return NoContent();
        }
        catch (InvalidOperationException e)
        {
            return BadRequest(new { Error = e.Message });
        }
    }
}