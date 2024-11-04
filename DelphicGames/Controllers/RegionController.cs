using DelphicGames.Data;
using DelphicGames.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DelphicGames.Controllers;

[ApiController]
[Route("/api/regions")]
public class RegionController : ControllerBase
{
    private readonly ApplicationContext _context;

    public RegionController(ApplicationContext context)
    {
        _context = context;
    }

    // GET: api/regions
    [HttpGet]
    public async Task<ActionResult> GetRegions()
    {
        var regions = await _context.Regions
            .Include(r => r.Cities)
            .Select(r => new RegionDto(r.Id, r.Name, r.Cities.Select(c => new CityDto(c.Id, c.Name)).ToList()))
            .ToListAsync();
        return Ok(regions);
    }

    // GET: api/region/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetRegion(int id)
    {
        var region = await _context.Regions.Include(r => r.Cities).FirstOrDefaultAsync(r => r.Id == id);

        if (region == null)
        {
            return NotFound();
        }

        var regionDto = new RegionDto(region.Id, region.Name, region.Cities.Select(c => new CityDto(c.Id, c.Name)).ToList());
        return Ok(regionDto);
    }

    // POST: api/region
    [HttpPost]
    public async Task<ActionResult> PostRegion(AddRegionDto regionDto)
    {
        var region = new Region { Name = regionDto.Name };
        await _context.Regions.AddAsync(region);

        foreach (var cityDto in regionDto.Cities)
        {
            var city = new City { Name = cityDto.Name, Region = region };
            await _context.Cities.AddAsync(city);
        }

        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetRegion), new { id = region.Id }, region);
    }

    // DELETE: api/Region/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteRegion(int id)
    {
        var region = await _context.Regions.FindAsync(id);
        if (region == null)
        {
            return NotFound();
        }

        _context.Regions.Remove(region);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("{id:int}/cities")]
    public async Task<ActionResult<IEnumerable<CityDto>>> GetCitiesByRegion(int id)
    {
        var region = await _context.Regions
            .Include(r => r.Cities)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (region == null)
        {
            return NotFound();
        }

        var cities = region.Cities.Select(c => new CityDto(c.Id, c.Name)).ToList();
        return Ok(cities);
    }
}

public record AddRegionDto(string Name, List<AddCityDto> Cities);
public record AddCityDto(string Name);

public record RegionDto(int Id, string Name, List<CityDto> Cities);

public record CityDto(int Id, string Name);