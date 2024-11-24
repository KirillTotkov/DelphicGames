﻿using DelphicGames.Data.Models;
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
    private readonly ILogger<StreamsController> _logger;
    private readonly StreamService _streamService;

    public StreamsController(StreamService streamService, ILogger<StreamsController> logger)
    {
        _streamService = streamService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult> AddStream([FromBody] AddStreamDto? streamDto)
    {
        if (streamDto == null)
        {
            return BadRequest("Данные дня не должны быть null.");
        }

        try
        {
            await _streamService.AddStream(streamDto);
            return Ok("День добавлен успешно.");
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Неверные данные при добавлении дня.");
            return BadRequest(new { Error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при добавлении дня.");
            return StatusCode(500, "Внутренняя ошибка сервера.");
        }
    }

    [HttpDelete]
    public async Task<ActionResult> DeleteStream([FromQuery] int id)
    {
        if (id <= 0)
        {
            return BadRequest("Неверный идентификатор трансляции.");
        }

        try
        {
            await _streamService.DeleteStream(id);
            return Ok("Трансляция удалена успешно.");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Трансляция не найдена.");
            return NotFound(new { Error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении трансляции.");
            return StatusCode(500, "Внутренняя ошибка сервера.");
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAllStreams()
    {
        var streams = await _streamService.GetAllStreams();
        return Ok(streams);
    }

    [HttpGet("nominations/{nominationId:int}")]
    public async Task<ActionResult> GetNominationStreams(int nominationId)
    {
        var streams = await _streamService.GetNominationStreams(nominationId);
        return Ok(streams);
    }

    [HttpPut("{streamId:int}")]
    public async Task<ActionResult> UpdateStream([FromRoute] int streamId, [FromBody] UpdateStreamDto dto)
    {
        try
        {
            await _streamService.UpdateStream(streamId, dto);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, "Внутренняя ошибка сервера.");
        }
    }

    [HttpPost("start/{streamId:int}")]
    public async Task<IActionResult> StartStream(int streamId)
    {
        try
        {
            await _streamService.StartStreamAsync(streamId);
            return Ok("Трансляция начата.");
        }
        catch (FfmpegProcessException ex)
        {
            return BadRequest($"Ошибка запуска трансляции");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, "Внутренняя ошибка сервера.");
        }
    }


    [HttpPost("stop/{streamId:int}")]
    public async Task<IActionResult> StopStream(int streamId)
    {
        try
        {
            await _streamService.StopStreamAsync(streamId);
            return Ok("Трансляция остановлена.");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, "Внутренняя ошибка сервера.");
        }
    }

    [HttpPost("start/all")]
    public async Task<IActionResult> StartAllStreams()
    {
        try
        {
            await _streamService.StartAllStreams();
            return Ok("Все трансляции начаты.");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpPost("stop/all")]
    public async Task<IActionResult> StopAllStreams()
    {
        try
        {
            await _streamService.StopAllStreams();
            return Ok("Все трансляции остановлены.");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpPost("start/nomination")]
    public async Task<IActionResult> StartNominationStreams([FromQuery] int nominationId)
    {
        try
        {
            await _streamService.StartNominationStreams(nominationId);
            return Ok($"Трансляции для камеры начаты.");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpPost("stop/nomination")]
    public async Task<IActionResult> StopNominationStreams([FromQuery] int nominationId)
    {
        try
        {
            await _streamService.StopNominationStreams(nominationId);
            return Ok($"Трансляции для камеры остановлены.");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpPost("start/day/{day:int}")]
    public async Task<IActionResult> StartDayStreams(int day)
    {
        try
        {
            bool allLaunched = await _streamService.StartStreamsByDay(day);
            if (allLaunched)
            {
                return Ok(new { success = true, partial = false, message = $"День {day}: все трансляции были запущены." });
            }
            else
            {
                return Ok(new { success = true, partial = true, message = $"День {day}: не все трансляции были запущены"});
            }

        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpPost("stop/day/{dayId:int}")]
    public async Task<IActionResult> StopDayStreams(int dayId)
    {
        try
        {
            await _streamService.StopStreamsByDay(dayId);
            return Ok($"Трансляции для дня остановлены.");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpGet("active")]
    public IActionResult GetActiveStreamsProcesses()
    {
        var streams =  _streamService.GetActiveStreamsProcesses();
        return Ok(streams);
    }
    
}