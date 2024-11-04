#nullable disable

using System.Text;
using DelphicGames.Data.Models;
using DelphicGames.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace DelphicGames.Pages.Account;

[AllowAnonymous]
public class RegisterConfirmationModel : PageModel
{
    private readonly UserManager<User> _userManager;

    private readonly EmailSettings _emailSettings;

    public bool EnableSendConfirmationEmail { get; set; }
    public string EmailConfirmationUrl { get; set; }

    public RegisterConfirmationModel(UserManager<User> userManager, IOptions<EmailSettings> emailSettings)
    {
        _userManager = userManager;
        _emailSettings = emailSettings.Value;
        EnableSendConfirmationEmail = _emailSettings.EnableSendConfirmationEmail;
    }

    public async Task<IActionResult> OnGetAsync(string email, string returnUrl = null)
    {
        if (string.IsNullOrEmpty(email))
        {
            return RedirectToPage("/Index");
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            return NotFound($"Unable to load user with email '{email}'.");
        }

        if (!EnableSendConfirmationEmail)
        {
            var userId = await _userManager.GetUserIdAsync(user);
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            EmailConfirmationUrl = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { userId = userId, code = code, returnUrl = returnUrl },
                protocol: Request.Scheme);
        }

        return Page();
    }
}