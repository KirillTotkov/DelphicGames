using DelphicGames.Data.Models;
using DelphicGames.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DelphicGames.Controllers;

[ApiController]
[Route("api/nominations")]
[Authorize(Roles = $"{nameof(UserRoles.Root)},{nameof(UserRoles.Admin)}")]
public class NominationController : ControllerBase
{
    private readonly NominationService _nominationService;

    public NominationController(NominationService nominationService)
    {
        _nominationService = nominationService;
    }

    [HttpGet]
    public async Task<ActionResult> GetNominations()
    {
        var nominations = await _nominationService.GetNominations();
        return Ok(nominations);
    }

    [HttpGet("with-cameras")]
    public async Task<ActionResult> GetAllNominationsWithCameras()
    {
        var nominations = await _nominationService.GetNominationsWithCameras();
        return Ok(nominations);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetNomination(int id)
    {
        var nomination = await _nominationService.GetNominationWithCameras(id);
        if (nomination == null)
        {
            return NotFound();
        }

        return Ok(nomination);
    }

    [HttpPost]
    public async Task<ActionResult> CreateNomination(AddNominationDto dto)
    {
        try
        {
            var nomination = await _nominationService.AddNomination(dto);
            var result = new GetNominationDto(
                nomination.Id,
                nomination.Name,
                nomination.StreamUrl,
                nomination.Cameras.Select(c => new GetCameraDto(c.Id, c.Name, c.Url)).ToList(),
                nomination.Platforms.Select(np => new GetNominationPlatformDto(np.PlatformId, np.Platform.Name, np.Token))
                    .ToList()
            );
            return CreatedAtAction(nameof(GetNomination), new { id = nomination.Id }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> UpdateNomination(int id, AddNominationDto dto)
    {
        try
        {
            await _nominationService.UpdateNomination(id, dto);
            return Ok();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteNomination(int id)
    {
        try
        {
            await _nominationService.DeleteNomination(id);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }
}