﻿using DelphicGames.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DelphicGames.Pages;

[Authorize(Roles = $"{nameof(UserRoles.Root)},{nameof(UserRoles.Admin)}")]
public class AdminCamerasModel : PageModel
{
    public void OnGet()
    {
        
    }
}