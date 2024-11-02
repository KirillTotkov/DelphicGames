namespace DelphicGames.Data.Models;

public class Nomination
{
    public int Id { get; set; }
    public string Name { get; set; }
    public List<Camera> Cameras { get; set; }
}