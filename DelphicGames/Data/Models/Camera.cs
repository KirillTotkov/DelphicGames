namespace DelphicGames.Data.Models;

public class Camera
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Url { get; set; }
    public List<CameraPlatforms> CameraPlatforms { get; set; }
}