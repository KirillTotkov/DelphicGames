using DelphicGames.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DelphicGames.Pages;

[Authorize(Roles = $"{nameof(UserRoles.Root)},{nameof(UserRoles.Admin)}, {nameof(UserRoles.Specialist)}")]
public class CamerasModel : PageModel
{
    public void OnGet()
    {
    }
}