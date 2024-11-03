using DelphicGames.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DelphicGames.Pages.Admin.Users;

[Authorize(Roles = "Root")]
public class DeleteModel : PageModel
{
    private readonly UserManager<User> _userManager;
    private readonly ILogger<DeleteModel> _logger;

    public DeleteModel(UserManager<User> userManager, ILogger<DeleteModel> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    [BindProperty] public User User { get; set; }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        User = await _userManager.FindByIdAsync(id);
        if (User == null)
        {
            return NotFound();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var result = await _userManager.DeleteAsync(user);
        if (result.Succeeded)
        {
            _logger.LogInformation("Пользователь удален.");
            return RedirectToPage("Index");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return Page();
    }
}