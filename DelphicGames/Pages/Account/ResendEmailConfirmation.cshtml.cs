﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using DelphicGames.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace DelphicGames.Pages.Account;

[AllowAnonymous]
public class ResendEmailConfirmationModel : PageModel
{
    private readonly IEmailSender _emailSender;
    private readonly UserManager<User> _userManager;

    public ResendEmailConfirmationModel(UserManager<User> userManager, IEmailSender emailSender)
    {
        _userManager = userManager;
        _emailSender = emailSender;
    }
        
    [BindProperty]
    public InputModel Input { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _userManager.FindByEmailAsync(Input.Email);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Пользователь с таким адресом электронной почты не найден");
            return Page();
        }
            
        if (user.EmailConfirmed)
        {
            ModelState.AddModelError(string.Empty, "Ваш адрес электронной почты уже подтвержден");
            return Page();
        }

        var userId = await _userManager.GetUserIdAsync(user);
        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
        var callbackUrl = Url.Page(
            "/Account/ConfirmEmail",
            pageHandler: null,
            values: new { userId = userId, code = code },
            protocol: Request.Scheme);
        await _emailSender.SendEmailAsync(
            Input.Email,
            "Подтвердите свой адрес электронной почты",
            $"Пожалуйста, подтвердите свой аккаунт <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>нажав здесь</a>.");

        ModelState.AddModelError(string.Empty, "Письмо с подтверждением отправлено. Пожалуйста, проверьте свою электронную почту");
        return Page();
    }
        
    public class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}