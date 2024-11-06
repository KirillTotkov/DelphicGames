using DelphicGames.Data.Models;
using DelphicGames.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DelphicGames.Controllers;

[ApiController]
[Route("api/cameraplatform")]
public class CameraPlatformController : ControllerBase
{
    private readonly CameraPlatformService _cameraPlatformService;

    public CameraPlatformController(CameraPlatformService cameraPlatformService)
    {
        _cameraPlatformService = cameraPlatformService;
    }


    [HttpGet("GetCameraPlatforms")]
    [Authorize(Roles = $"{nameof(UserRoles.Specialist)},{nameof(UserRoles.Root)},{nameof(UserRoles.Admin)}")]
    public async Task<ActionResult> GetCameraPlatforms()
    {
        var cameraPlatforms = await _cameraPlatformService.GetCameraPlatforms();
        if (cameraPlatforms == null)
        {
            return NotFound();
        }

        return Ok(cameraPlatforms);
    }

    // update
    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{nameof(UserRoles.Specialist)},{nameof(UserRoles.Root)},{nameof(UserRoles.Admin)}")]
    public async Task<ActionResult> UpdateCameraPlatform(int id, UpdateCameraPlatformDto cameraPlatform)
    {
        try
        {
            var result = await _cameraPlatformService.UpdateCameraPlatform(id, cameraPlatform);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = $"{nameof(UserRoles.Specialist)},{nameof(UserRoles.Root)},{nameof(UserRoles.Admin)}")]
    public async Task<ActionResult> DeleteCameraPlatform(int id)
    {
        try
        {
            var result = await _cameraPlatformService.DeleteCameraPlatform(id);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    // Get all platforms
    [HttpGet("platforms")]
    [Authorize(Roles = $"{nameof(UserRoles.Admin)},{nameof(UserRoles.Root)}")]
    public async Task<ActionResult> GetPlatforms()
    {
        var platforms = await _cameraPlatformService.GetPlatforms();
        return Ok(platforms);
    }

    // Get platforms and tokens for a specific camera
    [HttpGet("cameraplatform/{cameraId:int}")]
    [Authorize(Roles = $"{nameof(UserRoles.Admin)},{nameof(UserRoles.Root)}")]
    public async Task<ActionResult> GetCameraPlatforms(int cameraId)
    {
        var cameraPlatforms = await _cameraPlatformService.GetCameraPlatforms(cameraId);
        if (cameraPlatforms == null)
        {
            return NotFound();
        }
        return Ok(cameraPlatforms);
    }

    // Update tokens for a camera's platforms
    [HttpPost("cameraplatform/{cameraId:int}")]
    [Authorize(Roles = $"{nameof(UserRoles.Admin)},{nameof(UserRoles.Root)}")]
    public async Task<ActionResult> UpdateCameraTokens(int cameraId, [FromBody] CameraPlatformService.UpdateCameraTokensDto dto)
    {
        try
        {
            await _cameraPlatformService.UpdateCameraTokens(cameraId, dto);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }
}