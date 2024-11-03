namespace DelphicGames.Data.Models;

public class City
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public List<Camera> Cameras { get; set; }
    public required Region Region { get; set; }
    public int RegionId { get; set; }
}