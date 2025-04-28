using BlogApp.BLL.Interfaces;
using BlogApp.Core.Constants;
using BlogApp.Core.Entities;
using BlogApp.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BlogApp.Web.Controllers
{
    [Authorize]
    public class CommentsController : Controller
    {
        private readonly ICommentService _commentService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<CommentsController> _logger;

        public CommentsController(
            ICommentService commentService,
            UserManager<ApplicationUser> userManager,
            ILogger<CommentsController> logger)
        {
            _commentService = commentService;
            _userManager = userManager;
            _logger = logger;
        }

        // POST: /Comments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{AppRoles.Commenter},{AppRoles.Administrator}")]
        public async Task<IActionResult> Create(CreateCommentViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized("User not found.");

            if (!await _commentService.CanUserCommentAsync(userId))
            {
                _logger.LogWarning("User {UserId} attempted to create comment without permission.", userId);
                TempData["ErrorMessage"] = "You do not have permission to comment.";
                return RedirectToAction("Details", "Articles", new { id = model.ArticleId });
            }

            if (ModelState.IsValid)
            {
                var comment = new Comment
                {
                    Content = model.Content,
                };

                var createdComment = await _commentService.AddCommentAsync(comment, model.ArticleId, userId);

                if (createdComment != null)
                {
                    TempData["SuccessMessage"] = "Comment added successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to add comment.";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Comment could not be added. Please ensure it's not empty and within length limits.";
                _logger.LogWarning("Invalid ModelState for Create Comment POST by User {UserId} for Article {ArticleId}.", userId, model.ArticleId);
            }

            return RedirectToAction("Details", "Articles", new { id = model.ArticleId });
        }

        // POST: /Comments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditCommentViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Challenge();

            if (!await _commentService.CanUserModifyCommentAsync(model.Id, userId))
            {
                _logger.LogWarning("User {UserId} forbidden from POSTING edit for comment {CommentId}.", userId, model.Id);
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                var commentToUpdate = new Comment
                {
                    Id = model.Id,
                    Content = model.Content
                };

                bool success = await _commentService.UpdateCommentAsync(commentToUpdate);

                if (success)
                {
                    TempData["SuccessMessage"] = "Comment updated successfully.";
                    return RedirectToAction("Details", "Articles", new { id = model.ArticleId });
                }
                else
                {
                    ModelState.AddModelError("", "Failed to update comment.");
                    _logger.LogWarning("Comment update failed in service for Comment {CommentId} by User {UserId}", model.Id, userId);
                }
            }

            return View(model);
        }


        // POST: /Comments/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int commentId, int articleId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Challenge();

            if (!await _commentService.CanUserModifyCommentAsync(commentId, userId))
            {
                _logger.LogWarning("User {UserId} forbidden from deleting comment {CommentId}.", userId, commentId);
                TempData["ErrorMessage"] = "You do not have permission to delete this comment.";
                return RedirectToAction("Details", "Articles", new { id = articleId });
            }

            bool success = await _commentService.DeleteCommentAsync(commentId);

            if (success)
            {
                TempData["SuccessMessage"] = "Comment deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to delete comment.";
                _logger.LogWarning("Comment delete failed in service for Comment {CommentId} by User {UserId}", commentId, userId);
            }

            return RedirectToAction("Details", "Articles", new { id = articleId });
        }

    }
}