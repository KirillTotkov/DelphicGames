#nullable disable

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DelphicGames.Pages.Account;

[AllowAnonymous]
public class RegisterConfirmationModel : PageModel
{
    public IActionResult OnGet(string returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");

        return Page();
    }
}