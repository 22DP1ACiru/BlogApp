using BlogApp.BLL.Interfaces;
using BlogApp.Core.Entities;
using BlogApp.Web.Controllers;
using BlogApp.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

[AllowAnonymous]
public class AccountController : Controller
{
    private readonly IAccountService _accountService;
    private readonly IEmailSender _emailSender;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;


    public AccountController(
        IAccountService accountService,
        IEmailSender emailSender,
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager)
    {
        _accountService = accountService;
        _emailSender = emailSender;
        _signInManager = signInManager;
        _userManager = userManager;
    }

    // GET: /Account/Login
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    // POST: /Account/Login
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (ModelState.IsValid)
        {
            var result = await _accountService.LoginUserAsync(model.Email, model.Password, model.RememberMe);

            if (result.Succeeded)
            {
                return RedirectToLocal(returnUrl);
            }
            if (result.RequiresTwoFactor) { return RedirectToAction("LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = model.RememberMe }); }
            if (result.IsLockedOut) { return RedirectToAction("Lockout"); }
            else
            {
                var user = await _accountService.FindUserByEmailAsync(model.Email);
                if (user != null && !await _accountService.IsEmailConfirmedAsync(user))
                {
                    ModelState.AddModelError(string.Empty, "You must confirm your email before logging in.");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                }
                return View(model);
            }
        }
        return View(model);
    }

    // POST: /Account/Logout
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    // GET: /Account/Register
    [HttpGet]
    public IActionResult Register(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    // POST: /Account/Register
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (ModelState.IsValid)
        {
            var user = new ApplicationUser
            {
                UserName = model.Username,
                Email = model.Email,
            };

            var result = await _accountService.RegisterUserAsync(user, model.Password);

            if (result.Succeeded)
            {
                var code = await _accountService.GenerateEmailConfirmationTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

                var callbackUrl = Url.Action(nameof(ConfirmEmail), "Account", new { userId = user.Id, code = code }, protocol: Request.Scheme);

                await _emailSender.SendEmailAsync(model.Email, "Confirm your email",
                    $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                return RedirectToAction(nameof(RegisterConfirmation), new { email = model.Email });
            }

            foreach (var error in result.Errors)
            {
                // Check if the error is about duplicate username
                if (error.Code == "DuplicateUserName")
                {
                    ModelState.AddModelError(nameof(model.Username), error.Description); // Attach error to Username field
                }
                else
                {
                    ModelState.AddModelError(string.Empty, error.Description); // General error
                }
            }
        }
        return View(model);
    }

    // GET: /Account/RegisterConfirmation
    [HttpGet]
    public IActionResult RegisterConfirmation(string email)
    {
        if (string.IsNullOrEmpty(email)) return RedirectToAction("Index", "Home");
        ViewBag.Email = email;
        return View();
    }

    // GET: /Account/ConfirmEmail
    [HttpGet]
    public async Task<IActionResult> ConfirmEmail(string userId, string code)
    {
        if (userId == null || code == null) return RedirectToAction("Index", "Home");
        var user = await _accountService.FindUserByIdAsync(userId);
        if (user == null) return View("Error");

        string decodedCode;
        try { decodedCode = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code)); }
        catch (FormatException)
        {
            ViewBag.StatusMessage = "Error confirming your email: Invalid token format.";
            return View("ConfirmEmailResult");
        }

        var result = await _accountService.ConfirmEmailAsync(user, decodedCode);
        ViewBag.StatusMessage = result.Succeeded ? "Thank you for confirming your email. You can now log in." : "Error confirming your email.";
        return View("ConfirmEmailResult");
    }

    // GET: /Account/ForgotPassword
    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    // POST: /Account/ForgotPassword
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = await _accountService.FindUserByEmailAsync(model.Email);
            if (user == null || !(await _accountService.IsEmailConfirmedAsync(user)))
            {
                return RedirectToAction(nameof(ForgotPasswordConfirmation));
            }

            var code = await _accountService.GeneratePasswordResetTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var callbackUrl = Url.Action(nameof(ResetPassword), "Account", new { code = code }, protocol: Request.Scheme);

            await _emailSender.SendEmailAsync(model.Email, "Reset Password", $"Please reset your password by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");
            return RedirectToAction(nameof(ForgotPasswordConfirmation));
        }
        return View(model);
    }

    // GET: /Account/ForgotPasswordConfirmation
    [HttpGet]
    public IActionResult ForgotPasswordConfirmation()
    {
        return View();
    }

    // GET: /Account/ResetPassword
    [HttpGet]
    public IActionResult ResetPassword(string? code = null)
    {
        if (code == null) return View("Error");
        var model = new ResetPasswordViewModel { Code = code };
        return View(model);
    }

    // POST: /Account/ResetPassword
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await _accountService.FindUserByEmailAsync(model.Email);
        if (user == null) return RedirectToAction(nameof(ResetPasswordConfirmation));

        string decodedCode;
        try { decodedCode = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(model.Code)); }
        catch (FormatException)
        {
            ModelState.AddModelError(string.Empty, "Invalid password reset token.");
            return View(model);
        }

        var result = await _accountService.ResetPasswordAsync(user, decodedCode, model.Password);
        if (result.Succeeded) return RedirectToAction(nameof(ResetPasswordConfirmation));

        foreach (var error in result.Errors)
        {
            if (error.Code == "InvalidToken") { ModelState.AddModelError(string.Empty, "Password reset failed. Please request a new link."); }
            else { ModelState.AddModelError(string.Empty, error.Description); }
        }
        return View(model);
    }

    // GET: /Account/ResetPasswordConfirmation
    [HttpGet]
    public IActionResult ResetPasswordConfirmation()
    {
        return View();
    }

    // --- Helper Method ---
    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
        else return RedirectToAction(nameof(HomeController.Index), "Home");
    }
}