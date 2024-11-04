using DelphicGames.Data.Models;
using DelphicGames.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DelphicGames.Controllers;

[ApiController]
[Route("api/platforms")]
[Authorize(Roles = $"{nameof(UserRoles.Root)},{nameof(UserRoles.Admin)}")]
public class PlatformController : ControllerBase
{
    private readonly PlatformService _platformService;

    public PlatformController(PlatformService platformService)
    {
        _platformService = platformService;
    }

    [HttpGet]
    public async Task<ActionResult> GetPlatforms()
    {
        var platforms = await _platformService.GetPlatforms();
        return Ok(platforms);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetPlatform(int id)
    {
        var platform = await _platformService.GetPlatform(id);
        if (platform == null)
        {
            return NotFound();
        }

        return Ok(platform);
    }

    [HttpPost]
    public async Task<ActionResult> CreatePlatform(AddPlatformDto platformDto)
    {
        try
        {
            var newPlatform = await _platformService.CreatePlatform(platformDto);
            return CreatedAtAction(nameof(GetPlatform), new { id = newPlatform.Id }, newPlatform);
        }
        catch (Exception ex) when (ex is ValidationException or DuplicateEntityException)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> UpdatePlatform(int id, UpdatePlatformDto platformDto)
    {
        try
        {
            var updatedPlatform = await _platformService.UpdatePlatform(id, platformDto);
            return Ok(updatedPlatform);
        }
        catch (Exception ex) when (ex is ValidationException or DuplicateEntityException)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeletePlatform(int id)
    {
        await _platformService.DeletePlatform(id);
        return Ok();
    }
}