namespace DelphicGames.Data.Models;

public class Nomination
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string StreamUrl { get; set; }
    public List<Camera> Cameras { get; set; } = [];
    public List<StreamEntity> Streams { get; set; } = [];
}