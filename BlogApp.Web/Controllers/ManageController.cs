using BlogApp.BLL.Interfaces;
using BlogApp.Core.Entities;
using BlogApp.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

[Authorize]
public class ManageController : Controller
{
    private readonly IAccountService _accountService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly ILogger<ManageController> _logger;

    public ManageController(
        IAccountService accountService,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IWebHostEnvironment webHostEnvironment,
        ILogger<ManageController> logger)
    {
        _accountService = accountService;
        _userManager = userManager;
        _signInManager = signInManager;
        _webHostEnvironment = webHostEnvironment;
        _logger = logger;
    }

    private async Task<ApplicationUser> GetCurrentUserAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) throw new InvalidOperationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        return user;
    }

    // GET: /Manage/Index (Profile)
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await GetCurrentUserAsync();
        var model = new ProfileViewModel
        {
            Username = user.UserName,
            Email = user.Email,
            ProfilePictureUrl = user.ProfilePictureUrl
        };
        // Preserve TempData if it was set by a POST action that redirected here
        if (TempData["StatusMessage"] != null)
        {
            ViewBag.StatusMessage = TempData["StatusMessage"];
        }
        return View(model);
    }

    // POST: /Manage/Index (Profile Update)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ProfileViewModel model)
    {
        var user = await GetCurrentUserAsync(); // Get user details early

        // If ModelState is invalid (e.g., due to ProfilePicture validation errors for type/size)
        // we need to repopulate the display fields before returning the view.
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("ModelState is invalid for user {UserId} during profile update.", user.Id);
            // Repopulate fields that are not posted back because they are disabled in the view
            model.Username = user.UserName;
            model.Email = user.Email;
            model.ProfilePictureUrl = user.ProfilePictureUrl; // Show existing picture if new one failed validation
            return View(model);
        }

        // ModelState is valid, proceed with logic for updating profile picture
        bool profileUpdated = false;
        string oldProfilePictureUrl = user.ProfilePictureUrl;

        if (model.ProfilePicture != null && model.ProfilePicture.Length > 0)
        {
            long maxFileSize = 1024 * 1024; // 1 MB
            var allowedContentTypes = new[] { "image/jpeg", "image/png", "image/gif" };

            if (model.ProfilePicture.Length > maxFileSize)
            {
                ModelState.AddModelError(nameof(model.ProfilePicture), $"File size exceeds the limit of {maxFileSize / 1024 / 1024} MB.");
            }
            else if (!allowedContentTypes.Contains(model.ProfilePicture.ContentType.ToLowerInvariant()))
            {
                ModelState.AddModelError(nameof(model.ProfilePicture), "Invalid file type. Only JPG, PNG, and GIF are allowed.");
            }
            else
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "profiles");
                Directory.CreateDirectory(uploadsFolder);
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(model.ProfilePicture.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                try
                {
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ProfilePicture.CopyToAsync(fileStream);
                    }
                    user.ProfilePictureUrl = $"/images/profiles/{uniqueFileName}";
                    profileUpdated = true;
                    _logger.LogInformation("User {UserId} uploaded new profile picture: {FilePath}", user.Id, filePath);

                    if (!string.IsNullOrEmpty(oldProfilePictureUrl) && oldProfilePictureUrl != user.ProfilePictureUrl)
                    {
                        string oldFileName = Path.GetFileName(oldProfilePictureUrl);
                        string oldFilePath = Path.Combine(uploadsFolder, oldFileName);
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            try
                            {
                                System.IO.File.Delete(oldFilePath);
                                _logger.LogInformation("Deleted old profile picture for user {UserId}: {OldFilePath}", user.Id, oldFilePath);
                            }
                            catch (IOException ex)
                            {
                                _logger.LogError(ex, "Error deleting old profile picture file {OldFilePath} for user {UserId}", oldFilePath, user.Id);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving profile picture file for user {UserId}", user.Id);
                    ModelState.AddModelError(string.Empty, "An error occurred while saving the profile picture.");
                    user.ProfilePictureUrl = oldProfilePictureUrl;
                    profileUpdated = false;
                }
            }
        }

        // If there were errors during file processing AFTER the initial ModelState.IsValid check (e.g. inside the 'else' block above)
        if (!ModelState.IsValid)
        {
            // Repopulate for view display
            model.Username = user.UserName;
            model.Email = user.Email;
            model.ProfilePictureUrl = oldProfilePictureUrl; // Show original if upload failed
            return View(model);
        }

        if (profileUpdated)
        {
            var result = await _accountService.UpdateUserProfileAsync(user);
            if (!result.Succeeded)
            {
                user.ProfilePictureUrl = oldProfilePictureUrl; // Revert on DB save failure
                ModelState.AddModelError(string.Empty, "An error occurred updating your profile database record.");
                foreach (var error in result.Errors) { ModelState.AddModelError(string.Empty, error.Description); }

                // Repopulate for view display
                model.Username = user.UserName;
                model.Email = user.Email;
                model.ProfilePictureUrl = oldProfilePictureUrl;
                return View(model);
            }
            await _signInManager.RefreshSignInAsync(user);
            TempData["StatusMessage"] = "Your profile picture has been updated.";
        }
        else if (model.ProfilePicture != null && model.ProfilePicture.Length > 0)
        {
            // This case means a file was provided, but it failed validation within the 'if (model.ProfilePicture != null)' block
            // ModelState errors would have been added there.
            // The 'if (!ModelState.IsValid)' block further up should catch this and return the view.
            // Adding a generic message if for some reason it wasn't set.
            if (ModelState.ErrorCount == 0)
                TempData["StatusMessage"] = "Profile update failed due to an issue with the uploaded file. Please check the requirements.";
        }
        else
        {
            // No new picture uploaded, and no errors.
            TempData["StatusMessage"] = "No changes to profile picture were made.";
        }

        return RedirectToAction(nameof(Index));
    }

    // POST: /Manage/Index (Remove Profile Picture)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveProfilePicture()
    {
        var user = await GetCurrentUserAsync();
        string? currentPictureUrl = user.ProfilePictureUrl;

        if (string.IsNullOrEmpty(currentPictureUrl))
        {
            TempData["StatusMessage"] = "No profile picture to remove.";
            return RedirectToAction(nameof(Index));
        }

        user.ProfilePictureUrl = null;
        var result = await _accountService.UpdateUserProfileAsync(user);

        if (!result.Succeeded)
        {
            _logger.LogError("Failed to clear ProfilePictureUrl in DB for user {UserId}", user.Id);
            TempData["StatusMessage"] = "Error removing profile picture information.";
        }
        else
        {
            _logger.LogInformation("Cleared ProfilePictureUrl in DB for user {UserId}", user.Id);
            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "profiles");
            string? fileName = null;
            try
            {
                fileName = Path.GetFileName(currentPictureUrl);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Could not extract filename from URL '{Url}' for user {UserId}", currentPictureUrl, user.Id);
            }

            if (!string.IsNullOrEmpty(fileName))
            {
                string filePath = Path.Combine(uploadsFolder, fileName);
                if (System.IO.File.Exists(filePath))
                {
                    try
                    {
                        System.IO.File.Delete(filePath);
                        _logger.LogInformation("Deleted profile picture file {FilePath} for user {UserId}", filePath, user.Id);
                        TempData["StatusMessage"] = "Profile picture removed successfully.";
                    }
                    catch (IOException ex)
                    {
                        _logger.LogError(ex, "Error deleting profile picture file {FilePath} for user {UserId} after DB update.", filePath, user.Id);
                        TempData["StatusMessage"] = "Profile picture information removed, but an error occurred deleting the file.";
                    }
                }
                else
                {
                    _logger.LogWarning("Profile picture file not found for deletion: {FilePath}", filePath);
                    TempData["StatusMessage"] = "Profile picture removed successfully.";
                }
            }
            else
            {
                TempData["StatusMessage"] = "Profile picture information removed.";
            }
            await _signInManager.RefreshSignInAsync(user);
        }
        return RedirectToAction(nameof(Index));
    }

    // GET: /Manage/ChangePassword
    [HttpGet]
    public IActionResult ChangePassword()
    {
        // Preserve TempData if it was set by a POST action that redirected here
        if (TempData["StatusMessage"] != null)
        {
            ViewBag.StatusMessage = TempData["StatusMessage"];
        }
        return View();
    }

    // POST: /Manage/ChangePassword
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await GetCurrentUserAsync();
        var changePasswordResult = await _accountService.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);

        if (!changePasswordResult.Succeeded)
        {
            foreach (var error in changePasswordResult.Errors) { ModelState.AddModelError(string.Empty, error.Description); }
            return View(model);
        }

        await _signInManager.RefreshSignInAsync(user);
        TempData["StatusMessage"] = "Your password has been changed.";
        return RedirectToAction(nameof(ChangePassword));
    }
}