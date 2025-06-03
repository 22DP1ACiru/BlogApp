using BlogApp.Core.Constants;
using BlogApp.Core.Entities;
using BlogApp.BLL.Interfaces;
using BlogApp.BLL.DTOs;
using BlogApp.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BlogApp.Web.Controllers
{
    [Authorize(Roles = AppRoles.Administrator)]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IModerationService _moderationService;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            IModerationService moderationService,
            RoleManager<IdentityRole> roleManager,
            ILogger<AdminController> logger)
        {
            _userManager = userManager;
            _moderationService = moderationService;
            _roleManager = roleManager;
            _logger = logger;
        }

        // GET: /Admin or /Admin/Index
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            var userRoleViewModels = new List<UserRoleViewModel>();

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

        // ... (Role assignment methods remain the same) ...
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignAuthorRole(string userId)
        {
            return await AssignRoleAsync(userId, AppRoles.Author);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveAuthorRole(string userId)
        {
            return await RemoveRoleAsync(userId, AppRoles.Author);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignRankerRole(string userId)
        {
            return await AssignRoleAsync(userId, AppRoles.Ranker);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveRankerRole(string userId)
        {
            return await RemoveRoleAsync(userId, AppRoles.Ranker);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignCommenterRole(string userId)
        {
            return await AssignRoleAsync(userId, AppRoles.Commenter);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveCommenterRole(string userId)
        {
            return await RemoveRoleAsync(userId, AppRoles.Commenter);
        }
        private async Task<IActionResult> AssignRoleAsync(string userId, string roleName)
        {
            if (string.IsNullOrEmpty(userId)) return BadRequest("User ID cannot be empty.");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = $"User with ID '{userId}' not found.";
                return RedirectToAction(nameof(Index));
            }

            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                _logger.LogError("'{RoleName}' role does not exist. Run seeder.", roleName);
                TempData["ErrorMessage"] = $"Role '{roleName}' does not exist.";
                return RedirectToAction(nameof(Index));
            }

            if (await _userManager.IsInRoleAsync(user, roleName))
            {
                TempData["WarningMessage"] = $"User '{user.UserName}' already has the '{roleName}' role.";
                return RedirectToAction(nameof(Index));
            }

            var result = await _userManager.AddToRoleAsync(user, roleName);
            if (result.Succeeded)
            {
                _logger.LogInformation("Assigned role '{RoleName}' to user '{UserName}' (ID: {UserId})", roleName, user.UserName, userId);
                TempData["SuccessMessage"] = $"Successfully assigned '{roleName}' role to '{user.UserName}'.";
            }
            else
            {
                _logger.LogError("Error assigning role '{RoleName}' to user '{UserName}'. Errors: {Errors}", roleName, user.UserName, string.Join(", ", result.Errors.Select(e => e.Description)));
                TempData["ErrorMessage"] = $"Error assigning '{roleName}' role to '{user.UserName}'.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<IActionResult> RemoveRoleAsync(string userId, string roleName)
        {
            if (string.IsNullOrEmpty(userId)) return BadRequest("User ID cannot be empty.");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = $"User with ID '{userId}' not found.";
                return RedirectToAction(nameof(Index));
            }

            var currentAdminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (roleName == AppRoles.Administrator && user.Id == currentAdminUserId)
            {
                _logger.LogWarning("Admin User {AdminUserId} attempted to remove their own Administrator role.", currentAdminUserId);
                TempData["ErrorMessage"] = "Error: Cannot remove your own Administrator role.";
                return RedirectToAction(nameof(Index));
            }

            if (!await _userManager.IsInRoleAsync(user, roleName))
            {
                TempData["WarningMessage"] = $"User '{user.UserName}' does not have the '{roleName}' role.";
                return RedirectToAction(nameof(Index));
            }

            var result = await _userManager.RemoveFromRoleAsync(user, roleName);
            if (result.Succeeded)
            {
                _logger.LogInformation("Removed role '{RoleName}' from user '{UserName}' (ID: {UserId})", roleName, user.UserName, userId);
                TempData["SuccessMessage"] = $"Successfully removed '{roleName}' role from '{user.UserName}'.";
            }
            else
            {
                _logger.LogError("Error removing role '{RoleName}' from user '{UserName}'. Errors: {Errors}", roleName, user.UserName, string.Join(", ", result.Errors.Select(e => e.Description)));
                TempData["ErrorMessage"] = $"Error removing '{roleName}' role from '{user.UserName}'.";
            }

            return RedirectToAction(nameof(Index));
        }


        // GET: /Admin/ReportedComments
        [HttpGet]
        public async Task<IActionResult> ReportedComments()
        {
            var groupedReportDtos = await _moderationService.GetGroupedPendingReportsAsync();

            // Map DTOs to ViewModels
            var viewModels = groupedReportDtos.Select(dto => new AdminReportedCommentViewModel
            {
                CommentId = dto.CommentId,
                ReportedCommentContentPreview = dto.CommentContentPreview,
                FullCommentContent = dto.FullCommentContent,
                ArticleId = dto.ArticleId,
                ArticleTitle = dto.ArticleTitle,
                IsCommentBlocked = dto.IsCommentBlocked,
                PendingReportCount = dto.PendingReportCount,
                IndividualReports = dto.IndividualReportDetails.Select(detailDto => new IndividualReportViewModel
                {
                    ReportId = detailDto.ReportId,
                    ReporterUsername = detailDto.ReporterUsername,
                    ReportDate = detailDto.ReportDate,
                    Reason = detailDto.Reason,
                    Status = detailDto.Status
                    // ReviewedByAdminUsername and ReviewedDate can be mapped if they were included in ReportDetailDto
                }).ToList()
            }).ToList();

            return View(viewModels);
        }

        // POST: /Admin/BlockComment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BlockComment(int commentId, string? returnUrl = null)
        {
            var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (adminUserId == null) return Challenge();

            bool success = await _moderationService.BlockCommentAsync(commentId, adminUserId);

            if (success) TempData["SuccessMessage"] = "Comment blocked successfully. All associated pending reports have been marked as actioned.";
            else TempData["ErrorMessage"] = "Failed to block comment (it might already be blocked, deleted, or had no pending reports).";

            return RedirectToAction(nameof(ReportedComments));
        }

        // POST: /Admin/UnblockComment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnblockComment(int commentId, int? articleId = null, string? returnUrl = null)
        {
            var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (adminUserId == null) return Challenge();

            bool success = await _moderationService.UnblockCommentAsync(commentId, adminUserId);

            if (success) TempData["SuccessMessage"] = "Comment unblocked successfully.";
            else TempData["ErrorMessage"] = "Failed to unblock comment (it might not be blocked or doesn't exist).";

            if (Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
            if (articleId.HasValue) return RedirectToAction("Details", "Articles", new { id = articleId.Value, fragment = $"comment-{commentId}" });
            return RedirectToAction(nameof(ReportedComments));
        }

        // POST: /Admin/DismissReport
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DismissReport(int reportId)
        {
            var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (adminUserId == null) return Challenge();

            bool success = await _moderationService.DismissReportAsync(reportId, adminUserId);

            if (success) TempData["SuccessMessage"] = "Report dismissed successfully.";
            else TempData["ErrorMessage"] = "Failed to dismiss report (it might have already been actioned or doesn't exist).";

            return RedirectToAction(nameof(ReportedComments));
        }
    }
}