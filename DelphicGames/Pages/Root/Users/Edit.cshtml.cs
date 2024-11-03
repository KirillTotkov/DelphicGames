using DelphicGames.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DelphicGames.Pages.Root.Users;

[Authorize(Roles = nameof(UserRoles.Root))]
public class EditModel : PageModel
{
    private readonly UserManager<User> _userManager;
    private readonly ILogger<EditModel> _logger;
    
    public EditModel(UserManager<User> userManager, ILogger<EditModel> logger)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _logger = logger;
    }
    [BindProperty] public EditUserInput Input { get; set; }

    public string UserId { get; set; }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        UserId = user.Id;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string id)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var resetPassResult = await _userManager.RemovePasswordAsync(user);
        if (!resetPassResult.Succeeded)
        {
            foreach (var error in resetPassResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }

        var addPassResult = await _userManager.AddPasswordAsync(user, Input.NewPassword);
        if (addPassResult.Succeeded)
        {
            _logger.LogInformation("Пароль пользователя изменен.");
            return RedirectToPage("Index");
        }

        foreach (var error in addPassResult.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return Page();
    }

    public class EditUserInput
    {
        public string NewPassword { get; set; }
    }
}