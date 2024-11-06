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
}