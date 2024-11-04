namespace DelphicGames.Data.Models;

public class CameraPlatform
{
    public int CameraId { get; set; }
    public Camera Camera { get; set; }
    public int PlatformId { get; set; }
    public Platform Platform { get; set; }
    public string? Token { get; set; }
    public bool IsActive { get; set; }
}