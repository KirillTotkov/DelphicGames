namespace DelphicGames.Data.Models;

public class NominationPlatform
{
    public int NominationId { get; set; }
    public Nomination Nomination { get; set; }
    public int PlatformId { get; set; }
    public Platform Platform { get; set; }
    public string? Token { get; set; }
    public bool IsActive { get; set; }
}