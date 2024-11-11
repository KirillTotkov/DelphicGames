namespace DelphicGames.Data.Models;

public class Camera
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Url { get; set; }
    public List<CameraPlatform> CameraPlatforms { get; set; } = [];
    public Nomination? Nomination { get; set; }
    public int? NominationId { get; set; }
    public User User { get; set; }
    public string UserId { get; set; }
}