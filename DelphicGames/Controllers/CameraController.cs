using DelphicGames.Data;
using DelphicGames.Data.Models;
using DelphicGames.Services;
using Microsoft.AspNetCore.Mvc;

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
    public async Task<IActionResult> AddCamera([FromBody] AddCameraDto dto)
    {
        await _cameraService.CreateCamera(dto);

        return Ok();
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
}

