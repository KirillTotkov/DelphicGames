﻿using DelphicGames.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DelphicGames.Pages.Root.Users;

[Authorize(Roles = nameof(UserRoles.Root))]
public class CreateModel : PageModel
{
    private readonly ILogger<CreateModel> _logger;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<User> _userManager;

    public CreateModel(UserManager<User> userManager, RoleManager<IdentityRole> roleManager,
        ILogger<CreateModel> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    [BindProperty] public CreateUserInput Input { get; set; }
    public List<SelectListItem> Roles { get; set; }


    public async Task OnGetAsync()
    {
        Roles = await _roleManager.Roles.Select(r => new SelectListItem { Value = r.Name, Text = r.Name })
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            Roles = _roleManager.Roles.Select(r => new SelectListItem { Value = r.Name, Text = r.Name }).ToList();
            return Page();
        }

        var user = new User { UserName = Input.Email, Email = Input.Email };
        var result = await _userManager.CreateAsync(user, Input.Password);

        if (result.Succeeded)
        {
            if (!string.IsNullOrEmpty(Input.Role))
            {
                if (await _roleManager.RoleExistsAsync(Input.Role))
                {
                    user.EmailConfirmed = true;
                    await _userManager.AddToRoleAsync(user, Input.Role);
                }
            }

            _logger.LogInformation("Пользователь создан и назначена роль.");
            return RedirectToPage("Index");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        Roles = _roleManager.Roles.Select(r => new SelectListItem { Value = r.Name, Text = r.Name }).ToList();
        return Page();
    }

    public class CreateUserInput
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
    }
}