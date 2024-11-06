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

    [HttpPost("CreateCameraPlatform")]
    [Authorize(Roles = $"{nameof(UserRoles.Specialist)},{nameof(UserRoles.Root)},{nameof(UserRoles.Admin)}")]
    public async Task<IActionResult> CreateCameraPlatform(AddCameraPlatformDto dto)
    {
        try
        {
            var cameraPlatform = await _cameraPlatformService.CreateCameraPlatform(dto);

            return NoContent();
        }
        catch (InvalidOperationException e)
        {
            return BadRequest(new { Error = e.Message });
        }
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