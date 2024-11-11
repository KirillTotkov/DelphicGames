using Microsoft.AspNetCore.Identity;

namespace DelphicGames.Data.Models;

public class User : IdentityUser
{
    public List<Camera>? Cameras { get; set; } = new();
}