namespace DelphicGames.Data.Models;

public class StreamEntity
{
    public int NominationId { get; set; }
    public Nomination Nomination { get; set; }
    public string PlatformName { get; set; }
    public string PlatformUrl { get; set; }
    public string? Token { get; set; }
    public int Day { get; set; }
    public bool IsActive { get; set; }
}