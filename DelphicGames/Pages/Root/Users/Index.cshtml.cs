using DelphicGames.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DelphicGames.Pages.Root.Users;

[Authorize(Roles = nameof(UserRoles.Root))]
public class IndexModel : PageModel
{
    private readonly UserManager<User> _userManager;

    public IndexModel(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    public IList<User> Users { get; set; }

    public async Task OnGetAsync()
    {
        Users = await _userManager.Users.ToListAsync();
    }
}