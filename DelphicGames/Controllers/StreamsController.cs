using DelphicGames.Data.Models;
using DelphicGames.Services;
using DelphicGames.Services.Streaming;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DelphicGames.Controllers;

[ApiController]
[Route("api/streams")]
[Authorize(Roles = $"{nameof(UserRoles.Root)},{nameof(UserRoles.Admin)}")]
public class StreamsController : ControllerBase
{
    private readonly StreamService _streamService;

    public StreamsController(StreamService streamService)
    {
        _streamService = streamService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllStreams()
    {
        var streams = await _streamService.GetAllStreams();
        return Ok(streams);
    }

    [HttpPost("start")]
    public IActionResult StartStream(AddStreamDto streamDto)
    {
        try
        {
            _streamService.StartStream(streamDto);
            return Ok("Трансляция начата.");
        }
        catch (FfmpegProcessException ex)
        {
            return BadRequest($"Ошибка запуска трансляции");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception)
        {
            return StatusCode(500, "Внутренняя ошибка сервера.");
        }
    }


    [HttpPost("stop")]
    public IActionResult StopStream([FromBody] AddStreamDto streamDto)
    {
        try
        {
            _streamService.StopStream(streamDto.NominationId, streamDto.PlatformId);
            return Ok("Трансляция остановлена.");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("start/all")]
    public IActionResult StartAllStreams()
    {
        try
        {
            _streamService.StartAllStreams();
            return Ok("Все трансляции начаты.");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("stop/all")]
    public IActionResult StopAllStreams()
    {
        try
        {
            _streamService.StopAllStreams();
            return Ok("Все трансляции остановлены.");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("start/platform")]
    public IActionResult StartPlatformStreams([FromQuery] int platformId)
    {
        try
        {
            _streamService.StartPlatformStreams(platformId);
            return Ok($"Трансляции на платформе начаты.");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("stop/platform")]
    public IActionResult StopPlatformStreams([FromQuery] int platformId)
    {
        try
        {
            _streamService.StopPlatformStreams(platformId);
            return Ok($"Трансляции на платформе остановлены.");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("start/nomination")]
    public IActionResult StartNominationStreams([FromQuery] int nominationId)
    {
        try
        {
            _streamService.StartNominationStreams(nominationId);
            return Ok($"Трансляции для камеры начаты.");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("stop/nomination")]
    public IActionResult StopNominationStreams([FromQuery] int nominationId)
    {
        try
        {
            _streamService.StopNominationStreams(nominationId);
            return Ok($"Трансляции для камеры остановлены.");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }


}
