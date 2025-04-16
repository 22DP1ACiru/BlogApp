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
        ViewBag.StatusMessage = TempData["StatusMessage"];
        return View(model);
    }

    // POST: /Manage/Index (Profile Update)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ProfileViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var currentUser = await GetCurrentUserAsync();
            model.Email = currentUser.Email;
            model.ProfilePictureUrl = currentUser.ProfilePictureUrl;
            return View(model);
        }

        var user = await GetCurrentUserAsync();
        bool profileUpdated = false;
        string oldProfilePictureUrl = user.ProfilePictureUrl; // Store old URL for deletion later

        // --- Handle Profile Picture Upload ---
        if (model.ProfilePicture != null && model.ProfilePicture.Length > 0)
        {
            // Validate File
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
                // Generate unique filename and path
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "profiles");
                // Ensure the directory exists
                Directory.CreateDirectory(uploadsFolder);

                string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(model.ProfilePicture.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Save the new file
                try
                {
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ProfilePicture.CopyToAsync(fileStream);
                    }

                    // Update user's ProfilePictureUrl
                    // Store the web-accessible path (relative to wwwroot)
                    user.ProfilePictureUrl = $"/images/profiles/{uniqueFileName}";
                    profileUpdated = true;
                    _logger.LogInformation("User {UserId} uploaded new profile picture: {FilePath}", user.Id, filePath);

                    // Delete the old profile picture file
                    if (!string.IsNullOrEmpty(oldProfilePictureUrl) && oldProfilePictureUrl != user.ProfilePictureUrl)
                    {
                        // Convert web path back to physical path for deletion
                        // Remove leading '/' if present
                        string oldFileName = Path.GetFileName(oldProfilePictureUrl); // Extract filename from URL
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
                                // Log error but don't necessarily fail the whole operation
                                _logger.LogError(ex, "Error deleting old profile picture file {OldFilePath} for user {UserId}", oldFilePath, user.Id);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving profile picture file for user {UserId}", user.Id);
                    ModelState.AddModelError(string.Empty, "An error occurred while saving the profile picture.");
                    // Ensure the profile picture URL isn't updated if saving failed
                    user.ProfilePictureUrl = oldProfilePictureUrl;
                    profileUpdated = false;
                }
            }
        }

        // If there were errors during file processing, return the view
        if (!ModelState.IsValid)
        {
            // Re-populate non-posted fields if returning view due to file error
            model.Username = user.UserName;
            model.Email = user.Email;
            model.ProfilePictureUrl = oldProfilePictureUrl; // Show the original picture if upload failed
            return View(model);
        }


        // Save changes to the user profile (only if picture was updated)
        if (profileUpdated)
        {
            var result = await _accountService.UpdateUserProfileAsync(user);
            if (!result.Succeeded)
            {
                // Revert URL if save fails? Or assume temporary failure? For now, add error.
                user.ProfilePictureUrl = oldProfilePictureUrl; // Maybe revert on DB save failure
                ModelState.AddModelError(string.Empty, "An error occurred updating your profile database record.");
                foreach (var error in result.Errors) { ModelState.AddModelError(string.Empty, error.Description); }

                // Re-populate non-posted fields before returning view on error
                model.Username = user.UserName;
                model.Email = user.Email;
                model.ProfilePictureUrl = oldProfilePictureUrl;
                return View(model);
            }
            await _signInManager.RefreshSignInAsync(user);
            TempData["StatusMessage"] = "Your profile has been updated.";
        }
        else if (model.ProfilePicture != null && model.ProfilePicture.Length > 0)
        {
            // If a file was uploaded but didn't pass validation or save correctly,
            // status message might already be set by ModelState errors.
            // Add a generic one if nothing else was set.
            if (ModelState.ErrorCount == 0) // Check if specific errors were already added
                TempData["StatusMessage"] = "Profile update failed. Please check the requirements.";
        }
        else
        {
            // If no file was uploaded and no other changes were made
            TempData["StatusMessage"] = "No changes were detected.";
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
            // Nothing to remove
            TempData["StatusMessage"] = "No profile picture to remove.";
            return RedirectToAction(nameof(Index));
        }

        // Clear the URL in the user record
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
                // Extract filename from potentially null/empty string
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

            // Refresh claims if needed (might not be strictly necessary for just URL change)
            await _signInManager.RefreshSignInAsync(user);
        }

        return RedirectToAction(nameof(Index));
    }

    // GET: /Manage/ChangePassword
    [HttpGet]
    public IActionResult ChangePassword()
    {
        ViewBag.StatusMessage = TempData["StatusMessage"];
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