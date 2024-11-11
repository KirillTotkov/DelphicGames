using Microsoft.AspNetCore.Identity;

namespace DelphicGames.Data.Models;

public class User : IdentityUser
{
    public City? City { get; set; }
    public Region? Region { get; set; }

    public List<Camera>? Cameras { get; set; } = new();
}