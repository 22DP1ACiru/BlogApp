using BlogApp.Core.Constants;
using BlogApp.Core.Entities;
using BlogApp.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlogApp.Web.Controllers
{
    [Authorize(Roles = AppRoles.Administrator)]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<AdminController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        // GET: /Admin or /Admin/Index
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            var userRoleViewModels = new List<UserRoleViewModel>();

            // Prepare view models with user roles
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userRoleViewModels.Add(new UserRoleViewModel
                {
                    UserId = user.Id,
                    Username = user.UserName,
                    Email = user.Email,
                    Roles = roles
                });
            }

            return View(userRoleViewModels);
        }

        // POST: /Admin/AssignAuthorRole
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignAuthorRole(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return BadRequest("User ID cannot be empty.");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = $"User with ID '{userId}' not found.";
                return RedirectToAction(nameof(Index));
            }

            if (!await _roleManager.RoleExistsAsync(AppRoles.Author))
            {
                _logger.LogError("'{RoleName}' role does not exist. Run seeder.", AppRoles.Author);
                TempData["ErrorMessage"] = $"Role '{AppRoles.Author}' does not exist.";
                return RedirectToAction(nameof(Index));
            }

            // Check if user already has the role
            if (await _userManager.IsInRoleAsync(user, AppRoles.Author))
            {
                TempData["WarningMessage"] = $"User '{user.UserName}' already has the '{AppRoles.Author}' role.";
                return RedirectToAction(nameof(Index));
            }

            // Assign the role
            var result = await _userManager.AddToRoleAsync(user, AppRoles.Author);
            if (result.Succeeded)
            {
                _logger.LogInformation("Assigned role '{RoleName}' to user '{UserName}' (ID: {UserId})", AppRoles.Author, user.UserName, userId);
                TempData["SuccessMessage"] = $"Successfully assigned '{AppRoles.Author}' role to '{user.UserName}'.";
            }
            else
            {
                _logger.LogError("Error assigning role '{RoleName}' to user '{UserName}'. Errors: {Errors}", AppRoles.Author, user.UserName, string.Join(", ", result.Errors.Select(e => e.Description)));
                TempData["ErrorMessage"] = $"Error assigning role to '{user.UserName}'.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/RemoveAuthorRole
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveAuthorRole(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return BadRequest("User ID cannot be empty.");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = $"User with ID '{userId}' not found.";
                return RedirectToAction(nameof(Index));
            }

            // Check if user actually has the role
            if (!await _userManager.IsInRoleAsync(user, AppRoles.Author))
            {
                TempData["WarningMessage"] = $"User '{user.UserName}' does not have the '{AppRoles.Author}' role.";
                return RedirectToAction(nameof(Index));
            }

            // Remove the role
            var result = await _userManager.RemoveFromRoleAsync(user, AppRoles.Author);
            if (result.Succeeded)
            {
                _logger.LogInformation("Removed role '{RoleName}' from user '{UserName}' (ID: {UserId})", AppRoles.Author, user.UserName, userId);
                TempData["SuccessMessage"] = $"Successfully removed '{AppRoles.Author}' role from '{user.UserName}'.";
            }
            else
            {
                _logger.LogError("Error removing role '{RoleName}' from user '{UserName}'. Errors: {Errors}", AppRoles.Author, user.UserName, string.Join(", ", result.Errors.Select(e => e.Description)));
                TempData["ErrorMessage"] = $"Error removing role from '{user.UserName}'.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}