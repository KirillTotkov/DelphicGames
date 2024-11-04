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
    public async Task<ActionResult<IEnumerable<Region>>> GetRegions()
    {
        return await _context.Regions.Include(r => r.Cities).ToListAsync();
    }
    
    // GET: api/region/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Region>> GetRegion(int id)
    {
        var region = await _context.Regions.Include(r => r.Cities).FirstOrDefaultAsync(r => r.Id == id);

        if (region == null)
        {
            return NotFound();
        }

        return region;
    }
    
    // POST: api/region
    [HttpPost]
    public async Task<ActionResult<Region>> PostRegion(Region region)
    {
        _context.Regions.Add(region);
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

    

}