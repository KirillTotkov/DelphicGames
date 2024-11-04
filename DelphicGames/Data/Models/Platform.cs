namespace DelphicGames.Data.Models;

public class Platform
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Url { get; set; }
    public List<CameraPlatform> CameraPlatforms { get; set; } = [];
}