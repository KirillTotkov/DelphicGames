namespace DelphicGames.Data.Models;

public class Camera
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Url { get; set; }
    public List<CameraPlatforms> CameraPlatforms { get; set; }  = [];
    public Nomination? Nomination { get; set; }
    public int? NominationId { get; set; }
}