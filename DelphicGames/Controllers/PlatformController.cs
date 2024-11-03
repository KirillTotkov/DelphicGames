using DelphicGames.Services;
using Microsoft.AspNetCore.Mvc;

namespace DelphicGames.Controllers;

[ApiController]
[Route("api/[controller]")]
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
        await _platformService.CreatePlatform(platformDto);
        return Ok();
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> UpdatePlatform(int id, UpdatePlatformDto platformDto)
    {
        await _platformService.UpdatePlatform(id, platformDto);
        return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeletePlatform(int id)
    {
        await _platformService.DeletePlatform(id);
        return Ok();
    }
}